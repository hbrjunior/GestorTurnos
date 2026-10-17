namespace GestorTurnos.Domain.Rules;

public static class ShiftRules
{
    public static bool HasValidTimeRange(TimeOnly start, TimeOnly end) => start < end;

    // Adjacent intervals are allowed: [08:00, 12:00) and [12:00, 16:00).
    public static bool Overlaps(TimeOnly start, TimeOnly end,
        TimeOnly existingStart, TimeOnly existingEnd) =>
        start < existingEnd && end > existingStart;
}
