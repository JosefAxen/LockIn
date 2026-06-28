# Ångra "avsluta pass" Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` at the start of EVERY task session. Each task maps to one commit. Read the full spec before starting: `docs/superpowers/specs/2026-06-28-ångra-avsluta-pass-design.md`.

**Goal:** Lägg till en diskret "Ångra"-länk på PostWorkoutPage som låter användaren återgå till sitt pågående pass utan att data förloras. Använder Defer-save-mönstret: `CompletedAt` sätts inte förrän användaren aktivt bekräftar med "Klar".

**Architecture:** `ActiveWorkoutViewModel` (Singleton) behåller `_session`-referensen och hela `Exercises`-state tills `CommitFinishAsync()` anropas. `PostWorkoutViewModel` injiceras med `ActiveWorkoutViewModel` och kallar antingen `CommitFinishAsync()` (Klar) eller `ResumeFromPostWorkout()` (Ångra). Ingen ny DB-tabell, inga migrationer.

**Tech Stack:** .NET MAUI 10 iOS, SQLite, CommunityToolkit.Mvvm, Shell navigation, AppResources i18n

## Global Constraints

- Singleton `ActiveWorkoutViewModel` — inga tillfälliga kopior av state
- Navigation via `Shell.Current.GoToAsync("..")` för att gå tillbaka
- `[ObservableProperty]`, `[RelayCommand]` från CommunityToolkit.Mvvm
- AppResources.resx (svenska) + AppResources.en.resx (engelska) — nya strängar i båda
- DesignTokens för färger — inga hårdkodade hex
- Alla publika metoder i `DatabaseService` anropar `await InitAsync()` först
- Commit efter varje task

---

### Task 1: i18n — Lägg till "Ångra"-sträng

**Filer att ändra:**
- `LockIn/Resources/Strings/AppResources.resx`
- `LockIn/Resources/Strings/AppResources.en.resx`

**Vad:**
Lägg till en ny sträng efter befintliga `PostWorkout_*`-nycklar:

```xml
<!-- AppResources.resx (svenska) -->
<data name="PostWorkout_UndoFinish" xml:space="preserve">
  <value>Ångra — återgå till passet</value>
</data>

<!-- AppResources.en.resx (engelska) -->
<data name="PostWorkout_UndoFinish" xml:space="preserve">
  <value>Undo — return to workout</value>
</data>
```

**Commit:** `feat(i18n): lägg till PostWorkout_UndoFinish-sträng för ångra-funktion`

---

### Task 2: ActiveWorkoutViewModel — Dela upp FinishWorkoutAsync

**Fil att ändra:** `LockIn/ViewModels/ActiveWorkoutViewModel.cs`

**Vad:**

**2a. Bryt ut `CommitFinishAsync()`** — ny publik async metod som innehåller den logik som nu sker inuti `FinishWorkoutAsync()` men som ska skjutas upp till "Klar"-trycket:

```csharp
public async Task CommitFinishAsync(string notes)
{
    if (_session is null) return;
    _session.CompletedAt = DateTime.Now;
    _session.Notes = notes;
    await db.SaveSessionAsync(_session);
    await ApplyProgressionAsync();
    _session = null;
    notifications.CancelTimer();
    ForceDeactivateCore();   // intern hjälpmetod, se nedan
}
```

**2b. Bryt ut `ForceDeactivateCore()`** — intern `private void` med den state-rensning som idag sker i `ForceDeactivate()`, men *utan* orphan-session-hanteringen (den hanteras separat i `ForceDeactivate()`).

```csharp
private void ForceDeactivateCore()
{
    _clockCts?.Cancel();
    timer.Tick -= OnTimerTick;
    timer.Completed -= OnTimerCompleted;
    timer.Cancel();
    state.Deactivate();
    Exercises.Clear();
    _supersetRound.Clear();
    TemplateName = AppResources.ActiveWorkout_FreeWorkout;
    ElapsedTime = "0:00";
    HasPR = false;
    PrMessage = "";
    HasAutoProgress = false;
    AutoProgressMessage = "";
    _activeTimerSection = null;
    _currentTimerSection = null;
}
```

Uppdatera `ForceDeactivate()` att anropa `ForceDeactivateCore()` plus sin befintliga orphan-hantering.

**2c. Ändra `FinishWorkoutAsync()`** — ta bort `CompletedAt`-sättning, `db.SaveSessionAsync`, `ApplyProgressionAsync`, `_session = null`, `notifications.CancelTimer()`. Behåll klock- och timer-stopp. Lägg till `state.Deactivate()` (döljer "PASS PÅGÅR"-banner utan att rensa Exercises). Skicka `_session.Id`:

```csharp
[RelayCommand]
private async Task FinishWorkoutAsync()
{
    var confirmed = await Shell.Current.DisplayAlert(
        AppResources.ActiveWorkout_FinishConfirm_Title,
        AppResources.ActiveWorkout_FinishConfirm_Body,
        AppResources.ActiveWorkout_FinishConfirm_Yes,
        AppResources.Common_Continue);
    if (!confirmed) return;

    _clockCts?.Cancel();
    timer.Tick -= OnTimerTick;
    timer.Completed -= OnTimerCompleted;
    timer.Cancel();

    if (_session is null) return;

    // CompletedAt sätts INTE här — defer till CommitFinishAsync
    var sessionId = _session.Id;
    state.Deactivate();   // döljer bannern men lämnar _session och Exercises intakta

    await Shell.Current.GoToAsync(nameof(PostWorkoutPage), new Dictionary<string, object>
    {
        { "SessionId", sessionId }
    });
}
```

**2d. Lägg till `ResumeFromPostWorkout()`** — publik metod som återaktiverar state och startar klockan igen:

```csharp
public void ResumeFromPostWorkout()
{
    if (_session is null) return;
    state.Activate();
    // Återstarta klockan från befintlig _startTime
    StartClock();
}
```

**Commit:** `feat(vm): dela upp FinishWorkoutAsync – defer CompletedAt-sparning till CommitFinishAsync`

---

### Task 3: PostWorkoutViewModel — Injicera ActiveWorkoutViewModel + UndoFinish

**Fil att ändra:** `LockIn/ViewModels/PostWorkoutViewModel.cs`

**Vad:**

**3a. Injicera `ActiveWorkoutViewModel`** — lägg till som constructor-parameter (redan Singleton i DI):

```csharp
public partial class PostWorkoutViewModel(
    DatabaseService db,
    IHealthService health,
    PhotoService photos,
    ActiveWorkoutViewModel activeWorkout) : ObservableObject
```

**3b. Lägg till `UndoFinishCommand`:**

```csharp
[RelayCommand]
private async Task UndoFinishAsync()
{
    activeWorkout.ResumeFromPostWorkout();
    await Shell.Current.GoToAsync("..");
}
```

**3c. Uppdatera `DoneAsync()`** — anropa `CommitFinishAsync` istället för att sätta notes direkt:

```csharp
[RelayCommand]
private async Task DoneAsync()
{
    await activeWorkout.CommitFinishAsync(Notes);
    await Shell.Current.Navigation.PopToRootAsync(false);
    await Shell.Current.GoToAsync("//TrainPage");
}
```

**3d. Lägg till `_committed`-guard** — bool-flagga för att förhindra dubbel-commit (t.ex. om `OnDisappearing` och `DoneAsync` båda triggas):

```csharp
private bool _committed;

[RelayCommand]
private async Task DoneAsync()
{
    if (_committed) return;
    _committed = true;
    await activeWorkout.CommitFinishAsync(Notes);
    await Shell.Current.Navigation.PopToRootAsync(false);
    await Shell.Current.GoToAsync("//TrainPage");
}

[RelayCommand]
private async Task UndoFinishAsync()
{
    _committed = true;  // markera som "hanterad" — ingen commit
    activeWorkout.ResumeFromPostWorkout();
    await Shell.Current.GoToAsync("..");
}

// Återställ guard när sidan laddas (ny session)
partial void OnSessionIdChanged(int value)
{
    _committed = false;
    _ = LoadAsync(value);
}
```

**Commit:** `feat(vm): PostWorkoutViewModel – UndoFinishCommand + defer commit till DoneAsync`

---

### Task 4: PostWorkoutPage.xaml — Lägg till "Ångra"-länk i UI

**Fil att ändra:** `LockIn/Views/PostWorkoutPage.xaml`

**Vad:**
Lägg till "Ångra"-länk direkt ovanför "Klar"-knappen (ZIndex 22, under knappen på 23). Lägg det som ett eget element i root-`Grid`:

```xml
<!-- Ångra-länk ovanför KLAR-knappen -->
<Label Text="{loc:Localize PostWorkout_UndoFinish}"
       TextColor="{StaticResource ForgeMuted}"
       FontFamily="DMSansRegular"
       FontSize="13"
       TextDecorations="Underline"
       HorizontalOptions="Center"
       VerticalOptions="End"
       Margin="16,0,16,104"
       ZIndex="22">
    <Label.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding UndoFinishCommand}"/>
    </Label.GestureRecognizers>
</Label>
```

Placeringen `Margin="16,0,16,104"` placerar "Ångra" ca 8dp ovanför "Klar"-knappen (40 safeinset + 56 knapphöjd + 8 gap = 104).

**Commit:** `feat(ui): PostWorkoutPage – lägg till Ångra-länk ovanför Klar-knappen`

---

### Task 5: OnDisappearing-guard i PostWorkoutPage.xaml.cs

**Fil att ändra:** `LockIn/Views/PostWorkoutPage.xaml.cs`

**Vad:**
Lägg till `OnDisappearing`-override i code-behind som committar sessionen om sidan försvinner utan att varken "Klar" eller "Ångra" trycktes (t.ex. iOS swipe-back-gesture). VM:s `_committed`-flagga används som guard:

```csharp
protected override void OnDisappearing()
{
    base.OnDisappearing();
    // Om varken DoneAsync eller UndoFinishAsync körts, commit sessionen
    // (hanterar iOS swipe-back-gesture och oväntad navigation)
    if (BindingContext is PostWorkoutViewModel vm)
        _ = vm.CommitIfNotDoneAsync();
}
```

Och i `PostWorkoutViewModel`:

```csharp
public async Task CommitIfNotDoneAsync()
{
    if (_committed) return;
    _committed = true;
    await activeWorkout.CommitFinishAsync(Notes);
}
```

**Commit:** `feat(vm): PostWorkoutPage – OnDisappearing-guard committar session vid swipe-back`

---

### Task 6: DI-registrering — PostWorkoutViewModel med ActiveWorkoutViewModel

**Fil att ändra:** `LockIn/MauiProgram.cs`

**Vad:**
Kontrollera att `ActiveWorkoutViewModel` är registrerad som Singleton och att `PostWorkoutViewModel` är registrerad som Transient (eller Singleton). Om `PostWorkoutViewModel` är Transient kan DI-containern automatiskt injicera Singleton `ActiveWorkoutViewModel` — vanligtvis inga ändringar behövs, men verifiera att registreringen är korrekt.

Sök efter befintlig registrering. Om `PostWorkoutViewModel` registreras med `AddTransient<PostWorkoutViewModel>()` är det korrekt — DI löser Singleton `ActiveWorkoutViewModel` automatiskt.

**Commit:** `chore(di): verifiera DI-registrering för PostWorkoutViewModel med ActiveWorkoutViewModel`

(Om inga ändringar krävs — slå ihop med Task 5-commit och skippa detta som separat commit.)

---

## Sammanfattning av ändringar

| Fil | Typ | Ändring |
|-----|-----|---------|
| `AppResources.resx` | i18n | +1 sträng `PostWorkout_UndoFinish` |
| `AppResources.en.resx` | i18n | +1 sträng `PostWorkout_UndoFinish` |
| `ActiveWorkoutViewModel.cs` | ViewModel | `FinishWorkoutAsync` refaktorerad; +`CommitFinishAsync`, +`ResumeFromPostWorkout`, +`ForceDeactivateCore` |
| `PostWorkoutViewModel.cs` | ViewModel | +`ActiveWorkoutViewModel` injektion; +`UndoFinishCommand`; +`CommitIfNotDoneAsync`; `DoneAsync` uppdaterad; +`_committed`-guard |
| `PostWorkoutPage.xaml` | UI | +`Label` med `UndoFinishCommand`-gesture |
| `PostWorkoutPage.xaml.cs` | Code-behind | +`OnDisappearing`-override |
| `MauiProgram.cs` | DI | Verifikation (troligtvis inga ändringar) |

**Inga nya DB-migreringar. Inga nya modeller. Inga nya sidor.**
