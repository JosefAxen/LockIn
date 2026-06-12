using SQLite;

namespace LockIn.Models;

public enum WeightUnit { Kg, Lbs }

[Table("AppSettings")]
public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public WeightUnit WeightUnit { get; set; } = WeightUnit.Kg;

    public int WeeklyWorkoutGoal { get; set; } = 4;
}
