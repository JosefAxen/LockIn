using SQLite;

namespace LockIn.Models;

public enum MuscleGroup
{
    Chest, Back, Shoulders, Biceps, Triceps, Legs, Core, FullBody, Other
}

[Table("Exercises")]
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public int DefaultRestSeconds { get; set; } = 120;

    public MuscleGroup MuscleGroup { get; set; }

    public string Notes { get; set; } = "";
}
