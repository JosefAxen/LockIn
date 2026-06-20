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

    // Auto-progression (Fas 2)
    public int TargetRepsMin { get; set; } = 0;
    public int TargetRepsMax { get; set; } = 0;
    public decimal WeightIncrementKg { get; set; } = 2.5m;
    // 0 = None, 1 = Suggest
    public int AutoProgressMode { get; set; } = 0;

    // Superset (Fas 3)
    public int? SupersetGroupId { get; set; }
}
