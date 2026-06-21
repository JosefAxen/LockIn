# Bevel Recovery-spår Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ersätt LockIn:s "vilodagar-baserade" Recovery-uppskattning med en HRV-/RHR-/sömn-baserad formel (Whoop-stil), exponera sömn-stadier (Core/Deep/REM), visa en idag-rekommendation på HemPage, och rita ut Strain-target på Strain-ringen.

**Architecture:** All data hämtas från HealthKit (Apple Watch). HRV-bindningen `HeartRateVariabilitySdnn` finns nu i .NET iOS 26-SDK (stavningen var `Sdnn`, inte `SDNN` — det var vad som strulade förra gången). `HealthKitService` får tre nya metoder (HRV trend, RHR trend, sleep-stages). `HemViewModel` får tre nya properties (`SleepStages`, `TodayRecommendation`, `StrainTarget`) plus uppdaterad Recovery-formel. `HemPage.xaml` får ett sleep-detaljkort och en idag-rekommendation-banner ovanför ring-raden. `MetricRingView` får en ny bindable `TargetProgress` som ritar ett halv-transparent halo-stråk i ringen.

**Tech Stack:** .NET MAUI 10, HealthKit binding 26.0.11017, SkiaSharp 3.116.1, CommunityToolkit.Mvvm 8.4.2.

## Global Constraints

- Plattform: `net10.0-ios`, iOS 15+
- HealthKit-behörigheter krävs (HRV, RHR, SleepAnalysis) — alla ska request:as i `RequestPermissionsAsync` med try/catch (binding kan saknas i SDK-versioner)
- Inga nya NuGet-paket — använd befintliga (`SkiaSharp`, `CommunityToolkit.Mvvm`)
- Svenska texter i UI (Recovery → "ÅTERHÄMTNING", men engelska heter Strain/Recovery i ringarna fortsätter — appen blandar redan)
- Idempotens på alla nya properties: noll-data-fall ska visa "–" och rendera ringen i 0%-läge utan crash
- Inga tester finns i projektet — verifiering = `dotnet build` (0 errors) + manuell inspektion i iOS-simulator
- Bump `ApplicationVersion` med 1 inför sista commit och push

---

## Filöversikt

**Modifieras:**
- `LockIn/Services/IHealthService.cs` — interface utökas med tre nya metoder + två nya record-typer
- `LockIn/Services/NullHealthService.cs` — stubs för nya metoder
- `LockIn/Platforms/iOS/HealthKitService.cs` — implementation av HRV, RHR, SleepStages
- `LockIn/ViewModels/HemViewModel.cs` — nya properties + Recovery-formel + rekommendations-logik
- `LockIn/Controls/MetricRingView.cs` — ny bindable `TargetProgress`
- `LockIn/Views/HemPage.xaml` — idag-rekommendation-banner + sleep-detaljkort + Strain-target text
- `LockIn/LockIn.csproj` — ApplicationVersion 9 → 10

**Skapas:** inga nya filer

**Verifiering:** `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` ger 0 errors per task. Slutgiltig verifiering = manuell körning i iOS-simulator på Mac (utanför denna sessions räckvidd — användarens TestFlight tar över).

---

## Task 1: HealthKit-utvidgning — HRV och vilopuls

**Files:**
- Modify: `LockIn/Services/IHealthService.cs`
- Modify: `LockIn/Services/NullHealthService.cs`
- Modify: `LockIn/Platforms/iOS/HealthKitService.cs`

**Interfaces:**
- Produces: `record HrvSample(double TodayMs, double BaselineMs)`, `record RestingHrSample(double TodayBpm, double BaselineBpm)`, `Task<HrvSample> GetHrvSampleAsync()`, `Task<RestingHrSample> GetRestingHrSampleAsync()` på `IHealthService`

- [ ] **Step 1: Lägg till record-typer och nya metoder i IHealthService**

Ersätt hela `LockIn/Services/IHealthService.cs` med:
```csharp
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
```

- [ ] **Step 2: Uppdatera NullHealthService med stubs**

Ersätt hela `LockIn/Services/NullHealthService.cs` med:
```csharp
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
    public Task<HrvSample> GetHrvSampleAsync()
        => Task.FromResult(new HrvSample(0, 0));
    public Task<RestingHrSample> GetRestingHrSampleAsync()
        => Task.FromResult(new RestingHrSample(0, 0));
    public Task<SleepStages> GetSleepStagesLastNightAsync()
        => Task.FromResult(new SleepStages(0, 0, 0, 0, 0, 0));
}
```

- [ ] **Step 3: Implementera HRV och RHR i HealthKitService**

Lägg till `HeartRateVariabilitySdnn` och `RestingHeartRate` till `s_readTypes`-arrayen i `LockIn/Platforms/iOS/HealthKitService.cs`. Hitta:
```csharp
private static readonly HKObjectType[] s_readTypes =
[
    HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!,
    HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis)!,
];
```

Ersätt med:
```csharp
private static readonly HKObjectType[] s_readTypes =
[
    HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRateVariabilitySdnn)!,
    HKQuantityType.Create(HKQuantityTypeIdentifier.RestingHeartRate)!,
    HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis)!,
];
```

Lägg sedan till en `s_ms`-enhet uppe vid övriga enheter:
```csharp
private static readonly HKUnit s_ms = HKUnit.FromString("ms");
```

Lägg till metoderna `GetHrvSampleAsync` och `GetRestingHrSampleAsync` precis efter `GetSleepHoursLastNightAsync`:
```csharp
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
```

- [ ] **Step 4: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add LockIn/Services/IHealthService.cs LockIn/Services/NullHealthService.cs LockIn/Platforms/iOS/HealthKitService.cs
git commit -m "feat(healthkit): HRV + vilopuls trend-queries"
```

---

## Task 2: HealthKit-utvidgning — sömn-stadier (Core/Deep/REM)

**Files:**
- Modify: `LockIn/Platforms/iOS/HealthKitService.cs`

**Interfaces:**
- Consumes: `SleepStages`-record (definierad i Task 1)
- Produces: `Task<SleepStages> GetSleepStagesLastNightAsync()` på `HealthKitService`

- [ ] **Step 1: Lägg till GetSleepStagesLastNightAsync**

Lägg till metoden precis efter `GetSleepHoursLastNightAsync` i `HealthKitService.cs`:
```csharp
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
```

- [ ] **Step 2: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Platforms/iOS/HealthKitService.cs
git commit -m "feat(healthkit): sömn-stadier Core/Deep/REM/Awake"
```

---

## Task 3: HemViewModel — HRV-baserad Recovery-formel

**Files:**
- Modify: `LockIn/ViewModels/HemViewModel.cs`

**Interfaces:**
- Consumes: `HrvSample`, `RestingHrSample`, `SleepStages` från `IHealthService` (Task 1+2)
- Produces: uppdaterad `RecoveryProgress` (float 0-1) + `RecoveryText` (string), ny `SleepStages` property, ny `RecoveryComponentsText` (string)

- [ ] **Step 1: Lägg till nya observable properties**

I `HemViewModel.cs`, hitta blocket där `RecoveryProgress` deklareras:
```csharp
[ObservableProperty] private float  _recoveryProgress;
[ObservableProperty] private string _recoveryText = "–";
```

Lägg till direkt efter den raden:
```csharp
[ObservableProperty] private string _recoveryComponentsText = "";
[ObservableProperty] private SleepStages _sleepStages = new(0, 0, 0, 0, 0, 0);
```

Säkerställ att `using LockIn.Services;` finns högst upp (det gör det redan via `IHealthService`-importen).

- [ ] **Step 2: Lägg till nya HealthKit-tasks i LoadAsync**

Hitta i `LoadAsync`:
```csharp
var sleepTask          = health.GetSleepHoursLastNightAsync();

await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
    stepsTask, caloriesTask, heartRateTask,
    weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask,
    sleepTask);
```

Ersätt med:
```csharp
var sleepHoursTask     = health.GetSleepHoursLastNightAsync();
var sleepStagesTask    = health.GetSleepStagesLastNightAsync();
var hrvTask            = health.GetHrvSampleAsync();
var rhrTask            = health.GetRestingHrSampleAsync();

await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
    stepsTask, caloriesTask, heartRateTask,
    weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask,
    sleepHoursTask, sleepStagesTask, hrvTask, rhrTask);
```

- [ ] **Step 3: Ersätt Recovery-blocket med HRV-baserad formel**

Hitta blocket:
```csharp
// Recovery = beräknas från vilodagar (sessionsdata, inga extra HealthKit-behörigheter)
// 0 pass senaste 48h = 100%, 1 pass senaste 48h = 60%, 2+ = 20%
int passLast24h = recentSessions.Count(s =>
    s.CompletedAt.HasValue && s.CompletedAt.Value >= DateTime.Now.AddHours(-24));
int passLast48h = recentSessions.Count(s =>
    s.CompletedAt.HasValue && s.CompletedAt.Value >= DateTime.Now.AddHours(-48));
double recoveryPct = Math.Max(0, 100 - passLast24h * 40 - (passLast48h - passLast24h) * 20);
RecoveryProgress = (float)(recoveryPct / 100.0);
RecoveryText     = ((int)recoveryPct).ToString();

// Sleep = sömntimmar / 8
double sleepH = sleepTask.Result;
SleepProgress = sleepH > 0 ? (float)Math.Clamp(sleepH / 8.0, 0.0, 1.0) : 0f;
SleepText     = sleepH > 0 ? $"{sleepH:F1}h" : "–";
```

Ersätt med:
```csharp
var hrv   = hrvTask.Result;
var rhr   = rhrTask.Result;
var stages = sleepStagesTask.Result;
double sleepH = sleepHoursTask.Result;

// Komponenter à la Whoop. Var komponent skalas till 0–100 från ett rimligt intervall.
// Saknad data ger 50 (neutralt) så Recovery inte krashar till 0.
double hrvComp = hrv.BaselineMs > 0 && hrv.TodayMs > 0
    ? Math.Clamp((hrv.TodayMs / hrv.BaselineMs - 0.7) / 0.6, 0, 1) * 100
    : 50;
double rhrComp = rhr.BaselineBpm > 0 && rhr.TodayBpm > 0
    ? Math.Clamp((rhr.BaselineBpm / rhr.TodayBpm - 0.85) / 0.3, 0, 1) * 100
    : 50;
double sleepComp = sleepH > 0 ? Math.Clamp(sleepH / 8.0, 0, 1) * 100 : 50;

double recoveryPct = hrvComp * 0.5 + rhrComp * 0.3 + sleepComp * 0.2;
RecoveryProgress = (float)(recoveryPct / 100.0);
RecoveryText     = ((int)recoveryPct).ToString();
RecoveryComponentsText = hrv.TodayMs > 0
    ? $"HRV {hrv.TodayMs:F0}ms · VILOPULS {rhr.TodayBpm:F0}bpm · SÖMN {sleepH:F1}h"
    : "Anslut Apple Watch för riktig återhämtningsdata";

SleepStages   = stages;
SleepProgress = sleepH > 0 ? (float)Math.Clamp(sleepH / 8.0, 0.0, 1.0) : 0f;
SleepText     = sleepH > 0 ? $"{sleepH:F1}h" : "–";
```

- [ ] **Step 4: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add LockIn/ViewModels/HemViewModel.cs
git commit -m "feat(recovery): HRV/RHR/sömn-baserad formel"
```

---

## Task 4: Idag-rekommendation + Strain-target på HemViewModel

**Files:**
- Modify: `LockIn/ViewModels/HemViewModel.cs`

**Interfaces:**
- Consumes: `RecoveryProgress` (float 0-1), `recentSessions` (List<WorkoutSession>) som finns i `LoadAsync`-scope
- Produces: `TodayRecommendation` (string), `TodayRecommendationDetail` (string), `StrainTarget` (float 0-1), `StrainTargetText` (string)

- [ ] **Step 1: Lägg till nya properties**

I `HemViewModel.cs`, hitta `SleepStages`-deklarationen från Task 3 och lägg till efter:
```csharp
[ObservableProperty] private string _todayRecommendation       = "Hämtar data…";
[ObservableProperty] private string _todayRecommendationDetail = "";
[ObservableProperty] private float  _strainTarget;
[ObservableProperty] private string _strainTargetText = "–";
```

- [ ] **Step 2: Lägg till en BuildRecommendation-hjälpmetod**

Lägg till denna metod längst ner i klassen, före den avslutande `}`:
```csharp
private static (string Headline, string Detail) BuildRecommendation(
    double recoveryPct,
    IReadOnlyList<WorkoutSession> recentSessions)
{
    string headline = recoveryPct switch
    {
        < 33 => "VILA",
        < 66 => "LÄTT PASS",
        _    => "KÖR HÅRT"
    };

    string body = recoveryPct switch
    {
        < 33 => "Återhämtning prioriteras. Cardio på låg puls eller vilodag.",
        < 66 => "Måttlig volym idag — undvik tunga PR-försök.",
        _    => "Du är redo för tunga lyft och PR-försök."
    };

    // Lägg till muskelgrupps-tips baserat på senaste passets datum
    var lastSession = recentSessions
        .Where(s => s.CompletedAt.HasValue)
        .OrderByDescending(s => s.CompletedAt!.Value)
        .FirstOrDefault();
    if (lastSession is not null && recoveryPct >= 33)
    {
        var hoursSince = (DateTime.Now - lastSession.CompletedAt!.Value).TotalHours;
        if (hoursSince < 36)
            body += " Träna annan muskelgrupp än senaste passet.";
    }

    return (headline, body);
}
```

- [ ] **Step 3: Anropa BuildRecommendation i LoadAsync**

I `LoadAsync`, hitta blocket från Task 3 där `RecoveryComponentsText` sätts. Lägg till direkt efter:
```csharp
var (recHead, recDetail) = BuildRecommendation(recoveryPct, recentSessions);
TodayRecommendation       = recHead;
TodayRecommendationDetail = recDetail;
```

- [ ] **Step 4: Lägg till Strain-target i LoadAsync**

Hitta blocket där `StrainProgress` sätts:
```csharp
// Strain = träningsscore (redan uträknat)
StrainProgress = GaugeProgress;
StrainText     = ((int)TrainingScore).ToString();
```

Lägg till direkt efter:
```csharp
// Strain-target = optimal träningsbelastning baserat på recovery (Whoop-stil)
StrainTarget     = (float)(recoveryPct / 100.0);
StrainTargetText = $"MÅL {(int)recoveryPct}";
```

- [ ] **Step 5: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add LockIn/ViewModels/HemViewModel.cs
git commit -m "feat(hem): idag-rekommendation + strain-target"
```

---

## Task 5: MetricRingView — rita ut target-halo

**Files:**
- Modify: `LockIn/Controls/MetricRingView.cs`

**Interfaces:**
- Produces: `TargetProgress` BindableProperty (float 0-1, default 0) — ritar ett halv-transparent halo på bågen tills target-punkten

- [ ] **Step 1: Lägg till TargetProgress-bindable**

I `MetricRingView.cs`, hitta blocket med befintliga BindableProperties (Progress, RingColor, CenterText). Lägg till direkt efter `CenterTextProperty`:
```csharp
public static readonly BindableProperty TargetProgressProperty =
    BindableProperty.Create(nameof(TargetProgress), typeof(float), typeof(MetricRingView), 0f,
        propertyChanged: (b, _, _) => ((MetricRingView)b).InvalidateSurface());
```

Lägg till getter/setter direkt efter `CenterText`:
```csharp
public float TargetProgress
{
    get => (float)GetValue(TargetProgressProperty);
    set => SetValue(TargetProgressProperty, value);
}
```

- [ ] **Step 2: Rita target-bågen i OnPaintSurface**

Hitta blocket i `OnPaintSurface` där bakgrundsbågen (track) ritas:
```csharp
canvas.DrawPath(trackPath, trackPaint);
```

Lägg till direkt efter (innan "Active arc"-blocket):
```csharp
// Target halo — halv-transparent stråk till TargetProgress
float targetSweep = TotalSweepDeg * Math.Clamp(TargetProgress, 0f, 1f);
if (targetSweep > 0.5f)
{
    var c = RingColor;
    var skTarget = new SKColor(
        (byte)(c.Red   * 255),
        (byte)(c.Green * 255),
        (byte)(c.Blue  * 255),
        (byte)90);
    using var targetPath = new SKPath();
    targetPath.AddArc(rect, StartAngleDeg, targetSweep);
    using var targetPaint = new SKPaint
    {
        IsAntialias = true,
        Style       = SKPaintStyle.Stroke,
        StrokeWidth = sw,
        StrokeCap   = SKStrokeCap.Round,
        Color       = skTarget
    };
    canvas.DrawPath(targetPath, targetPaint);
}
```

- [ ] **Step 3: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Controls/MetricRingView.cs
git commit -m "feat(rings): target-halo bindable property"
```

---

## Task 6: HemPage UI — idag-rekommendation, sleep-stages-kort, strain-target

**Files:**
- Modify: `LockIn/Views/HemPage.xaml`

**Interfaces:**
- Consumes: `TodayRecommendation`, `TodayRecommendationDetail`, `StrainTarget`, `StrainTargetText`, `RecoveryComponentsText`, `SleepStages` på HemViewModel

- [ ] **Step 1: Lägg till idag-rekommendation-banner ovanför ring-raden**

I `LockIn/Views/HemPage.xaml`, hitta:
```xml
<!-- ═══ STRAIN / RECOVERY / SÖMN-RINGAR ═══ -->
<Label Text="STRAIN · RECOVERY · SÖMN" Style="{StaticResource SectionLabel}" Margin="22,18,0,10"/>
```

Ersätt med:
```xml
<!-- ═══ IDAG-REKOMMENDATION ═══ -->
<Border Margin="16,16,16,4"
        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
        StrokeShape="RoundRectangle 18"
        StrokeThickness="0"
        Padding="20,16">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
    </Border.Shadow>
    <VerticalStackLayout Spacing="6">
        <Label Text="IDAG"
               Style="{StaticResource SectionLabel}"
               TextColor="{StaticResource ForgeAccent}"/>
        <Label Text="{Binding TodayRecommendation}"
               FontFamily="BebasNeue" FontSize="32"
               CharacterSpacing="1"
               LineHeight="1"
               TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
        <Label Text="{Binding TodayRecommendationDetail}"
               Style="{StaticResource MutedLabel}"
               LineBreakMode="WordWrap"/>
    </VerticalStackLayout>
</Border>

<!-- ═══ STRAIN / RECOVERY / SÖMN-RINGAR ═══ -->
<Label Text="STRAIN · RECOVERY · SÖMN" Style="{StaticResource SectionLabel}" Margin="22,18,0,10"/>
```

- [ ] **Step 2: Lägg till TargetProgress och målruta på Strain-ringen**

Hitta:
```xml
<StackLayout Grid.Column="0" Spacing="8" HorizontalOptions="Center">
    <controls:MetricRingView Progress="{Binding StrainProgress}"
                             RingColor="{StaticResource ForgeAccentAmber}"
                             CenterText="{Binding StrainText}"
                             WidthRequest="90" HeightRequest="90"
                             HorizontalOptions="Center"/>
    <Label Text="STRAIN" Style="{StaticResource SectionLabel}" HorizontalTextAlignment="Center"/>
</StackLayout>
```

Ersätt med:
```xml
<StackLayout Grid.Column="0" Spacing="6" HorizontalOptions="Center">
    <controls:MetricRingView Progress="{Binding StrainProgress}"
                             TargetProgress="{Binding StrainTarget}"
                             RingColor="{StaticResource ForgeAccentAmber}"
                             CenterText="{Binding StrainText}"
                             WidthRequest="90" HeightRequest="90"
                             HorizontalOptions="Center"/>
    <Label Text="STRAIN" Style="{StaticResource SectionLabel}" HorizontalTextAlignment="Center"/>
    <Label Text="{Binding StrainTargetText}"
           FontFamily="BebasNeue" FontSize="10" CharacterSpacing="1.5"
           TextColor="{StaticResource ForgeAccentAmber}"
           Opacity="0.7"
           HorizontalTextAlignment="Center"/>
</StackLayout>
```

- [ ] **Step 3: Lägg till komponent-text under ring-raden**

Hitta avslutningen av ring-Grid:en (sista `</Grid>` efter SÖMN-stacken). Lägg till direkt efter, men före nästa kommentar-sektion:
```xml
<Label Text="{Binding RecoveryComponentsText}"
       Style="{StaticResource MutedLabel}"
       FontSize="10"
       HorizontalTextAlignment="Center"
       Margin="16,6,16,0"/>
```

- [ ] **Step 4: Lägg till sleep-detaljkort efter ring-raden**

Direkt efter Label från Step 3, lägg till:
```xml
<!-- ═══ SÖMN-DETALJER ═══ -->
<Label Text="SÖMN" Style="{StaticResource SectionLabel}" Margin="22,18,0,10"/>
<Border Margin="16,0,16,0"
        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
        StrokeShape="RoundRectangle 18"
        StrokeThickness="0"
        Padding="20,16">
    <Border.Shadow>
        <Shadow Brush="Black" Offset="0,2" Radius="10" Opacity="0.18"/>
    </Border.Shadow>
    <VerticalStackLayout Spacing="10">
        <Grid ColumnDefinitions="*,*,*,*" ColumnSpacing="10">
            <VerticalStackLayout Grid.Column="0" Spacing="2">
                <Label Text="{Binding SleepStages.CoreMinutes, StringFormat='{0:F0}m'}"
                       FontSize="22" FontAttributes="Bold"
                       TextColor="{StaticResource ForgeAccentBlue}"/>
                <Label Text="CORE" Style="{StaticResource SectionLabel}" FontSize="9"/>
            </VerticalStackLayout>
            <VerticalStackLayout Grid.Column="1" Spacing="2">
                <Label Text="{Binding SleepStages.DeepMinutes, StringFormat='{0:F0}m'}"
                       FontSize="22" FontAttributes="Bold"
                       TextColor="{StaticResource ForgeAccentPurple}"/>
                <Label Text="DEEP" Style="{StaticResource SectionLabel}" FontSize="9"/>
            </VerticalStackLayout>
            <VerticalStackLayout Grid.Column="2" Spacing="2">
                <Label Text="{Binding SleepStages.RemMinutes, StringFormat='{0:F0}m'}"
                       FontSize="22" FontAttributes="Bold"
                       TextColor="{StaticResource ForgeAccent}"/>
                <Label Text="REM" Style="{StaticResource SectionLabel}" FontSize="9"/>
            </VerticalStackLayout>
            <VerticalStackLayout Grid.Column="3" Spacing="2">
                <Label Text="{Binding SleepStages.AwakeMinutes, StringFormat='{0:F0}m'}"
                       FontSize="22" FontAttributes="Bold"
                       TextColor="{StaticResource ForgeAccentAmber}"/>
                <Label Text="AWAKE" Style="{StaticResource SectionLabel}" FontSize="9"/>
            </VerticalStackLayout>
        </Grid>
    </VerticalStackLayout>
</Border>
```

- [ ] **Step 5: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add LockIn/Views/HemPage.xaml
git commit -m "feat(hem): idag-banner, strain-target, sömn-stages"
```

---

## Task 7: Version bump och push

**Files:**
- Modify: `LockIn/LockIn.csproj:34`

- [ ] **Step 1: Bumpa ApplicationVersion 9 → 10**

Hitta rad 34:
```xml
<ApplicationVersion>9</ApplicationVersion>
```

Ändra till:
```xml
<ApplicationVersion>10</ApplicationVersion>
```

- [ ] **Step 2: Verifiera full build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Committa version bump**

```bash
git add LockIn/LockIn.csproj
git commit -m "chore: bump ApplicationVersion till 10"
```

- [ ] **Step 4: Push**

```bash
git push origin master
```

Expected: push lyckas, GitHub Actions triggar TestFlight-bygge.

---

## Spec coverage

| Krav från Bevel-spåret | Task |
|------------------------|------|
| Riktig HRV-baserad Recovery (ersätt vilodags-uppskattning) | Task 1 + 3 |
| Vilopuls-trend som komponent i Recovery | Task 1 + 3 |
| Sömn-detaljer Core/Deep/REM (inte bara timmar) | Task 2 + 6 |
| Sleep som komponent i Recovery | Task 3 |
| Idag-rekommendation på HemPage | Task 4 + 6 |
| Muskelgrupps-tips baserat på senaste pass | Task 4 |
| Strain-target = dagens optimala belastning | Task 4 + 5 + 6 |
| Visuell Strain-target på Strain-ringen | Task 5 + 6 |
| Komponent-transparens (visa HRV/VILOPULS/SÖMN-värden) | Task 3 + 6 |
| Version bump + push | Task 7 |
