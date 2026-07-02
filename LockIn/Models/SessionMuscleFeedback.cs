using SQLite;

namespace LockIn.Models;

[Table("SessionMuscleFeedback")]
public class SessionMuscleFeedback
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionId { get; set; }
    public int MuscleGroupValue { get; set; }
    public int PumpLevelValue { get; set; }
    public int VolumeLevelValue { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
    [Ignore]
    public PumpLevel Pump
    {
        get => (PumpLevel)PumpLevelValue;
        set => PumpLevelValue = (int)value;
    }
    [Ignore]
    public VolumeLevel Volume
    {
        get => (VolumeLevel)VolumeLevelValue;
        set => VolumeLevelValue = (int)value;
    }
}
