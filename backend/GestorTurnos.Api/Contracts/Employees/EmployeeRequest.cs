using System.ComponentModel.DataAnnotations;

namespace GestorTurnos.Api.Contracts.Employees;

public sealed class EmployeeRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O país é obrigatório.")]
    [RegularExpression(
        "^[a-zA-Z]{2}$",
        ErrorMessage = "Informe um código ISO de duas letras.")]
    public string CountryCode { get; set; } = string.Empty;
}