# Jämförelse sessionvis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Visa en kompakt sammanfattning av föregående sessions sets direkt under övningsnamnet i varje övningssektion i `ActiveWorkoutPage`.

**Architecture:** `prevSets` hämtas redan i `ActiveWorkoutViewModel.AddExerciseSectionAsync` — det behöver bara omvandlas till en display-sträng (`PrevSessionSummary`) som lagras i `WorkoutExerciseSection` och binds till en ny muted Label i XAML under övningsnamnet.

**Tech Stack:** .NET MAUI 10 iOS, CommunityToolkit.Mvvm 8.4.2, XAML data binding, StaticResource design tokens

## Global Constraints

- Inga DB-ändringar — `GetLastSessionSetsAsync` används as-is
- Inga i18n-ändringar — prefixet "Förra: " är hårdkodat svenska (appen är på svenska)
- Inga hårdkodade hex-färger — använd `{StaticResource ForgeMuted}` för muted text
- `[ObservableProperty]` används INTE för `PrevSessionSummary` — sätts en gång vid skapande, ändras aldrig
- Verifiera alltid med `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5`
- Commit efter varje task

---

### Task 1: WorkoutExerciseSection + ActiveWorkoutViewModel

**Files:**
- Modify: `LockIn/ViewModels/WorkoutExerciseSection.cs`
- Modify: `LockIn/ViewModels/ActiveWorkoutViewModel.cs` (runt rad 144–173)

**Interfaces:**
- Produces: `WorkoutExerciseSection.PrevSessionSummary` (string, `get; set;`, default `""`)
- Produces: `WorkoutExerciseSection.HasPrevSession` (bool, readonly computed)

- [ ] **Step 1: Lägg till `PrevSessionSummary` och `HasPrevSession` i `WorkoutExerciseSection.cs`**

Öppna `LockIn/ViewModels/WorkoutExerciseSection.cs`. Lägg till direkt efter `public bool HasDescription => ...` (rad 14):

```csharp
public string PrevSessionSummary { get; set; } = "";
public bool HasPrevSession => !string.IsNullOrEmpty(PrevSessionSummary);
```

Filen är 58 rader. De nya raderna hamnar runt rad 15–16.

- [ ] **Step 2: Beräkna `PrevSessionSummary` i `ActiveWorkoutViewModel.cs`**

Öppna `LockIn/ViewModels/ActiveWorkoutViewModel.cs`. Hitta rad 144:

```csharp
var prevSets = await db.GetLastSessionSetsAsync(exercise.Id, _session!.Id);
```

Lägg till direkt efter den raden (efter `prevSets`-fetchen, innan for-loopen på rad 146):

```csharp
string prevSummary = "";
if (prevSets.Count > 0)
{
    var parts = prevSets
        .Select(s => s.SetType == SetType.Time && s.DurationSeconds > 0
            ? $"{s.DurationSeconds}s"
            : s.WeightKg > 0
                ? $"{s.WeightKg:G}×{s.Reps}"
                : s.Reps > 0
                    ? $"{s.Reps}r"
                    : null)
        .Where(p => p is not null);
    prevSummary = parts.Any() ? "Förra: " + string.Join(" · ", parts) : "";
}
section.PrevSessionSummary = prevSummary;
```

`SetType` importeras redan via `LockIn.Models` (det används i rad 156 i samma metod). `section` är deklarerad på rad 126.

Den färdiga sekvensen (rad 144 och framåt) ska se ut:

```csharp
var prevSets = await db.GetLastSessionSetsAsync(exercise.Id, _session!.Id);

string prevSummary = "";
if (prevSets.Count > 0)
{
    var parts = prevSets
        .Select(s => s.SetType == SetType.Time && s.DurationSeconds > 0
            ? $"{s.DurationSeconds}s"
            : s.WeightKg > 0
                ? $"{s.WeightKg:G}×{s.Reps}"
                : s.Reps > 0
                    ? $"{s.Reps}r"
                    : null)
        .Where(p => p is not null);
    prevSummary = parts.Any() ? "Förra: " + string.Join(" · ", parts) : "";
}
section.PrevSessionSummary = prevSummary;

for (int s = 1; s <= sets; s++)
{
    // ... befintlig set-loop, rör inte denna ...
```

- [ ] **Step 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add LockIn/ViewModels/WorkoutExerciseSection.cs LockIn/ViewModels/ActiveWorkoutViewModel.cs
git commit -m "feat(workout): PrevSessionSummary på WorkoutExerciseSection"
```

---

### Task 2: ActiveWorkoutPage.xaml — visa summary under övningsnamnet

**Files:**
- Modify: `LockIn/Views/ActiveWorkoutPage.xaml` (runt rad 200–203)

**Interfaces:**
- Consumes: `WorkoutExerciseSection.PrevSessionSummary` (string) från Task 1
- Consumes: `WorkoutExerciseSection.HasPrevSession` (bool) från Task 1

- [ ] **Step 1: Hitta övningshuvudet i `ActiveWorkoutPage.xaml`**

Öppna `LockIn/Views/ActiveWorkoutPage.xaml`. Sök efter `ExerciseName` — det finns ett enda ställe (runt rad 200–203):

```xml
<Label Grid.Column="1"
       Text="{Binding ExerciseName}"
       FontFamily="DMSansMedium" FontSize="16"
       VerticalOptions="Center"/>
```

Det sitter i en `Grid` med `ColumnDefinitions="12,*,Auto,Auto,Auto"` (övningshuvudet). Labeln är i kolumn 1 (`*`).

- [ ] **Step 2: Ersätt labeln med en `VerticalStackLayout`**

Ersätt hela Label-blocket ovan med:

```xml
<VerticalStackLayout Grid.Column="1" VerticalOptions="Center" Spacing="2">
    <Label Text="{Binding ExerciseName}"
           FontFamily="DMSansMedium" FontSize="16"/>
    <Label Text="{Binding PrevSessionSummary}"
           IsVisible="{Binding HasPrevSession}"
           FontFamily="DMSansRegular" FontSize="11"
           TextColor="{StaticResource ForgeMuted}"
           LineBreakMode="TailTruncation"/>
</VerticalStackLayout>
```

`ForgeMuted` är definierad i `LockIn/Resources/Styles/Colors.xaml` som `#A2A2A2`. `DMSansRegular` är registrerat i `MauiProgram.cs`. `HasPrevSession` är false när `PrevSessionSummary` är tom — labeln tar då inte plats vertikalt i stacken.

- [ ] **Step 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Views/ActiveWorkoutPage.xaml
git commit -m "feat(ui): visa senaste sessionens sets under övningsnamnet"
```
