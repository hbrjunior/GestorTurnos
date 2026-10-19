
using GestorTurnos.Domain.Rules;
using Xunit;

namespace GestorTurnos.Tests;

public sealed class ShiftRulesTests
{
    [Theory]
    // Sobreposição parcial
    [InlineData("08:00", "12:00", "11:00", "15:00", true)]
    [InlineData("08:00", "12:00", "07:00", "09:00", true)]

    // Turno completamente contido
    [InlineData("08:00", "12:00", "09:00", "10:00", true)]

    // Turnos exatamente iguais
    [InlineData("08:00", "12:00", "08:00", "12:00", true)]

    // Turno existente completamente contido no novo
    [InlineData("08:00", "18:00", "10:00", "12:00", true)]

    // Intervalos adjacentes (permitidos)
    [InlineData("08:00", "12:00", "12:00", "16:00", false)]
    [InlineData("08:00", "12:00", "06:00", "08:00", false)]

    // Turnos sem interseção
    [InlineData("08:00", "10:00", "14:00", "18:00", false)]
    [InlineData("14:00", "18:00", "08:00", "10:00", false)]

    // Sobreposição de apenas um minuto
    [InlineData("08:00", "12:00", "11:59", "16:00", true)]

    // Turno com início na mesma hora de outro
    [InlineData("08:00", "12:00", "08:00", "10:00", true)]

    // Turno com fim na mesma hora de outro
    [InlineData("08:00", "12:00", "10:00", "12:00", true)]
    public void DetectsOverlaps(
        string start,
        string end,
        string existingStart,
        string existingEnd,
        bool expected)
    {
        var result = ShiftRules.Overlaps(
            TimeOnly.Parse(start),
            TimeOnly.Parse(end),
            TimeOnly.Parse(existingStart),
            TimeOnly.Parse(existingEnd));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("08:00", "08:00", false)]
    [InlineData("18:00", "08:00", false)]
    [InlineData("08:00", "17:00", true)]
    [InlineData("00:00", "23:59", true)]
    [InlineData("00:00", "00:00", false)]
    [InlineData("23:59", "00:00", false)]
    [InlineData("12:00", "12:01", true)]
    public void ValidatesTimeRange(
        string start,
        string end,
        bool expected)
    {
        var result = ShiftRules.HasValidTimeRange(
            TimeOnly.Parse(start),
            TimeOnly.Parse(end));

        Assert.Equal(expected, result);
    }
}
