using SQLite;
namespace LockIn.Models;

[Table("CycleSessions")]
public class CycleSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleWeekId { get; set; }
    public int DayOfWeek { get; set; }   // 0=Måndag … 6=Söndag
    public int TemplateId { get; set; }  // 0 = inget pass
    public int SortOrder { get; set; }
}
