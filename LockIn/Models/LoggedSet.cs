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
    // null = RIR inte loggad. Kolumnen är redan nullable i SQLite (sqlite-net sätter
    // NOT NULL bara vid [NotNull]/PK), så ingen migrering behövs.
    public int? RIR { get; set; }
    public DateTime LoggedAt { get; set; }
    public bool IsPR { get; set; }
    public SetType SetType { get; set; } = SetType.Normal;
    public int DurationSeconds { get; set; } = 0;
}
