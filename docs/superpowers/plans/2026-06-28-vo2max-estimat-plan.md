# VO2Max-estimat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Läs det senaste VO2Max-värdet från HealthKit och visa det på HemPage som ett konditionsmått.

**Architecture:** `IHealthService` utökas med `GetVO2MaxAsync()` som returnerar `double` (0.0 = ej tillgängligt). `HealthKitService` använder befintlig privat `GetLatestSampleValueAsync`-helper med `HKQuantityTypeIdentifier.VO2Max` och en 10-årig tidshorisont. `HemViewModel` lägger till en `Vo2MaxText`-property och laddar värdet parallellt med övrig hälsodata i `LoadHealthDataAsync`. HemPage får ett nytt full-bredd kort direkt under 2×2-griden.

**Tech Stack:** .NET MAUI 10 iOS, HealthKit (via iOS-bindings), CommunityToolkit.Mvvm 8.4.2, XAML data binding, AppResources i18n, AppThemeBinding (light/dark tokens)

## Global Constraints

- `#if IOS` — ingen HealthKit-kod utanför plattformsspecifik kod; redan uppfyllt via IHealthService/NullHealthService-abstraktionen
- Inga hårdkodade hex-färger — använd `{StaticResource ForgeAccentBlue}` / `{AppThemeBinding ...}`
- `[RelayCommand]` på `async Task` — aldrig på `void` (ej relevant här men principen gäller)
- Verifiera alltid med `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5`
- Commit efter varje task

---

### Task 1: IHealthService + HealthKitService + NullHealthService

**Files:**
- Modify: `LockIn/Services/IHealthService.cs`
- Modify: `LockIn/Platforms/iOS/HealthKitService.cs`
- Modify: `LockIn/Services/NullHealthService.cs`

**Interfaces:**
- Produces: `IHealthService.GetVO2MaxAsync()` → `Task<double>` (0.0 = ej tillgänglig)

- [ ] **Step 1: Lägg till `GetVO2MaxAsync` i `IHealthService.cs`**

Öppna `LockIn/Services/IHealthService.cs`. Lägg till direkt efter `GetEstimatedMaxHeartRateAsync`-raden (sista raden i interface, rad 33):

```csharp
    Task<double> GetVO2MaxAsync();
```

Filen efter ändringen (relevant del):

```csharp
    Task<int> GetEstimatedMaxHeartRateAsync();
    Task<double> GetVO2MaxAsync();
}
```

- [ ] **Step 2: Lägg till `s_vo2MaxUnit` + VO2Max i `s_readTypes` i `HealthKitService.cs`**

Öppna `LockIn/Platforms/iOS/HealthKitService.cs`.

Lägg till `s_vo2MaxUnit` direkt efter `s_kg`-raden (rad 16):

```csharp
    private static readonly HKUnit s_vo2MaxUnit = HKUnit.FromString("ml/(kg·min)");
```

Lägg till VO2Max i `s_readTypes`-arrayen (rad 18–26). Den nya arrayen ska se ut så här:

```csharp
    private static readonly HKObjectType[] s_readTypes =
    [
        HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRateVariabilitySdnn)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.RestingHeartRate)!,
        HKCategoryType.Create(HKCategoryTypeIdentifier.SleepAnalysis)!,
        HKQuantityType.Create(HKQuantityTypeIdentifier.VO2Max)!,
    ];
```

- [ ] **Step 3: Implementera `GetVO2MaxAsync` i `HealthKitService.cs`**

Hitta `GetEstimatedMaxHeartRateAsync`-metoden (runt rad 210) och lägg till `GetVO2MaxAsync` direkt efter den (efter dess avslutande `}`):

```csharp
    public Task<double> GetVO2MaxAsync() =>
        GetLatestSampleValueAsync(
            HKQuantityTypeIdentifier.VO2Max,
            s_vo2MaxUnit,
            DateTime.Today.AddYears(-10));
```

`GetLatestSampleValueAsync` är en befintlig privat helper (runt rad 270) som tar `(HKQuantityTypeIdentifier typeId, HKUnit unit, DateTime from)`, skapar en `HKSampleQuery` med limit=1 sorterat på startdatum descending, och returnerar `0.0` vid timeout (10s) eller om inga samples finns. Att använda 10 år tillbaka säkerställer att alla historiska VO2Max-mätningar ingår.

- [ ] **Step 4: Lägg till no-op i `NullHealthService.cs`**

Öppna `LockIn/Services/NullHealthService.cs`. Lägg till direkt efter `GetEstimatedMaxHeartRateAsync`-raden (sista metoden, rad 26):

```csharp
    public Task<double> GetVO2MaxAsync() => Task.FromResult(0.0);
```

- [ ] **Step 5: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add LockIn/Services/IHealthService.cs LockIn/Platforms/iOS/HealthKitService.cs LockIn/Services/NullHealthService.cs
git commit -m "feat(health): GetVO2MaxAsync i IHealthService + HealthKitService + NullHealthService"
```

---

### Task 2: i18n + HemViewModel

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`
- Modify: `LockIn/ViewModels/HemViewModel.cs`

**Interfaces:**
- Consumes: `IHealthService.GetVO2MaxAsync()` från Task 1
- Produces: `HemViewModel.Vo2MaxText` (string, `[ObservableProperty]`) — bindas av HemPage i Task 3

- [ ] **Step 1: Lägg till i18n-nycklar i `AppResources.resx` (svenska)**

Öppna `LockIn/Resources/Strings/AppResources.resx`. Hitta `Hem_HeartRate_Sub`-raden (rad 221). Lägg till direkt efter den:

```xml
  <data name="Hem_VO2Max_Label" xml:space="preserve"><value>VO2MAX</value></data>
  <data name="Hem_VO2Max_Sub" xml:space="preserve"><value>ml/kg/min</value></data>
```

- [ ] **Step 2: Lägg till samma nycklar i `AppResources.en.resx` (engelska)**

Öppna `LockIn/Resources/Strings/AppResources.en.resx`. Hitta `Hem_HeartRate_Sub`-raden (rad 221). Lägg till direkt efter den:

```xml
  <data name="Hem_VO2Max_Label" xml:space="preserve"><value>VO2MAX</value></data>
  <data name="Hem_VO2Max_Sub" xml:space="preserve"><value>ml/kg/min</value></data>
```

- [ ] **Step 3: Lägg till C#-properties i `AppResources.cs`**

Öppna `LockIn/Resources/Strings/AppResources.cs`. Hitta `Hem_HeartRate_Sub`-raden (rad 206). Lägg till direkt efter den:

```csharp
    public static string Hem_VO2Max_Label => Get(nameof(Hem_VO2Max_Label));
    public static string Hem_VO2Max_Sub   => Get(nameof(Hem_VO2Max_Sub));
```

- [ ] **Step 4: Lägg till `Vo2MaxText` ObservableProperty i `HemViewModel.cs`**

Öppna `LockIn/ViewModels/HemViewModel.cs`. Hitta `_sleepText`-raden (runt rad 46):

```csharp
    [ObservableProperty] private string _sleepText    = "–";
```

Lägg till direkt efter den:

```csharp
    [ObservableProperty] private string _vo2MaxText = "–";
```

- [ ] **Step 5: Lägg till laddning av VO2Max i `LoadHealthDataAsync`**

I `HemViewModel.cs`, hitta blocket där alla health-tasks deklareras (runt rad 100–110):

```csharp
            var maxHrTask          = health.GetEstimatedMaxHeartRateAsync();
```

Lägg till direkt efter den raden:

```csharp
            var vo2MaxTask         = health.GetVO2MaxAsync();
```

Hitta `Task.WhenAll`-anropet (runt rad 121–126):

```csharp
            await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
                stepsTask, caloriesTask, heartRateTask,
                weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask,
                sleepHoursTask, sleepStagesTask, hrvTask, rhrTask,
                hrSamplesTask, maxHrTask,
                volTodayTask, volAcuteTask, volChrTask);
```

Ersätt med (lägg till `vo2MaxTask` i `Task.WhenAll`):

```csharp
            await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
                stepsTask, caloriesTask, heartRateTask,
                weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask,
                sleepHoursTask, sleepStagesTask, hrvTask, rhrTask,
                hrSamplesTask, maxHrTask, vo2MaxTask,
                volTodayTask, volAcuteTask, volChrTask);
```

Hitta sedan blocket där hälsostats tilldelas (runt rad 155–166) — hitta `HeartRateValues`-tilldelningen:

```csharp
            HeartRateValues = weeklyMaxHRTask.Result.ToArray();
```

Lägg till direkt efter den raden:

```csharp
            var vo2Max = vo2MaxTask.Result;
            Vo2MaxText = vo2Max > 0 ? $"{vo2Max:F1}" : "–";
```

- [ ] **Step 6: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add LockIn/Resources/Strings/AppResources.resx LockIn/Resources/Strings/AppResources.en.resx LockIn/Resources/Strings/AppResources.cs LockIn/ViewModels/HemViewModel.cs
git commit -m "feat(hem): Vo2MaxText ObservableProperty + laddning i LoadHealthDataAsync + i18n-nycklar"
```

---

### Task 3: HemPage.xaml — VO2Max-kort

**Files:**
- Modify: `LockIn/Views/HemPage.xaml` (runt rad 377–379)

**Interfaces:**
- Consumes: `HemViewModel.Vo2MaxText` (string) från Task 2

- [ ] **Step 1: Hitta insättningspunkten i `HemPage.xaml`**

Öppna `LockIn/Views/HemPage.xaml`. Hitta den avslutande `</Grid>`-taggen för 2×2-datakort-griden (runt rad 377) — det är taggen som stänger `<Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" ... Margin="16,0">` (runt rad 311).

Direkt efter `</Grid>` (rad 377) och direkt före `<!-- = MUSKELHEATMAP = -->` (rad 379).

- [ ] **Step 2: Lägg till VO2Max-kort**

Sätt in följande XAML direkt efter `</Grid>` (2x2-griden), med en blank rad som separator:

```xml
                <!-- = VO2MAX = -->
                <Border Margin="16,8,16,0"
                        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface}}"
                        StrokeShape="RoundRectangle 18"
                        StrokeThickness="0"
                        Padding="16,14,16,14">
                    <VerticalStackLayout Spacing="0">
                        <Label Text="{loc:Localize Hem_VO2Max_Label}" Style="{StaticResource SectionLabel}" Margin="0,0,0,4"/>
                        <Label Text="{Binding Vo2MaxText}"
                               FontSize="36" CharacterSpacing="0" LineHeight="1"
                               TextColor="{AppThemeBinding Light={StaticResource LightAccentBlue}, Dark={StaticResource ForgeAccentBlue}}"/>
                        <Label Text="{loc:Localize Hem_VO2Max_Sub}" Style="{StaticResource MutedLabel}" Margin="0,2,0,0"/>
                    </VerticalStackLayout>
                </Border>
```

Notera:
- `Margin="16,8,16,0"` ger 8px top-gap mot 2x2-griden och 0px botten (nästa sektion, MUSKELHEATMAP, har sin egen `Margin="22,18,0,10"`-label)
- `AppThemeBinding` för `BackgroundColor` matchar exakt befintliga standalone-kort (sömn-sektionen, rad 217–218)
- `LightAccentBlue` = `#0284C7`, `ForgeAccentBlue` = `#38BDF8` — definierade i `Colors.xaml`
- `SectionLabel` och `MutedLabel` är befintliga stilar i `Styles.xaml`

- [ ] **Step 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Views/HemPage.xaml
git commit -m "feat(ui): VO2Max-kort på HemPage"
```
