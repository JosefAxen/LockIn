# Vikt-sync med Apple Health Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Skriv BodyMass-mätvärden till HealthKit varje gång användaren loggar sin kroppsvikt i BodyWeightPage.

**Architecture:** `IHealthService` utökas med `SaveBodyMassAsync(decimal kg, DateTime at)`. `HealthKitService` implementerar den via `HKQuantitySample` med enheten `"kg"` och `HKQuantityTypeIdentifier.BodyMass` — exakt samma mönster som befintlig `SaveWorkoutAsync`. `BodyWeightViewModel` injicerar `IHealthService` och anropar `SaveBodyMassAsync` direkt efter DB-save i `LogWeightAsync`.

**Tech Stack:** .NET MAUI 10 iOS, HealthKit (via iOS-bindings), CommunityToolkit.Mvvm 8.4.2, sqlite-net-pcl

## Global Constraints

- `#if IOS` — ingen HealthKit-kod; redan uppfyllt via `IHealthService`/`NullHealthService`-abstraktionen
- `[RelayCommand]` på `async Task` — aldrig på `void`
- Inga hårdkodade hex-färger, inga UI-ändringar
- Commit efter varje task
- `BodyWeightViewModel` är `AddTransient` i MauiProgram.cs — DI löser konstruktorberoenden automatiskt; inga ändringar i MauiProgram.cs krävs

---

### Task 1: IHealthService + HealthKitService + NullHealthService

**Files:**
- Modify: `LockIn/Services/IHealthService.cs`
- Modify: `LockIn/Platforms/iOS/HealthKitService.cs`
- Modify: `LockIn/Services/NullHealthService.cs`

**Interfaces:**
- Produces: `IHealthService.SaveBodyMassAsync(decimal kg, DateTime at)` — `Task`-returnerande metod

- [ ] **Step 1: Lägg till `SaveBodyMassAsync` i `IHealthService.cs`**

Öppna `LockIn/Services/IHealthService.cs`. Lägg till metoden direkt efter `SaveCardioWorkoutAsync`-raden (rad 26):

```csharp
    Task SaveBodyMassAsync(decimal kg, DateTime at);
```

Filen ser nu ut (relevant del):
```csharp
    Task SaveWorkoutAsync(DateTime start, DateTime end, double activeKcal);
    Task SaveCardioWorkoutAsync(CardioActivityType type, DateTime start, DateTime end, double kcal, double distanceMeters);
    Task SaveBodyMassAsync(decimal kg, DateTime at);
    Task<double> GetSleepHoursLastNightAsync();
```

- [ ] **Step 2: Lägg till `BodyMass` i `s_writeTypes` och `s_kg`-enhet i `HealthKitService.cs`**

Öppna `LockIn/Platforms/iOS/HealthKitService.cs`.

Lägg till `s_kg` direkt efter `s_ms`-raden (rad 15):
```csharp
    private static readonly HKUnit s_kg = HKUnit.FromString("kg");
```

Lägg till `BodyMass` i `s_writeTypes`-arrayen (rad 27–30):
```csharp
    private static readonly HKObjectType[] s_writeTypes =
    [
        HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.BodyMass)!,
    ];
```

- [ ] **Step 3: Implementera `SaveBodyMassAsync` i `HealthKitService.cs`**

Lägg till metoden direkt efter `SaveCardioWorkoutAsync` (efter den avslutande `}` för den metoden, runt rad 360):

```csharp
    public async Task SaveBodyMassAsync(decimal kg, DateTime at)
    {
        if (!HKHealthStore.IsHealthDataAvailable) return;

        var bodyMassType = HKQuantityType.Create(HKQuantityTypeIdentifier.BodyMass);
        if (bodyMassType is null) return;

        var qty    = HKQuantity.FromQuantity(s_kg, (double)kg);
        var nsDate = ToNSDate(at);
        var sample = HKQuantitySample.FromType(bodyMassType, qty, nsDate, nsDate);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _store.SaveObject(sample, (ok, err) =>
        {
            if (err is not null)
                System.Diagnostics.Debug.WriteLine($"[HealthKit] SaveBodyMass: {err.LocalizedDescription}");
            tcs.TrySetResult(ok);
        });
        try   { await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch (TimeoutException) { System.Diagnostics.Debug.WriteLine("[HealthKit] SaveBodyMass timeout"); }
    }
```

- [ ] **Step 4: Lägg till no-op i `NullHealthService.cs`**

Öppna `LockIn/Services/NullHealthService.cs`. Lägg till direkt efter `SaveCardioWorkoutAsync`-raden (rad 15):

```csharp
    public Task SaveBodyMassAsync(decimal kg, DateTime at) => Task.CompletedTask;
```

- [ ] **Step 5: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```
Förväntat: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add LockIn/Services/IHealthService.cs LockIn/Platforms/iOS/HealthKitService.cs LockIn/Services/NullHealthService.cs
git commit -m "feat(health): SaveBodyMassAsync i IHealthService + HealthKitService + NullHealthService"
```

---

### Task 2: BodyWeightViewModel — anropa SaveBodyMassAsync efter vägning

**Files:**
- Modify: `LockIn/ViewModels/BodyWeightViewModel.cs`

**Interfaces:**
- Consumes: `IHealthService.SaveBodyMassAsync(decimal kg, DateTime at)` från Task 1

- [ ] **Step 1: Lägg till `IHealthService health` i konstruktorn**

Öppna `LockIn/ViewModels/BodyWeightViewModel.cs`. Ändra primary constructor (rad 11):

Från:
```csharp
public partial class BodyWeightViewModel(DatabaseService db) : ObservableObject
```

Till:
```csharp
public partial class BodyWeightViewModel(DatabaseService db, IHealthService health) : ObservableObject
```

- [ ] **Step 2: Anropa `SaveBodyMassAsync` i `LogWeightAsync`**

I `LogWeightAsync` (rad 116–130), lägg till HealthKit-anropet direkt efter `db.SaveBodyWeightEntryAsync`:

Från:
```csharp
        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = DateTime.Now, WeightKg = kg });
        await LoadAsync();
```

Till:
```csharp
        var loggedAt = DateTime.Now;
        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = loggedAt, WeightKg = kg });
        try { await health.SaveBodyMassAsync(kg, loggedAt); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[BodyWeight] HealthKit sync misslyckades: {ex.Message}"); }
        await LoadAsync();
```

- [ ] **Step 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```
Förväntat: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add LockIn/ViewModels/BodyWeightViewModel.cs
git commit -m "feat(body): synka BodyMass till Apple Health vid vägning"
```
