using SQLite;

namespace LockIn.Models;

[Table("WorkoutTemplates")]
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] public string Name { get; set; } = string.Empty;

    public bool IsCustom { get; set; } = true;

    public int DefaultDaysPerWeek { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore] public DateTime? LastUsedAt { get; set; }

    [Ignore]
    public string LastUsedText => LastUsedAt is DateTime d
        ? $"Senast: {d:yyyy-MM-dd}"
        : "Aldrig kort";
}
