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

    private static readonly HKObjectType[] s_readTypes =
    [
        HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!,
    ];

    private static readonly HKObjectType[] s_writeTypes =
    [
        HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
    ];

    public async Task<bool> RequestPermissionsAsync()
    {
        if (!HKHealthStore.IsHealthDataAvailable)
        {
            System.Diagnostics.Debug.WriteLine("[HealthKit] Inte tillgängligt (simulator?)");
            return false;
        }

        var readSet  = new NSSet<HKObjectType>(s_readTypes);
        var writeSet = new NSSet<HKObjectType>(s_writeTypes);

        try
        {
            var result = await _store.RequestAuthorizationToShareAsync(writeSet, readSet);

            if (result.Item2 is { } error)
                System.Diagnostics.Debug.WriteLine($"[HealthKit] Auth-fel: {error.LocalizedDescription}");
            else
                System.Diagnostics.Debug.WriteLine($"[HealthKit] Behörighet begärd OK, success={result.Item1}");

            // iOS returnerar alltid "true" även om användaren nekar — frågor returnerar 0 vid nekad åtkomst.
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HealthKit] Undantag vid auth: {ex}");
            return false;
        }
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

    public async Task SaveWorkoutAsync(DateTime start, DateTime end, double activeKcal)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return;
        if (end <= start) return;

        var energyType = HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned);
        if (energyType is null) return;

        var safeKcal = double.IsNaN(activeKcal) ? 0.0 : Math.Max(0, activeKcal);
        var kcalQty  = HKQuantity.FromQuantity(s_kcal, safeKcal);
        var sample   = HKQuantitySample.FromType(energyType, kcalQty, ToNSDate(start), ToNSDate(end));

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.SaveObject(sample, (ok, err) =>
        {
            if (err is not null)
                System.Diagnostics.Debug.WriteLine($"[HealthKit] SaveEnergy: {err.LocalizedDescription}");
            tcs.TrySetResult(ok);
        });
        try   { await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { System.Diagnostics.Debug.WriteLine("[HealthKit] SaveEnergy timeout"); }
    }

    private Task<double> GetTodaySumAsync(HKQuantityTypeIdentifier typeId, HKUnit unit) =>
        GetTodayStatAsync(typeId, unit, HKStatisticsOptions.CumulativeSum,
            (r, u) => r.SumQuantity()?.GetDoubleValue(u) ?? 0);

    private Task<double> GetTodayDiscreteMaxAsync(HKQuantityTypeIdentifier typeId, HKUnit unit) =>
        GetTodayStatAsync(typeId, unit, HKStatisticsOptions.DiscreteMax,
            (r, u) => r.MaximumQuantity()?.GetDoubleValue(u) ?? 0);

    private async Task<double> GetTodayStatAsync(
        HKQuantityTypeIdentifier typeId,
        HKUnit unit,
        HKStatisticsOptions options,
        Func<HKStatistics, HKUnit, double> extract)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return 0.0;
        var type = HKQuantityType.Create(typeId);
        if (type is null) return 0.0;

        var start = ToNSDate(DateTime.Today);
        var end   = ToNSDate(DateTime.Now);
        var pred  = HKQuery.GetPredicateForSamples(start, end, HKQueryOptions.StrictStartDate);

        var tcs = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var query = new HKStatisticsQuery(type, pred, options,
            (_, result, err) =>
            {
                if (err is not null)
                    System.Diagnostics.Debug.WriteLine($"[HealthKit] Frågefel {typeId}: {err.LocalizedDescription}");
                tcs.TrySetResult(result is not null ? extract(result, unit) : 0.0);
            });
        _store.ExecuteQuery(query);
        try   { return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { return 0.0; }
    }

    // 7 separata queries (en per dag) — undviker HKStatisticsCollectionQuery.Statistics
    // som ändrades från metod till property i .NET iOS 9.
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

            var tcs   = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
            var query = new HKStatisticsQuery(type, pred, options,
                (_, result, _) =>
                {
                    var val = options == HKStatisticsOptions.CumulativeSum
                        ? result?.SumQuantity()?.GetDoubleValue(unit) ?? 0
                        : result?.MaximumQuantity()?.GetDoubleValue(unit) ?? 0;
                    tcs.TrySetResult(val);
                });
            _store.ExecuteQuery(query);
            tasks[i] = tcs.Task
                .WaitAsync(TimeSpan.FromSeconds(10))
                .ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : 0.0);
        }

        return Task.WhenAll(tasks);
    }

    private static NSDate ToNSDate(DateTime dt)
    {
        var epoch = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return NSDate.FromTimeIntervalSinceReferenceDate((dt.ToUniversalTime() - epoch).TotalSeconds);
    }
}
