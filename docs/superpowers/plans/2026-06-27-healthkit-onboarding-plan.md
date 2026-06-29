# HealthKit Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Begär HealthKit-behörighet i slutet av onboarding-flödet (vid `FinishOnboardingAsync`) så att nya användare får upp iOS-behörighetsdialogen utan att behöva hitta Settings-toggeln.

**Architecture:** `OnboardingViewModel` injiceras med `IHealthService`. I `FinishOnboardingAsync` anropas `health.RequestPermissionsAsync()` precis innan appen byter till `AppShell`. Om iOS beviljar returnerar metoden `true` — då sätts `Preferences healthkit_sync_enabled = true` så att Settings-toggeln reflekterar det verkliga tillståndet. `NullHealthService` returnerar redan `Task.FromResult(false)` (passande för icke-iOS plattformar). Ingen ny UI — iOS visar sin egna native HealthKit-dialog.

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Mvvm, IHealthService-abstraktion

## Global Constraints

- Alla färger via `StaticResource` eller `DesignTokens.*` — inga hårdkodade hex
- Inga i18n-strängar behövs — iOS-dialogen är native och hanteras av OS
- Injicera `IHealthService` via konstruktor-parameter (DI löser det automatiskt)
- Commit efter task

---

### Task 1: Injicera IHealthService i OnboardingViewModel + anropa vid finish

**Files:**
- Modify: `LockIn/ViewModels/OnboardingViewModel.cs`

**Interfaces:**
- Consumes: `IHealthService.RequestPermissionsAsync() → Task<bool>` (returnerar true om iOS-användaren beviljar)
- Consumes: `Preferences.Default.Set("healthkit_sync_enabled", true)` för att synka med SettingsViewModel

**Context:**
`OnboardingViewModel`-konstruktorn är `OnboardingViewModel(DatabaseService db)` idag.

`FinishOnboardingAsync` ser ut så här (förkortat):
```csharp
private async Task FinishOnboardingAsync(bool activateProgram)
{
    var settings = await db.GetAppSettingsAsync();
    settings.UserName               = UserNameInput.Trim();
    settings.WeeklyWorkoutGoal      = SelectedWeeklyGoal;
    settings.HasCompletedOnboarding = true;
    await db.SaveAppSettingsAsync(settings);

    if (activateProgram && RecommendedProgram is { } program)
        await db.ActivateProgramAsync(program);

    MainThread.BeginInvokeOnMainThread(() =>
    {
        var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
        Application.Current!.Windows[0].Page = shell;
    });
}
```

`NullHealthService.RequestPermissionsAsync()` returnerar `Task.FromResult(false)` — korrekt för icke-iOS.

- [ ] **Step 1: Lägg till `using LockIn.Services;` om det saknas**

Lägg till bland using-direktiven i `LockIn/ViewModels/OnboardingViewModel.cs` om `LockIn.Services` inte redan importeras.

- [ ] **Step 2: Ändra konstruktorn till att ta IHealthService**

Ändra:
```csharp
public partial class OnboardingViewModel(DatabaseService db) : ObservableObject
```
Till:
```csharp
public partial class OnboardingViewModel(DatabaseService db, IHealthService health) : ObservableObject
```

- [ ] **Step 3: Anropa RequestPermissionsAsync i FinishOnboardingAsync**

Lägg till dessa rader direkt efter `await db.ActivateProgramAsync(...)` och innan `MainThread.BeginInvokeOnMainThread`:

```csharp
var granted = await health.RequestPermissionsAsync();
if (granted)
    Preferences.Default.Set("healthkit_sync_enabled", true);
```

Fullständig `FinishOnboardingAsync` efteråt:
```csharp
private async Task FinishOnboardingAsync(bool activateProgram)
{
    var settings = await db.GetAppSettingsAsync();
    settings.UserName               = UserNameInput.Trim();
    settings.WeeklyWorkoutGoal      = SelectedWeeklyGoal;
    settings.HasCompletedOnboarding = true;
    await db.SaveAppSettingsAsync(settings);

    if (activateProgram && RecommendedProgram is { } program)
        await db.ActivateProgramAsync(program);

    var granted = await health.RequestPermissionsAsync();
    if (granted)
        Preferences.Default.Set("healthkit_sync_enabled", true);

    MainThread.BeginInvokeOnMainThread(() =>
    {
        var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
        Application.Current!.Windows[0].Page = shell;
    });
}
```

- [ ] **Step 4: Bygg projektet**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add LockIn/ViewModels/OnboardingViewModel.cs
git commit -m "feat(onboarding): request HealthKit permission at onboarding completion"
```
