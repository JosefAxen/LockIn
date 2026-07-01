using SQLite;

namespace LockIn.Models;

// Per-muskelgrupp volym-landmärken enligt RP-metoden (docs/research avsnitt 2).
// MEV = Minimum Effective Volume, MRV = Maximum Recoverable Volume.
// Värdena är illustrativa startpunkter — RP:s egen litteratur betonar individuell kalibrering.
[Table("MuscleVolumeLandmarks")]
public class MuscleVolumeLandmarks
{
    [PrimaryKey]
    public int MuscleGroupValue { get; set; }

    public int MEV { get; set; }

    public int MRV { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
}
