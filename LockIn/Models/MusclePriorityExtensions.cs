namespace LockIn.Models;

public static class MusclePriorityExtensions
{
    public static string ToRomanNumeral(this MusclePriorityLevel p) => p switch
    {
        MusclePriorityLevel.Emphasize => "III",
        MusclePriorityLevel.Grow      => "II",
        MusclePriorityLevel.Maintain  => "I",
        _                              => "II",
    };

    public static Color ToTagColor(this MusclePriorityLevel p) => p switch
    {
        MusclePriorityLevel.Emphasize => DesignTokens.AccentOrange,
        MusclePriorityLevel.Grow      => Color.FromArgb("#71717A"),
        MusclePriorityLevel.Maintain  => Color.FromArgb("#52525B"),
        _                              => Color.FromArgb("#52525B"),
    };
}
