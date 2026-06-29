using SQLite;
namespace LockIn.Models;

[Table("CycleWeeks")]
public class CycleWeek
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int WeekNumber { get; set; }
    public int IntensityPercent { get; set; }
    public string Label { get; set; } = "";
}
