namespace GestorTurnos.Api.Contracts.Imports;

public sealed record ImportReport(
    int TotalRows,
    int ImportedRows,
    int FailedRows,
    IReadOnlyList<ImportRowResult> Results
);