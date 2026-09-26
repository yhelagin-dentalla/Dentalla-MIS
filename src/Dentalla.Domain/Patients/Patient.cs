namespace Dentalla.Domain.Patients;

public sealed class Patient
{
    public Guid Id { get; private set; }
    public string CardNumber { get; private set; } = string.Empty;
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
}
