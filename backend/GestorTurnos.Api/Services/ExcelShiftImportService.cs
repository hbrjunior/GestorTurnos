using System.Data;
using System.Globalization;
using ClosedXML.Excel;
using GestorTurnos.Api.Contracts.Imports;
using GestorTurnos.Domain.Entities;
using GestorTurnos.Domain.Rules;
using GestorTurnos.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Api.Services;

public sealed class ExcelShiftImportService(
    AppDbContext db)
{
    private const int MaxRows = 500;

    public async Task<ImportReport> ImportAsync(
        Stream stream,
        CancellationToken ct)
    {
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException(
                "A planilha não possui abas.");

        var expectedHeaders = new[]
        {
            "Email", "Fecha", "HoraInicio", "HoraFin"
        };

        for (var col = 1; col <= expectedHeaders.Length; col++)
        {
            var actual = sheet.Cell(1, col).GetString().Trim();

            if (!actual.Equals(
                expectedHeaders[col - 1],
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Cabeçalho inválido na coluna {col}. " +
                    $"Esperado: {expectedHeaders[col - 1]}.");
            }
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        if (lastRow - 1 > MaxRows)
            throw new InvalidDataException(
                $"O arquivo excede o limite de {MaxRows} linhas.");

        var results = new List<ImportRowResult>();

        for (var row = 2; row <= lastRow; row++)
        {
            ct.ThrowIfCancellationRequested();

            var email = sheet.Cell(row, 1)
                .GetString().Trim().ToLowerInvariant();

            var dateCell = sheet.Cell(row, 2);
            var startCell = sheet.Cell(row, 3);
            var endCell = sheet.Cell(row, 4);

            if (email.Length == 0 &&
                dateCell.IsEmpty() &&
                startCell.IsEmpty() &&
                endCell.IsEmpty())
            {
                continue;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new InvalidDataException(
                        "Email obrigatório.");

                if (!TryReadDate(dateCell, out var date))
                    throw new InvalidDataException(
                        "Data inválida.");

                if (!TryReadTime(startCell, out var start) ||
                    !TryReadTime(endCell, out var end))
                {
                    throw new InvalidDataException(
                        "Horário inválido.");
                }

                if (!ShiftRules.HasValidTimeRange(start, end))
                    throw new InvalidDataException(
                        "HoraFim deve ser maior que HoraInicio.");

                await ImportRowAsync(
                    email, date, start, end, ct);

                results.Add(new ImportRowResult(
                    row, email, true,
                    "Turno importado com sucesso."));
            }
            catch (InvalidDataException ex)
            {
                results.Add(new ImportRowResult(
                    row, email, false, ex.Message));
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                results.Add(new ImportRowResult(
                    row, email, false,
                    "Conflito de concorrência. Tente novamente."));
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 1205 ||
                       sql.Number == 547))
            {
                results.Add(new ImportRowResult(
                    row, email, false,
                    "Conflito de gravação ou colaborador inexistente."));
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        var imported = results.Count(r => r.Success);

        return new ImportReport(
            results.Count,
            imported,
            results.Count - imported,
            results);
    }

    private async Task ImportRowAsync(
        string email,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        CancellationToken ct)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == email, ct);

        if (employee is null)
            throw new InvalidDataException(
                "Colaborador não encontrado.");

        var overlaps = await db.Shifts.AnyAsync(s =>
            s.EmployeeId == employee.Id &&
            s.Date == date &&
            start < s.EndTime &&
            end > s.StartTime,
            ct);

        if (overlaps)
            throw new InvalidDataException(
                "Sobreposição de horários.");

        db.Shifts.Add(new Shift
        {
            EmployeeId = employee.Id,
            Date = date,
            StartTime = start,
            EndTime = end
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static bool TryReadDate(
        IXLCell cell,
        out DateOnly value)
    {
        value = default;

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            value = DateOnly.FromDateTime(dateTime);
            return true;
        }

        return DateOnly.TryParseExact(
            cell.GetString().Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value);
    }

    private static bool TryReadTime(
        IXLCell cell,
        out TimeOnly value)
    {
        value = default;

        if (cell.TryGetValue<TimeSpan>(out var duration))
        {
            if (duration < TimeSpan.Zero ||
                duration >= TimeSpan.FromDays(1))
                return false;

            value = TimeOnly.FromTimeSpan(duration);
            return true;
        }

        return TimeOnly.TryParseExact(
            cell.GetString().Trim(),
            new[] { "HH:mm", "HH:mm:ss" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value);
    }
}
