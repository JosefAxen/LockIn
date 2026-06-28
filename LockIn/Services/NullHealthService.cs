using LockIn.Models;

namespace LockIn.Services;

public class NullHealthService : IHealthService
{
    public Task<bool>    RequestPermissionsAsync()      => Task.FromResult(false);
    public Task<int>     GetTodayStepsAsync()           => Task.FromResult(0);
    public Task<double>  GetTodayActiveCaloriesAsync()  => Task.FromResult(0.0);
    public Task<int>     GetTodayMaxHeartRateAsync()    => Task.FromResult(0);
    public Task<double[]> GetWeeklyStepsAsync()         => Task.FromResult(new double[7]);
    public Task<double[]> GetWeeklyCaloriesAsync()      => Task.FromResult(new double[7]);
    public Task<double[]> GetWeeklyMaxHeartRateAsync()  => Task.FromResult(new double[7]);
    public Task SaveWorkoutAsync(DateTime start, DateTime end, double activeKcal) => Task.CompletedTask;
    public Task SaveCardioWorkoutAsync(CardioActivityType type, DateTime start, DateTime end, double kcal, double distanceMeters) => Task.CompletedTask;
    public Task SaveBodyMassAsync(decimal kg, DateTime at) => Task.CompletedTask;
    public Task<double> GetSleepHoursLastNightAsync()   => Task.FromResult(0.0);
    public Task<HrvSample> GetHrvSampleAsync()
        => Task.FromResult(new HrvSample(0, 0));
    public Task<RestingHrSample> GetRestingHrSampleAsync()
        => Task.FromResult(new RestingHrSample(0, 0));
    public Task<SleepStages> GetSleepStagesLastNightAsync()
        => Task.FromResult(new SleepStages(0, 0, 0, 0, 0, 0));
    public Task<List<HeartRateSample>> GetTodayHeartRateSamplesAsync()
        => Task.FromResult(new List<HeartRateSample>());
    public Task<int> GetEstimatedMaxHeartRateAsync() => Task.FromResult(190);
}
