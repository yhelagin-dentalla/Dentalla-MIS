namespace Dentalla.Domain.Patients;

public sealed class Patient
{
    public Guid Id { get; private set; }
    public Guid? PersonId { get; private set; }
    public string CardNumber { get; private set; } = string.Empty;

    // Transitional legacy projection. Kept until Stage 02 reconciliation/backfill is complete.
    public string FullName { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }

    private Patient() { }

    public Patient(Guid id, string cardNumber, string fullName, DateOnly? birthDate)
    {
        Id = id;
        CardNumber = cardNumber;
        FullName = fullName;
        BirthDate = birthDate;
    }

    public void LinkPerson(Guid personId)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("Person id must not be empty.", nameof(personId));

        if (PersonId.HasValue && PersonId.Value != personId)
            throw new InvalidOperationException("Patient is already linked to another person.");

        PersonId = personId;
    }
}
