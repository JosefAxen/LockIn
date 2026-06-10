using SQLite;

namespace LockIn.Models;

[Table("TemplateExercises")]
public class TemplateExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TemplateId { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    public int OrderIndex { get; set; }

    public int Sets { get; set; } = 3;

    public int Reps { get; set; } = 8;

    public decimal TargetWeight { get; set; }

    public int DefaultRestSeconds { get; set; } = 120;
}
