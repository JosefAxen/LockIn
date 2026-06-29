# LockIn — i18n Plan 3: Statisk data, achievements, notiser, datumformat

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lokalisera all återstående statisk UI-text: achievement-titlar/-beskrivningar, programbeskrivningar/-dagetiketter, notifikationssträngar samt fixa hårdkodade datum-kulturkoder (sv-SE).

**Architecture:** Följer Plan 2-mönstret — nya nycklar i AppResources.resx + .en.resx + AppResources.cs, sedan ersätt hårdkodade strängar med AppResources-anrop. `WorkoutProgram` och `ProgramDay` (båda positional records i `WorkoutPrograms.cs`) utökas med computed properties för lokalisering. `HistoryViewModel` och `BodyWeightViewModel` får datum-kulturfix utan nya resx-nycklar.

**Tech Stack:** .NET MAUI 10 iOS, C#, .resx resursfiler, `AppResources.cs` statisk wrapper (`Get(string key)` returnerar nyckeln som fallback), `AppResources.Get(string)` används i C#-kod (inte `{loc:Localize}` i XAML — all ny lokalisering i denna plan sker i C#).

## Global Constraints

- Appen är på **svenska som default** (`AppResources.resx`) + engelska satellit (`AppResources.en.resx`)
- **Identisk key-set** i .resx och .en.resx — varje ny nyckel läggs i BÅDA filerna
- `AppResources.cs`: en property per nyckel: `public static string Key => Get(nameof(Key));`
- `AppResources.Get(string key)` returnerar nyckeln som fallback om resursen saknas — inga null-crashes
- Inga duplikat-nycklar — kontrollera alltid mot befintliga nycklar i AppResources.cs
- Nyckelprefix: `Achievement_` för achievements, `Program_` för program, `Notification_` för notiser
- **Övningsnamn (Bänkpress, Marklyft etc.) översätts INTE** — de är DB-lookup-nycklar och ska förbli svenska
- Bygg: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` → 0 errors, ≤106 warnings
- Varje task committas separat
- **Pusha aldrig** utan explicit instruktion från användaren

---

## Filstruktur

| Fil | Ändring |
|-----|---------|
| `LockIn/Services/AchievementService.cs` | 20 achievements: ersätt hårdkodade `Title` + `Description` med `AppResources.*` |
| `LockIn/Data/WorkoutPrograms.cs` | Lägg till `LabelKey init-property` på `ProgramDay`-record + `LocalizedLabel` computed; `LocalizedDescription` computed på `WorkoutProgram`; fyll i `LabelKey` för alla 21 days |
| `LockIn/Views/ProgramDetailPage.xaml` | Byt `{Binding Label}` → `{Binding LocalizedLabel}` |
| `LockIn/Views/LibraryPage.xaml` | Byt `{Binding Description}` → `{Binding LocalizedDescription}` |
| `LockIn/Services/NotificationService.cs` | Ersätt `Title` + `Body` med `AppResources.*` |
| `LockIn/ViewModels/HistoryViewModel.cs` | `new CultureInfo("sv-SE")` → `CultureInfo.CurrentUICulture` |
| `LockIn/ViewModels/BodyWeightViewModel.cs` | Lägg till `CultureInfo.CurrentUICulture` i `.ToString("d MMM yyyy")` och `.ToString("d MMM")` |
| `LockIn/Resources/Strings/AppResources.resx` | +40 Achievement_ + 27 Program_ + 2 Notification_ nycklar |
| `LockIn/Resources/Strings/AppResources.en.resx` | Identisk key-set, engelska värden |
| `LockIn/Resources/Strings/AppResources.cs` | +69 properties |
| `LockIn/LockIn.csproj` | `ApplicationVersion` 54 → 55 |

---

## Task 1: Achievement-titlar och -beskrivningar

**Files:**
- Modify: `LockIn/Services/AchievementService.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: Befintlig `AppResources.Get(string key)`, `using LockIn.Resources.Strings;` (redan i filen om Plan 2 berörde den, annars lägg till)
- Produces: 40 nycklar `Achievement_<EnumName>_Title` + `Achievement_<EnumName>_Description`. AchievementService.cs läser dem. `AchievementId`-enumvärden: `FirstWorkout`, `Sessions5`, `Sessions10`, `Sessions25`, `Sessions50`, `Sessions100`, `WeekStreak1`, `WeekStreak4`, `WeekStreak12`, `FirstPR`, `PR10`, `PR50`, `TotalVolume100k`, `TotalVolume500k`, `TotalVolume1M`, `AllMuscleGroups`, `LongSession`, `EarlyBird`, `NightOwl`, `FirstCustomExercise`.

- [ ] **Step 1: Lägg till 40 svenska achievement-nycklar i AppResources.resx**

Läs `LockIn/Resources/Strings/AppResources.resx`. Lägg till ett nytt block i slutet (före `</root>`):

```xml
  <!-- Achievements -->
  <data name="Achievement_FirstWorkout_Title" xml:space="preserve"><value>Första steget</value></data>
  <data name="Achievement_FirstWorkout_Description" xml:space="preserve"><value>Du klarade ditt allra första pass!</value></data>
  <data name="Achievement_Sessions5_Title" xml:space="preserve"><value>Varm i kläderna</value></data>
  <data name="Achievement_Sessions5_Description" xml:space="preserve"><value>5 pass avklarade.</value></data>
  <data name="Achievement_Sessions10_Title" xml:space="preserve"><value>I rullning</value></data>
  <data name="Achievement_Sessions10_Description" xml:space="preserve"><value>10 pass avklarade.</value></data>
  <data name="Achievement_Sessions25_Title" xml:space="preserve"><value>Vana</value></data>
  <data name="Achievement_Sessions25_Description" xml:space="preserve"><value>25 pass avklarade — nu sitter det!</value></data>
  <data name="Achievement_Sessions50_Title" xml:space="preserve"><value>Dedikerad</value></data>
  <data name="Achievement_Sessions50_Description" xml:space="preserve"><value>50 pass avklarade.</value></data>
  <data name="Achievement_Sessions100_Title" xml:space="preserve"><value>LockIn Legend</value></data>
  <data name="Achievement_Sessions100_Description" xml:space="preserve"><value>100 pass avklarade. Legendarisk.</value></data>
  <data name="Achievement_WeekStreak1_Title" xml:space="preserve"><value>Aktiv vecka</value></data>
  <data name="Achievement_WeekStreak1_Description" xml:space="preserve"><value>Tränat minst en gång två veckor i rad.</value></data>
  <data name="Achievement_WeekStreak4_Title" xml:space="preserve"><value>En månad stark</value></data>
  <data name="Achievement_WeekStreak4_Description" xml:space="preserve"><value>4 veckor i rad med träning.</value></data>
  <data name="Achievement_WeekStreak12_Title" xml:space="preserve"><value>Järnvilja</value></data>
  <data name="Achievement_WeekStreak12_Description" xml:space="preserve"><value>12 veckor i rad — tre månader!</value></data>
  <data name="Achievement_FirstPR_Title" xml:space="preserve"><value>Nytt rekord</value></data>
  <data name="Achievement_FirstPR_Description" xml:space="preserve"><value>Du slog ditt första personliga rekord!</value></data>
  <data name="Achievement_PR10_Title" xml:space="preserve"><value>PR-maskin</value></data>
  <data name="Achievement_PR10_Description" xml:space="preserve"><value>10 personliga rekord totalt.</value></data>
  <data name="Achievement_PR50_Title" xml:space="preserve"><value>Rekordjägare</value></data>
  <data name="Achievement_PR50_Description" xml:space="preserve"><value>50 personliga rekord totalt.</value></data>
  <data name="Achievement_TotalVolume100k_Title" xml:space="preserve"><value>100-tonslyftet</value></data>
  <data name="Achievement_TotalVolume100k_Description" xml:space="preserve"><value>Du har totalt lyft 100 000 kg.</value></data>
  <data name="Achievement_TotalVolume500k_Title" xml:space="preserve"><value>Halvmiljonär</value></data>
  <data name="Achievement_TotalVolume500k_Description" xml:space="preserve"><value>Du har totalt lyft 500 000 kg.</value></data>
  <data name="Achievement_TotalVolume1M_Title" xml:space="preserve"><value>Miljonlyftare</value></data>
  <data name="Achievement_TotalVolume1M_Description" xml:space="preserve"><value>En miljon kg totalt lyft.</value></data>
  <data name="Achievement_AllMuscleGroups_Title" xml:space="preserve"><value>Komplett</value></data>
  <data name="Achievement_AllMuscleGroups_Description" xml:space="preserve"><value>Alla muskelgrupper tränade under sju dagar.</value></data>
  <data name="Achievement_LongSession_Title" xml:space="preserve"><value>Maratonpass</value></data>
  <data name="Achievement_LongSession_Description" xml:space="preserve"><value>Du genomförde ett pass längre än 90 minuter.</value></data>
  <data name="Achievement_EarlyBird_Title" xml:space="preserve"><value>Morgonfågeln</value></data>
  <data name="Achievement_EarlyBird_Description" xml:space="preserve"><value>Pass startat före klockan 07:00.</value></data>
  <data name="Achievement_NightOwl_Title" xml:space="preserve"><value>Nattuglan</value></data>
  <data name="Achievement_NightOwl_Description" xml:space="preserve"><value>Pass startat efter klockan 21:00.</value></data>
  <data name="Achievement_FirstCustomExercise_Title" xml:space="preserve"><value>Uppfinnaren</value></data>
  <data name="Achievement_FirstCustomExercise_Description" xml:space="preserve"><value>Du skapade en egen övning.</value></data>
```

- [ ] **Step 2: Lägg till 40 engelska nycklar i AppResources.en.resx**

Läs `LockIn/Resources/Strings/AppResources.en.resx`. Lägg till identisk key-set, engelska värden:

```xml
  <!-- Achievements -->
  <data name="Achievement_FirstWorkout_Title" xml:space="preserve"><value>First Step</value></data>
  <data name="Achievement_FirstWorkout_Description" xml:space="preserve"><value>You completed your very first workout!</value></data>
  <data name="Achievement_Sessions5_Title" xml:space="preserve"><value>Warming Up</value></data>
  <data name="Achievement_Sessions5_Description" xml:space="preserve"><value>5 workouts completed.</value></data>
  <data name="Achievement_Sessions10_Title" xml:space="preserve"><value>In the Zone</value></data>
  <data name="Achievement_Sessions10_Description" xml:space="preserve"><value>10 workouts completed.</value></data>
  <data name="Achievement_Sessions25_Title" xml:space="preserve"><value>Habitual</value></data>
  <data name="Achievement_Sessions25_Description" xml:space="preserve"><value>25 workouts completed — it's a habit now!</value></data>
  <data name="Achievement_Sessions50_Title" xml:space="preserve"><value>Dedicated</value></data>
  <data name="Achievement_Sessions50_Description" xml:space="preserve"><value>50 workouts completed.</value></data>
  <data name="Achievement_Sessions100_Title" xml:space="preserve"><value>LockIn Legend</value></data>
  <data name="Achievement_Sessions100_Description" xml:space="preserve"><value>100 workouts completed. Legendary.</value></data>
  <data name="Achievement_WeekStreak1_Title" xml:space="preserve"><value>Active Week</value></data>
  <data name="Achievement_WeekStreak1_Description" xml:space="preserve"><value>Trained at least once two weeks in a row.</value></data>
  <data name="Achievement_WeekStreak4_Title" xml:space="preserve"><value>One Month Strong</value></data>
  <data name="Achievement_WeekStreak4_Description" xml:space="preserve"><value>4 consecutive weeks of training.</value></data>
  <data name="Achievement_WeekStreak12_Title" xml:space="preserve"><value>Iron Will</value></data>
  <data name="Achievement_WeekStreak12_Description" xml:space="preserve"><value>12 consecutive weeks — three months!</value></data>
  <data name="Achievement_FirstPR_Title" xml:space="preserve"><value>New Record</value></data>
  <data name="Achievement_FirstPR_Description" xml:space="preserve"><value>You set your first personal record!</value></data>
  <data name="Achievement_PR10_Title" xml:space="preserve"><value>PR Machine</value></data>
  <data name="Achievement_PR10_Description" xml:space="preserve"><value>10 personal records total.</value></data>
  <data name="Achievement_PR50_Title" xml:space="preserve"><value>Record Hunter</value></data>
  <data name="Achievement_PR50_Description" xml:space="preserve"><value>50 personal records total.</value></data>
  <data name="Achievement_TotalVolume100k_Title" xml:space="preserve"><value>100-Ton Club</value></data>
  <data name="Achievement_TotalVolume100k_Description" xml:space="preserve"><value>You have lifted a total of 100,000 kg.</value></data>
  <data name="Achievement_TotalVolume500k_Title" xml:space="preserve"><value>Half-Million Club</value></data>
  <data name="Achievement_TotalVolume500k_Description" xml:space="preserve"><value>You have lifted a total of 500,000 kg.</value></data>
  <data name="Achievement_TotalVolume1M_Title" xml:space="preserve"><value>Million Lifter</value></data>
  <data name="Achievement_TotalVolume1M_Description" xml:space="preserve"><value>One million kg lifted in total.</value></data>
  <data name="Achievement_AllMuscleGroups_Title" xml:space="preserve"><value>Complete</value></data>
  <data name="Achievement_AllMuscleGroups_Description" xml:space="preserve"><value>All muscle groups trained within seven days.</value></data>
  <data name="Achievement_LongSession_Title" xml:space="preserve"><value>Marathon Session</value></data>
  <data name="Achievement_LongSession_Description" xml:space="preserve"><value>You completed a workout longer than 90 minutes.</value></data>
  <data name="Achievement_EarlyBird_Title" xml:space="preserve"><value>Early Bird</value></data>
  <data name="Achievement_EarlyBird_Description" xml:space="preserve"><value>Workout started before 07:00.</value></data>
  <data name="Achievement_NightOwl_Title" xml:space="preserve"><value>Night Owl</value></data>
  <data name="Achievement_NightOwl_Description" xml:space="preserve"><value>Workout started after 21:00.</value></data>
  <data name="Achievement_FirstCustomExercise_Title" xml:space="preserve"><value>Inventor</value></data>
  <data name="Achievement_FirstCustomExercise_Description" xml:space="preserve"><value>You created a custom exercise.</value></data>
```

- [ ] **Step 3: Lägg till 40 properties i AppResources.cs**

Läs `LockIn/Resources/Strings/AppResources.cs`. Lägg till ett nytt block efter de befintliga sektionerna:

```csharp
    // ── Achievements ──────────────────────────────────────────────────────
    public static string Achievement_FirstWorkout_Title            => Get(nameof(Achievement_FirstWorkout_Title));
    public static string Achievement_FirstWorkout_Description      => Get(nameof(Achievement_FirstWorkout_Description));
    public static string Achievement_Sessions5_Title               => Get(nameof(Achievement_Sessions5_Title));
    public static string Achievement_Sessions5_Description         => Get(nameof(Achievement_Sessions5_Description));
    public static string Achievement_Sessions10_Title              => Get(nameof(Achievement_Sessions10_Title));
    public static string Achievement_Sessions10_Description        => Get(nameof(Achievement_Sessions10_Description));
    public static string Achievement_Sessions25_Title              => Get(nameof(Achievement_Sessions25_Title));
    public static string Achievement_Sessions25_Description        => Get(nameof(Achievement_Sessions25_Description));
    public static string Achievement_Sessions50_Title              => Get(nameof(Achievement_Sessions50_Title));
    public static string Achievement_Sessions50_Description        => Get(nameof(Achievement_Sessions50_Description));
    public static string Achievement_Sessions100_Title             => Get(nameof(Achievement_Sessions100_Title));
    public static string Achievement_Sessions100_Description       => Get(nameof(Achievement_Sessions100_Description));
    public static string Achievement_WeekStreak1_Title             => Get(nameof(Achievement_WeekStreak1_Title));
    public static string Achievement_WeekStreak1_Description       => Get(nameof(Achievement_WeekStreak1_Description));
    public static string Achievement_WeekStreak4_Title             => Get(nameof(Achievement_WeekStreak4_Title));
    public static string Achievement_WeekStreak4_Description       => Get(nameof(Achievement_WeekStreak4_Description));
    public static string Achievement_WeekStreak12_Title            => Get(nameof(Achievement_WeekStreak12_Title));
    public static string Achievement_WeekStreak12_Description      => Get(nameof(Achievement_WeekStreak12_Description));
    public static string Achievement_FirstPR_Title                 => Get(nameof(Achievement_FirstPR_Title));
    public static string Achievement_FirstPR_Description           => Get(nameof(Achievement_FirstPR_Description));
    public static string Achievement_PR10_Title                    => Get(nameof(Achievement_PR10_Title));
    public static string Achievement_PR10_Description              => Get(nameof(Achievement_PR10_Description));
    public static string Achievement_PR50_Title                    => Get(nameof(Achievement_PR50_Title));
    public static string Achievement_PR50_Description              => Get(nameof(Achievement_PR50_Description));
    public static string Achievement_TotalVolume100k_Title         => Get(nameof(Achievement_TotalVolume100k_Title));
    public static string Achievement_TotalVolume100k_Description   => Get(nameof(Achievement_TotalVolume100k_Description));
    public static string Achievement_TotalVolume500k_Title         => Get(nameof(Achievement_TotalVolume500k_Title));
    public static string Achievement_TotalVolume500k_Description   => Get(nameof(Achievement_TotalVolume500k_Description));
    public static string Achievement_TotalVolume1M_Title           => Get(nameof(Achievement_TotalVolume1M_Title));
    public static string Achievement_TotalVolume1M_Description     => Get(nameof(Achievement_TotalVolume1M_Description));
    public static string Achievement_AllMuscleGroups_Title         => Get(nameof(Achievement_AllMuscleGroups_Title));
    public static string Achievement_AllMuscleGroups_Description   => Get(nameof(Achievement_AllMuscleGroups_Description));
    public static string Achievement_LongSession_Title             => Get(nameof(Achievement_LongSession_Title));
    public static string Achievement_LongSession_Description       => Get(nameof(Achievement_LongSession_Description));
    public static string Achievement_EarlyBird_Title               => Get(nameof(Achievement_EarlyBird_Title));
    public static string Achievement_EarlyBird_Description         => Get(nameof(Achievement_EarlyBird_Description));
    public static string Achievement_NightOwl_Title                => Get(nameof(Achievement_NightOwl_Title));
    public static string Achievement_NightOwl_Description          => Get(nameof(Achievement_NightOwl_Description));
    public static string Achievement_FirstCustomExercise_Title     => Get(nameof(Achievement_FirstCustomExercise_Title));
    public static string Achievement_FirstCustomExercise_Description => Get(nameof(Achievement_FirstCustomExercise_Description));
```

- [ ] **Step 4: Uppdatera AchievementService.cs**

Läs `LockIn/Services/AchievementService.cs`. Filen innehåller ett `IReadOnlyList<AchievementDef>` med hårdkodade `Title` och `Description`. Lägg till `using LockIn.Resources.Strings;` om det saknas.

Nuvarande kod (rad 9-31):
```csharp
public static readonly IReadOnlyList<AchievementDef> All = new[]
{
    new AchievementDef(AchievementId.FirstWorkout,       "🏋️", "Första steget",   "Du klarade ditt allra första pass!"),
    new AchievementDef(AchievementId.Sessions5,          "🔥", "Varm i kläderna", "5 pass avklarade."),
    // ... (20 rader)
};
```

Ersätt med (alla 20 rader):
```csharp
public static readonly IReadOnlyList<AchievementDef> All = new[]
{
    new AchievementDef(AchievementId.FirstWorkout,        "🏋️", AppResources.Achievement_FirstWorkout_Title,        AppResources.Achievement_FirstWorkout_Description),
    new AchievementDef(AchievementId.Sessions5,           "🔥", AppResources.Achievement_Sessions5_Title,           AppResources.Achievement_Sessions5_Description),
    new AchievementDef(AchievementId.Sessions10,          "⚡", AppResources.Achievement_Sessions10_Title,          AppResources.Achievement_Sessions10_Description),
    new AchievementDef(AchievementId.Sessions25,          "💪", AppResources.Achievement_Sessions25_Title,          AppResources.Achievement_Sessions25_Description),
    new AchievementDef(AchievementId.Sessions50,          "🎯", AppResources.Achievement_Sessions50_Title,          AppResources.Achievement_Sessions50_Description),
    new AchievementDef(AchievementId.Sessions100,         "🏆", AppResources.Achievement_Sessions100_Title,         AppResources.Achievement_Sessions100_Description),
    new AchievementDef(AchievementId.WeekStreak1,         "📅", AppResources.Achievement_WeekStreak1_Title,         AppResources.Achievement_WeekStreak1_Description),
    new AchievementDef(AchievementId.WeekStreak4,         "🗓️", AppResources.Achievement_WeekStreak4_Title,        AppResources.Achievement_WeekStreak4_Description),
    new AchievementDef(AchievementId.WeekStreak12,        "🔩", AppResources.Achievement_WeekStreak12_Title,        AppResources.Achievement_WeekStreak12_Description),
    new AchievementDef(AchievementId.FirstPR,             "⭐", AppResources.Achievement_FirstPR_Title,             AppResources.Achievement_FirstPR_Description),
    new AchievementDef(AchievementId.PR10,                "🎖️", AppResources.Achievement_PR10_Title,              AppResources.Achievement_PR10_Description),
    new AchievementDef(AchievementId.PR50,                "🥇", AppResources.Achievement_PR50_Title,               AppResources.Achievement_PR50_Description),
    new AchievementDef(AchievementId.TotalVolume100k,     "💯", AppResources.Achievement_TotalVolume100k_Title,    AppResources.Achievement_TotalVolume100k_Description),
    new AchievementDef(AchievementId.TotalVolume500k,     "💎", AppResources.Achievement_TotalVolume500k_Title,    AppResources.Achievement_TotalVolume500k_Description),
    new AchievementDef(AchievementId.TotalVolume1M,       "👑", AppResources.Achievement_TotalVolume1M_Title,      AppResources.Achievement_TotalVolume1M_Description),
    new AchievementDef(AchievementId.AllMuscleGroups,     "🌟", AppResources.Achievement_AllMuscleGroups_Title,    AppResources.Achievement_AllMuscleGroups_Description),
    new AchievementDef(AchievementId.LongSession,         "⏳", AppResources.Achievement_LongSession_Title,        AppResources.Achievement_LongSession_Description),
    new AchievementDef(AchievementId.EarlyBird,           "🌅", AppResources.Achievement_EarlyBird_Title,          AppResources.Achievement_EarlyBird_Description),
    new AchievementDef(AchievementId.NightOwl,            "🦉", AppResources.Achievement_NightOwl_Title,           AppResources.Achievement_NightOwl_Description),
    new AchievementDef(AchievementId.FirstCustomExercise, "🔧", AppResources.Achievement_FirstCustomExercise_Title, AppResources.Achievement_FirstCustomExercise_Description),
};
```

**Varning:** `All` är en statisk field-initializer som körs vid klass-laddning. AppResources läses korrekt även vid statisk initiering eftersom `_resourceManager` initieras av dess egen statiska konstruktor.

- [ ] **Step 5: Bygg**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add LockIn/Services/AchievementService.cs \
        LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(i18n): localize achievement titles and descriptions (40 keys)"
```

---

## Task 2: Programbeskrivningar + dagetiketter

**Files:**
- Modify: `LockIn/Data/WorkoutPrograms.cs`
- Modify: `LockIn/Views/ProgramDetailPage.xaml`
- Modify: `LockIn/Views/LibraryPage.xaml`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: `AppResources.Get(string key)` (redan importerat i WorkoutPrograms.cs via Plan 2); `WorkoutProgram` record (positional: `Id, Name, Description, DaysPerWeek, Days`); `ProgramDay` record (positional: `Label, Exercises`)
- Produces: `ProgramDay.LabelKey` (init-property) + `ProgramDay.LocalizedLabel` (computed); `WorkoutProgram.LocalizedDescription` (computed). XAML bindar till `LocalizedLabel` och `LocalizedDescription`.

- [ ] **Step 1: Lägg till 27 svenska nycklar i AppResources.resx**

Läs `LockIn/Resources/Strings/AppResources.resx`. Lägg till nytt block (program-IDs är alltid lowercase):

```xml
  <!-- Programs — descriptions and day labels -->
  <data name="Program_ppl_Description" xml:space="preserve"><value>Klassiskt 6-dagarsprogram. Tre muskelgrupper roteras för maximal frekvens och volym.</value></data>
  <data name="Program_ppl_Day1_Label" xml:space="preserve"><value>Push A</value></data>
  <data name="Program_ppl_Day2_Label" xml:space="preserve"><value>Pull A</value></data>
  <data name="Program_ppl_Day3_Label" xml:space="preserve"><value>Legs A</value></data>
  <data name="Program_ppl_Day4_Label" xml:space="preserve"><value>Push B</value></data>
  <data name="Program_ppl_Day5_Label" xml:space="preserve"><value>Pull B</value></data>
  <data name="Program_ppl_Day6_Label" xml:space="preserve"><value>Legs B</value></data>
  <data name="Program_upperlower_Description" xml:space="preserve"><value>Effektivt 4-dagarsprogram som delar kroppen i övre och undre halvan.</value></data>
  <data name="Program_upperlower_Day1_Label" xml:space="preserve"><value>Upper A — Styrka</value></data>
  <data name="Program_upperlower_Day2_Label" xml:space="preserve"><value>Lower A — Styrka</value></data>
  <data name="Program_upperlower_Day3_Label" xml:space="preserve"><value>Upper B — Hypertrofi</value></data>
  <data name="Program_upperlower_Day4_Label" xml:space="preserve"><value>Lower B — Hypertrofi</value></data>
  <data name="Program_fullbody_Description" xml:space="preserve"><value>Träna hela kroppen tre gånger i veckan. Bra för nybörjare och de med lite träningstid.</value></data>
  <data name="Program_fullbody_Day1_Label" xml:space="preserve"><value>Måndag</value></data>
  <data name="Program_fullbody_Day2_Label" xml:space="preserve"><value>Onsdag</value></data>
  <data name="Program_fullbody_Day3_Label" xml:space="preserve"><value>Fredag</value></data>
  <data name="Program_startingstrength_Description" xml:space="preserve"><value>Mark Rippetoes klassiska nybörjarprogram. Fokus på de 5 grundlyften med linjär progression.</value></data>
  <data name="Program_startingstrength_Day1_Label" xml:space="preserve"><value>Pass A</value></data>
  <data name="Program_startingstrength_Day2_Label" xml:space="preserve"><value>Pass B</value></data>
  <data name="Program_texasmethod_Description" xml:space="preserve"><value>Klassiskt mellannivåprogram med volymdag, återhämtningsdag och intensitetsdag. Linjär progression vecka för vecka.</value></data>
  <data name="Program_texasmethod_Day1_Label" xml:space="preserve"><value>Måndag — Volym</value></data>
  <data name="Program_texasmethod_Day2_Label" xml:space="preserve"><value>Onsdag — Återhämtning</value></data>
  <data name="Program_texasmethod_Day3_Label" xml:space="preserve"><value>Fredag — Intensitet</value></data>
  <data name="Program_531bbb_Description" xml:space="preserve"><value>Jim Wendlers 5/3/1 med Boring But Big-tillägget. Bygg styrka med de tunga seten och massa med 5×10-volymarbetet.</value></data>
  <data name="Program_531bbb_Day1_Label" xml:space="preserve"><value>Pass A — Knäböj &amp; Bänkpress</value></data>
  <data name="Program_531bbb_Day2_Label" xml:space="preserve"><value>Pass B — Marklyft &amp; Militärpress</value></data>
  <data name="Program_531bbb_Day3_Label" xml:space="preserve"><value>Pass C — Styrka &amp; Accessoarer</value></data>
```

OBS: `&` i XML-värden escapes som `&amp;`.

- [ ] **Step 2: Lägg till 27 engelska nycklar i AppResources.en.resx**

Läs `LockIn/Resources/Strings/AppResources.en.resx`. Identisk key-set:

```xml
  <!-- Programs — descriptions and day labels -->
  <data name="Program_ppl_Description" xml:space="preserve"><value>Classic 6-day program. Three muscle groups rotated for maximum frequency and volume.</value></data>
  <data name="Program_ppl_Day1_Label" xml:space="preserve"><value>Push A</value></data>
  <data name="Program_ppl_Day2_Label" xml:space="preserve"><value>Pull A</value></data>
  <data name="Program_ppl_Day3_Label" xml:space="preserve"><value>Legs A</value></data>
  <data name="Program_ppl_Day4_Label" xml:space="preserve"><value>Push B</value></data>
  <data name="Program_ppl_Day5_Label" xml:space="preserve"><value>Pull B</value></data>
  <data name="Program_ppl_Day6_Label" xml:space="preserve"><value>Legs B</value></data>
  <data name="Program_upperlower_Description" xml:space="preserve"><value>Efficient 4-day program splitting the body into upper and lower halves.</value></data>
  <data name="Program_upperlower_Day1_Label" xml:space="preserve"><value>Upper A — Strength</value></data>
  <data name="Program_upperlower_Day2_Label" xml:space="preserve"><value>Lower A — Strength</value></data>
  <data name="Program_upperlower_Day3_Label" xml:space="preserve"><value>Upper B — Hypertrophy</value></data>
  <data name="Program_upperlower_Day4_Label" xml:space="preserve"><value>Lower B — Hypertrophy</value></data>
  <data name="Program_fullbody_Description" xml:space="preserve"><value>Train your whole body three times a week. Great for beginners and those with limited training time.</value></data>
  <data name="Program_fullbody_Day1_Label" xml:space="preserve"><value>Monday</value></data>
  <data name="Program_fullbody_Day2_Label" xml:space="preserve"><value>Wednesday</value></data>
  <data name="Program_fullbody_Day3_Label" xml:space="preserve"><value>Friday</value></data>
  <data name="Program_startingstrength_Description" xml:space="preserve"><value>Mark Rippetoe's classic beginner program. Focus on the 5 core lifts with linear progression.</value></data>
  <data name="Program_startingstrength_Day1_Label" xml:space="preserve"><value>Session A</value></data>
  <data name="Program_startingstrength_Day2_Label" xml:space="preserve"><value>Session B</value></data>
  <data name="Program_texasmethod_Description" xml:space="preserve"><value>Classic intermediate program with a volume day, recovery day, and intensity day. Linear progression week by week.</value></data>
  <data name="Program_texasmethod_Day1_Label" xml:space="preserve"><value>Monday — Volume</value></data>
  <data name="Program_texasmethod_Day2_Label" xml:space="preserve"><value>Wednesday — Recovery</value></data>
  <data name="Program_texasmethod_Day3_Label" xml:space="preserve"><value>Friday — Intensity</value></data>
  <data name="Program_531bbb_Description" xml:space="preserve"><value>Jim Wendler's 5/3/1 with the Boring But Big supplement. Build strength with heavy sets and mass with 5×10 volume work.</value></data>
  <data name="Program_531bbb_Day1_Label" xml:space="preserve"><value>Session A — Squat &amp; Bench</value></data>
  <data name="Program_531bbb_Day2_Label" xml:space="preserve"><value>Session B — Deadlift &amp; Press</value></data>
  <data name="Program_531bbb_Day3_Label" xml:space="preserve"><value>Session C — Strength &amp; Accessories</value></data>
```

- [ ] **Step 3: Lägg till 27 properties i AppResources.cs**

Lägg till nytt block:

```csharp
    // ── Programs ──────────────────────────────────────────────────────────
    public static string Program_ppl_Description            => Get(nameof(Program_ppl_Description));
    public static string Program_ppl_Day1_Label             => Get(nameof(Program_ppl_Day1_Label));
    public static string Program_ppl_Day2_Label             => Get(nameof(Program_ppl_Day2_Label));
    public static string Program_ppl_Day3_Label             => Get(nameof(Program_ppl_Day3_Label));
    public static string Program_ppl_Day4_Label             => Get(nameof(Program_ppl_Day4_Label));
    public static string Program_ppl_Day5_Label             => Get(nameof(Program_ppl_Day5_Label));
    public static string Program_ppl_Day6_Label             => Get(nameof(Program_ppl_Day6_Label));
    public static string Program_upperlower_Description     => Get(nameof(Program_upperlower_Description));
    public static string Program_upperlower_Day1_Label      => Get(nameof(Program_upperlower_Day1_Label));
    public static string Program_upperlower_Day2_Label      => Get(nameof(Program_upperlower_Day2_Label));
    public static string Program_upperlower_Day3_Label      => Get(nameof(Program_upperlower_Day3_Label));
    public static string Program_upperlower_Day4_Label      => Get(nameof(Program_upperlower_Day4_Label));
    public static string Program_fullbody_Description       => Get(nameof(Program_fullbody_Description));
    public static string Program_fullbody_Day1_Label        => Get(nameof(Program_fullbody_Day1_Label));
    public static string Program_fullbody_Day2_Label        => Get(nameof(Program_fullbody_Day2_Label));
    public static string Program_fullbody_Day3_Label        => Get(nameof(Program_fullbody_Day3_Label));
    public static string Program_startingstrength_Description => Get(nameof(Program_startingstrength_Description));
    public static string Program_startingstrength_Day1_Label  => Get(nameof(Program_startingstrength_Day1_Label));
    public static string Program_startingstrength_Day2_Label  => Get(nameof(Program_startingstrength_Day2_Label));
    public static string Program_texasmethod_Description    => Get(nameof(Program_texasmethod_Description));
    public static string Program_texasmethod_Day1_Label     => Get(nameof(Program_texasmethod_Day1_Label));
    public static string Program_texasmethod_Day2_Label     => Get(nameof(Program_texasmethod_Day2_Label));
    public static string Program_texasmethod_Day3_Label     => Get(nameof(Program_texasmethod_Day3_Label));
    public static string Program_531bbb_Description         => Get(nameof(Program_531bbb_Description));
    public static string Program_531bbb_Day1_Label          => Get(nameof(Program_531bbb_Day1_Label));
    public static string Program_531bbb_Day2_Label          => Get(nameof(Program_531bbb_Day2_Label));
    public static string Program_531bbb_Day3_Label          => Get(nameof(Program_531bbb_Day3_Label));
```

- [ ] **Step 4: Utöka WorkoutProgram-record med LocalizedDescription**

Läs `LockIn/Data/WorkoutPrograms.cs`. Nuvarande record-definition (rad 10-13):
```csharp
public record WorkoutProgram(string Id, string Name, string Description, int DaysPerWeek, List<ProgramDay> Days)
{
    public string DaysPerWeekText => string.Format(AppResources.Library_DaysPerWeek_Format, DaysPerWeek);
}
```

Lägg till `LocalizedDescription` computed property:
```csharp
public record WorkoutProgram(string Id, string Name, string Description, int DaysPerWeek, List<ProgramDay> Days)
{
    public string DaysPerWeekText      => string.Format(AppResources.Library_DaysPerWeek_Format, DaysPerWeek);
    public string LocalizedDescription => AppResources.Get($"Program_{Id}_Description");
}
```

`AppResources.Get()` returnerar nyckeln som fallback om resursen saknas — inga null-crashes.

- [ ] **Step 5: Utöka ProgramDay-record med LabelKey och LocalizedLabel**

Nuvarande definition (rad 9):
```csharp
public record ProgramDay(string Label, List<ProgramExercise> Exercises);
```

Ändra till (lägg till record-body med init-property och computed property):
```csharp
public record ProgramDay(string Label, List<ProgramExercise> Exercises)
{
    public string LabelKey     { get; init; } = string.Empty;
    public string LocalizedLabel => string.IsNullOrEmpty(LabelKey) ? Label : AppResources.Get(LabelKey);
}
```

- [ ] **Step 6: Fyll i LabelKey på alla ProgramDay-instanser i WorkoutPrograms.cs**

Uppdatera samtliga `new ProgramDay(...)` med `{ LabelKey = "..." }`. Exakt mappning:

**PPL (rad ~22-63):**
```csharp
new ProgramDay("Push A",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day1_Label" },
new ProgramDay("Pull A",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day2_Label" },
new ProgramDay("Legs A",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day3_Label" },
new ProgramDay("Push B",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day4_Label" },
new ProgramDay("Pull B",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day5_Label" },
new ProgramDay("Legs B",  new List<ProgramExercise> { ... }) { LabelKey = "Program_ppl_Day6_Label" },
```

**Upper/Lower (rad ~70-98):**
```csharp
new ProgramDay("Upper A — Styrka",     new List<ProgramExercise> { ... }) { LabelKey = "Program_upperlower_Day1_Label" },
new ProgramDay("Lower A — Styrka",     new List<ProgramExercise> { ... }) { LabelKey = "Program_upperlower_Day2_Label" },
new ProgramDay("Upper B — Hypertrofi", new List<ProgramExercise> { ... }) { LabelKey = "Program_upperlower_Day3_Label" },
new ProgramDay("Lower B — Hypertrofi", new List<ProgramExercise> { ... }) { LabelKey = "Program_upperlower_Day4_Label" },
```

**Full Body (rad ~105-129):**
```csharp
new ProgramDay("Måndag", new List<ProgramExercise> { ... }) { LabelKey = "Program_fullbody_Day1_Label" },
new ProgramDay("Onsdag", new List<ProgramExercise> { ... }) { LabelKey = "Program_fullbody_Day2_Label" },
new ProgramDay("Fredag", new List<ProgramExercise> { ... }) { LabelKey = "Program_fullbody_Day3_Label" },
```

**Starting Strength (rad ~135-145):**
```csharp
new ProgramDay("Pass A", new List<ProgramExercise> { ... }) { LabelKey = "Program_startingstrength_Day1_Label" },
new ProgramDay("Pass B", new List<ProgramExercise> { ... }) { LabelKey = "Program_startingstrength_Day2_Label" },
```

**Texas Method (rad ~151-168):**
```csharp
new ProgramDay("Måndag — Volym",        new List<ProgramExercise> { ... }) { LabelKey = "Program_texasmethod_Day1_Label" },
new ProgramDay("Onsdag — Återhämtning", new List<ProgramExercise> { ... }) { LabelKey = "Program_texasmethod_Day2_Label" },
new ProgramDay("Fredag — Intensitet",   new List<ProgramExercise> { ... }) { LabelKey = "Program_texasmethod_Day3_Label" },
```

**5/3/1 BBB (rad ~175-195):**
```csharp
new ProgramDay("Pass A — Knäböj & Bänkpress",     new List<ProgramExercise> { ... }) { LabelKey = "Program_531bbb_Day1_Label" },
new ProgramDay("Pass B — Marklyft & Militärpress", new List<ProgramExercise> { ... }) { LabelKey = "Program_531bbb_Day2_Label" },
new ProgramDay("Pass C — Styrka & Accessoarer",    new List<ProgramExercise> { ... }) { LabelKey = "Program_531bbb_Day3_Label" },
```

OBS: Det exakta antalet `ProgramExercise` i varje lista varierar — kopiera dem som de är, ändra bara `ProgramDay`-instansieringen.

- [ ] **Step 7: Byt XAML-bindningar till LocalizedDescription och LocalizedLabel**

Verifiera vilka bindningar som används:
```bash
grep -n "Binding Description\|Binding Label" LockIn/Views/LibraryPage.xaml LockIn/Views/ProgramDetailPage.xaml
```

**I LibraryPage.xaml:** Byt `{Binding Description}` → `{Binding LocalizedDescription}` för program-kort.

**I ProgramDetailPage.xaml:** Byt `{Binding Label}` → `{Binding LocalizedLabel}` för day-headers. OBS: Kontrollera kontexten — det kan finnas `Label`-bindningar för `TemplateExercise` eller andra objekt. Byt BARA de som är på `ProgramDay`-datamall.

- [ ] **Step 8: Bygg**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: 0 errors.

- [ ] **Step 9: Commit**

```bash
git add LockIn/Data/WorkoutPrograms.cs \
        LockIn/Views/ProgramDetailPage.xaml \
        LockIn/Views/LibraryPage.xaml \
        LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(i18n): localize program descriptions and day labels (27 keys)"
```

---

## Task 3: Notifikationssträngar + datum-kulturfix

**Files:**
- Modify: `LockIn/Services/NotificationService.cs`
- Modify: `LockIn/ViewModels/HistoryViewModel.cs`
- Modify: `LockIn/ViewModels/BodyWeightViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: `AppResources.Get(string key)`, `System.Globalization.CultureInfo.CurrentUICulture`
- Produces: 2 `Notification_`-nycklar. HistoryViewModel och BodyWeightViewModel använder `CultureInfo.CurrentUICulture` för datumvisning i stället för `sv-SE` respektive ingen kultur.

- [ ] **Step 1: Lägg till 2 svenska notifikationsnycklar i AppResources.resx**

Läs `LockIn/Resources/Strings/AppResources.resx`. Lägg till:

```xml
  <!-- Notifications -->
  <data name="Notification_RestTimer_Title" xml:space="preserve"><value>Vilotimer klar!</value></data>
  <data name="Notification_RestTimer_Body_Format" xml:space="preserve"><value>Dags för nästa set – {0}</value></data>
```

- [ ] **Step 2: Lägg till 2 engelska nycklar i AppResources.en.resx**

Läs `LockIn/Resources/Strings/AppResources.en.resx`. Lägg till:

```xml
  <!-- Notifications -->
  <data name="Notification_RestTimer_Title" xml:space="preserve"><value>Rest timer done!</value></data>
  <data name="Notification_RestTimer_Body_Format" xml:space="preserve"><value>Time for your next set – {0}</value></data>
```

- [ ] **Step 3: Lägg till 2 properties i AppResources.cs**

```csharp
    // ── Notifications ─────────────────────────────────────────────────────
    public static string Notification_RestTimer_Title       => Get(nameof(Notification_RestTimer_Title));
    public static string Notification_RestTimer_Body_Format => Get(nameof(Notification_RestTimer_Body_Format));
```

- [ ] **Step 4: Uppdatera NotificationService.cs**

Läs `LockIn/Services/NotificationService.cs`. Nuvarande kod (rad 25-30):
```csharp
var content = new UNMutableNotificationContent
{
    Title = "Vilotimer klar!",
    Body = $"Dags för nästa set – {exerciseName}",
    Sound = UNNotificationSound.Default
};
```

Lägg till `using LockIn.Resources.Strings;` om det saknas. Byt till:
```csharp
var content = new UNMutableNotificationContent
{
    Title = AppResources.Notification_RestTimer_Title,
    Body  = string.Format(AppResources.Notification_RestTimer_Body_Format, exerciseName),
    Sound = UNNotificationSound.Default
};
```

- [ ] **Step 5: Fixa sv-SE CultureInfo i HistoryViewModel.cs**

Läs `LockIn/ViewModels/HistoryViewModel.cs`. Hitta rad ~118:
```csharp
var culture = new System.Globalization.CultureInfo("sv-SE");
CalendarTitle = new DateTime(CalendarYear, CalendarMonth, 1)
    .ToString("MMMM yyyy", culture).ToUpper();
```

Byt `new System.Globalization.CultureInfo("sv-SE")` till `System.Globalization.CultureInfo.CurrentUICulture`:
```csharp
var culture = System.Globalization.CultureInfo.CurrentUICulture;
CalendarTitle = new DateTime(CalendarYear, CalendarMonth, 1)
    .ToString("MMMM yyyy", culture).ToUpper();
```

- [ ] **Step 6: Lägg till CultureInfo.CurrentUICulture i BodyWeightViewModel.cs datumformatering**

Läs `LockIn/ViewModels/BodyWeightViewModel.cs`. Hitta rad ~35 och ~83:

Rad ~35:
```csharp
LatestDate = latest.LoggedAt.ToString("d MMM yyyy");
```
Byt till:
```csharp
LatestDate = latest.LoggedAt.ToString("d MMM yyyy", System.Globalization.CultureInfo.CurrentUICulture);
```

Rad ~83:
```csharp
var body = string.Format(AppResources.BodyWeight_DeleteBody_Format,
    $"{entry.WeightKg:F1}", entry.LoggedAt.ToString("d MMM"));
```
Byt till:
```csharp
var body = string.Format(AppResources.BodyWeight_DeleteBody_Format,
    $"{entry.WeightKg:F1}", entry.LoggedAt.ToString("d MMM", System.Globalization.CultureInfo.CurrentUICulture));
```

- [ ] **Step 7: Bygg**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add LockIn/Services/NotificationService.cs \
        LockIn/ViewModels/HistoryViewModel.cs \
        LockIn/ViewModels/BodyWeightViewModel.cs \
        LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(i18n): localize notification strings + fix hardcoded sv-SE culture (2 keys)"
```

---

## Task 4: Version bump + slutbygge

**Files:**
- Modify: `LockIn/LockIn.csproj`

**Interfaces:**
- Inga nya — enbart version och verifiering.

- [ ] **Step 1: Bumpa ApplicationVersion 54 → 55**

Läs `LockIn/LockIn.csproj`. Hitta:
```xml
<ApplicationVersion>54</ApplicationVersion>
```
Byt till:
```xml
<ApplicationVersion>55</ApplicationVersion>
```

- [ ] **Step 2: Slutbygge**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -10
```

Förväntat: BUILD SUCCEEDED, 0 errors, ≤106 warnings.

- [ ] **Step 3: App-wide grep-verifiering**

Verifiera att inga achievement-strängar, programbeskrivningar eller notis-strängar är kvar hårdkodade:

```bash
# Achievement-titlar
grep -rn "Varm i kläderna\|Järnvilja\|Maratonpass\|Morgonfågeln\|Nattuglan\|Halvmiljonär\|Miljonlyftare\|Rekordjägare\|Uppfinnaren" LockIn/ --include="*.cs"

# Program-beskrivningar
grep -rn "Klassiskt 6-dagars\|Effektivt 4-dagars\|Mark Rippetoe\|Jim Wendler\|Linjär progression vecka" LockIn/ --include="*.cs"

# Notis-strängar
grep -rn "Vilotimer klar\|Dags för nästa set" LockIn/ --include="*.cs"

# Hårdkodad sv-SE
grep -rn "CultureInfo(\"sv-SE\")" LockIn/ --include="*.cs"
```

Förväntat: Inga träffar (eller bara i .resx-filer, som är korrekt).

- [ ] **Step 4: Commit**

```bash
git add LockIn/LockIn.csproj
git commit -m "feat(i18n): plan 3 complete — static data, achievements, notifications localized

ApplicationVersion bumped to 55. Manual simulator verification (sv + en locale) pending on Mac."
```

---

## Verifierings-checklista (efter Task 4)

- [ ] `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` → 0 errors, ≤106 warnings
- [ ] `grep -rn "Järnvilja\|Maratonpass\|Morgonfågeln" LockIn/ --include="*.cs"` → inga träffar
- [ ] `grep -rn "Klassiskt 6-dagars\|Jim Wendler" LockIn/ --include="*.cs"` → inga träffar
- [ ] `grep -rn "Vilotimer klar\|Dags för nästa set" LockIn/ --include="*.cs"` → inga träffar
- [ ] `grep -rn "CultureInfo(\"sv-SE\")" LockIn/ --include="*.cs"` → inga träffar
- [ ] .resx och .en.resx har identisk key-set (`diff <(grep 'data name' .resx) <(grep 'data name' .en.resx)` → tom)
- [ ] `ApplicationVersion` = 55 i LockIn.csproj
- [ ] Manuell simulator-verifiering (sv + en locale) pending på Mac

---

## Vad som INTE ingår i Plan 3

- **Övningsnamn** (Bänkpress, Marklyft) — stannar svenska. Spec §3.1.
- **Numeriska vikter och tal** — `InvariantCulture` används korrekt vid *parsning*; display-format i `.ToString("F0")` etc. lämnas oförändrade (komma vs punkt i vikter är utanför scope).
- **DatabaseService seed-data** (övningsbeskrivningar) — stannar svenska.
- **Push till git** — användaren beslutar.
