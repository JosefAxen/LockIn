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

    public string UserName { get; set; } = "";

    public int HeightCm { get; set; } = 0;

    public bool HasCompletedOnboarding { get; set; } = false;

    // Fryst morgon-recovery för ansträngningsmål — sätts en gång per dag vid första laddning.
    public string MorningRecoveryDate { get; set; } = "";
    public double MorningRecoveryPct  { get; set; } = 0;

    public int ReminderDays        { get; set; } = 0;
    public int ReminderTimeMinutes { get; set; } = 0;
}
