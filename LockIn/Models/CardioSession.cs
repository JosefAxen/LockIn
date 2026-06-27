using SQLite;

namespace LockIn.Models;

public class CardioSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public CardioActivityType ActivityType { get; set; } = CardioActivityType.Running;
    public string CustomActivityName { get; set; } = "";

    public DateTime StartedAt { get; set; } = DateTime.Now;
    public int DurationMinutes { get; set; }
    public double DistanceKm { get; set; }
    public int AvgHeartRate { get; set; }
    public int CaloriesBurned { get; set; }
    public string Notes { get; set; } = "";
}
