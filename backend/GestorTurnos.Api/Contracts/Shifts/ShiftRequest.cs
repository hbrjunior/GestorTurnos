using System.ComponentModel.DataAnnotations;

namespace GestorTurnos.Api.Contracts.Shifts;

public sealed class ShiftRequest
{
    [Required]
    public Guid? EmployeeId { get; set; }

    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required]
    public TimeOnly? EndTime { get; set; }
}