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
    private static readonly HKUnit s_ms    = HKUnit.FromString("ms");

    private static readonly HKObjectType[] s_readTypes =
    [
        HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRateVariabilitySdnn)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.RestingHeartRate)!,
        HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis)!,
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

    // Antal sömntimmar föregående natt (alla sömn-stages utom InBed)
    public async Task<double> GetSleepHoursLastNightAsync()
    {
        if (!HKHealthStore.IsHealthDataAvailable) return 0.0;
        var type = HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis);
        if (type is null) return 0.0;

        // Täcker en hel natt: igår 18:00 → idag 12:00
        var start = ToNSDate(DateTime.Today.AddDays(-1).AddHours(18));
        var end   = ToNSDate(DateTime.Today.AddHours(12));
        var pred  = HKQuery.GetPredicateForSamples(start, end, HKQueryOptions.StrictStartDate);

        var tcs = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var query = new HKSampleQuery(type, pred, 200, null, (_, samples, err) =>
        {
            if (err is not null)
                System.Diagnostics.Debug.WriteLine($"[HealthKit] Sleep query error: {err.LocalizedDescription}");

            double seconds = 0;
            if (samples is not null)
                foreach (var s in samples)
                    if (s is HKCategorySample cs && cs.Value != 0) // 0 = InBed, allt annat = sömn
                        seconds += cs.EndDate.SecondsSinceReferenceDate - cs.StartDate.SecondsSinceReferenceDate;

            tcs.TrySetResult(seconds / 3600.0);
        });
        _store.ExecuteQuery(query);
        try   { return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { return 0.0; }
    }

    public async Task<SleepStages> GetSleepStagesLastNightAsync()
    {
        if (!HKHealthStore.IsHealthDataAvailable) return new SleepStages(0, 0, 0, 0, 0, 0);
        var type = HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis);
        if (type is null) return new SleepStages(0, 0, 0, 0, 0, 0);

        var start = ToNSDate(DateTime.Today.AddDays(-1).AddHours(18));
        var end   = ToNSDate(DateTime.Today.AddHours(12));
        var pred  = HKQuery.GetPredicateForSamples(start, end, HKQueryOptions.StrictStartDate);

        var tcs = new TaskCompletionSource<SleepStages>(TaskCreationOptions.RunContinuationsAsynchronously);
        var query = new HKSampleQuery(type, pred, 500, null, (_, samples, err) =>
        {
            if (err is not null)
                System.Diagnostics.Debug.WriteLine($"[HealthKit] SleepStages err: {err.LocalizedDescription}");

            double inBed = 0, asleepUnspec = 0, awake = 0, core = 0, deep = 0, rem = 0;
            if (samples is not null)
            {
                foreach (var s in samples)
                {
                    if (s is not HKCategorySample cs) continue;
                    var secs = cs.EndDate.SecondsSinceReferenceDate - cs.StartDate.SecondsSinceReferenceDate;
                    // Apple value mapping: 0=InBed 1=AsleepUnspecified 2=Awake 3=AsleepCore 4=AsleepDeep 5=AsleepREM
                    switch ((int)cs.Value)
                    {
                        case 0: inBed        += secs; break;
                        case 1: asleepUnspec += secs; break;
                        case 2: awake        += secs; break;
                        case 3: core         += secs; break;
                        case 4: deep         += secs; break;
                        case 5: rem          += secs; break;
                    }
                }
            }

            double coreMin  = core  / 60.0;
            double deepMin  = deep  / 60.0;
            double remMin   = rem   / 60.0;
            double awakeMin = awake / 60.0;
            double inBedMin = inBed / 60.0;
            // Om enhet inte rapporterar stages, fall tillbaka på AsleepUnspecified
            if (coreMin + deepMin + remMin < 1 && asleepUnspec > 0)
                coreMin = asleepUnspec / 60.0;
            double totalAsleepMin = coreMin + deepMin + remMin;
            tcs.TrySetResult(new SleepStages(
                TotalHours:   totalAsleepMin / 60.0,
                CoreMinutes:  coreMin,
                DeepMinutes:  deepMin,
                RemMinutes:   remMin,
                AwakeMinutes: awakeMin,
                InBedMinutes: inBedMin));
        });
        _store.ExecuteQuery(query);
        try   { return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { return new SleepStages(0, 0, 0, 0, 0, 0); }
    }

    public async Task<HrvSample> GetHrvSampleAsync()
    {
        var today    = await GetWindowAverageAsync(HKQuantityTypeIdentifier.HeartRateVariabilitySdnn, s_ms,
                                                   DateTime.Today.AddHours(-12), DateTime.Now);
        var baseline = await GetWindowAverageAsync(HKQuantityTypeIdentifier.HeartRateVariabilitySdnn, s_ms,
                                                   DateTime.Today.AddDays(-7), DateTime.Today);
        return new HrvSample(today, baseline);
    }

    public async Task<RestingHrSample> GetRestingHrSampleAsync()
    {
        var today    = await GetWindowAverageAsync(HKQuantityTypeIdentifier.RestingHeartRate, s_bpm,
                                                   DateTime.Today, DateTime.Now);
        var baseline = await GetWindowAverageAsync(HKQuantityTypeIdentifier.RestingHeartRate, s_bpm,
                                                   DateTime.Today.AddDays(-7), DateTime.Today);
        return new RestingHrSample(today, baseline);
    }

    private async Task<double> GetWindowAverageAsync(
        HKQuantityTypeIdentifier typeId, HKUnit unit, DateTime start, DateTime end)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return 0.0;
        var type = HKQuantityType.Create(typeId);
        if (type is null) return 0.0;

        var pred = HKQuery.GetPredicateForSamples(ToNSDate(start), ToNSDate(end), HKQueryOptions.StrictStartDate);
        var tcs  = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var query = new HKStatisticsQuery(type, pred, HKStatisticsOptions.DiscreteAverage,
            (_, result, err) =>
            {
                if (err is not null)
                    System.Diagnostics.Debug.WriteLine($"[HealthKit] Avg query error {typeId}: {err.LocalizedDescription}");
                tcs.TrySetResult(result?.AverageQuantity()?.GetDoubleValue(unit) ?? 0.0);
            });
        _store.ExecuteQuery(query);
        try   { return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { return 0.0; }
    }

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
