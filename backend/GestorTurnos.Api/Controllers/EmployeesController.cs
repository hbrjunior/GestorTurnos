
using GestorTurnos.Api.Contracts.Employees;
using GestorTurnos.Domain.Entities;
using GestorTurnos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestorTurnos.Api.Controllers;

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController(
    AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>>
        GetAll(CancellationToken ct)
    {
        var employees = await db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeResponse(
                e.Id, e.Name, e.Email, e.CountryCode))
            .ToListAsync(ct);

        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>>
        GetById(Guid id, CancellationToken ct)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmployeeResponse(
                e.Id, e.Name, e.Email, e.CountryCode))
            .FirstOrDefaultAsync(ct);

        if (employee is null)
            return NotFound();

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>>
        Create(
            EmployeeRequest request,
            CancellationToken ct)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var country = request.CountryCode.Trim().ToUpperInvariant();

        if (!IsValidName(name))
            return BadRequest("Nome deve conter entre 2 e 150 caracteres.");

        if (await EmailExists(email, null, ct))
            return Conflict("Já existe um colaborador com esse email.");

        var employee = new Employee
        {
            Name = name,
            Email = email,
            CountryCode = country
        };

        db.Employees.Add(employee);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (IsUniqueViolation(ex))
        {
            return Conflict("Já existe um colaborador com esse email.");
        }

        var response = ToResponse(employee);

        return CreatedAtAction(
            nameof(GetById),
            new { id = employee.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        EmployeeRequest request,
        CancellationToken ct)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee is null)
            return NotFound();

        var name = request.Name.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var country = request.CountryCode.Trim().ToUpperInvariant();

        if (!IsValidName(name))
            return BadRequest("Nome deve conter entre 2 e 150 caracteres.");

        if (await EmailExists(email, id, ct))
            return Conflict("Já existe um colaborador com esse email.");

        employee.Name = name;
        employee.Email = email;
        employee.CountryCode = country;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (IsUniqueViolation(ex))
        {
            return Conflict("Já existe um colaborador com esse email.");
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employee is null)
            return NotFound();

        var hasShifts = await db.Shifts
            .AnyAsync(s => s.EmployeeId == id, ct);

        if (hasShifts)
        {
            return Conflict(
                "Não é possível excluir um colaborador com turnos cadastrados.");
        }

        db.Employees.Remove(employee);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (IsForeignKeyViolation(ex))
        {
            return Conflict(
                "Não é possível excluir um colaborador com turnos cadastrados.");
        }

        return NoContent();
    }

    private Task<bool> EmailExists(
        string email,
        Guid? excludedId,
        CancellationToken ct)
    {
        return db.Employees.AsNoTracking()
            .AnyAsync(e =>
                e.Email == email &&
                (!excludedId.HasValue || e.Id != excludedId.Value),
                ct);
    }

    private static bool IsValidName(string name) =>
        name.Length is >= 2 and <= 150;

    private static EmployeeResponse ToResponse(Employee e) =>
        new(e.Id, e.Name, e.Email, e.CountryCode);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627);

    private static bool IsForeignKeyViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql &&
        sql.Number == 547;
}
