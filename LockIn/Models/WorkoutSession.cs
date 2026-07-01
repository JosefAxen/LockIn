using SQLite;

namespace LockIn.Models;

[Table("WorkoutSessions")]
public class WorkoutSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string Notes { get; set; } = "";

    public int? PumpRating { get; set; }

    public int? SorenessRating { get; set; }

    public int? PerformanceRating { get; set; }
}
