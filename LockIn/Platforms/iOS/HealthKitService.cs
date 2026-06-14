using Foundation;
using HealthKit;
using LockIn.Services;

namespace LockIn.Platforms.iOS;

public class HealthKitService : IHealthService
{
    private readonly HKHealthStore _store = new();

    private static readonly HKUnit s_count = HKUnit.Count;
    private static readonly HKUnit s_kcal  = HKUnit.FromString("kcal");
    private static readonly HKUnit s_bpm   = HKUnit.FromString("count/min");

    public Task<bool> RequestPermissionsAsync()
    {
        if (!HKHealthStore.IsHealthDataAvailable)
            return Task.FromResult(false);

        var readTypes = NSSet.FromNSObjects(
            HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
            HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
            HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!
        );

        var tcs = new TaskCompletionSource<bool>();
        _store.RequestAuthorization(new NSSet(), readTypes, (success, error) =>
            tcs.TrySetResult(success));
        return tcs.Task;
    }

    public async Task<int> GetTodayStepsAsync() =>
        (int)await GetTodaySumAsync(HKQuantityTypeIdentifier.StepCount, s_count);

    public Task<double> GetTodayActiveCaloriesAsync() =>
        GetTodaySumAsync(HKQuantityTypeIdentifier.ActiveEnergyBurned, s_kcal);

    public async Task<int> GetTodayMaxHeartRateAsync() =>
        (int)await GetTodayDiscreteMaxAsync(HKQuantityTypeIdentifier.HeartRate, s_bpm);

    public Task<double[]> GetWeeklyStepsAsync() =>
        GetDailyStatsAsync(HKQuantityTypeIdentifier.StepCount, s_count, HKStatisticsOptions.CumulativeSum);

    public Task<double[]> GetWeeklyCaloriesAsync() =>
        GetDailyStatsAsync(HKQuantityTypeIdentifier.ActiveEnergyBurned, s_kcal, HKStatisticsOptions.CumulativeSum);

    public Task<double[]> GetWeeklyMaxHeartRateAsync() =>
        GetDailyStatsAsync(HKQuantityTypeIdentifier.HeartRate, s_bpm, HKStatisticsOptions.DiscreteMax);

    private Task<double> GetTodaySumAsync(HKQuantityTypeIdentifier typeId, HKUnit unit)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(0.0);
        var type = HKQuantityType.Create(typeId);
        if (type is null) return Task.FromResult(0.0);

        var start = ToNSDate(DateTime.Today);
        var end   = ToNSDate(DateTime.Now);
        var pred  = HKQuery.GetPredicateForSamples(start, end, HKQueryOptions.StrictStartDate);

        var tcs = new TaskCompletionSource<double>();
        var query = new HKStatisticsQuery(type, pred, HKStatisticsOptions.CumulativeSum,
            (q, result, error) => tcs.TrySetResult(result?.SumQuantity()?.GetDoubleValue(unit) ?? 0));
        _store.ExecuteQuery(query);
        return tcs.Task;
    }

    private Task<double> GetTodayDiscreteMaxAsync(HKQuantityTypeIdentifier typeId, HKUnit unit)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(0.0);
        var type = HKQuantityType.Create(typeId);
        if (type is null) return Task.FromResult(0.0);

        var start = ToNSDate(DateTime.Today);
        var end   = ToNSDate(DateTime.Now);
        var pred  = HKQuery.GetPredicateForSamples(start, end, HKQueryOptions.StrictStartDate);

        var tcs = new TaskCompletionSource<double>();
        var query = new HKStatisticsQuery(type, pred, HKStatisticsOptions.DiscreteMax,
            (q, result, error) => tcs.TrySetResult(result?.MaximumQuantity()?.GetDoubleValue(unit) ?? 0));
        _store.ExecuteQuery(query);
        return tcs.Task;
    }

    // Uses 7 separate HKStatisticsQuery (one per day) to avoid HKStatisticsCollectionQuery.Statistics
    // which changed from a method to a property in .NET iOS 9.
    private Task<double[]> GetDailyStatsAsync(HKQuantityTypeIdentifier typeId, HKUnit unit, HKStatisticsOptions options)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(new double[7]);
        var type = HKQuantityType.Create(typeId);
        if (type is null) return Task.FromResult(new double[7]);

        var tasks = new Task<double>[7];
        for (int i = 0; i < 7; i++)
        {
            var dayStart = ToNSDate(DateTime.Today.AddDays(i - 6));
            var dayEnd   = ToNSDate(DateTime.Today.AddDays(i - 5));
            var pred     = HKQuery.GetPredicateForSamples(dayStart, dayEnd, HKQueryOptions.StrictStartDate);

            var tcs = new TaskCompletionSource<double>();
            var query = new HKStatisticsQuery(type, pred, options,
                (q, result, error) =>
                {
                    var val = options == HKStatisticsOptions.CumulativeSum
                        ? result?.SumQuantity()?.GetDoubleValue(unit) ?? 0
                        : result?.MaximumQuantity()?.GetDoubleValue(unit) ?? 0;
                    tcs.TrySetResult(val);
                });
            _store.ExecuteQuery(query);
            tasks[i] = tcs.Task;
        }

        return Task.WhenAll(tasks);
    }

    private static NSDate ToNSDate(DateTime dt)
    {
        var epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return NSDate.FromTimeIntervalSinceReferenceDate((dt.ToUniversalTime() - epoch).TotalSeconds);
    }
}
