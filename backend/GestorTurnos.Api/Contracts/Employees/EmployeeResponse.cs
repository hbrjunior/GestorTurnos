namespace GestorTurnos.Api.Contracts.Employees;

public sealed record EmployeeResponse(
    Guid Id,
    string Name,
    string Email,
    string CountryCode
);
