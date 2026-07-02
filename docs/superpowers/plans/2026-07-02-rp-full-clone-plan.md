# RP Hypertrophy — Full klon-implementation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development eller superpowers:executing-plans. Steps använder checkbox (`- [ ]`) syntax för tracking.

**Goal:** Reproducera RP Hypertrophy-appens template- och mesocykel-flöde exakt i Vana Strength, inklusive muskelprioriteter (Emphasize/Grow/Maintain), auto-genererade övningar, per-muskelgrupp feedback och obligatorisk cykel-arkitektur.

**Architecture:** Templates blir strukturerade objekt (Days → MuscleSlots → Exercises). Mesocycler binds till en template plus egen muskelprio-uppgörelse. Feedback flyttar från global session-nivå till per-övning (JointPain) + per-muskelgrupp (Pump/Volume). Etapp 1 skriver om alla modeller + services innan UI rörs; Etapp 2 bygger nytt UI ovanpå.

**Tech Stack:** .NET MAUI 10 (`net10.0-ios`), CommunityToolkit.Mvvm, sqlite-net-pcl, SkiaSharp, svenska strängar via `AppResources.resx` + `AppResources.cs`-wrapper.

## Global Constraints

- Inga tester finns i projektet — verifiering sker via `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
- **Reset gammal data:** vid uppgradering raderas alla gamla `WorkoutTemplates`, `TemplateExercises`, `TrainingCycles`, `CycleWeeks`, `CycleSessions`, `WorkoutSessions`, `SessionExercises`, `LoggedSets`, `MuscleVolumeLandmarks`. Loggat historiskt data offras — appen har inga riktiga användare än.
- Idempotent DB-migreringar via try-catch (mönster: `DatabaseService.cs` rader 44-45).
- Öka `ApplicationVersion` med +1 i `LockIn.csproj` inför varje push (TestFlight-krav).
- Pusha aldrig utan explicit instruktion.
- Nya strängar läggs alltid till både i `AppResources.resx` och `AppResources.cs`-wrappern.
- Nya sidor registreras i `MauiProgram.cs` (DI) och `AppShell.xaml.cs` (Routing).
- Nya SkiaSharp-kontroller använder `SKPaint`-baserat text-API (inte `SKFont`).
- Muskelgrupper utökas till RP:s exakta lista (12 st) — **behåll enum-nummer 0-9 för Chest/Back/Shoulders/Biceps/Triceps/Legs/Core/FullBody/Other/Forearms** och lägg till Quads=10, Hamstrings=11, Glutes=12, Calves=13, Traps=14, Abs=15. `Legs` och `Core` behålls som legacy-mapping men används inte för nya templates.
- Meso Builder-flow: **frekvens → muskelgrupper → prioriteter → auto-generera övningar → skapa cykel**. Template picker: välj en av 4-6 seedade templates.
- Set-menyalternativ (RP-exakt): Add note, Move up, Move down, Replace, Joint pain, Add set, Skip sets, Remove exercise.
- Feedback är **soft** — dialog visas efter sista setet per muskelgrupp, men användaren kan skippa.
- RIR-mål visas som placeholder-text i reps-fältet (t.ex. `3 RIR`). Separat RIR-fält per set tas bort — faktisk RIR fångas i feedback-dialog per övning.
- Startvikt-hint: onboarding-card visas första passet i vecka 1 av en ny cykel, dismissable.
- Full mesocycle stats-sida (`MesocycleStatsPage`) implementeras.

---

## Bakgrund

Vår nuvarande implementation (Fas 1 + Fas 2 pushad i version 74/75) täcker grunderna men saknar RP:s kärnkoncept: strukturerade templates med muskelgrupp-blocks per dag, muskelprioriteter (Emphasize/Grow/Maintain) som styr volym-progression, per-muskelgrupp feedback, och auto-genererade övningsval. Denna plan bygger om hela template/cykel/feedback-arkitekturen till RP:s modell.

Screenshot-analys: [docs/research/rp-app-screenshots/](../../research/rp-app-screenshots/) IMG_0932 till IMG_0955.
Research-rapport: [docs/research/2026-06-30-rp-hypertrophy-research.md](../../research/2026-06-30-rp-hypertrophy-research.md).

---

## File Structure

**Modeller (Models/):**
- `MusclePriority.cs` — enum (Emphasize, Grow, Maintain)
- `WorkoutTemplate.cs` — omskrivet: Name, IsCustom, TemplateAuthor, DefaultDaysPerWeek
- `TemplateDay.cs` — ny: TemplateId, DayIndex, Label, DefaultDayOfWeek (nullable)
- `TemplateMuscleSlot.cs` — ny: TemplateDayId, MuscleGroup, SortOrder
- `TemplateExercise.cs` — omskrivet: TemplateMuscleSlotId, ExerciseId, SortOrder, TargetRepsMin, TargetRepsMax
- `Mesocycle.cs` — ersätter TrainingCycle: Name, TemplateId, StartDate, WeekCount, IsActive, UnitSystem
- `MesocyclePriority.cs` — ny: MesocycleId, MuscleGroup, PriorityLevel
- `MesocycleWeek.cs` — ersätter CycleWeek: MesocycleId, WeekNumber, IsDeload, IntensityPercent
- `WorkoutSession.cs` — omskrivet: byt TemplateId/CycleWeekId till MesocycleWeekId+TemplateDayId, ta bort Pump/Soreness/Performance
- `SessionExercise.cs` — omskrivet: lägg till MuscleGroup, TemplateMuscleSlotId, Notes, IsSkipped
- `SessionMuscleFeedback.cs` — ny: SessionId, MuscleGroup, PumpLevel, VolumeLevel
- `SessionExerciseFeedback.cs` — ny: SessionExerciseId, JointPainLevel
- `MuscleGroup.cs` — enum-utökning (Quads, Hamstrings, Glutes, Calves, Traps, Abs)
- `EquipmentType.cs` — säkerställ RP-täckning
- `AppSettings.cs` — lägg till PreferredEquipmentJson, HasSeenStartWeightHint

**Services (Services/):**
- `TemplateService.cs` — ny: CRUD för templates + planning board
- `MesocycleService.cs` — ny: CRUD, aktivering, veckoberäkning
- `ExerciseRecommendationService.cs` — ny: auto-pick per (muskelgrupp, prio, equipment)
- `MesoBuilderServiceV2.cs` — ersätter nuvarande MesoBuilderService
- `VolumeProgressionService.cs` — ny: planerad volym per vecka baserat på prio
- `WeightProgressionService.cs` — modifiera för nya modellen
- `SetProgressionEngine.cs` — modifiera: läser SessionMuscleFeedback istället för global
- `CoachPromptEngine.cs` — modifiera för nya feedback-modellen
- `CycleContextService.cs` — modifiera för nya Mesocycle-modellen
- `DatabaseService.cs` — komplett omskrivning av alla template/cycle/session-metoder + seed
- `WeeklyVolumeService.cs` — modifiera för nya modellen

**Views/ViewModels — nya:**
- `TemplatePickerPage` + VM — välj färdig template
- `MesoBuilderPageV2` + VM — ersätter befintlig
- `TemplatePlanningBoardPage` + VM — edit muskelstruktur per dag
- `MesocycleStatsPage` + VM — full stats för aktiv cykel
- `MuscleFeedbackSheet` + VM — bottomsheet för Pump/Volume-feedback
- `JointPainSheet` + VM — bottomsheet för joint pain per övning
- `ExerciseSlotPicker` + VM — välj muskelgrupp för slot

**Views/ViewModels — modifierade:**
- `PeriodizationPage` + VM — visa mesocykler + launcher till Meso Builder / Template Picker
- `ActiveWorkoutPage` + VM — RP-header, muscle group pills, RIR som placeholder, set-meny
- `PostWorkoutPage` + VM — "Workout complete!"-vy
- `LibraryPage` + VM — visa templates + länk till TemplatePlanningBoard
- `ExercisePickerPage` + VM — filter (muscle + equipment + author) + auto-recommend
- `OnboardingPage` + VM — equipment-preferens-steg
- `HemPage`/`KroppPage`/`SettingsPage` — mindre uppdateringar för nya modeller

---

# ETAPP 1 — Data-modell + Services (15 tasks)

Målet med Etapp 1 är att bygga hela den nya arkitekturen på back-end innan UI:t rörs. Under denna etapp bräker UI:t (LibraryPage, TemplateEditPage, ActiveWorkoutPage, PeriodizationPage m.fl.) eftersom de pratar med gamla modellen. **Detta är accepterat** — appen ska inte köras publikt under övergången.

## Task 1: Enum-utvidgning + nya prio-enums

**Files:**
- Modify: `LockIn/Models/Exercise.cs` (utvidga `MuscleGroup`-enum)
- Create: `LockIn/Models/MusclePriority.cs`
- Create: `LockIn/Models/FeedbackLevels.cs`

**Interfaces:**
- Produces: `MuscleGroup`-enum utökad med Quads=10, Hamstrings=11, Glutes=12, Calves=13, Traps=14, Abs=15. `MusclePriorityLevel { Emphasize=2, Grow=1, Maintain=0 }`. `JointPainLevel { None=0, Low=1, Moderate=2, ALotOf=3 }`. `PumpLevel { Low=1, Moderate=2, Amazing=3 }`. `VolumeLevel { NotEnough=1, JustRight=2, PushedLimits=3, TooMuch=4 }`.

- [ ] **Steg 1:** Utvidga `MuscleGroup` i `Exercise.cs`:

```csharp
public enum MuscleGroup
{
    Chest=0, Back=1, Shoulders=2, Biceps=3, Triceps=4,
    Legs=5, Core=6, FullBody=7, Other=8, Forearms=9,
    Quads=10, Hamstrings=11, Glutes=12, Calves=13, Traps=14, Abs=15
}
```

- [ ] **Steg 2:** Skapa `Models/MusclePriority.cs`:

```csharp
namespace LockIn.Models;

public enum MusclePriorityLevel
{
    Maintain = 0,
    Grow     = 1,
    Emphasize = 2,
}
```

- [ ] **Steg 3:** Skapa `Models/FeedbackLevels.cs`:

```csharp
namespace LockIn.Models;

public enum JointPainLevel  { None=0, Low=1, Moderate=2, ALotOf=3 }
public enum PumpLevel       { Low=1, Moderate=2, Amazing=3 }
public enum VolumeLevel     { NotEnough=1, JustRight=2, PushedLimits=3, TooMuch=4 }
```

- [ ] **Steg 4:** Bygg: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`. Förväntat resultat: eventuella switch-case i befintlig kod som täcker MuscleGroup men saknar de nya värdena rapporterar warnings. Ignorera för nu — task 4 (Reset) skriver om databas-sökningar.

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 1 — utvidga muskelgrupp-enum + prio-enums`

---

## Task 2: Nya template-modeller

**Files:**
- Modify: `LockIn/Models/WorkoutTemplate.cs` — bygg om
- Create: `LockIn/Models/TemplateDay.cs`
- Create: `LockIn/Models/TemplateMuscleSlot.cs`
- Modify: `LockIn/Models/TemplateExercise.cs` — bygg om

**Interfaces:**
- Consumes: `MuscleGroup` (Task 1)
- Produces: `WorkoutTemplate { Id, Name, IsCustom, DefaultDaysPerWeek }`, `TemplateDay { Id, TemplateId, DayIndex, Label, DefaultDayOfWeek }`, `TemplateMuscleSlot { Id, TemplateDayId, MuscleGroup, SortOrder }`, `TemplateExercise { Id, TemplateMuscleSlotId, ExerciseId, SortOrder, TargetRepsMin, TargetRepsMax }`.

- [ ] **Steg 1:** Skriv om `WorkoutTemplate.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("WorkoutTemplates")]
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [NotNull] public string Name { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = true;
    public int DefaultDaysPerWeek { get; set; } = 4;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore] public DateTime? LastUsedAt { get; set; }
}
```

- [ ] **Steg 2:** Skapa `Models/TemplateDay.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("TemplateDays")]
public class TemplateDay
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int TemplateId { get; set; }
    public int DayIndex { get; set; }         // 0..N-1, matchar rendering-ordning
    public string Label { get; set; } = "";   // t.ex. "Push A"
    public int? DefaultDayOfWeek { get; set; } // 0=Mon..6=Sun, null = asynkron
}
```

- [ ] **Steg 3:** Skapa `Models/TemplateMuscleSlot.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("TemplateMuscleSlots")]
public class TemplateMuscleSlot
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int TemplateDayId { get; set; }
    public int MuscleGroupValue { get; set; }
    public int SortOrder { get; set; }

    [Ignore]
    public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
}
```

- [ ] **Steg 4:** Skriv om `TemplateExercise.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("TemplateExercises")]
public class TemplateExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int TemplateMuscleSlotId { get; set; }
    [Indexed] public int ExerciseId { get; set; }
    public int SortOrder { get; set; }
    public int TargetRepsMin { get; set; } = 8;
    public int TargetRepsMax { get; set; } = 12;
    public int DefaultRestSeconds { get; set; } = 120;
}
```

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 2 — nya template-modeller (Days, Slots, Exercises)`

---

## Task 3: Nya mesocycle-modeller

**Files:**
- Delete: `LockIn/Models/TrainingCycle.cs`, `LockIn/Models/CycleWeek.cs`, `LockIn/Models/CycleSession.cs`
- Create: `LockIn/Models/Mesocycle.cs`
- Create: `LockIn/Models/MesocyclePriority.cs`
- Create: `LockIn/Models/MesocycleWeek.cs`

**Interfaces:**
- Consumes: `MuscleGroup`, `MusclePriorityLevel` (Task 1), `WorkoutTemplate` (Task 2)
- Produces: `Mesocycle { Id, Name, TemplateId, StartDate, WeekCount, IsActive, UnitSystem, CreatedAt }`, `MesocyclePriority { Id, MesocycleId, MuscleGroup, PriorityLevel }`, `MesocycleWeek { Id, MesocycleId, WeekNumber, IsDeload, IntensityPercent }`.

- [ ] **Steg 1:** Skapa `Models/Mesocycle.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

public enum UnitSystem { Kg = 0, Lb = 1 }

[Table("Mesocycles")]
public class Mesocycle
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [NotNull] public string Name { get; set; } = "";
    public int TemplateId { get; set; }
    public DateTime StartDate { get; set; }
    public int WeekCount { get; set; }
    public bool IsActive { get; set; }
    public int UnitSystemValue { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore] public UnitSystem UnitSystem
    {
        get => (UnitSystem)UnitSystemValue;
        set => UnitSystemValue = (int)value;
    }
}
```

- [ ] **Steg 2:** Skapa `Models/MesocyclePriority.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("MesocyclePriorities")]
public class MesocyclePriority
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int MesocycleId { get; set; }
    public int MuscleGroupValue { get; set; }
    public int PriorityLevelValue { get; set; }

    [Ignore] public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
    [Ignore] public MusclePriorityLevel PriorityLevel
    {
        get => (MusclePriorityLevel)PriorityLevelValue;
        set => PriorityLevelValue = (int)value;
    }
}
```

- [ ] **Steg 3:** Skapa `Models/MesocycleWeek.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("MesocycleWeeks")]
public class MesocycleWeek
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int MesocycleId { get; set; }
    public int WeekNumber { get; set; }        // 1-indexed
    public bool IsDeload { get; set; }
    public int IntensityPercent { get; set; }  // 50-90
}
```

- [ ] **Steg 4:** Delete gamla filerna: `TrainingCycle.cs`, `CycleWeek.cs`, `CycleSession.cs`. Ta även bort `PeriodizationViewModel.cs` och `CycleDetailViewModel.cs` (byggs om i Etapp 2).

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 3 — nya mesocycle-modeller (Mesocycle, Priority, Week)`

---

## Task 4: Session-modeller uppdaterade

**Files:**
- Modify: `LockIn/Models/WorkoutSession.cs`
- Modify: `LockIn/Models/SessionExercise.cs`
- Create: `LockIn/Models/SessionMuscleFeedback.cs`
- Create: `LockIn/Models/SessionExerciseFeedback.cs`

**Interfaces:**
- Consumes: `MesocycleWeek`, `TemplateDay`, `TemplateMuscleSlot`, `MuscleGroup`, `JointPainLevel`, `PumpLevel`, `VolumeLevel`
- Produces: `WorkoutSession { Id, MesocycleWeekId, TemplateDayId, StartedAt, CompletedAt, Notes }`, `SessionExercise { Id, SessionId, ExerciseId, MuscleGroupValue, TemplateMuscleSlotId, OrderIndex, IsSkipped, Notes, SupersetGroupId }`, feedback-tabellerna.

- [ ] **Steg 1:** Skriv om `WorkoutSession.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("WorkoutSessions")]
public class WorkoutSession
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int? MesocycleWeekId { get; set; }  // null = fritt pass
    public int? TemplateDayId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
}
```

- [ ] **Steg 2:** Skriv om `SessionExercise.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("SessionExercises")]
public class SessionExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionId { get; set; }
    public int ExerciseId { get; set; }
    public int MuscleGroupValue { get; set; }  // cache från Exercise.MuscleGroup vid sessionsstart
    public int? TemplateMuscleSlotId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsSkipped { get; set; }
    public string Notes { get; set; } = "";
    public int? SupersetGroupId { get; set; }

    [Ignore] public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
}
```

- [ ] **Steg 3:** Skapa `Models/SessionMuscleFeedback.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("SessionMuscleFeedback")]
public class SessionMuscleFeedback
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionId { get; set; }
    public int MuscleGroupValue { get; set; }
    public int PumpLevelValue { get; set; }
    public int VolumeLevelValue { get; set; }

    [Ignore] public MuscleGroup MuscleGroup
    {
        get => (MuscleGroup)MuscleGroupValue;
        set => MuscleGroupValue = (int)value;
    }
    [Ignore] public PumpLevel Pump
    {
        get => (PumpLevel)PumpLevelValue;
        set => PumpLevelValue = (int)value;
    }
    [Ignore] public VolumeLevel Volume
    {
        get => (VolumeLevel)VolumeLevelValue;
        set => VolumeLevelValue = (int)value;
    }
}
```

- [ ] **Steg 4:** Skapa `Models/SessionExerciseFeedback.cs`:

```csharp
using SQLite;

namespace LockIn.Models;

[Table("SessionExerciseFeedback")]
public class SessionExerciseFeedback
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionExerciseId { get; set; }
    public int JointPainLevelValue { get; set; }

    [Ignore] public JointPainLevel JointPain
    {
        get => (JointPainLevel)JointPainLevelValue;
        set => JointPainLevelValue = (int)value;
    }
}
```

- [ ] **Steg 5:** Ta bort `WorkoutSession.PumpRating`, `SorenessRating`, `PerformanceRating` (redan gjort i Steg 1). Ta bort `WorkoutSession.CycleWeekId` och `TemplateId` — ersatta med `MesocycleWeekId` och `TemplateDayId`.

- [ ] **Steg 6:** Commit: `feat(rp-clone): Task 4 — session-modeller + feedback-tabeller`

---

## Task 5: DB reset + createTable-kaskad + AppSettings

**Files:**
- Modify: `LockIn/Services/DatabaseService.cs` — komplett `InitCoreAsync`-omskrivning
- Modify: `LockIn/Models/AppSettings.cs`

**Interfaces:**
- Produces: `DatabaseService` med de nya tabellerna registrerade. Alla gamla data raderas vid initiering.

- [ ] **Steg 1:** Utöka `AppSettings.cs`:

```csharp
public string PreferredEquipmentJson { get; set; } = "";  // JSON array av EquipmentType-int
public bool HasSeenStartWeightHint { get; set; } = false;
public int OnboardingStep { get; set; } = 0;  // ny — trackar var i onboarding användaren är
```

- [ ] **Steg 2:** Ersätt `InitCoreAsync` i `DatabaseService.cs`:

```csharp
private async Task InitCoreAsync()
{
    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lockin.db");
    _db = new SQLiteAsyncConnection(dbPath,
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

    // RP-clone reset: droppa gamla RP-relaterade tabeller
    await _db.ExecuteAsync("DROP TABLE IF EXISTS WorkoutTemplates");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS TemplateExercises");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS TrainingCycles");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS CycleWeeks");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS CycleSessions");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS WorkoutSessions");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS SessionExercises");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS LoggedSets");
    await _db.ExecuteAsync("DROP TABLE IF EXISTS MuscleVolumeLandmarks");

    // Ny schema
    await _db.CreateTableAsync<Exercise>();
    await _db.CreateTableAsync<WorkoutTemplate>();
    await _db.CreateTableAsync<TemplateDay>();
    await _db.CreateTableAsync<TemplateMuscleSlot>();
    await _db.CreateTableAsync<TemplateExercise>();
    await _db.CreateTableAsync<Mesocycle>();
    await _db.CreateTableAsync<MesocyclePriority>();
    await _db.CreateTableAsync<MesocycleWeek>();
    await _db.CreateTableAsync<WorkoutSession>();
    await _db.CreateTableAsync<SessionExercise>();
    await _db.CreateTableAsync<LoggedSet>();
    await _db.CreateTableAsync<SessionMuscleFeedback>();
    await _db.CreateTableAsync<SessionExerciseFeedback>();
    await _db.CreateTableAsync<MuscleVolumeLandmarks>();
    await _db.CreateTableAsync<AppSettings>();
    await _db.CreateTableAsync<BodyWeightEntry>();
    await _db.CreateTableAsync<BodyCompositionEntry>();
    await _db.CreateTableAsync<UserAchievement>();
    await _db.CreateTableAsync<WorkoutPhoto>();
    await _db.CreateTableAsync<CardioSession>();

    // AppSettings-migreringar (endast nya fält)
    try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN PreferredEquipmentJson TEXT NOT NULL DEFAULT ''"); }
    catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
    try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HasSeenStartWeightHint INTEGER NOT NULL DEFAULT 0"); }
    catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
    try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN OnboardingStep INTEGER NOT NULL DEFAULT 0"); }
    catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

    await SeedAsync();
    await SeedExerciseDescriptionsAsync();
    await SeedForearmExercisesAsync();
    await SeedExerciseDbAsync();
    await SeedSwedishNamesAsync();
    await SeedMuscleVolumeLandmarksAsync();
    // Task 14 lägger till SeedTemplateLibraryAsync()
}
```

- [ ] **Steg 3:** Uppdatera `SeedMuscleVolumeLandmarksAsync` med de nya muskelgrupperna (Quads, Hamstrings, Glutes, Calves, Traps, Abs). Standardvärden (från research):

```csharp
var defaults = new (MuscleGroup Group, int Mev, int Mrv)[]
{
    (MuscleGroup.Chest,      10, 20),
    (MuscleGroup.Back,       12, 22),
    (MuscleGroup.Shoulders,  10, 20),
    (MuscleGroup.Biceps,      8, 17),
    (MuscleGroup.Triceps,     8, 17),
    (MuscleGroup.Quads,      10, 20),
    (MuscleGroup.Hamstrings,  8, 18),
    (MuscleGroup.Glutes,      8, 16),
    (MuscleGroup.Calves,      8, 16),
    (MuscleGroup.Traps,       6, 14),
    (MuscleGroup.Forearms,    6, 14),
    (MuscleGroup.Abs,         8, 18),
};
```

- [ ] **Steg 4:** Ta bort alla gamla metoder i `DatabaseService.cs` som pratar med raderade tabeller:
  - `GetTemplatesAsync`, `GetTemplateExercisesAsync`, `SaveTemplateAsync`, `DeleteTemplateAsync`
  - `GetCyclesAsync`, `GetCycleWeeksAsync`, `GetCycleSessionsAsync`, `SaveCycleAsync`, `DeleteCycleAsync`
  - `GetSessionsAsync`, `SaveSessionAsync` (kommer i Task 7)
  - `GetSessionExercisesAsync`, `SaveSessionExerciseAsync`
  - `GetLoggedSetsAsync`, `SaveLoggedSetAsync`
  - Volym-metoder som pratar med gamla session-strukturen (`GetSessionVolumeByMuscleGroupAsync`, `GetRawVolumeForRangeAsync`, etc.) — omskrivs i Task 7/13.

- [ ] **Steg 5:** Bygg. **Väntad utfall:** många kompileringsfel i ViewModels som anropar borttagna metoder. Det är accepterat — Task 6-13 återinför metoder gradvis. Fixa fel i services först; UI-fel lämnas till Etapp 2.

- [ ] **Steg 6:** Commit: `feat(rp-clone): Task 5 — DB reset + createTable-kaskad + AppSettings-flaggor`

---

## Task 6: TemplateService

**Files:**
- Create: `LockIn/Services/TemplateService.cs`
- Modify: `LockIn/Services/DatabaseService.cs` — lägg till metoder för template-CRUD

**Interfaces:**
- Consumes: `WorkoutTemplate`, `TemplateDay`, `TemplateMuscleSlot`, `TemplateExercise` (Task 2)
- Produces:
```csharp
public interface ITemplateService
{
    Task<List<WorkoutTemplate>> ListAsync();
    Task<WorkoutTemplate?> GetAsync(int id);
    Task<TemplateDetail> GetDetailAsync(int templateId);
    Task<int> SaveAsync(WorkoutTemplate template, List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDay,
        Dictionary<int, List<TemplateExercise>> exercisesBySlot);
    Task DeleteAsync(int templateId);
    Task<WorkoutTemplate> CloneAsync(int sourceTemplateId, string newName);
}

public sealed record TemplateDetail(
    WorkoutTemplate Template,
    IReadOnlyList<TemplateDay> Days,
    IReadOnlyDictionary<int, IReadOnlyList<TemplateMuscleSlot>> SlotsByDay,
    IReadOnlyDictionary<int, IReadOnlyList<TemplateExercise>> ExercisesBySlot);
```

- [ ] **Steg 1:** Lägg till `DatabaseService`-metoder:
  - `GetTemplatesAsync()` — `SELECT * FROM WorkoutTemplates ORDER BY CreatedAt DESC`
  - `GetTemplateDaysAsync(int templateId)`, `GetTemplateMuscleSlotsAsync(int templateDayId)`, `GetTemplateExercisesForSlotAsync(int templateMuscleSlotId)`
  - `SaveTemplateWithChildrenAsync(...)` — transactional insert/update med kaskad
  - `DeleteTemplateAsync(int templateId)` — kaskadborttagning

- [ ] **Steg 2:** Skapa `Services/TemplateService.cs` med implementationen ovan. `GetDetailAsync` hämtar allt i sekvens och bygger `TemplateDetail`-record.

- [ ] **Steg 3:** `CloneAsync` — kopiera struktur men ny Id-serie. Namn: `"{sourceName} (kopia)"`.

- [ ] **Steg 4:** Registrera i `MauiProgram.cs`: `builder.Services.AddSingleton<ITemplateService, TemplateService>();`

- [ ] **Steg 5:** Bygg. Verifiera 0 errors.

- [ ] **Steg 6:** Commit: `feat(rp-clone): Task 6 — TemplateService CRUD`

---

## Task 7: MesocycleService + session-koppling

**Files:**
- Create: `LockIn/Services/MesocycleService.cs`
- Modify: `LockIn/Services/DatabaseService.cs` — session-metoder

**Interfaces:**
- Consumes: `Mesocycle`, `MesocyclePriority`, `MesocycleWeek`, `WorkoutSession`, `TemplateDay`
- Produces:
```csharp
public interface IMesocycleService
{
    Task<List<Mesocycle>> ListAsync();
    Task<Mesocycle?> GetActiveAsync();
    Task<MesocycleDetail?> GetDetailAsync(int mesocycleId);
    Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks);
    Task ActivateAsync(int mesocycleId);
    Task DeleteAsync(int mesocycleId);
    Task<MesocycleWeek?> GetCurrentWeekAsync();
    Task<MesocyclePriority?> GetPriorityAsync(int mesocycleId, MuscleGroup muscle);
}

public sealed record MesocycleDetail(
    Mesocycle Meso,
    WorkoutTemplate Template,
    IReadOnlyList<MesocycleWeek> Weeks,
    IReadOnlyList<MesocyclePriority> Priorities);
```

- [ ] **Steg 1:** `DatabaseService`-metoder: `GetMesocyclesAsync`, `GetMesocyclePrioritiesAsync(int)`, `GetMesocycleWeeksAsync(int)`, `SaveMesocycleAsync(...)`, `DeleteMesocycleAsync(int)`.

- [ ] **Steg 2:** `DatabaseService.SaveWorkoutSessionAsync(WorkoutSession)`, `GetWorkoutSessionAsync(int)`, `GetSessionsInRangeAsync(from, to)`, `GetSessionsForMesocycleAsync(int)`.

- [ ] **Steg 3:** Skapa `Services/MesocycleService.cs`. `GetCurrentWeekAsync` beräknar från `DateTime.Today - meso.StartDate` i veckor. `ActivateAsync` sätter alla andra `IsActive=false` i en transaktion.

- [ ] **Steg 4:** Registrera i `MauiProgram.cs`.

- [ ] **Steg 5:** Bygg. Commit: `feat(rp-clone): Task 7 — MesocycleService + session-CRUD`

---

## Task 8: LoggedSet + rekonstruerade session-metoder

**Files:**
- Modify: `LockIn/Services/DatabaseService.cs` — logged-set-metoder

**Interfaces:**
- Consumes: `LoggedSet` (samma struktur som förut)
- Produces:
```csharp
Task<int> SaveLoggedSetAsync(LoggedSet set);
Task<List<LoggedSet>> GetLoggedSetsAsync(int sessionExerciseId);
Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId);
Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId);
```

- [ ] **Steg 1:** Behåll `LoggedSet.cs` som den är (Id, SessionExerciseId, SetNumber, WeightKg, Reps, RIR, LoggedAt, IsPR, SetType, DurationSeconds). `RIR` behålls som int-fält även om UI:t flyttar det till placeholder — faktisk RIR fångas i feedback-sheet.

- [ ] **Steg 2:** Återinför de listade metoderna i `DatabaseService`.

- [ ] **Steg 3:** Uppdatera `GetLastSessionSetsAsync` att joinar mot nya `SessionExercise.SessionId` istället för gamla struktur — SQL:en är samma.

- [ ] **Steg 4:** Bygg. Commit: `feat(rp-clone): Task 8 — logged-set-metoder rekonstruerade`

---

## Task 9: ExerciseRecommendationService

**Files:**
- Create: `LockIn/Services/ExerciseRecommendationService.cs`
- Modify: `LockIn/Services/DatabaseService.cs` — hjälpmetoder för exercise-lookup

**Interfaces:**
- Consumes: `Exercise`, `MuscleGroup`, `EquipmentType`, `MusclePriorityLevel`, `AppSettings.PreferredEquipmentJson`
- Produces:
```csharp
public interface IExerciseRecommendationService
{
    Task<List<Exercise>> RecommendForMuscleAsync(
        MuscleGroup muscle,
        MusclePriorityLevel priority,
        IReadOnlyList<int> excludeExerciseIds,
        int count);
}
```

- [ ] **Steg 1:** Läs `AppSettings.PreferredEquipmentJson` (JSON-array av EquipmentType-int). Fallback: alla equipment.

- [ ] **Steg 2:** Query `Exercises WHERE MuscleGroup = ? AND Equipment IN (...) AND Id NOT IN (excluded)`. Sortera:
  - `Mechanic = Compound` före `Isolation`
  - `Level = Beginner/Intermediate` före `Expert`
  - Slumpa inom samma kategori (deterministic seed = mesocycle-Id + slot-Id)

- [ ] **Steg 3:** För `Emphasize`-prio: returnera 2-3 övningar (compound + isolation-varians). För `Grow`: 1-2 övningar. För `Maintain`: 1 övning.

- [ ] **Steg 4:** Registrera i `MauiProgram.cs`.

- [ ] **Steg 5:** Bygg. Commit: `feat(rp-clone): Task 9 — ExerciseRecommendationService`

---

## Task 10: MesoBuilderServiceV2

**Files:**
- Delete: `LockIn/Services/MesoBuilderService.cs` (den gamla)
- Create: `LockIn/Services/MesoBuilderServiceV2.cs`

**Interfaces:**
- Consumes: `ITemplateService`, `IMesocycleService`, `IExerciseRecommendationService`, `MusclePriorityLevel`
- Produces:
```csharp
public sealed record MesoBuilderSpec(
    string MesocycleName,
    int WeekCount,             // inkl. deload
    UnitSystem UnitSystem,
    int DaysPerWeek,
    IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> Priorities,
    IReadOnlyList<int> PreferredEquipment,
    DateTime StartDate);

public interface IMesoBuilderService
{
    Task<Mesocycle> BuildAsync(MesoBuilderSpec spec);
    Task<Mesocycle> StartFromTemplateAsync(int templateId, string mesocycleName,
        DateTime startDate, int weekCount, UnitSystem unit,
        IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> priorities);
}
```

- [ ] **Steg 1:** `BuildAsync`:
  1. Skapa ny `WorkoutTemplate { IsCustom=true, Name="{mesocycleName} template" }`
  2. Skapa `TemplateDay[]` — DayIndex 0..DaysPerWeek-1, DefaultDayOfWeek fördelad jämnt (t.ex. Mon/Wed/Fri för 3 dagar)
  3. Fördela muskelgrupperna över dagarna enligt prioritet. Emphasize-muskler får plats i flera dagar; Maintain i färre. Använd standard-split-mönster (PPL/ULP/FullBody) baserat på dags-antal.
  4. För varje `TemplateMuscleSlot`: anropa `ExerciseRecommendationService.RecommendForMuscleAsync` — skapa `TemplateExercise[]` med default `TargetRepsMin/Max` (8-12 för compound, 12-15 för isolation).
  5. Skapa `Mesocycle` + `MesocyclePriority[]` från spec.
  6. Skapa `MesocycleWeek[]` — accumulation 70–90% linjärt, sista veckan Deload=true, 50%.

- [ ] **Steg 2:** `StartFromTemplateAsync`: skippar template-generering, skapar bara ny Mesocycle + Priorities + Weeks kopplade till en befintlig template.

- [ ] **Steg 3:** Delete `MesoBuilderService.cs`. Registrera nya i `MauiProgram.cs`: `AddSingleton<IMesoBuilderService, MesoBuilderServiceV2>()`.

- [ ] **Steg 4:** Bygg. Kommentera bort eventuell `MesoBuilderViewModel` som inte kompilerar — den skrivs om i Etapp 2.

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 10 — MesoBuilderServiceV2 med prio-baserad auto-generering`

---

## Task 11: VolumeProgressionService

**Files:**
- Create: `LockIn/Services/VolumeProgressionService.cs`

**Interfaces:**
- Consumes: `IMesocycleService`, `IWeeklyVolumeService`, `MusclePriorityLevel`, `MuscleVolumeLandmarks`
- Produces:
```csharp
public sealed record PlannedVolume(MuscleGroup Muscle, int WeekNumber, int TargetSets);

public interface IVolumeProgressionService
{
    Task<IReadOnlyList<PlannedVolume>> GetPlannedVolumeForMesocycleAsync(int mesocycleId);
    Task<int> GetTargetSetsForCurrentWeekAsync(MuscleGroup muscle);
}
```

- [ ] **Steg 1:** Algoritm för `GetPlannedVolumeForMesocycleAsync`:
  - Hämta mesocycle, priorities, weeks
  - Hämta MEV/MRV per muskelgrupp
  - För varje muskel + varje vecka (utom deload):
    - **Emphasize:** Start = MEV. Öka +2 sets/vecka mot MRV. Cap vid MRV.
    - **Grow:** Start = MEV. Öka +1 set/vecka. Cap vid MRV.
    - **Maintain:** Fast vid ~MV (6-8 sets, hämtas från AppSettings eller default 6).
  - Deload-vecka: 50% av accumulation-veckans sista sets, avrundat nedåt, min 2.

- [ ] **Steg 2:** `GetTargetSetsForCurrentWeekAsync` returnerar värdet för aktuell vecka baserat på `MesocycleService.GetCurrentWeekAsync`.

- [ ] **Steg 3:** Registrera i `MauiProgram.cs`.

- [ ] **Steg 4:** Bygg. Commit: `feat(rp-clone): Task 11 — VolumeProgressionService`

---

## Task 12: Uppdatera WeightProgressionService + SetProgressionEngine + CycleContextService

**Files:**
- Modify: `LockIn/Services/WeightProgressionService.cs`
- Modify: `LockIn/Services/SetProgressionEngine.cs`
- Modify: `LockIn/Services/CycleContextService.cs`
- Delete: (om inte redan gjort) referenser till borttagna WorkoutSession-fält

**Interfaces:**
- Consumes: nya modeller
- Produces: samma publika interface men internt uppdaterade

- [ ] **Steg 1:** `WeightProgressionService.SuggestNextWeightAsync` — samma logik, men jämför mot `LoggedSet.RIR` från senaste `SessionExercise` med samma `ExerciseId`. Använd `WorkoutSession.CompletedAt IS NOT NULL` som filter.

- [ ] **Steg 2:** `SetProgressionEngine.AnalyzeAsync`:
  - Ersätt läsning av `WorkoutSession.PumpRating` etc. med query mot `SessionMuscleFeedback` för senaste 2-3 pass som tränat samma muskel.
  - Deload-trigger: senaste passets `SessionMuscleFeedback` visar `VolumeLevel=TooMuch` på flera muskler.
  - Volym-advice: två pass i rad med `VolumeLevel=NotEnough` för muskel → +1 set. `PushedLimits + JointPain=Moderate+` → -1 set.

- [ ] **Steg 3:** `CycleContextService.GetCurrentAsync`:
  - Hämta aktiv Mesocycle via `MesocycleService.GetActiveAsync`
  - Hämta `MesocycleWeek` från vecka-beräkning
  - Behåll `CycleContext`-recorden men uppdatera fälten (byt `TrainingCycle` → `Mesocycle`, `CycleWeek` → `MesocycleWeek`).

- [ ] **Steg 4:** Bygg. Fixa alla kompilfel i services (inte ViewModels).

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 12 — services uppdaterade för nya modellen`

---

## Task 13: WeeklyVolumeService uppdaterad

**Files:**
- Modify: `LockIn/Services/WeeklyVolumeService.cs`

**Interfaces:**
- Consumes: nya session-modellen + `IVolumeProgressionService`
- Produces: `WeeklyVolumeReport` uppdaterad med `PlannedSets`-fält

- [ ] **Steg 1:** Utöka `WeeklyVolumeReport`:

```csharp
public sealed record WeeklyVolumeReport(
    MuscleGroup Muscle,
    int SetsThisWeek,
    int PlannedSets,      // från VolumeProgressionService
    int MEV,
    int MRV,
    VolumeZone Zone);
```

- [ ] **Steg 2:** Byt `_db.GetCompletedSessionsInRangeAsync` mot ny motsvarighet, byt `GetSessionVolumeByMuscleGroupAsync` mot ny query som räknar sets från nya `SessionExercise.MuscleGroupValue`.

- [ ] **Steg 3:** Om aktiv mesocycle finns → hämta `PlannedSets` från `IVolumeProgressionService`. Annars = 0.

- [ ] **Steg 4:** Bygg. Commit: `feat(rp-clone): Task 13 — WeeklyVolumeService använder nya modellen`

---

## Task 14: Seed template-bibliotek (4 färdiga templates)

**Files:**
- Create: `LockIn/Data/TemplateLibrary.cs` — statisk klass med template-definitioner
- Modify: `LockIn/Services/DatabaseService.cs` — `SeedTemplateLibraryAsync`

**Interfaces:**
- Produces: 4 seedade templates i `WorkoutTemplates` med `IsCustom=false`

- [ ] **Steg 1:** Skapa `Data/TemplateLibrary.cs` med statiska definitioner för fyra templates. Varje template har en record:

```csharp
public sealed record TemplateDef(
    string Name,
    int DaysPerWeek,
    IReadOnlyList<TemplateDayDef> Days);
public sealed record TemplateDayDef(
    string Label,
    int DefaultDayOfWeek,
    IReadOnlyList<MuscleGroup> Muscles);
```

Definitioner:
1. **PPL 6-day** — Push (Chest, Shoulders, Triceps) / Pull (Back, Biceps, Traps) / Legs (Quads, Hamstrings, Glutes, Calves), 2 varv/vecka
2. **Upper/Lower 4-day** — Upper (Chest, Back, Shoulders, Biceps, Triceps) / Lower (Quads, Hamstrings, Glutes, Calves) × 2
3. **Full Body 3-day** — alla huvudmuskler varje dag, olika volym-fokus per dag
4. **Bro-split 5-day** — Chest / Back / Shoulders / Legs / Arms

Ingen exercise-tilldelning i templates — övningar väljs vid `StartFromTemplateAsync` via `ExerciseRecommendationService`.

- [ ] **Steg 2:** `SeedTemplateLibraryAsync` i `DatabaseService`: kontrollera `SELECT COUNT(*) FROM WorkoutTemplates WHERE IsCustom = 0`. Om 0, kör seed.

- [ ] **Steg 3:** Lägg till `await SeedTemplateLibraryAsync()` sist i `InitCoreAsync`.

- [ ] **Steg 4:** Bygg. Kör appen på simulator, verifiera i SQLite att 4 templates finns med `IsCustom=0`.

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 14 — template-bibliotek seedat (4 templates)`

---

## Task 15: DI + MauiProgram cleanup

**Files:**
- Modify: `LockIn/MauiProgram.cs`

**Interfaces:**
- Produces: DI-container med alla nya services registrerade och gamla borttagna

- [ ] **Steg 1:** Kontrollera att följande är registrerade:
  - `ITemplateService`
  - `IMesocycleService`
  - `IExerciseRecommendationService`
  - `IMesoBuilderService` (V2)
  - `IVolumeProgressionService`
  - `IWeightProgressionService`
  - `ISetProgressionEngine`
  - `IWeeklyVolumeService`
  - `ICycleContextService`

- [ ] **Steg 2:** Ta bort registreringar av borttagna sidor/VM (t.ex. `MesoBuilderPage/VM` — ersätts i Task 21, `PeriodizationPage/VM` — ersätts i Task 20, `CycleDetailPage/VM` — tas bort).

- [ ] **Steg 3:** Ta bort borttagna route:er i `AppShell.xaml.cs`.

- [ ] **Steg 4:** Bygg. **Etapp 1 klar** — services ska kompilera. UI-lager har fortsatt fel; det är Etapp 2:s jobb.

- [ ] **Steg 5:** Commit: `feat(rp-clone): Task 15 — Etapp 1 klar (services + modeller)`

---

# ETAPP 2 — UI Rebuild (15 tasks)

Nu byggs alla UI-sidor om till RP-modellen.

## Task 16: Onboarding — equipment-preferens-steg

**Files:**
- Modify: `LockIn/ViewModels/OnboardingViewModel.cs`
- Modify: `LockIn/Views/OnboardingPage.xaml` + `.xaml.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx` + `AppResources.cs`

**Interfaces:**
- Produces: Ny onboarding-flow som sparar `AppSettings.PreferredEquipmentJson`

- [ ] **Steg 1:** I `OnboardingViewModel` — ny state `SelectedEquipment: HashSet<int>`. Toggle-commands för varje EquipmentType.

- [ ] **Steg 2:** Ny wizard-sida i XAML med 9 rader (Barbell, Bodyweight, Bodyweight loadable, Cable, Dumbbell, Freemotion, Machine, Machine assistance, Smith machine). Checkbox-style toggle. RP-liknande layout från IMG_0940.

- [ ] **Steg 3:** På "Klar" → serialisera hash till JSON, spara i `AppSettings.PreferredEquipmentJson`, sätt `HasCompletedOnboarding=true`.

- [ ] **Steg 4:** i18n-nycklar: `Onboarding_Equipment_Title`, `Onboarding_Equipment_Hint`, `Equipment_Barbell` etc.

- [ ] **Steg 5:** Bygg + testa på simulator. Commit.

---

## Task 17: LibraryPage om-design

**Files:**
- Modify: `LockIn/ViewModels/LibraryViewModel.cs`
- Modify: `LockIn/Views/LibraryPage.xaml`

**Interfaces:**
- Consumes: `ITemplateService`
- Produces: LibraryPage visar egna templates (IsCustom=true) + länk till Planning Board

- [ ] **Steg 1:** `LibraryViewModel.LoadAsync()` hämtar templates via `ITemplateService.ListAsync()`.

- [ ] **Steg 2:** Ny layout: överst en "Nytt template"-knapp som leder till Planning Board. Under: lista med kort per template med Namn, DaysPerWeek, senaste användning.

- [ ] **Steg 3:** Swipe-to-delete via `ITemplateService.DeleteAsync`.

- [ ] **Steg 4:** Tapp på ett kort → navigera till `TemplatePlanningBoardPage` med query-parameter `TemplateId`.

- [ ] **Steg 5:** Bygg + test. Commit.

---

## Task 18: TemplatePlanningBoardPage — edit muskelstruktur per dag

**Files:**
- Create: `LockIn/Views/TemplatePlanningBoardPage.xaml` + `.xaml.cs`
- Create: `LockIn/ViewModels/TemplatePlanningBoardViewModel.cs`
- Modify: `LockIn/MauiProgram.cs` + `AppShell.xaml.cs`

**Interfaces:**
- Consumes: `ITemplateService`, `IExerciseRecommendationService`
- Produces: Full CRUD för template-struktur (dagar, slots, övningar) enligt RP:s IMG_0935 planning board

- [ ] **Steg 1:** ViewModel-state:

```csharp
[ObservableProperty] private string _templateName;
public ObservableCollection<TemplateDayViewModel> Days { get; }
// TemplateDayViewModel har Label, DefaultDayOfWeek, ObservableCollection<MuscleSlotViewModel> Slots
// MuscleSlotViewModel har MuscleGroup, ObservableCollection<ExerciseViewModel> Exercises
```

- [ ] **Steg 2:** XAML — översta raden är dag-tab-bar (Mon/Tue/Wed... eller Day 1/Day 2/... beroende på DefaultDayOfWeek). Aktiv dag visar sina muskel-slots vertikalt. Varje slot har muskelpill (III/II/I) + lista av övningar med drag-handle + "Lägg till övning"-knapp.

- [ ] **Steg 3:** Långtryck på övning → set-meny (Move up/down, Remove).

- [ ] **Steg 4:** "Lägg till muskel"-knapp → `ExerciseSlotPicker` (bottomsheet med muskelgrupp-lista).

- [ ] **Steg 5:** "Lägg till övning" i slot → `ExercisePickerPage` (Task 22).

- [ ] **Steg 6:** "Auto-fyll övningar"-knapp → för varje slot utan övningar, kör `ExerciseRecommendationService`.

- [ ] **Steg 7:** "Spara"-knapp → `ITemplateService.SaveAsync`. Toast bekräftar.

- [ ] **Steg 8:** Bygg + test. Commit.

---

## Task 19: PeriodizationPage — mesocykel-hub

**Files:**
- Modify: `LockIn/ViewModels/PeriodizationViewModel.cs` — bygg om
- Modify: `LockIn/Views/PeriodizationPage.xaml` — bygg om

**Interfaces:**
- Consumes: `IMesocycleService`
- Produces: Ny hub som visar aktiva/tidigare mesocykler + 2-knappars launcher

- [ ] **Steg 1:** ViewModel: `ObservableCollection<MesocycleRow>` (Name, WeekCount, IsActive, StartDate, ProgressPercent). Load via `IMesocycleService.ListAsync`.

- [ ] **Steg 2:** XAML: två knappar överst — "MESO BUILDER" och "VÄLJ TEMPLATE" (matchar RP:s IMG_0941-kort). Under: lista med kort per mesocykel. Aktiv har extra badge.

- [ ] **Steg 3:** Tapp på cykel → `MesocycleStatsPage` (Task 28).

- [ ] **Steg 4:** Swipe-to-delete + confirm-dialog.

- [ ] **Steg 5:** Bygg + test. Commit.

---

## Task 20: MesoBuilderPageV2 — full wizard (frekvens → prio → skapa)

**Files:**
- Modify: `LockIn/ViewModels/MesoBuilderViewModel.cs` — bygg om helt
- Modify: `LockIn/Views/MesoBuilderPage.xaml` — bygg om helt

**Interfaces:**
- Consumes: `IMesoBuilderService`, `AppSettings.PreferredEquipmentJson`
- Produces: Wizard som kör IMG_0942 (frekvens) → IMG_0943 (prio) → IMG_0946 (namn/veckor/enhet) → skapar mesocycle

- [ ] **Steg 1:** ViewModel steg-state:

```csharp
[ObservableProperty] private int _step; // 0=frekvens, 1=prio, 2=namn/veckor
[ObservableProperty] private int _daysPerWeek = 4;
public ObservableCollection<MusclePriorityRow> Priorities { get; }
[ObservableProperty] private string _mesocycleName;
[ObservableProperty] private int _weekCount = 5;
[ObservableProperty] private UnitSystem _unit = UnitSystem.Kg;
```

- [ ] **Steg 2:** Steg 0 (frekvens): 5 radio-buttons för 2/3/4/5/6 dagar. Continue-knapp.

- [ ] **Steg 3:** Steg 1 (prio): tre kolumner — Emphasize / Grow / Maintain (matchar IMG_0943 tab-bar). "+ Lägg till muskel"-knapp öppnar muskelgrupp-picker och lägger till i vald kategori. Långtryck flyttar mellan kategorier. Ordning inom kategori: upp/ner-pilar (IMG_0937).

- [ ] **Steg 4:** Steg 2 (namn/veckor/enhet): matchar IMG_0946. Namn-Entry, veckor 4/5/6/7/8-toggle, enhet lb/kg-toggle. "Skapa"-knapp anropar `IMesoBuilderService.BuildAsync`.

- [ ] **Steg 5:** Toast "Cykeln har skapats", navigera till MesocycleStatsPage.

- [ ] **Steg 6:** Bygg + test hela flödet. Commit.

---

## Task 21: TemplatePickerPage — välj färdig template

**Files:**
- Create: `LockIn/Views/TemplatePickerPage.xaml` + `.xaml.cs`
- Create: `LockIn/ViewModels/TemplatePickerViewModel.cs`
- Modify: `LockIn/MauiProgram.cs` + `AppShell.xaml.cs`

**Interfaces:**
- Consumes: `ITemplateService`, `IMesoBuilderService.StartFromTemplateAsync`
- Produces: Sida som listar seedade templates + låter användaren välja + konfigurera mesocycle-inställningar

- [ ] **Steg 1:** VM listar templates med `IsCustom=false` via `ITemplateService.ListAsync()`.

- [ ] **Steg 2:** XAML: kort per template med Namn + DaysPerWeek + beskrivning ("Push, Pull, Legs — klassisk 6-dagars split").

- [ ] **Steg 3:** Tapp → bottomsheet med:
  - Mesocycle-namn (Entry, default: templatens namn + " – " + datum)
  - Startdatum (DatePicker, default idag)
  - Veckor (4/5/6/7/8)
  - Enhet (lb/kg)
  - Muskelprio-sammanfattning (defaults från template-strukturen; alla Grow)
  - "Skapa" → `IMesoBuilderService.StartFromTemplateAsync`

- [ ] **Steg 4:** Bygg + test. Commit.

---

## Task 22: ExercisePickerPage — filter + auto-recommend

**Files:**
- Modify: `LockIn/ViewModels/ExercisePickerViewModel.cs`
- Modify: `LockIn/Views/ExercisePickerPage.xaml`

**Interfaces:**
- Consumes: `IExerciseRecommendationService`, `AppSettings.PreferredEquipmentJson`
- Produces: Sida som matchar IMG_0933, 0934, 0944 (filter + rec)

- [ ] **Steg 1:** Sökfält överst + "Filter"-knapp bredvid → öppnar bottomsheet med muskelgrupp-checkboxes + author-toggle (RP Strength/Custom) + equipment-multi-select.

- [ ] **Steg 2:** "Get a recommendation"-knapp i botten → `IExerciseRecommendationService.RecommendForMuscleAsync` returnerar top 3, bottomsheet visar dem, användaren tap:ar en och den ersätter valet.

- [ ] **Steg 3:** Filter-badge visar antal aktiva filter ("Chest +9 Exercise Types").

- [ ] **Steg 4:** Bygg + test. Commit.

---

## Task 23: ActiveWorkoutPage — RP-header + muscle group pills

**Files:**
- Modify: `LockIn/Views/ActiveWorkoutPage.xaml`
- Modify: `LockIn/ViewModels/ActiveWorkoutViewModel.cs`
- Create: `LockIn/Controls/MusclePriorityPill.cs` (SkiaSharp eller ren XAML)

**Interfaces:**
- Consumes: `IMesocycleService.GetCurrentWeekAsync`, `ICycleContextService`
- Produces: Header med "WEEK N DAY M · Monday · Template Name" + muscle group pills med prio-nivå

- [ ] **Steg 1:** VM: `WeekNumber`, `DayNumber`, `DayLabel`, `TemplateName`. Beräkna från aktiva mesocycle + session's `TemplateDayId`.

- [ ] **Steg 2:** XAML-header: stort "WEEK 1 DAY 1", subtext "Monday · Push A". Kalender-ikon till höger (öppnar `MesocycleStatsPage`) + kebab-meny.

- [ ] **Steg 3:** `MusclePriorityPill` custom control: visar 3 vertikala staplar. Emphasize = 3 fyllda, Grow = 2, Maintain = 1. Färgat efter muskelgrupp.

- [ ] **Steg 4:** Muskelgrupp-sektionsheader (ovan varje övningsblock): `[Pill] CHEST` i versaler. `MuscleGroup` cachas på `WorkoutExerciseSection`.

- [ ] **Steg 5:** Bygg + test. Commit.

---

## Task 24: ActiveWorkoutPage — RIR som placeholder + ny set-rad

**Files:**
- Modify: `LockIn/Views/ActiveWorkoutPage.xaml`
- Modify: `LockIn/ViewModels/LoggedSetRow.cs`
- Modify: `LockIn/ViewModels/WorkoutExerciseSection.cs`

**Interfaces:**
- Produces: Set-rader matchar IMG_0947/0948 — Weight / Reps (med "3 RIR" placeholder) / Log-checkbox

- [ ] **Steg 1:** Ta bort separat RIR-Entry från XAML-set-raden. Reps-Entry får placeholder-text som binds till `TargetRirPlaceholder` (t.ex. `"3 RIR"`).

- [ ] **Steg 2:** `LoggedSetRow.TargetRirPlaceholder` beräknas från `TargetRir`-property (satt av `CycleContextService`).

- [ ] **Steg 3:** Efter första logg för övning: liten hint under raden "We recommend 3 RIR" (bind `RecommendedRirHint`).

- [ ] **Steg 4:** Log-checkbox till höger — tap:ar för att markera setet som loggat. Om weight+reps är tomma → toast "Ange vikt och reps".

- [ ] **Steg 5:** Blå tap:bar info-banner "How to choose your starting weight" ovanför set-tabellen — visas ENDAST i vecka 1 och om `AppSettings.HasSeenStartWeightHint = false`. Tap → bottomsheet med RP-liknande vägledning + "Ok, got it" som sätter flaggan.

- [ ] **Steg 6:** Bygg + test. Commit.

---

## Task 25: ActiveWorkoutPage — set-meny (Add note / Move / Replace / Skip / Remove)

**Files:**
- Modify: `LockIn/Views/ActiveWorkoutPage.xaml`
- Modify: `LockIn/ViewModels/ActiveWorkoutViewModel.cs`

**Interfaces:**
- Consumes: `ITemplateService`, `IExerciseRecommendationService`
- Produces: Kebab-meny (⋯) per övning matchar IMG_0952

- [ ] **Steg 1:** Kebab-knapp i övningens header → `DisplayActionSheetAsync` med val: New note, Move up, Move down, Replace, Joint pain, Add set, Skip sets, Remove exercise.

- [ ] **Steg 2:** `Add note` → `DisplayPromptAsync`, sparas på `SessionExercise.Notes`.

- [ ] **Steg 3:** `Move up/down` → ändrar `OrderIndex`, uppdaterar `ObservableCollection`. Endast för denna session.

- [ ] **Steg 4:** `Replace` → öppnar `ExercisePickerPage` med query-parameter `ReplaceMuscle`. Val ersätter `SessionExercise.ExerciseId`. Loggat data bevaras.

- [ ] **Steg 5:** `Joint pain` → öppnar `JointPainSheet` direkt (bottomsheet från IMG_0949).

- [ ] **Steg 6:** `Add set` → lägger till en tom `LoggedSetRow` (behåll befintlig kod).

- [ ] **Steg 7:** `Skip sets` → markerar återstående sets som skipp:ade, hoppar till nästa övning.

- [ ] **Steg 8:** `Remove exercise` → Confirm-dialog matchar IMG_0953/0954 med "Repeat"-toggle. Om Repeat ON → ta bort från alla framtida veckors motsvarande TemplateExercise.

- [ ] **Steg 9:** Bygg + test. Commit.

---

## Task 26: Feedback-flöden — JointPainSheet + MuscleFeedbackSheet

**Files:**
- Create: `LockIn/Views/JointPainSheet.xaml` + `.xaml.cs`
- Create: `LockIn/ViewModels/JointPainSheetViewModel.cs`
- Create: `LockIn/Views/MuscleFeedbackSheet.xaml` + `.xaml.cs`
- Create: `LockIn/ViewModels/MuscleFeedbackSheetViewModel.cs`
- Modify: `LockIn/ViewModels/ActiveWorkoutViewModel.cs` — trigger:a feedback vid rätt tillfällen

**Interfaces:**
- Consumes: `SessionExerciseFeedback`, `SessionMuscleFeedback`
- Produces: Två bottomsheets matchar IMG_0949 (joint pain) och IMG_0950 (chest pump + volume)

- [ ] **Steg 1:** `JointPainSheet` — 4 knappar (None / Low pain / Moderate pain / A lot of pain) med RP-röda rand. Frågar "How did your joints feel during {ExerciseName}?".

- [ ] **Steg 2:** `MuscleFeedbackSheet` — kombinerar Joint pain (om saknas) + Pump (3 val) + Volume (4 val). Layout matchar IMG_0950. `Save` sparar till `SessionMuscleFeedback` + eventuell `SessionExerciseFeedback`.

- [ ] **Steg 3:** Triggning i ActiveWorkoutViewModel: när sista `LoggedSetRow` för en muskelgrupp markeras loggad → `MuscleFeedbackSheet` visas. Skip-knapp finns (soft feedback).

- [ ] **Steg 4:** `SessionMuscleFeedback` sparas efter `Save`; `SetProgressionEngine` läser det för framtida rekommendationer.

- [ ] **Steg 5:** i18n för alla nya strängar (Feedback_JointPain_*, Feedback_Pump_*, Feedback_Volume_*).

- [ ] **Steg 6:** Bygg + test. Commit.

---

## Task 27: PostWorkoutPage — "Workout complete!"

**Files:**
- Modify: `LockIn/Views/PostWorkoutPage.xaml`
- Modify: `LockIn/ViewModels/PostWorkoutViewModel.cs`

**Interfaces:**
- Produces: PostWorkout-vy matchar IMG_0955

- [ ] **Steg 1:** Ta bort de tre feedback-raderna (pump/soreness/perf) som lades i Fas 1 Task 3 — feedback samlas nu per övning/muskel under passet.

- [ ] **Steg 2:** Ny layout: stor rubrik "Workout complete!". Två rader "Completed: N", "Skipped: N". Blå card "Advancing through your meso" med kort text från RP-mall om vila.

- [ ] **Steg 3:** "View mesocycle stats"-länk → `MesocycleStatsPage`. Röd primär "Next workout"-knapp → tillbaka till TrainPage/HemPage.

- [ ] **Steg 4:** Notes-Entry finns kvar (frivillig).

- [ ] **Steg 5:** Bygg + test. Commit.

---

## Task 28: MesocycleStatsPage — full stats-sida

**Files:**
- Create: `LockIn/Views/MesocycleStatsPage.xaml` + `.xaml.cs`
- Create: `LockIn/ViewModels/MesocycleStatsViewModel.cs`
- Create: `LockIn/Controls/MesocycleVolumeChart.cs` (SkiaSharp)

**Interfaces:**
- Consumes: `IMesocycleService`, `IWeeklyVolumeService`, `IVolumeProgressionService`
- Produces: Detaljerad vy per aktiv/vald mesocycle

- [ ] **Steg 1:** Header: Mesocycle-namn, veckoprogress ("Vecka 3 av 5"), status-badge (Aktiv/Klar).

- [ ] **Steg 2:** Sektion 1: veckoöversikt — horisontell rad med veckor 1..N, aktiv vecka markerad. Tapp på vecka visar dagens sessioner + logged sets.

- [ ] **Steg 3:** Sektion 2: `MesocycleVolumeChart` — stapel per muskelgrupp per vecka, MEV/MRV som horisontella linjer. Färgkodat efter zon.

- [ ] **Steg 4:** Sektion 3: RIR-progression — linjediagram RIR-mål per vecka.

- [ ] **Steg 5:** Sektion 4: Joint pain-tendens — heatmap per övning över veckor.

- [ ] **Steg 6:** Sektion 5: "Deload-status" — visar om deload är aktiv, kommande, eller passerad.

- [ ] **Steg 7:** Bygg + test. Commit.

---

## Task 29: HemPage/KroppPage/SettingsPage-anpassningar

**Files:**
- Modify: `LockIn/ViewModels/HemViewModel.cs`
- Modify: `LockIn/ViewModels/KroppViewModel.cs`
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`
- Modify: motsvarande XAML-filer

**Interfaces:**
- Produces: alla huvudsidor pratar med nya services och modeller

- [ ] **Steg 1:** HemViewModel: byt gamla `CycleContext`-integration mot nya via uppdaterade `ICycleContextService`.

- [ ] **Steg 2:** HemPage-chip "Nytt: Meso Builder" behåller sitt beteende men länkar till nya `MesoBuilderPage`.

- [ ] **Steg 3:** KroppPage — verifiera att `WeeklyVolumePage`-länken fungerar och `WeeklyVolumeReport` visar `PlannedSets` (Task 13).

- [ ] **Steg 4:** SettingsPage: ny rad "Utrustning" som öppnar en `EquipmentSettingsPage` — checkbox-lista över de 9 equipment-typerna, sparar `PreferredEquipmentJson`. "Volyminställningar" (befintlig från Fas 2) behålls.

- [ ] **Steg 5:** Bygg + test alla huvudsidor. Commit.

---

## Task 30: Slutlig städning, i18n-audit + release

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx` + `AppResources.cs` — sista i18n-kollen
- Modify: `LockIn/LockIn.csproj` — version bump
- Modify: `PRODUCT.md` — uppdatera med RP-full-klon

**Interfaces:**
- Produces: Version bumpad, alla strängar översatta, alla nya sidor täckta

- [ ] **Steg 1:** Grep hela `LockIn/`-mappen efter kvarvarande engelska strängar i XAML — skulle finnas i nya sidor. Flytta till `.resx`.

- [ ] **Steg 2:** Alla nya i18n-nycklar ska finnas i BÅDE `AppResources.resx` och `AppResources.cs`-wrapper.

- [ ] **Steg 3:** Öka `ApplicationVersion` med +1 i `LockIn.csproj`.

- [ ] **Steg 4:** Uppdatera `PRODUCT.md` — kilen-beskrivningen och "Vad appen gör" reflekterar RP-klonen.

- [ ] **Steg 5:** Full build, verifiera 0 errors.

- [ ] **Steg 6:** Manuellt test-checklista:
  1. Ny installation → onboarding med equipment-val → HemPage
  2. Bibliotek → tapp "Nytt template" → Planning Board → skapa custom template → Auto-fyll → spara
  3. Cykler → tapp "MESO BUILDER" → frekvens 4 → prio (Chest=Emphasize, Back=Grow, Quads=Grow, resten=Maintain) → namn "Test 1" → 5 veckor → skapa
  4. Bekräfta att Mesocycle syns som Aktiv på Cykler-sidan
  5. Starta pass från TrainPage → verifiera header "WEEK 1 DAY 1", muscle group pills syns
  6. Logga första setet i första övningen → "How to choose starting weight"-banner ska visas → tap "Got it"
  7. Reps-fältet visar "3 RIR" som placeholder → skriv 8 → tapp Log
  8. Efter sista setet i CHEST → MuscleFeedbackSheet öppnas → välj Moderate pump, Just right → Save
  9. Öppna kebab-meny på en övning → verifiera alla 8 alternativ syns
  10. Test "Replace exercise" → välj ny övning från picker → verifiera att den byts
  11. Avsluta pass → "Workout complete!" visas → tap "View mesocycle stats" → verifiera diagram fungerar
  12. KroppPage → Volym → verifiera att `PlannedSets` visas per muskel
  13. Cykler → tapp "VÄLJ TEMPLATE" → välj PPL 6-day → konfigurera → skapa

- [ ] **Steg 7:** Commit + push (kräver explicit user-instruktion enligt CLAUDE.md).

---

# Verifiering (end-to-end)

Ingen testsvit finns. Verifiering sker via:

1. `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` — 0 errors efter varje task
2. Manuell simulator-test enligt Task 30 Steg 6
3. TestFlight-verifikation efter push (kräver Mac + Xcode)

Efter Task 15 (Etapp 1 klar) förväntas UI-lager ha kompilfel — det är accepterat. Efter Task 30 ska hela appen kompilera och fungera.

---

# Prioriteringsordning & release-strategi

Etapp 1 (Task 1-15) är en enda logisk enhet — den ska mergas i sin helhet innan Etapp 2 påbörjas. Alternativt hela planen som en enda merge om vi vill undvika mellansteget där UI:t inte kompilerar.

**Rekommendation:** Bygg allt i en feature-branch (`feature/rp-full-clone`) i den ordning task-listan anger. Committa efter varje task. Merge till master efter Task 30.

---

# Ej i scope (framtida arbete)

- **Start from scratch** — helt tom template-flow (RP:s fjärde meso-alternativ)
- **Copy a mesocycle** — repetera tidigare cykel som ny
- **Delade templates / import** — dela template-JSON mellan användare
- **Video-instruktioner** — RP:s video-ikoner (▶) på varje övning
- **Övnings-historik direkt i pass** — RP:s history-ikon (⏱) per övning
- **Notifikationer per pass** — pre-workout påminnelse baserat på schemalagd dag
- **iPad/landscape-optimering** — nuvarande layout är portrait/iPhone

---

# Self-review

**Spec coverage:**
- ✓ Template-arkitektur helt om-arkitekturerad (Task 2, 6, 17, 18)
- ✓ Muskelprioritet per mesocycle (Task 3, 10, 20)
- ✓ Feedback ersatt helt med RP-modellen (Task 4, 26, 27)
- ✓ Reset gammal data (Task 5)
- ✓ Meso Builder + Template Picker (Task 10, 20, 21)
- ✓ Set-meny återspeglar RP exakt (Task 25)
- ✓ Soft feedback (Task 26)
- ✓ Både veckodag och asynkron day-struktur (Task 2 — DefaultDayOfWeek nullable)
- ✓ RP-exakt volym-progression (Task 11)
- ✓ RIR som placeholder (Task 24)
- ✓ Startvikt-hint som onboarding-card (Task 24)
- ✓ Full mesocycle stats-sida (Task 28)
- ✓ Full auto-picker exercise recommendation (Task 9)
- ✓ Equipment-preferens i onboarding (Task 16)
- ✓ 4 seedade templates (Task 14)
- ✓ Etapp 1 data-model först (Task 1-15)

**Placeholders scannat:** inga TBD, TODO, "similar to X" — allt konkret.

**Type consistency:** MuscleGroup-enum är utökad genomgående; MesocyclePriority.PriorityLevel är enum med `Emphasize/Grow/Maintain` konsekvent; JointPain/Pump/Volume-enums används i alla ViewModels och services.

**Scope check:** 30 tasks fördelade i 2 etapper. Etapp 1 är fristående (bygger back-end); Etapp 2 bygger UI. Realistiskt att bygga i denna ordning.
