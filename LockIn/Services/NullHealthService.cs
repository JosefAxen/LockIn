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
    public Task<double> GetSleepHoursLastNightAsync()   => Task.FromResult(0.0);
}
