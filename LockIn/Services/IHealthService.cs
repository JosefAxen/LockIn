namespace LockIn.Services;

public record HrvSample(double TodayMs, double BaselineMs);
public record RestingHrSample(double TodayBpm, double BaselineBpm);
public record SleepStages(
    double TotalHours,
    double CoreMinutes,
    double DeepMinutes,
    double RemMinutes,
    double AwakeMinutes,
    double InBedMinutes);

public interface IHealthService
{
    Task<bool> RequestPermissionsAsync();
    Task<int>    GetTodayStepsAsync();
    Task<double> GetTodayActiveCaloriesAsync();
    Task<int>    GetTodayMaxHeartRateAsync();
    Task<double[]> GetWeeklyStepsAsync();
    Task<double[]> GetWeeklyCaloriesAsync();
    Task<double[]> GetWeeklyMaxHeartRateAsync();
    Task SaveWorkoutAsync(DateTime start, DateTime end, double activeKcal);
    Task<double> GetSleepHoursLastNightAsync();
    Task<HrvSample> GetHrvSampleAsync();
    Task<RestingHrSample> GetRestingHrSampleAsync();
    Task<SleepStages> GetSleepStagesLastNightAsync();
}
