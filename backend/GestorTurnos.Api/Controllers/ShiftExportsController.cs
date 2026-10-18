
using GestorTurnos.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestorTurnos.Api.Controllers;

[ApiController]
[Route("api/shifts/export")]
public sealed class ShiftExportsController(
    ExcelShiftExportService exportService) : ControllerBase
{
    [HttpGet]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        if (!from.HasValue || !to.HasValue)
            return BadRequest(
                "Informe as datas inicial e final.");

        if (from.Value > to.Value)
            return BadRequest(
                "A data inicial não pode ser maior que a final.");

        var content = await exportService.ExportAsync(
            from.Value, to.Value, ct);

        var fileName =
            $"Turnos_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
