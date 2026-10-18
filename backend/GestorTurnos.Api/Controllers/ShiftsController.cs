
using GestorTurnos.Api.Contracts.Shifts;
using GestorTurnos.Api.Services;
using GestorTurnos.Domain.Entities;
using GestorTurnos.Domain.Rules;
using GestorTurnos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GestorTurnos.Api.Controllers;

[ApiController]
[Route("api/shifts")]

public sealed class ShiftsController(
    AppDbContext db,
    HolidayService holidayService) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<List<ShiftResponse>>> GetAll(
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
    {
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest("O período informado é inválido.");

        var query = db.Shifts
            .AsNoTracking()
            .Include(s => s.Employee)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.Date <= to.Value);

        var shifts = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync(ct);

        var holidayCache = new Dictionary<
            (string Country, int Year),
            IReadOnlyDictionary<DateOnly, string>?>();

        var countryYears = shifts
            .Select(s => (
                Country: s.Employee.CountryCode.ToUpperInvariant(),
                Year: s.Date.Year))
            .Distinct()
            .ToList();

        foreach (var item in countryYears)
        {
            holidayCache[item] =
                await holidayService.GetHolidaysAsync(
                    item.Country,
                    item.Year,
                    ct);
        }

        var result = shifts.Select(s =>
        {
            var key = (
                Country: s.Employee.CountryCode.ToUpperInvariant(),
                Year: s.Date.Year);

            var holidays = holidayCache[key];

            bool? isHoliday = holidays is null
                ? null
                : holidays.ContainsKey(s.Date);

            string? holidayName = null;

            if (holidays is not null)
                holidays.TryGetValue(s.Date, out holidayName);

            return new ShiftResponse(
                s.Id,
                s.EmployeeId,
                s.Employee.Name,
                s.Date,
                s.StartTime,
                s.EndTime,
                isHoliday,
                holidayName);
        }).ToList();

        return Ok(result);
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShiftResponse>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var shift = await db.Shifts.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShiftResponse(
                        s.Id,
                        s.EmployeeId,
                        s.Employee.Name,
                        s.Date,
                        s.StartTime,
                        s.EndTime,
                        null,
                        null))
            .FirstOrDefaultAsync(ct);

        return shift is null ? NotFound() : Ok(shift);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftResponse>> Create(
        ShiftRequest request,
        CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
            return validation;

        var employeeId = request.EmployeeId!.Value;
        var date = request.Date!.Value;
        var start = request.StartTime!.Value;
        var end = request.EndTime!.Value;

        try
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

            var employee = await db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

            if (employee is null)
                return BadRequest("Colaborador não encontrado.");

            if (await HasOverlap(employeeId, date, start, end, null, ct))
                return Conflict("O turno se sobrepõe a outro turno.");

            var shift = new Shift
            {
                EmployeeId = employeeId,
                Date = date,
                StartTime = start,
                EndTime = end
            };

            db.Shifts.Add(shift);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = shift.Id },
                new ShiftResponse(
                    shift.Id,
                    employeeId,
                    employee.Name,
                    date,
                    start,
                    end));
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            return Conflict(
                "Conflito de gravação. Consulte os turnos e tente novamente.");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        ShiftRequest request,
        CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
            return validation;

        var employeeId = request.EmployeeId!.Value;
        var date = request.Date!.Value;
        var start = request.StartTime!.Value;
        var end = request.EndTime!.Value;

        try
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

            var shift = await db.Shifts
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (shift is null)
                return NotFound();

            var employeeExists = await db.Employees
                .AnyAsync(e => e.Id == employeeId, ct);

            if (!employeeExists)
                return BadRequest("Colaborador não encontrado.");

            if (await HasOverlap(employeeId, date, start, end, id, ct))
                return Conflict("O turno se sobrepõe a outro turno.");

            shift.EmployeeId = employeeId;
            shift.Date = date;
            shift.StartTime = start;
            shift.EndTime = end;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return NoContent();
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            return Conflict(
                "Conflito de gravação. Consulte os turnos e tente novamente.");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        var shift = await db.Shifts
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (shift is null)
            return NotFound();

        db.Shifts.Remove(shift);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<bool> HasOverlap(
        Guid employeeId,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        Guid? excludedShiftId,
        CancellationToken ct)
    {
        return await db.Shifts.AnyAsync(s =>
            s.EmployeeId == employeeId &&
            s.Date == date &&
            (!excludedShiftId.HasValue ||
                s.Id != excludedShiftId.Value) &&
            start < s.EndTime &&
            end > s.StartTime,
            ct);
    }

    private BadRequestObjectResult? ValidateRequest(
        ShiftRequest request)
    {
        if (!request.EmployeeId.HasValue ||
            request.EmployeeId.Value == Guid.Empty)
            return BadRequest("Informe um colaborador válido.");

        if (!request.Date.HasValue)
            return BadRequest("Informe a data do turno.");

        if (!request.StartTime.HasValue ||
            !request.EndTime.HasValue)
            return BadRequest("Informe os horários do turno.");

        if (!ShiftRules.HasValidTimeRange(
                request.StartTime.Value,
                request.EndTime.Value))
        {
            return BadRequest(
                "O horário final deve ser maior que o inicial.");
        }

        return null;
    }

    private static bool IsConcurrencyConflict(Exception ex)
    {
        if (ex is DbUpdateException update &&
            update.InnerException is SqlException inner)
            return inner.Number == 1205 || inner.Number == 547;

        return ex is SqlException sql &&
            (sql.Number == 1205 || sql.Number == 547);
    }
}
