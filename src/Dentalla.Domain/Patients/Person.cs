namespace Dentalla.Domain.Patients;

public sealed class Person
{
    public Guid Id { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string MiddleName { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }

    private Person() { }

    public Person(Guid id, string lastName, string firstName, string? middleName, DateOnly? birthDate)
    {
        Id = id;
        LastName = lastName.Trim();
        FirstName = firstName.Trim();
        MiddleName = middleName?.Trim() ?? string.Empty;
        BirthDate = birthDate;
    }

    public string DisplayName => string.Join(" ", new[] { LastName, FirstName, MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
