using SQLite;
namespace LockIn.Models;

[Table("TrainingCycles")]
public class TrainingCycle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [NotNull]
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public int WeekCount { get; set; }
    public bool IsActive { get; set; }
}
