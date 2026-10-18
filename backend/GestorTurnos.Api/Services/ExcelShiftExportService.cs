
using ClosedXML.Excel;
using GestorTurnos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Api.Services;

public sealed class ExcelShiftExportService(
    AppDbContext db)
{
    public async Task<byte[]> ExportAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var shifts = await db.Shifts
            .AsNoTracking()
            .Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.Employee.Name)
            .Select(s => new
            {
                s.Employee.Name,
                s.Employee.Email,
                s.Employee.CountryCode,
                s.Date,
                s.StartTime,
                s.EndTime
            })
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Turnos");

        string[] headers =
        [
            "Colaborador",
            "Email",
            "País",
            "Fecha",
            "HoraInicio",
            "HoraFin"
        ];

        for (var col = 0; col < headers.Length; col++)
            sheet.Cell(1, col + 1).Value = headers[col];

        sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, 6).Style.Fill.BackgroundColor =
            XLColor.LightBlue;

        var row = 2;

        foreach (var shift in shifts)
        {
            sheet.Cell(row, 1).Value = shift.Name;
            sheet.Cell(row, 2).Value = shift.Email;
            sheet.Cell(row, 3).Value = shift.CountryCode;

            sheet.Cell(row, 4).Value =
                shift.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 4).Style.DateFormat.Format =
                "yyyy-mm-dd";

            sheet.Cell(row, 5).Value =
                shift.StartTime.ToTimeSpan();
            sheet.Cell(row, 5).Style.DateFormat.Format =
                "hh:mm";

            sheet.Cell(row, 6).Value =
                shift.EndTime.ToTimeSpan();
            sheet.Cell(row, 6).Style.DateFormat.Format =
                "hh:mm";

            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}
