using SQLite;

namespace LockIn.Models;

[Table("BodyWeightEntries")]
public class BodyWeightEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime LoggedAt { get; set; }

    public decimal WeightKg { get; set; }
}
