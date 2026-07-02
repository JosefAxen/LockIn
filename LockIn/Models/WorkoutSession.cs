using SQLite;

namespace LockIn.Models;

[Table("WorkoutSessions")]
public class WorkoutSession
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int? MesocycleWeekId { get; set; }
    public int? TemplateDayId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
}
