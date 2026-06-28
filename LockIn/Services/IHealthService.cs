using LockIn.Models;

namespace LockIn.Services;

public record HrvSample(double TodayMs, double BaselineMs);
public record RestingHrSample(double TodayBpm, double BaselineBpm);
public record HeartRateSample(DateTime Time, double Bpm);
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
    Task SaveCardioWorkoutAsync(CardioActivityType type, DateTime start, DateTime end, double kcal, double distanceMeters);
    Task SaveBodyMassAsync(decimal kg, DateTime at);
    Task<double> GetSleepHoursLastNightAsync();
    Task<HrvSample> GetHrvSampleAsync();
    Task<RestingHrSample> GetRestingHrSampleAsync();
    Task<SleepStages> GetSleepStagesLastNightAsync();
    Task<List<HeartRateSample>> GetTodayHeartRateSamplesAsync();
    Task<int> GetEstimatedMaxHeartRateAsync();
    Task<double> GetVO2MaxAsync();
}
