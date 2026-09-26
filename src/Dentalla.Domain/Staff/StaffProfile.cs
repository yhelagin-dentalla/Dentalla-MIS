namespace Dentalla.Domain.Staff;

public sealed class StaffProfile
{
    public Guid Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DisabledAtUtc { get; private set; }

    private StaffProfile() { }

    public StaffProfile(Guid id, string displayName, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Staff profile id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));

        Id = id;
        DisplayName = displayName.Trim();
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
        DisplayName = displayName.Trim();
    }

    public void Disable(DateTimeOffset atUtc)
    {
        IsActive = false;
        DisabledAtUtc = atUtc;
    }

    public void Enable()
    {
        IsActive = true;
        DisabledAtUtc = null;
    }
}
