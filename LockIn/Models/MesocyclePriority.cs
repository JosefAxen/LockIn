using SQLite;

namespace LockIn.Models;

[Table("MesocyclePriorities")]
public class MesocyclePriority
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int MesocycleId { get; set; }

    public int MuscleGroupValue { get; set; }

    public int PriorityLevelValue { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }

    [Ignore]
    public MusclePriorityLevel PriorityLevel
    {
        get => (MusclePriorityLevel)PriorityLevelValue;
        set => PriorityLevelValue = (int)value;
    }
}
