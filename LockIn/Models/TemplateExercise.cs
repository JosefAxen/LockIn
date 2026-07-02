using SQLite;

namespace LockIn.Models;

[Table("TemplateExercises")]
public class TemplateExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int TemplateMuscleSlotId { get; set; }

    [Indexed] public int ExerciseId { get; set; }

    public int SortOrder { get; set; }

    public int TargetRepsMin { get; set; } = 8;

    public int TargetRepsMax { get; set; } = 12;

    public int DefaultRestSeconds { get; set; } = 120;
}
