using SQLite;

namespace LockIn.Models;

[Table("TemplateMuscleSlots")]
public class TemplateMuscleSlot
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int TemplateDayId { get; set; }

    public int MuscleGroupValue { get; set; }

    public int SortOrder { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
}
