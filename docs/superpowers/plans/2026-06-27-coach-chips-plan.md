# Coach Chips Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ersätt de statiska CoachPrompts med upp till 2 kontextuella data-chips på HemPage som visar faktapåståenden (PR-proximity, muskelgap, volymtrend, veckosammanfattning, veckostreak) och expanderar i en bottom sheet vid tryck.

**Architecture:** En statisk `CoachPromptEngine` utvärderar en `CoachContext` och returnerar filtrerade `CoachChip`-records (cooldown via Preferences). HemViewModel laddar dem asynkront i `LoadAsync`. HemPage visar en horisontell `CollectionView` och öppnar en `CommunityToolkit.Maui.Popup` (CoachChipSheet) vid tap.

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Maui 14.2.0 (Popup), CommunityToolkit.Mvvm 8.4.2, sqlite-net-pcl, Microsoft.Maui.Essentials (Preferences).

## Global Constraints

- Appen är på svenska; alla UI-strängar går via `AppResources` — aldrig hårdkodade strängar i XAML eller C#
- Typsnitt: `BebasNeue` för headers/siffror, `DMSansMedium` för knappar/chips, `DMSansRegular` för brödtext
- Designtokens: hex-värden aldrig hårdkodade — använd `StaticResource`-nycklar från `Colors.xaml` (`ForgeSurface`, `ForgeSurface2`, `LightSurface2`, `ForgeText`, `ForgeBorder`, `ForgeBorderLight`, `ForgeAccent`)
- Ingen `.xaml.cs`-fil skrivs utan att klass i XAML matchar `x:Class`
- Bygg med: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` — verifiera efter varje task
- Commit efter varje task

---

## Filöversikt

| Fil | Åtgärd | Ansvar |
|-----|--------|--------|
| `LockIn/Models/CoachChip.cs` | Skapa | Record med PromptId, ChipText, DetailHeader, DetailBody |
| `LockIn/Models/CoachContext.cs` | Skapa | Record med all input-data till engine |
| `LockIn/Services/DatabaseService.cs` | Ändra | +GetRawVolumeForRangeAsync, +GetNearestPRGapAsync, +PRGapRow |
| `LockIn/Resources/Strings/AppResources.resx` | Ändra | 12 svenska chip-nycklar |
| `LockIn/Resources/Strings/AppResources.en.resx` | Ändra | 12 engelska chip-nycklar |
| `LockIn/Resources/Strings/AppResources.cs` | Ändra | 12 wrapper-properties |
| `LockIn/Services/CoachPromptEngine.cs` | Skapa | Statisk engine, 5 chips, cooldown via Preferences |
| `LockIn/Views/CoachChipSheet.xaml` | Skapa | CommunityToolkit.Maui Popup, visar DetailHeader + DetailBody |
| `LockIn/Views/CoachChipSheet.xaml.cs` | Skapa | Code-behind, constructor tar CoachChip |
| `LockIn/ViewModels/HemViewModel.cs` | Ändra | Byt CoachPrompts → CoachChips + HasCoachChips + LoadCoachChipsAsync |
| `LockIn/Views/HemPage.xaml` | Ändra | +horisontell chip-CollectionView efter IDAG-REK-kortet |
| `LockIn/Views/HemPage.xaml.cs` | Ändra | +OnChipTapped |

---

## Task 1 — Data Models

**Files:**
- Create: `LockIn/Models/CoachChip.cs`
- Create: `LockIn/Models/CoachContext.cs`

**Interfaces:**
- Produces: `CoachChip(string PromptId, string ChipText, string DetailHeader, string DetailBody)` — används av CoachPromptEngine och CoachChipSheet
- Produces: `CoachContext(...)` — används av CoachPromptEngine

- [ ] **Steg 1: Skapa CoachChip.cs**

```csharp
// LockIn/Models/CoachChip.cs
namespace LockIn.Models;

public record CoachChip(
    string PromptId,
    string ChipText,
    string DetailHeader,
    string DetailBody
);
```

- [ ] **Steg 2: Skapa CoachContext.cs**

```csharp
// LockIn/Models/CoachContext.cs
namespace LockIn.Models;

public record CoachContext(
    IReadOnlyList<WorkoutSession> RecentSessions,
    IReadOnlyList<WorkoutSession> WeekSessions,
    IReadOnlyList<WorkoutSession> PrevWeekSessions,
    IReadOnlyDictionary<MuscleGroup, double> MuscleScores,
    double RecoveryPct,
    double? NearestPRGapKg,
    string? NearestPRExerciseName,
    double NearestPRRecentMaxKg,
    double NearestPRAllTimeMaxKg,
    int DaysSinceLastWorkout,
    double ThisWeekVolumeKg,
    double PrevWeekVolumeKg,
    int WeekStreak
);
```

- [ ] **Steg 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Steg 4: Commit**

```bash
git add LockIn/Models/CoachChip.cs LockIn/Models/CoachContext.cs
git commit -m "feat(coach-chips): add CoachChip and CoachContext records"
```

---

## Task 2 — DatabaseService: två nya query-metoder

**Files:**
- Modify: `LockIn/Services/DatabaseService.cs`

**Interfaces:**
- Produces: `GetRawVolumeForRangeAsync(DateTime from, DateTime toExclusive) → Task<double>`
- Produces: `GetNearestPRGapAsync(DateTime recentCutoff) → Task<(string ExerciseName, double GapKg, double RecentMaxKg, double AllTimeMaxKg)?>`

- [ ] **Steg 1: Lägg till PRGapRow privat klass**

Hitta avsnittet `// ── Muscle scores ──` i `DatabaseService.cs` (runt rad 793). Lägg till `PRGapRow` i det privata "row-klasser"-blocket som finns längst ner i filen (runt rad 904 vid `BestSetRow`):

```csharp
private class PRGapRow
{
    public string ExerciseName { get; set; } = "";
    public double AllTimeMaxKg { get; set; }
    public double RecentMaxKg { get; set; }
    public double GapKg { get; set; }
}
```

- [ ] **Steg 2: Lägg till GetRawVolumeForRangeAsync**

Lägg till direkt efter `GetVolumeIntensityForDateAsync` (rad ~572):

```csharp
public async Task<double> GetRawVolumeForRangeAsync(DateTime from, DateTime toExclusive)
{
    await InitAsync();
    return await _db.ExecuteScalarAsync<double>(
        @"SELECT COALESCE(SUM(ls.WeightKg * ls.Reps), 0)
          FROM LoggedSets ls
          JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
          JOIN WorkoutSessions ws ON ws.Id = se.SessionId
          WHERE ws.CompletedAt IS NOT NULL
            AND ws.StartedAt >= ? AND ws.StartedAt < ?
            AND (ls.SetType = 0 OR ls.SetType IS NULL)", from, toExclusive);
}
```

- [ ] **Steg 3: Lägg till GetNearestPRGapAsync**

Lägg till direkt efter `GetRawVolumeForRangeAsync`:

```csharp
public async Task<(string ExerciseName, double GapKg, double RecentMaxKg, double AllTimeMaxKg)?> GetNearestPRGapAsync(DateTime recentCutoff)
{
    await InitAsync();
    var rows = await _db.QueryAsync<PRGapRow>(@"
        SELECT e.Name AS ExerciseName,
               MAX(ls.WeightKg) AS AllTimeMaxKg,
               MAX(CASE WHEN ws.CompletedAt >= ? THEN CAST(ls.WeightKg AS REAL) END) AS RecentMaxKg,
               (MAX(ls.WeightKg) - MAX(CASE WHEN ws.CompletedAt >= ? THEN CAST(ls.WeightKg AS REAL) END)) AS GapKg
        FROM LoggedSets ls
        JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
        JOIN WorkoutSessions ws ON ws.Id = se.SessionId
        JOIN Exercises e ON e.Id = se.ExerciseId
        WHERE ws.CompletedAt IS NOT NULL
          AND (ls.SetType = 0 OR ls.SetType IS NULL)
        GROUP BY e.Id, e.Name
        HAVING RecentMaxKg IS NOT NULL
           AND GapKg > 0
           AND GapKg <= 10
        ORDER BY GapKg ASC
        LIMIT 1",
        recentCutoff, recentCutoff);

    var row = rows.FirstOrDefault();
    if (row is null) return null;
    return (row.ExerciseName, row.GapKg, row.RecentMaxKg, row.AllTimeMaxKg);
}
```

- [ ] **Steg 4: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Steg 5: Commit**

```bash
git add LockIn/Services/DatabaseService.cs
git commit -m "feat(coach-chips): add GetRawVolumeForRangeAsync and GetNearestPRGapAsync to DatabaseService"
```

---

## Task 3 — i18n: lägg till chip-strängar

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Produces: 12 `AppResources.*`-properties — används av CoachPromptEngine i Task 4

- [ ] **Steg 1: Lägg till 12 svenska nycklar i AppResources.resx**

Öppna `LockIn/Resources/Strings/AppResources.resx`. Hitta sektionen `<!-- Hem — Coach prompts -->`. Lägg till efter de befintliga 3 nycklarna (Hem_Coach_WeeklySummary, Hem_Coach_RecoveryTips, Hem_Coach_NextSession):

```xml
  <!-- Hem — Coach chips (data-chips, ersätter frågeformat) -->
  <data name="Hem_Chip_PRProximity_Text" xml:space="preserve">
    <value>{0}: {1:F1} kg från PR</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Text" xml:space="preserve">
    <value>{0}: länge sedan</value>
  </data>
  <data name="Hem_Chip_VolumeTrendUp_Text" xml:space="preserve">
    <value>Volym: +{0}% mot förra</value>
  </data>
  <data name="Hem_Chip_VolumeTrendDown_Text" xml:space="preserve">
    <value>Volym: −{0}% mot förra</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Text" xml:space="preserve">
    <value>Veckan: {0} pass, {1:N0} kg</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Text" xml:space="preserve">
    <value>{0} veckor i rad</value>
  </data>
  <data name="Hem_Chip_PRProximity_Header" xml:space="preserve">
    <value>{0} — nära PR</value>
  </data>
  <data name="Hem_Chip_PRProximity_Body" xml:space="preserve">
    <value>{0}: {1:F1} kg kvar till nytt PR.&#xA;Nuvarande max: {2:F1} kg&#xA;PR: {3:F1} kg</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Header" xml:space="preserve">
    <value>{0} — länge sedan</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Body" xml:space="preserve">
    <value>Inga set för {0} den senaste veckan.</value>
  </data>
  <data name="Hem_Chip_VolumeTrend_Header" xml:space="preserve">
    <value>Volymtrend</value>
  </data>
  <data name="Hem_Chip_VolumeTrend_Body" xml:space="preserve">
    <value>Den här veckan: {0:N0} kg&#xA;Förra veckan: {1:N0} kg</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Header" xml:space="preserve">
    <value>Veckans träning</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Body" xml:space="preserve">
    <value>{0} pass · {1:N0} kg total volym</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Header" xml:space="preserve">
    <value>Veckostreak</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Body" xml:space="preserve">
    <value>{0} veckor i rad med minst ett pass.</value>
  </data>
```

- [ ] **Steg 2: Lägg till 12 engelska nycklar i AppResources.en.resx**

Öppna `LockIn/Resources/Strings/AppResources.en.resx`. Hitta `<!-- Hem — Coach prompts -->`. Lägg till efter befintliga:

```xml
  <!-- Hem — Coach chips -->
  <data name="Hem_Chip_PRProximity_Text" xml:space="preserve">
    <value>{0}: {1:F1} kg from PR</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Text" xml:space="preserve">
    <value>{0}: not trained lately</value>
  </data>
  <data name="Hem_Chip_VolumeTrendUp_Text" xml:space="preserve">
    <value>Volume: +{0}% vs last week</value>
  </data>
  <data name="Hem_Chip_VolumeTrendDown_Text" xml:space="preserve">
    <value>Volume: −{0}% vs last week</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Text" xml:space="preserve">
    <value>Week: {0} sessions, {1:N0} kg</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Text" xml:space="preserve">
    <value>{0} weeks in a row</value>
  </data>
  <data name="Hem_Chip_PRProximity_Header" xml:space="preserve">
    <value>{0} — close to PR</value>
  </data>
  <data name="Hem_Chip_PRProximity_Body" xml:space="preserve">
    <value>{0}: {1:F1} kg away from a new PR.&#xA;Current max: {2:F1} kg&#xA;PR: {3:F1} kg</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Header" xml:space="preserve">
    <value>{0} — not trained lately</value>
  </data>
  <data name="Hem_Chip_MuscleGap_Body" xml:space="preserve">
    <value>No sets for {0} in the past week.</value>
  </data>
  <data name="Hem_Chip_VolumeTrend_Header" xml:space="preserve">
    <value>Volume trend</value>
  </data>
  <data name="Hem_Chip_VolumeTrend_Body" xml:space="preserve">
    <value>This week: {0:N0} kg&#xA;Last week: {1:N0} kg</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Header" xml:space="preserve">
    <value>This week's training</value>
  </data>
  <data name="Hem_Chip_WeekSummary_Body" xml:space="preserve">
    <value>{0} sessions · {1:N0} kg total volume</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Header" xml:space="preserve">
    <value>Weekly streak</value>
  </data>
  <data name="Hem_Chip_StreakWeeks_Body" xml:space="preserve">
    <value>{0} consecutive weeks with at least one session.</value>
  </data>
```

- [ ] **Steg 3: Lägg till 16 wrapper-properties i AppResources.cs**

Hitta sektionen `// Hem — Coach prompts` i `AppResources.cs`. Lägg till direkt efter befintliga 3 coach-properties:

```csharp
    // Hem — Coach chips
    public static string Hem_Chip_PRProximity_Text    => Get(nameof(Hem_Chip_PRProximity_Text));
    public static string Hem_Chip_MuscleGap_Text      => Get(nameof(Hem_Chip_MuscleGap_Text));
    public static string Hem_Chip_VolumeTrendUp_Text  => Get(nameof(Hem_Chip_VolumeTrendUp_Text));
    public static string Hem_Chip_VolumeTrendDown_Text => Get(nameof(Hem_Chip_VolumeTrendDown_Text));
    public static string Hem_Chip_WeekSummary_Text    => Get(nameof(Hem_Chip_WeekSummary_Text));
    public static string Hem_Chip_StreakWeeks_Text     => Get(nameof(Hem_Chip_StreakWeeks_Text));
    public static string Hem_Chip_PRProximity_Header  => Get(nameof(Hem_Chip_PRProximity_Header));
    public static string Hem_Chip_PRProximity_Body    => Get(nameof(Hem_Chip_PRProximity_Body));
    public static string Hem_Chip_MuscleGap_Header    => Get(nameof(Hem_Chip_MuscleGap_Header));
    public static string Hem_Chip_MuscleGap_Body      => Get(nameof(Hem_Chip_MuscleGap_Body));
    public static string Hem_Chip_VolumeTrend_Header  => Get(nameof(Hem_Chip_VolumeTrend_Header));
    public static string Hem_Chip_VolumeTrend_Body    => Get(nameof(Hem_Chip_VolumeTrend_Body));
    public static string Hem_Chip_WeekSummary_Header  => Get(nameof(Hem_Chip_WeekSummary_Header));
    public static string Hem_Chip_WeekSummary_Body    => Get(nameof(Hem_Chip_WeekSummary_Body));
    public static string Hem_Chip_StreakWeeks_Header   => Get(nameof(Hem_Chip_StreakWeeks_Header));
    public static string Hem_Chip_StreakWeeks_Body     => Get(nameof(Hem_Chip_StreakWeeks_Body));
```

- [ ] **Steg 4: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Steg 5: Commit**

```bash
git add LockIn/Resources/Strings/AppResources.resx LockIn/Resources/Strings/AppResources.en.resx LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(coach-chips): add i18n strings for all 5 chip types (sv + en)"
```

---

## Task 4 — CoachPromptEngine

**Files:**
- Create: `LockIn/Services/CoachPromptEngine.cs`

**Interfaces:**
- Consumes: `CoachChip` (Task 1), `CoachContext` (Task 1), `AppResources.*` (Task 3)
- Produces: `CoachPromptEngine.Evaluate(CoachContext ctx) → IReadOnlyList<CoachChip>`

- [ ] **Steg 1: Skapa CoachPromptEngine.cs**

```csharp
// LockIn/Services/CoachPromptEngine.cs
using LockIn.Models;
using LockIn.Resources.Strings;
using Microsoft.Maui.Storage;

namespace LockIn.Services;

public static class CoachPromptEngine
{
    private const int MaxChips = 2;

    // Prioritetsordning för muskelgap-chip: tyngre grupper visas hellre
    private static readonly MuscleGroup[] s_muscleGapPriority =
    [
        MuscleGroup.Legs, MuscleGroup.Back, MuscleGroup.Chest,
        MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps,
        MuscleGroup.Core, MuscleGroup.Forearms
    ];

    public static IReadOnlyList<CoachChip> Evaluate(CoachContext ctx)
    {
        var candidates = new List<(int Priority, CoachChip Chip, TimeSpan Cooldown)>();

        // ── Chip 1: PR-proximity ─────────────────────────────────────────
        if (ctx.NearestPRGapKg is > 0 and <= 10 && ctx.NearestPRExerciseName is not null)
        {
            var chip = new CoachChip(
                PromptId: "pr-proximity",
                ChipText: string.Format(AppResources.Hem_Chip_PRProximity_Text,
                    ctx.NearestPRExerciseName, ctx.NearestPRGapKg.Value),
                DetailHeader: string.Format(AppResources.Hem_Chip_PRProximity_Header,
                    ctx.NearestPRExerciseName),
                DetailBody: string.Format(AppResources.Hem_Chip_PRProximity_Body,
                    ctx.NearestPRExerciseName,
                    ctx.NearestPRGapKg.Value,
                    ctx.NearestPRRecentMaxKg,
                    ctx.NearestPRAllTimeMaxKg));
            candidates.Add((1, chip, TimeSpan.FromHours(24)));
        }

        // ── Chip 2: Muskelgap ────────────────────────────────────────────
        foreach (var mg in s_muscleGapPriority)
        {
            if (!ctx.MuscleScores.TryGetValue(mg, out var score) || score > 0.0)
                continue;
            var muscleName = MuscleDisplayName(mg);
            if (string.IsNullOrEmpty(muscleName)) continue;

            var chip = new CoachChip(
                PromptId: $"muscle-gap-{mg.ToString().ToLowerInvariant()}",
                ChipText: string.Format(AppResources.Hem_Chip_MuscleGap_Text, muscleName),
                DetailHeader: string.Format(AppResources.Hem_Chip_MuscleGap_Header, muscleName),
                DetailBody: string.Format(AppResources.Hem_Chip_MuscleGap_Body, muscleName));
            candidates.Add((2, chip, TimeSpan.FromHours(48)));
            break; // bara en muskelgap-chip
        }

        // ── Chip 3: Volymtrend ───────────────────────────────────────────
        if (ctx.PrevWeekVolumeKg > 0)
        {
            var delta = ctx.ThisWeekVolumeKg - ctx.PrevWeekVolumeKg;
            var pct = delta / ctx.PrevWeekVolumeKg;
            if (Math.Abs(pct) > 0.15)
            {
                var absPct = (int)Math.Round(Math.Abs(pct) * 100);
                var chipText = pct > 0
                    ? string.Format(AppResources.Hem_Chip_VolumeTrendUp_Text, absPct)
                    : string.Format(AppResources.Hem_Chip_VolumeTrendDown_Text, absPct);
                var chip = new CoachChip(
                    PromptId: "volume-trend",
                    ChipText: chipText,
                    DetailHeader: AppResources.Hem_Chip_VolumeTrend_Header,
                    DetailBody: string.Format(AppResources.Hem_Chip_VolumeTrend_Body,
                        ctx.ThisWeekVolumeKg, ctx.PrevWeekVolumeKg));
                candidates.Add((3, chip, TimeSpan.FromHours(48)));
            }
        }

        // ── Chip 4: Veckosammanfattning ──────────────────────────────────
        var today = DateTime.Today;
        var dayOfWeek = ((int)today.DayOfWeek + 6) % 7; // 0=Mån, 6=Sön
        if (dayOfWeek >= 3 && ctx.WeekSessions.Count >= 2) // tors–sön
        {
            var chip = new CoachChip(
                PromptId: "week-summary",
                ChipText: string.Format(AppResources.Hem_Chip_WeekSummary_Text,
                    ctx.WeekSessions.Count, ctx.ThisWeekVolumeKg),
                DetailHeader: AppResources.Hem_Chip_WeekSummary_Header,
                DetailBody: string.Format(AppResources.Hem_Chip_WeekSummary_Body,
                    ctx.WeekSessions.Count, ctx.ThisWeekVolumeKg));
            candidates.Add((4, chip, TimeSpan.FromHours(24)));
        }

        // ── Chip 5: Veckostreak ──────────────────────────────────────────
        if (ctx.WeekStreak >= 2)
        {
            var chip = new CoachChip(
                PromptId: "streak-weeks",
                ChipText: string.Format(AppResources.Hem_Chip_StreakWeeks_Text, ctx.WeekStreak),
                DetailHeader: AppResources.Hem_Chip_StreakWeeks_Header,
                DetailBody: string.Format(AppResources.Hem_Chip_StreakWeeks_Body, ctx.WeekStreak));
            candidates.Add((5, chip, TimeSpan.FromHours(72)));
        }

        // ── Filtrera på cooldown, sortera på prioritet, returnera max 2 ──
        var now = DateTime.UtcNow;
        return candidates
            .Where(c => !IsOnCooldown(c.Chip.PromptId, c.Cooldown, now))
            .OrderBy(c => c.Priority)
            .Take(MaxChips)
            .Select(c => c.Chip)
            .ToList();
    }

    public static void MarkShown(string promptId)
    {
        Preferences.Set($"coach_chip_{promptId}_shown_at", DateTime.UtcNow.ToString("O"));
    }

    private static bool IsOnCooldown(string promptId, TimeSpan cooldown, DateTime now)
    {
        var key = $"coach_chip_{promptId}_shown_at";
        if (!Preferences.ContainsKey(key)) return false;
        var raw = Preferences.Get(key, "");
        if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lastShown))
            return false;
        return now - lastShown < cooldown;
    }

    private static string MuscleDisplayName(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => AppResources.Train_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Train_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Train_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Train_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Train_Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Train_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Train_Muscle_Core,
        MuscleGroup.Forearms  => AppResources.Train_Muscle_Forearms,
        _                     => ""
    };
}
```

- [ ] **Steg 2: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Steg 3: Commit**

```bash
git add LockIn/Services/CoachPromptEngine.cs
git commit -m "feat(coach-chips): add CoachPromptEngine with 5 contextual chips and cooldown"
```

---

## Task 5 — CoachChipSheet Popup

**Files:**
- Create: `LockIn/Views/CoachChipSheet.xaml`
- Create: `LockIn/Views/CoachChipSheet.xaml.cs`

**Interfaces:**
- Consumes: `CoachChip` (Task 1) — constructor-parameter
- Produces: `CoachChipSheet(CoachChip chip)` — visas via `await page.ShowPopupAsync(new CoachChipSheet(chip))`

- [ ] **Steg 1: Skapa CoachChipSheet.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<toolkit:Popup
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
    x:Class="LockIn.Views.CoachChipSheet"
    CanBeDismissedByTappingOutsideOfPopup="True"
    Color="{AppThemeBinding Light={StaticResource LightBackground}, Dark={StaticResource ForgeSurface2}}">

    <Border StrokeShape="RoundRectangle 24,24,0,0"
            BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface}, Dark={StaticResource ForgeSurface2}}"
            StrokeThickness="0"
            Padding="24,20,24,40"
            MinimumWidthRequest="320">

        <VerticalStackLayout Spacing="12">

            <!-- Drag handle -->
            <BoxView WidthRequest="36" HeightRequest="4" CornerRadius="2"
                     HorizontalOptions="Center"
                     BackgroundColor="{AppThemeBinding Light={StaticResource LightBorder}, Dark={StaticResource ForgeBorder}}"/>

            <!-- Header row -->
            <Grid ColumnDefinitions="*,36" Margin="0,8,0,0">
                <Label Grid.Column="0"
                       x:Name="HeaderLabel"
                       FontFamily="BebasNeue" FontSize="28" CharacterSpacing="0.5"
                       LineHeight="1"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
                <Border Grid.Column="1"
                        WidthRequest="32" HeightRequest="32"
                        StrokeShape="Ellipse"
                        BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface2}, Dark={StaticResource ForgeSurface3}}"
                        StrokeThickness="0"
                        VerticalOptions="Center">
                    <Label Text="×" FontFamily="DMSansRegular" FontSize="18"
                           TextColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
                           HorizontalOptions="Center" VerticalOptions="Center">
                        <Label.GestureRecognizers>
                            <TapGestureRecognizer Tapped="OnCloseTapped"/>
                        </Label.GestureRecognizers>
                    </Label>
                </Border>
            </Grid>

            <!-- Body -->
            <Label x:Name="BodyLabel"
                   FontFamily="DMSansRegular" FontSize="14"
                   LineHeight="1.6"
                   TextColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
                   LineBreakMode="WordWrap"/>

        </VerticalStackLayout>
    </Border>
</toolkit:Popup>
```

- [ ] **Steg 2: Skapa CoachChipSheet.xaml.cs**

```csharp
// LockIn/Views/CoachChipSheet.xaml.cs
using CommunityToolkit.Maui.Views;
using LockIn.Models;

namespace LockIn.Views;

public partial class CoachChipSheet : Popup
{
    public CoachChipSheet(CoachChip chip)
    {
        InitializeComponent();
        HeaderLabel.Text = chip.DetailHeader;
        BodyLabel.Text   = chip.DetailBody;
    }

    private void OnCloseTapped(object sender, TappedEventArgs e) => Close();
}
```

- [ ] **Steg 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Steg 4: Commit**

```bash
git add LockIn/Views/CoachChipSheet.xaml LockIn/Views/CoachChipSheet.xaml.cs
git commit -m "feat(coach-chips): add CoachChipSheet popup"
```

---

## Task 6 — HemViewModel: integration

**Files:**
- Modify: `LockIn/ViewModels/HemViewModel.cs`

**Interfaces:**
- Consumes: `CoachChip` (Task 1), `CoachContext` (Task 1), `CoachPromptEngine.Evaluate` (Task 4)
- Consumes: `db.GetRawVolumeForRangeAsync` (Task 2), `db.GetNearestPRGapAsync` (Task 2), `db.GetCurrentWeekStreakAsync` (befintlig)
- Produces: `ObservableCollection<CoachChip> CoachChips`, `bool HasCoachChips` — binds i HemPage.xaml

- [ ] **Steg 1: Byt ut CoachPrompts-property**

Hitta rad 76–81 i `HemViewModel.cs`:
```csharp
public IReadOnlyList<string> CoachPrompts { get; } = new[]
{
    AppResources.Hem_Coach_WeeklySummary,
    AppResources.Hem_Coach_RecoveryTips,
    AppResources.Hem_Coach_NextSession
};
```

Ersätt med:
```csharp
[ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<CoachChip> _coachChips = new();
[ObservableProperty] private bool _hasCoachChips;
```

- [ ] **Steg 2: Lägg till LoadCoachChipsAsync**

Lägg till metoden direkt före `private static DateTime GetMondayThisWeek()` (rad ~302):

```csharp
private async Task LoadCoachChipsAsync(
    IReadOnlyList<WorkoutSession> weekSessions,
    IReadOnlyList<WorkoutSession> recentSessions,
    IReadOnlyDictionary<MuscleGroup, double> muscleScores,
    double recoveryPct)
{
    try
    {
        var weekStart    = GetMondayThisWeek();
        var prevWeekStart = weekStart.AddDays(-7);
        var prevWeekEnd   = weekStart.AddSeconds(-1);
        var tomorrow      = DateTime.Today.AddDays(1);
        var recentCutoff  = DateTime.Now.AddDays(-30);

        var prevWeekSessionsTask  = db.GetCompletedSessionsInRangeAsync(prevWeekStart, prevWeekEnd);
        var thisWeekVolumeTask    = db.GetRawVolumeForRangeAsync(weekStart, tomorrow);
        var prevWeekVolumeTask    = db.GetRawVolumeForRangeAsync(prevWeekStart, weekStart);
        var nearestPRTask         = db.GetNearestPRGapAsync(recentCutoff);
        var weekStreakTask         = db.GetCurrentWeekStreakAsync();

        await Task.WhenAll(prevWeekSessionsTask, thisWeekVolumeTask,
                           prevWeekVolumeTask, nearestPRTask, weekStreakTask);

        var prResult = nearestPRTask.Result;
        var lastSession = recentSessions
            .Where(s => s.CompletedAt.HasValue)
            .OrderByDescending(s => s.CompletedAt!.Value)
            .FirstOrDefault();
        int daysSinceLast = lastSession is not null
            ? (int)(DateTime.Now - lastSession.CompletedAt!.Value).TotalDays
            : 999;

        var ctx = new CoachContext(
            RecentSessions:          recentSessions,
            WeekSessions:            weekSessions,
            PrevWeekSessions:        prevWeekSessionsTask.Result,
            MuscleScores:            muscleScores,
            RecoveryPct:             recoveryPct,
            NearestPRGapKg:          prResult.HasValue ? prResult.Value.GapKg : null,
            NearestPRExerciseName:   prResult.HasValue ? prResult.Value.ExerciseName : null,
            NearestPRRecentMaxKg:    prResult.HasValue ? prResult.Value.RecentMaxKg : 0,
            NearestPRAllTimeMaxKg:   prResult.HasValue ? prResult.Value.AllTimeMaxKg : 0,
            DaysSinceLastWorkout:    daysSinceLast,
            ThisWeekVolumeKg:        thisWeekVolumeTask.Result,
            PrevWeekVolumeKg:        prevWeekVolumeTask.Result,
            WeekStreak:              weekStreakTask.Result
        );

        var chips = CoachPromptEngine.Evaluate(ctx);
        CoachChips.Clear();
        foreach (var chip in chips)
            CoachChips.Add(chip);
        HasCoachChips = CoachChips.Count > 0;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[CoachChips] LoadCoachChipsAsync failed: {ex.Message}");
        HasCoachChips = false;
    }
}
```

- [ ] **Steg 3: Anropa LoadCoachChipsAsync i slutet av LoadAsync**

Hitta raden `var (recHead, recDetail) = BuildRecommendation(recoveryPct, recentSessions);` (nära rad 292). Lägg till ett anrop till `LoadCoachChipsAsync` **efter** heatmapen är laddad men **inom try-blocket**, precis före `finally`:

```csharp
await LoadCoachChipsAsync(weekSessions, recentSessions, 
    HeatmapItems.ToDictionary(t => /* ... */ ),  // — se steg 4 nedan
    recoveryPct);
```

**OBS:** `LoadHeatmapAsync()` bygger `HeatmapItems` men exponerar inte `scores`-dictionaryn direkt. Enklast är att göra `GetMuscleScoresAsync` separat i `LoadCoachChipsAsync` (en extra query). Ta **bort** det kombinerade anropet ovan och lägg istället till `db.GetMuscleScoresAsync()` som en task inuti `LoadCoachChipsAsync`. Ändra signaturen:

```csharp
private async Task LoadCoachChipsAsync(
    IReadOnlyList<WorkoutSession> weekSessions,
    IReadOnlyList<WorkoutSession> recentSessions,
    double recoveryPct)
```

Och inuti metoden, lägg till `muscleScoresTask` till `Task.WhenAll`:

```csharp
var muscleScoresTask      = db.GetMuscleScoresAsync();
// ... (övriga tasks)
await Task.WhenAll(prevWeekSessionsTask, thisWeekVolumeTask,
                   prevWeekVolumeTask, nearestPRTask, weekStreakTask, muscleScoresTask);
// ...
MuscleScores: muscleScoresTask.Result,
```

Anropet i slutet av `LoadAsync` (precis före `finally`-blocket):

```csharp
await LoadCoachChipsAsync(weekSessions, recentSessions, recoveryPct);
```

- [ ] **Steg 4: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors. Fixa eventuella kompileringsfel (namnkonflikter, saknade using-direktiv).

- [ ] **Steg 5: Commit**

```bash
git add LockIn/ViewModels/HemViewModel.cs
git commit -m "feat(coach-chips): integrate CoachChips into HemViewModel with LoadCoachChipsAsync"
```

---

## Task 7 — HemPage: UI + tap-handler

**Files:**
- Modify: `LockIn/Views/HemPage.xaml`
- Modify: `LockIn/Views/HemPage.xaml.cs`

**Interfaces:**
- Consumes: `CoachChips` (ObservableCollection<CoachChip>), `HasCoachChips` (bool) — från HemViewModel (Task 6)
- Consumes: `CoachChipSheet(CoachChip)` (Task 5)

- [ ] **Steg 1: Lägg till xmlns för CoachChip i HemPage.xaml**

Hitta toppen av `HemPage.xaml` (rad 1–14). Lägg till `xmlns:models`:

```xml
xmlns:models="clr-namespace:LockIn.Models"
```

Lägg till på samma rad som de befintliga `xmlns`-deklarationerna (efter `xmlns:ios="..."`).

- [ ] **Steg 2: Lägg till chip-raden i HemPage.xaml**

Hitta slutet av IDAG-REKOMMENDATION-kortet (rad ~129, sista `</Border>` för det kortet). Lägg till chip-raden direkt **efter** det kortets avslutande `</Border>` och **före** `<!-- ═══ STRAIN / RECOVERY / SÖMN-RINGAR ═══ -->`:

```xml
<!-- ═══ DATA-CHIPS ═══ -->
<CollectionView ItemsSource="{Binding CoachChips}"
                IsVisible="{Binding HasCoachChips}"
                Margin="16,10,0,0"
                HeightRequest="40"
                HorizontalScrollBarVisibility="Never"
                VerticalScrollBarVisibility="Never">
    <CollectionView.ItemsLayout>
        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="8"/>
    </CollectionView.ItemsLayout>
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:CoachChip">
            <Border StrokeShape="RoundRectangle 10"
                    Padding="14,0"
                    HeightRequest="36"
                    VerticalOptions="Center"
                    BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface2}, Dark={StaticResource ForgeSurface2}}"
                    Stroke="{AppThemeBinding Light={StaticResource LightBorder}, Dark={StaticResource ForgeBorderLight}}"
                    StrokeThickness="1">
                <Label Text="{Binding ChipText}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"
                       VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnChipTapped"/>
                </Border.GestureRecognizers>
            </Border>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

- [ ] **Steg 3: Lägg till OnChipTapped i HemPage.xaml.cs**

Öppna `LockIn/Views/HemPage.xaml.cs`. Lägg till `using CommunityToolkit.Maui.Views;` högst upp bland using-direktiven. Lägg sedan till metoden:

```csharp
private async void OnChipTapped(object sender, TappedEventArgs e)
{
    if (sender is not VisualElement view) return;
    if (view.BindingContext is not LockIn.Models.CoachChip chip) return;

    LockIn.Services.CoachPromptEngine.MarkShown(chip.PromptId);
    await this.ShowPopupAsync(new CoachChipSheet(chip));
}
```

- [ ] **Steg 4: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors. Fixa eventuella kompileringsfel.

- [ ] **Steg 5: Commit**

```bash
git add LockIn/Views/HemPage.xaml LockIn/Views/HemPage.xaml.cs
git commit -m "feat(coach-chips): add chip CollectionView and OnChipTapped to HemPage"
```

---

## Spec Coverage Check

| Spec-krav | Task |
|-----------|------|
| CoachChip record (PromptId, ChipText, DetailHeader, DetailBody) | Task 1 |
| CoachContext record med alla fält | Task 1 |
| GetRawVolumeForRangeAsync | Task 2 |
| GetNearestPRGapAsync | Task 2 |
| 12 i18n-nycklar (sv + en) + AppResources.cs wrapper | Task 3 |
| Chip 1 PR-proximity (cooldown 24h, prioritet 1) | Task 4 |
| Chip 2 Muskelgap (score==0, prioritet 2) | Task 4 |
| Chip 3 Volymtrend (>15%, cooldown 48h) | Task 4 |
| Chip 4 Veckosammanfattning (tors+, ≥2 pass) | Task 4 |
| Chip 5 Veckostreak (≥2 veckor, cooldown 72h) | Task 4 |
| Max 2 chips returneras | Task 4 |
| MarkShown vid tap (inte render) | Task 4 + Task 7 |
| CoachChipSheet Popup | Task 5 |
| Stäng-knapp × | Task 5 |
| CanBeDismissedByTappingOutsideOfPopup | Task 5 |
| CoachChips + HasCoachChips i HemViewModel | Task 6 |
| LoadCoachChipsAsync anropas i LoadAsync | Task 6 |
| Fallback: ingen popup/placeholder vid 0 chips (HasCoachChips=false) | Task 6 |
| Horisontell chip CollectionView i HemPage | Task 7 |
| Placering efter IDAG-REK, före ringarna | Task 7 |
| OnChipTapped → MarkShown + ShowPopupAsync | Task 7 |
| Ingen sektionsrubrik "COACH" | Task 7 |
