namespace LockIn.Services;

public interface IHealthService
{
    Task<bool> RequestPermissionsAsync();
    Task<int>    GetTodayStepsAsync();
    Task<double> GetTodayActiveCaloriesAsync();
    Task<int>    GetTodayMaxHeartRateAsync();
    Task<double[]> GetWeeklyStepsAsync();
    Task<double[]> GetWeeklyCaloriesAsync();
    Task<double[]> GetWeeklyMaxHeartRateAsync();
}
