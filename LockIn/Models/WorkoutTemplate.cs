using SQLite;

namespace LockIn.Models;

[Table("WorkoutTemplates")]
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;
    public string? ProgramId { get; set; }

    [Ignore]
    public DateTime? LastUsedAt { get; set; }

    [Ignore]
    public string LastUsedText => LastUsedAt.HasValue
        ? FormatRelativeDate(LastUsedAt.Value)
        : "ALDRIG GJORT";

    private static string FormatRelativeDate(DateTime date)
    {
        var days = (DateTime.Today - date.Date).Days;
        return days switch
        {
            0 => "SENAST IDAG",
            1 => "SENAST IGÅR",
            <= 6 => $"SENAST {days} DAGAR SEDAN",
            <= 13 => "SENAST FÖRRA VECKAN",
            <= 30 => $"SENAST {days / 7} VECKOR SEDAN",
            _ => $"SENAST {days / 30} MÅN SEDAN"
        };
    }
}
