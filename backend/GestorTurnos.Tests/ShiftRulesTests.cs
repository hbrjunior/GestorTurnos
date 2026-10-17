using GestorTurnos.Domain.Rules;
using Xunit;

namespace GestorTurnos.Tests;

public sealed class ShiftRulesTests
{
    [Theory]
    [InlineData("08:00", "12:00", "11:00", "15:00", true)]
    [InlineData("08:00", "12:00", "12:00", "16:00", false)]
    [InlineData("08:00", "12:00", "07:00", "09:00", true)]
    [InlineData("08:00", "12:00", "09:00", "10:00", true)]
    [InlineData("08:00", "12:00", "06:00", "08:00", false)]
    public void DetectsOverlaps(string a, string b, string c, string d, bool expected)
    {
        Assert.Equal(expected, ShiftRules.Overlaps(
            TimeOnly.Parse(a), TimeOnly.Parse(b), TimeOnly.Parse(c), TimeOnly.Parse(d)));
    }

    [Theory]
    [InlineData("08:00", "08:00", false)]
    [InlineData("18:00", "08:00", false)]
    [InlineData("08:00", "17:00", true)]
    public void ValidatesTimeRange(string a, string b, bool expected) =>
        Assert.Equal(expected, ShiftRules.HasValidTimeRange(TimeOnly.Parse(a), TimeOnly.Parse(b)));
}
