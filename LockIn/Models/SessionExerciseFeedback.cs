using SQLite;

namespace LockIn.Models;

[Table("SessionExerciseFeedback")]
public class SessionExerciseFeedback
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionExerciseId { get; set; }
    public int JointPainLevelValue { get; set; }

    [Ignore]
    public JointPainLevel JointPain
    {
        get => (JointPainLevel)JointPainLevelValue;
        set => JointPainLevelValue = (int)value;
    }
}
