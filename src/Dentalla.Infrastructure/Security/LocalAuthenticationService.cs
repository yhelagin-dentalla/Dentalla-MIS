using System.Security.Cryptography;
using System.Text;
using Dentalla.Application.Abstractions;
using Dentalla.Application.Security;
using Dentalla.Domain.Audit;
using Dentalla.Domain.Security;
using Dentalla.Domain.Staff;
using Dentalla.Infrastructure.Configuration;
using Dentalla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dentalla.Infrastructure.Security;

public sealed class LocalAuthenticationService(
    DentallaDbContext db,
    PasswordHasher passwordHasher,
    IServerClock clock,
    IOptions<DentallaServerOptions> options) : ILocalAuthenticationService
{
    public async Task<bool> IsBootstrapRequiredAsync(CancellationToken cancellationToken = default)
        => !await db.UserAccounts.AsNoTracking().AnyAsync(cancellationToken);

    public async Task<IssuedSession> BootstrapDirectorAsync(
        string userName,
        string password,
        string displayName,
        string? clientName,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        if (await db.UserAccounts.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Initial Director has already been created.");

        var normalized = NormalizeUserName(userName);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        var now = clock.UtcNow;
        var userId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var passwordHash = passwordHasher.Create(password);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var staff = new StaffProfile(staffId, displayName, now);
        var user = new UserAccount(userId, staffId, userName, now);
        var credential = new UserCredential(
            userId,
            passwordHash.HashBase64,
            passwordHash.SaltBase64,
            passwordHash.Iterations,
            now);
        var role = new UserRoleAssignment(
            Guid.NewGuid(),
            userId,
            SystemRoleCode.Director,
            now,
            null,
            userId,
            "Initial server bootstrap");

        db.StaffProfiles.Add(staff);
        db.UserAccounts.Add(user);
        db.UserCredentials.Add(credential);
        db.UserRoleAssignments.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        var issued = await IssueSessionAsync(user, staff, [SystemRoleCode.Director], clientName, clientIp, cancellationToken);

        db.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            now,
            userId,
            issued.Session.SessionId,
            "Security.BootstrapDirector",
            "Initial Director account created on Dentalla Server.",
            entityType: "UserAccount",
            entityId: userId.ToString(),
            roleContext: SystemRoleCode.Director,
            traceId: traceId,
            clientIp: clientIp));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return issued;
    }

    public async Task<IssuedSession?> LoginAsync(
        string userName,
        string password,
        string? clientName,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUserName(userName);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var user = await db.UserAccounts
            .SingleOrDefaultAsync(x => x.NormalizedUserName == normalized && x.IsActive, cancellationToken);
        if (user is null)
            return null;

        var credential = await db.UserCredentials
            .SingleOrDefaultAsync(x => x.UserAccountId == user.Id, cancellationToken);
        if (credential is null || !passwordHasher.Verify(password, credential.PasswordHashBase64, credential.PasswordSaltBase64, credential.PasswordIterations))
        {
            db.AuditEvents.Add(new AuditEvent(
                Guid.NewGuid(),
                clock.UtcNow,
                user.Id,
                null,
                "Security.LoginFailed",
                "Failed local authentication attempt.",
                entityType: "UserAccount",
                entityId: user.Id.ToString(),
                traceId: traceId,
                clientIp: clientIp));
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        var staff = await db.StaffProfiles
            .SingleAsync(x => x.Id == user.StaffProfileId && x.IsActive, cancellationToken);
        var roles = await GetActiveRolesAsync(user.Id, cancellationToken);
        var issued = await IssueSessionAsync(user, staff, roles, clientName, clientIp, cancellationToken);

        db.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            clock.UtcNow,
            user.Id,
            issued.Session.SessionId,
            "Security.LoginSucceeded",
            "Local authentication succeeded.",
            entityType: "UserAccount",
            entityId: user.Id.ToString(),
            roleContext: string.Join(',', roles),
            traceId: traceId,
            clientIp: clientIp));
        await db.SaveChangesAsync(cancellationToken);

        return issued;
    }

    public async Task<AuthenticatedSession?> AuthenticateAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var tokenHash = HashToken(accessToken);
        var now = clock.UtcNow;

        var session = await db.AuthSessions
            .SingleOrDefaultAsync(x => x.TokenHashHex == tokenHash, cancellationToken);
        if (session is null || !session.IsActiveAt(now))
            return null;

        var user = await db.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == session.UserAccountId && x.IsActive, cancellationToken);
        if (user is null)
            return null;

        var staff = await db.StaffProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == user.StaffProfileId && x.IsActive, cancellationToken);
        if (staff is null)
            return null;

        var roles = await GetActiveRolesAsync(user.Id, cancellationToken);

        if (now - session.LastSeenAtUtc >= TimeSpan.FromMinutes(5))
        {
            session.Touch(now);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new AuthenticatedSession(
            session.Id,
            user.Id,
            user.StaffProfileId,
            user.UserName,
            staff.DisplayName,
            roles,
            session.ExpiresAtUtc);
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        Guid actorUserAccountId,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default)
    {
        var session = await db.AuthSessions
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
            return;

        session.Revoke(clock.UtcNow);
        db.AuditEvents.Add(new AuditEvent(
            Guid.NewGuid(),
            clock.UtcNow,
            actorUserAccountId,
            sessionId,
            "Security.Logout",
            "Authentication session revoked by logout.",
            entityType: "AuthSession",
            entityId: sessionId.ToString(),
            traceId: traceId,
            clientIp: clientIp));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IssuedSession> IssueSessionAsync(
        UserAccount user,
        StaffProfile staff,
        IReadOnlyList<string> roles,
        string? clientName,
        string? clientIp,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var lifetimeHours = Math.Clamp(options.Value.LocalAuthSessionLifetimeHours, 1, 72);
        var expiresAt = now.AddHours(lifetimeHours);
        var rawToken = CreateToken();
        var session = new AuthSession(
            Guid.NewGuid(),
            user.Id,
            HashToken(rawToken),
            now,
            expiresAt,
            clientName,
            clientIp);

        db.AuthSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return new IssuedSession(
            rawToken,
            expiresAt,
            new AuthenticatedSession(
                session.Id,
                user.Id,
                user.StaffProfileId,
                user.UserName,
                staff.DisplayName,
                roles,
                expiresAt));
    }

    private async Task<IReadOnlyList<string>> GetActiveRolesAsync(Guid userAccountId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        return await db.UserRoleAssignments
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId
                && x.ValidFromUtc <= now
                && (x.ValidToUtc == null || x.ValidToUtc > now))
            .Select(x => x.RoleCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    private static string NormalizeUserName(string value) => value.Trim().ToUpperInvariant();

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
