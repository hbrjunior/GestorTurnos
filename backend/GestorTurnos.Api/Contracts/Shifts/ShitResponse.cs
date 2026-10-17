namespace GestorTurnos.Api.Contracts.Shifts;

public sealed record ShiftResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime
);