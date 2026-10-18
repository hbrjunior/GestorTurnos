
using ClosedXML.Excel;
using ClosedXML.Excel.Exceptions;
using GestorTurnos.Api.Contracts.Imports;
using GestorTurnos.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestorTurnos.Api.Controllers;

[ApiController]
[Route("api/shifts/import")]
public sealed class ShiftImportsController(
    ExcelShiftImportService importService,
    ILogger<ShiftImportsController> logger) : ControllerBase
{
    private const long MaxFileSize = 2 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportReport),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<ImportReport>> Import(
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Selecione um arquivo Excel.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(
                "Somente arquivos .xlsx são permitidos.");
        }

        if (file.Length > MaxFileSize)
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                "O arquivo excede o limite de 2 MB.");

        try
        {
            await using var stream = file.OpenReadStream();

            var report = await importService.ImportAsync(
                stream, ct);

            return Ok(report);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException &&
            (ex is ClosedXMLException ||
             ex is System.IO.InvalidDataException))
        {
            logger.LogWarning(ex,
                "Arquivo Excel inválido.");

            return BadRequest(
                "Não foi possível ler o arquivo Excel.");
        }
    }
}
