namespace GestorTurnos.Api.Contracts.Imports;

public sealed record ImportRowResult(
    int RowNumber,
    string Email,
    bool Success,
    string Message
);
