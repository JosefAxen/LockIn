using SQLite;

namespace LockIn.Models;

[Table("LoggedSets")]
public class LoggedSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionExerciseId { get; set; }

    public int SetNumber { get; set; }

    public decimal WeightKg { get; set; }

    public int Reps { get; set; }

    public int RIR { get; set; }

    public DateTime LoggedAt { get; set; }

    public bool IsPR { get; set; }
}
