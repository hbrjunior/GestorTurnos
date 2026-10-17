namespace GestorTurnos.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // ISO 3166-1 alpha-2: BR, ES, PT...
    public string CountryCode { get; set; } = string.Empty;
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
