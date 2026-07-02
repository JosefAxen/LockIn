using SQLite;

namespace LockIn.Models;

[Table("SessionExercises")]
public class SessionExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionId { get; set; }
    public int ExerciseId { get; set; }
    public int MuscleGroupValue { get; set; }
    public int? TemplateMuscleSlotId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsSkipped { get; set; }
    public string Notes { get; set; } = "";
    public int? SupersetGroupId { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
}
