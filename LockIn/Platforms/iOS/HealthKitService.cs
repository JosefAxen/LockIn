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

        var readTypes = new NSSet<HKObjectType>(
            HKObjectType.GetQuantityType(HKQuantityTypeIdentifier.StepCount)!,
            HKObjectType.GetQuantityType(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
            HKObjectType.GetQuantityType(HKQuantityTypeIdentifier.HeartRate)!
        );

        var tcs = new TaskCompletionSource<bool>();
        _store.RequestAuthorization(null, readTypes, (success, error) =>
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

    private Task<double> GetTodaySumAsync(NSString typeId, HKUnit unit)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(0.0);
        var type = HKObjectType.GetQuantityType(typeId);
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

    private Task<double> GetTodayDiscreteMaxAsync(NSString typeId, HKUnit unit)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(0.0);
        var type = HKObjectType.GetQuantityType(typeId);
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

    private Task<double[]> GetDailyStatsAsync(NSString typeId, HKUnit unit, HKStatisticsOptions options)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return Task.FromResult(new double[7]);
        var type = HKObjectType.GetQuantityType(typeId);
        if (type is null) return Task.FromResult(new double[7]);

        var anchorDate = ToNSDate(DateTime.Today.AddDays(-6));
        var endDate    = ToNSDate(DateTime.Now);
        var pred       = HKQuery.GetPredicateForSamples(anchorDate, endDate, HKQueryOptions.StrictStartDate);
        var interval   = new NSDateComponents { Day = 1 };

        var tcs = new TaskCompletionSource<double[]>();
        var query = new HKStatisticsCollectionQuery(type, pred, options, anchorDate, interval);

        query.InitialResultsHandler = (q, collection, error) =>
        {
            if (collection is null) { tcs.TrySetResult(new double[7]); return; }

            var result = new double[7];
            for (int i = 0; i < 7; i++)
            {
                var dayDate = ToNSDate(DateTime.Today.AddDays(i - 6));
                var stats   = collection.Statistics(dayDate);
                result[i] = options == HKStatisticsOptions.CumulativeSum
                    ? stats?.SumQuantity()?.GetDoubleValue(unit) ?? 0
                    : stats?.MaximumQuantity()?.GetDoubleValue(unit) ?? 0;
            }
            tcs.TrySetResult(result);
        };

        _store.ExecuteQuery(query);
        return tcs.Task;
    }

    private static NSDate ToNSDate(DateTime dt)
    {
        var epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return NSDate.FromTimeIntervalSinceReferenceDate((dt.ToUniversalTime() - epoch).TotalSeconds);
    }
}
