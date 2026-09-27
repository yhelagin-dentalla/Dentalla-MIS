namespace Dentalla.Domain.Clinical;

public static class ClinicalNoteStatusCode
{
    public const string Draft = "Draft";
    public const string Signed = "Signed";
}

public sealed class ClinicalNote
{
    public Guid Id { get; private set; }
    public Guid EncounterId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid AuthorStaffProfileId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public string StatusCode { get; private set; } = ClinicalNoteStatusCode.Draft;
    public int Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SignedAtUtc { get; private set; }
    public Guid? SignedByUserAccountId { get; private set; }

    private ClinicalNote() { }

    public ClinicalNote(Guid id, Guid encounterId, Guid patientId, Guid authorStaffProfileId, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new ArgumentException("Clinical note id is required.", nameof(id));
        if (encounterId == Guid.Empty) throw new ArgumentException("Encounter id is required.", nameof(encounterId));
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id is required.", nameof(patientId));
        if (authorStaffProfileId == Guid.Empty) throw new ArgumentException("Clinical note author is required.", nameof(authorStaffProfileId));

        Id = id;
        EncounterId = encounterId;
        PatientId = patientId;
        AuthorStaffProfileId = authorStaffProfileId;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void UpdateText(string? text, DateTimeOffset now)
    {
        if (StatusCode == ClinicalNoteStatusCode.Signed)
            throw new InvalidOperationException("A signed clinical note is immutable. Create an amendment instead.");

        Text = text?.Trim() ?? string.Empty;
        Version++;
        UpdatedAtUtc = now;
    }

    public void Sign(Guid signedByUserAccountId, DateTimeOffset now)
    {
        if (signedByUserAccountId == Guid.Empty) throw new ArgumentException("Signing user is required.", nameof(signedByUserAccountId));
        if (StatusCode == ClinicalNoteStatusCode.Signed) throw new InvalidOperationException("Clinical note is already signed.");
        if (string.IsNullOrWhiteSpace(Text)) throw new InvalidOperationException("Clinical note cannot be signed while empty.");

        StatusCode = ClinicalNoteStatusCode.Signed;
        SignedAtUtc = now;
        SignedByUserAccountId = signedByUserAccountId;
        UpdatedAtUtc = now;
    }
}
