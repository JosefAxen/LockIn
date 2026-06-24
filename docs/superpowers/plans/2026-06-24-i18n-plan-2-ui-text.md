# i18n Plan 2 — UI-text på alla 18 sidor (engelska översättning)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Översätt resterande 18 sidor + tillhörande ViewModels från svenska till engelska via `{loc:Localize Key}` och `AppResources.X`-referenser. Efter denna plan visas hela appen (utom övningsnamn) på engelska när iOS-locale = `en-*`.

**Architecture:** Bygger vidare på Plan 1:s infrastruktur — `AppResources.resx` (sv default) + `AppResources.en.resx` (en) + hand-skriven `AppResources.cs` static wrapper + `LocalizeExtension` XAML markup extension. En task per sida för granskbarhet — en commit per sida, en review-pass per sida.

**Tech Stack:** Befintlig — `System.Resources.ResourceManager`, .NET MAUI 10 XAML markup extension.

## Global Constraints

- Övningsnamn översätts INTE — `Exercises`-tabellen och `WorkoutProgram.Name` rörs ej. (Programbeskrivningar = Plan 3, namnen lämnas.)
- Nyckelkonvention: `Område_Element` eller `Område_Action`. En `Område`-prefix per sida (se prefix-tabellen nedan). Cross-cutting strängar går under `Common_`.
- Versal-strängar (BIBLIOTEK, FORTSÄTT, AVSLUTA PASS) lagras som versaler i resx. Engelska motsvarigheter följer samma stil (LIBRARY, CONTINUE, FINISH WORKOUT).
- Pluralisering där "1 vs N" syns i texten: suffix `_One` / `_Many`. ViewModel väljer rätt nyckel baserat på count. Använd ENDAST där det märks.
- Parametriserade strängar: `{0}`, `{1}` med `string.Format`. Skriv strängen så att ordningen är naturlig i båda språken.
- Saknad nyckel: `LocalizeExtension` returnerar nyckelnamnet bokstavligt så missar syns utan att krascha. Trycks fallback betyder ALDRIG att en sida ska shippas med engelska key-namn synliga.
- Hårdkodade hex-strängar förbjudna utanför `Colors.xaml` / `DesignTokens.cs` — orört.
- Inga unit-tests finns — verifiering = `dotnet build -c Debug` + grep-kontroller. Manuell simulator-verifiering uppskjuten till slutet av planen.
- Pusha aldrig utan explicit instruktion. `ApplicationVersion` bumps med +1 inför push (görs vid planens slut, INTE per task).

---

## Filöversikt

| Fil | Roll | Status |
|-----|------|--------|
| `LockIn/Resources/Strings/AppResources.resx` | Append per task — sv-värden | Modifieras 18+ ggr |
| `LockIn/Resources/Strings/AppResources.en.resx` | Append per task — en-värden | Modifieras 18+ ggr |
| `LockIn/Resources/Strings/AppResources.cs` | Append properties per task | Modifieras 18+ ggr |
| `LockIn/AppShell.xaml` | Tab-titlar Hem/Träna/Historik/Bibliotek/Kropp | Modifieras (Task 0) |
| `LockIn/Views/*.xaml` (18 st) | `Text="..."` → `{loc:Localize ...}` | Modifieras |
| `LockIn/ViewModels/*.cs` (~15 st) | DisplayAlert/Prompt-strängar → `AppResources.X` | Modifieras |
| `LockIn/LockIn.csproj` | `<ApplicationVersion>` bumps i sista task | Modifieras 1 ggn |

---

## Nyckelnamn-prefix per sida

| Sida | Prefix | Förväntade XAML-strängar |
|------|--------|------|
| AppShell tab-titlar | `Tab_` | 5 |
| HemPage | `Hem_` | 24 |
| TrainPage | `Train_` | 17 |
| HistoryPage | `History_` | 14 |
| LibraryPage | `Library_` | 21 |
| KroppPage | `Kropp_` | 21 |
| ActiveWorkoutPage | `ActiveWorkout_` | 6 (men många VM-strängar) |
| PostWorkoutPage | `PostWorkout_` | 13 |
| SessionDetailPage | `SessionDetail_` | 9 |
| ExercisePickerPage | `ExercisePicker_` | 5 |
| TemplateEditPage | `TemplateEdit_` | 16 |
| ExerciseProgressPage | `ExerciseProgress_` | 16 |
| CreateExercisePage | `CreateExercise_` | 11 |
| ProgramDetailPage | `ProgramDetail_` | 2 |
| SettingsPage | `Settings_` | 24 |
| BodyWeightPage | `BodyWeight_` | 6 |
| AchievementsPage | `Achievements_` | 1 |
| PlateCalculatorPage | `PlateCalculator_` | 15 |
| ProgressPhotosPage | `ProgressPhotos_` | 3 |

Total: ~228 XAML-strängar + ~28 DisplayAlert × 4 args = ~340 strängar.

---

## Common-katalog (delas av alla tasks)

Task 0 etablerar dessa Common-nycklar. Senare tasks återanvänder dem istället för att skapa egna duplikat:

| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Common_Cancel` | "Avbryt" | "Cancel" | (finns sedan Plan 1) |
| `Common_Skip` | "Hoppa över" | "Skip" | (finns sedan Plan 1) |
| `Common_OK` | "OK" | "OK" |
| `Common_Save` | "Spara" | "Save" |
| `Common_Delete` | "Ta bort" | "Delete" |
| `Common_Add` | "Lägg till" | "Add" |
| `Common_Edit` | "Redigera" | "Edit" |
| `Common_Done` | "Klar" | "Done" |
| `Common_Close` | "Stäng" | "Close" |
| `Common_Back` | "Tillbaka" | "Back" |
| `Common_Yes` | "Ja" | "Yes" |
| `Common_No` | "Nej" | "No" |
| `Common_Continue` | "Fortsätt" | "Continue" |
| `Common_Undo` | "Ångra" | "Undo" |
| `Common_Loading` | "Laddar..." | "Loading..." |
| `Common_Error` | "Fel" | "Error" |
| `Common_Confirm` | "Bekräfta" | "Confirm" |

**Återanvändning är obligatorisk.** Om en task behöver "Spara"/"Save", använd `Common_Save` — skapa inte `Hem_Save`, `Settings_Save` etc. Reviewer flaggar duplikat.

---

## Per-sida-process (gäller alla tasks)

Varje per-sida-task följer denna process. Steps i task-mallen är konkretiseringar av detta.

1. **Identifiera strängar.** Använd `grep` för att lista alla användarsynliga strängar i XAML och VM:
   ```bash
   grep -nE '(Text|Placeholder|Title)="[A-ZÅÄÖa-zåäö0-9][^"]*"' LockIn/Views/<Page>.xaml
   grep -nE '"[A-ZÅÄÖa-zåäö][^"]{2,}"' LockIn/ViewModels/<Page>ViewModel.cs
   ```
2. **Filtrera bort tekniska strängar.** Inte alla strängar ska översättas:
   - Programs-IDs (`"startingstrength"`, `"ppl"`) — tekniska, behåll
   - Hex-färger (`"#4ADE80"`) — utanför scope (Colors.xaml gäller)
   - Format-specifiers (`"d MMM yyyy"`, `"F1"`) — kultur-driven av .NET, behåll
   - Ikon-textsymboler (`"✓"`, `"→"`, `"·"`) — språkneutrala glyfer, behåll
   - Property-namn / nyckelnamn i ord-konfigurationer — tekniska, behåll
   - SINGLE-tecken numeriska labels (`"2"`, `"3"`) — språkneutrala, behåll
3. **Definiera nyckelnamn** per `<Prefix>_<Element>`-konvention. Exempel:
   - `Text="VECKANS POÄNG"` → `Hem_WeeklyScoreLabel`
   - `Text="Aktiva program"` → `Hem_ActiveProgramsHeader`
   - `Text="Lägg till mått"` → `Common_Add` (återanvänd Common, INTE `Kropp_AddMeasurement`)
4. **Lägg till nycklarna i båda resx-filerna** med exakt samma namn. Engelska översättningarna gör du själv — använd lyrikfri, naturlig engelska som matchar appens ton (kort, direkt, gymvärldsspråk där det stämmer).
5. **Lägg till motsvarande C# property i AppResources.cs** under en `// ── <Prefix> ─` kommentar-rad. Format:
   ```csharp
   public static string Hem_WeeklyScoreLabel => Get(nameof(Hem_WeeklyScoreLabel));
   ```
6. **Byt XAML strängar.** Lägg till `xmlns:loc="clr-namespace:LockIn.Resources.Strings"` i rotelementet om saknas. Byt sedan:
   ```xml
   Text="VECKANS POÄNG" → Text="{loc:Localize Hem_WeeklyScoreLabel}"
   ```
7. **Byt VM strängar.** Lägg till `using LockIn.Resources.Strings;` om saknas. Byt:
   ```csharp
   await Shell.Current.DisplayAlert("Bekräfta", $"Ta bort {namn}?", "Ta bort", "Avbryt");
   ```
   till:
   ```csharp
   await Shell.Current.DisplayAlert(
       AppResources.Common_Confirm,
       string.Format(AppResources.Kropp_DeleteMeasurementBody, namn),
       AppResources.Common_Delete,
       AppResources.Common_Cancel);
   ```
8. **Pluralisering där det märks.** Om text säger "1 set" vs "2 set", skapa `<Prefix>_<Thing>_One` + `<Prefix>_<Thing>_Many` och VM väljer:
   ```csharp
   var key = count == 1 ? AppResources.Hem_SessionsLeft_One : AppResources.Hem_SessionsLeft_Many;
   var text = string.Format(key, count);
   ```
9. **Bygg.** `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` — exit 0, 0 errors.
10. **Grep-verifiering.** Båda grep-kommandona från Step 1 ska nu returnera ENDAST `{loc:Localize ...}` / `AppResources.X` / tekniska strängar. Inkludera output i implementer-rapporten.
11. **Commit.** `git commit -m "feat(i18n): translate <Page>"`.

---

## Common phrases att vara observant på

Återkommande svenska fraser och deras föreslagna engelska översättningar (för konsistens över sidor):

| Svenska | Engelska | Notering |
|---------|----------|----------|
| "Pass" (substantiv) | "Workout" eller "session" | Kontext: "logga ett pass" → "log a workout", "X pass kvar" → "X sessions left" |
| "Övning" | "Exercise" | |
| "Set" | "Set" (samma) | |
| "Reps" / "Repetitioner" | "Reps" | |
| "Vikt" | "Weight" | |
| "Mall" | "Template" | |
| "Program" | "Program" (samma) | |
| "Kropp" | "Body" | |
| "Bibliotek" | "Library" | |
| "Träna" | "Train" (verb) eller "Training" | |
| "Historik" | "History" | |
| "Hem" | "Home" | |
| "Vecka" | "Week" | |
| "Vilotimer" | "Rest timer" | |
| "Logga" (verb) | "Log" | |
| "Streak" | "Streak" (samma) | |
| "Mått" | "Measurement" | |
| "PR" / "Personrekord" | "PR" / "Personal record" | |
| "Achievement" / "Bedrift" | "Achievement" | |
| "Återhämtning" | "Recovery" | |
| "Sömn" | "Sleep" | |
| "Strain" | "Strain" (samma) | |

Använd dessa konsekvent. När en task skapar en ny nyckel, kolla först om de återkommande termerna redan finns i `Common_`-katalogen eller en närliggande sidas prefix.

---

## Task-mall (kort referens)

Varje per-sida-task har följande struktur (filerna varierar):

```
- [ ] Step 1: Identifiera strängar (grep XAML + VM)
- [ ] Step 2: Definiera nyckelnamn (per konvention)
- [ ] Step 3: Lägg till sv-värden i AppResources.resx
- [ ] Step 4: Lägg till en-värden i AppResources.en.resx
- [ ] Step 5: Lägg till properties i AppResources.cs
- [ ] Step 6: Byt XAML strängar till {loc:Localize ...}
- [ ] Step 7: Byt VM strängar till AppResources.X (om VM rörs)
- [ ] Step 8: Bygg + 0 errors
- [ ] Step 9: Grep-verifiera inga kvarvarande svenska strängar
- [ ] Step 10: Commit
```

---

### Task 0: AppShell tab-titlar + Common-katalog

Etablera Common-baslager och översätt 5 tab-titlarna i `AppShell.xaml`. Eftersom Common-nycklarna behövs av flertalet senare tasks gör vi dem upfront.

**Files:**
- Modify: `LockIn/AppShell.xaml`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: `LocalizeExtension` (Plan 1), `AppResources.Get(string)` (Plan 1)
- Produces: 5 `Tab_` nycklar + 15 nya `Common_` nycklar. Återanvänds av alla senare tasks.

- [ ] **Step 1: Lägg till 15 nya Common-nycklar i AppResources.resx**

Lägg till följande efter de befintliga `Common_Cancel` / `Common_Skip` i `LockIn/Resources/Strings/AppResources.resx`:

```xml
  <data name="Common_OK" xml:space="preserve"><value>OK</value></data>
  <data name="Common_Save" xml:space="preserve"><value>Spara</value></data>
  <data name="Common_Delete" xml:space="preserve"><value>Ta bort</value></data>
  <data name="Common_Add" xml:space="preserve"><value>Lägg till</value></data>
  <data name="Common_Edit" xml:space="preserve"><value>Redigera</value></data>
  <data name="Common_Done" xml:space="preserve"><value>Klar</value></data>
  <data name="Common_Close" xml:space="preserve"><value>Stäng</value></data>
  <data name="Common_Back" xml:space="preserve"><value>Tillbaka</value></data>
  <data name="Common_Yes" xml:space="preserve"><value>Ja</value></data>
  <data name="Common_No" xml:space="preserve"><value>Nej</value></data>
  <data name="Common_Continue" xml:space="preserve"><value>Fortsätt</value></data>
  <data name="Common_Undo" xml:space="preserve"><value>Ångra</value></data>
  <data name="Common_Loading" xml:space="preserve"><value>Laddar...</value></data>
  <data name="Common_Error" xml:space="preserve"><value>Fel</value></data>
  <data name="Common_Confirm" xml:space="preserve"><value>Bekräfta</value></data>

  <!-- Tab-titlar -->
  <data name="Tab_Home" xml:space="preserve"><value>Hem</value></data>
  <data name="Tab_Train" xml:space="preserve"><value>Träna</value></data>
  <data name="Tab_History" xml:space="preserve"><value>Historik</value></data>
  <data name="Tab_Library" xml:space="preserve"><value>Bibliotek</value></data>
  <data name="Tab_Body" xml:space="preserve"><value>Kropp</value></data>
```

- [ ] **Step 2: Lägg till motsvarande engelska värden i AppResources.en.resx**

```xml
  <data name="Common_OK" xml:space="preserve"><value>OK</value></data>
  <data name="Common_Save" xml:space="preserve"><value>Save</value></data>
  <data name="Common_Delete" xml:space="preserve"><value>Delete</value></data>
  <data name="Common_Add" xml:space="preserve"><value>Add</value></data>
  <data name="Common_Edit" xml:space="preserve"><value>Edit</value></data>
  <data name="Common_Done" xml:space="preserve"><value>Done</value></data>
  <data name="Common_Close" xml:space="preserve"><value>Close</value></data>
  <data name="Common_Back" xml:space="preserve"><value>Back</value></data>
  <data name="Common_Yes" xml:space="preserve"><value>Yes</value></data>
  <data name="Common_No" xml:space="preserve"><value>No</value></data>
  <data name="Common_Continue" xml:space="preserve"><value>Continue</value></data>
  <data name="Common_Undo" xml:space="preserve"><value>Undo</value></data>
  <data name="Common_Loading" xml:space="preserve"><value>Loading...</value></data>
  <data name="Common_Error" xml:space="preserve"><value>Error</value></data>
  <data name="Common_Confirm" xml:space="preserve"><value>Confirm</value></data>

  <!-- Tab titles -->
  <data name="Tab_Home" xml:space="preserve"><value>Home</value></data>
  <data name="Tab_Train" xml:space="preserve"><value>Train</value></data>
  <data name="Tab_History" xml:space="preserve"><value>History</value></data>
  <data name="Tab_Library" xml:space="preserve"><value>Library</value></data>
  <data name="Tab_Body" xml:space="preserve"><value>Body</value></data>
```

- [ ] **Step 3: Lägg till properties i AppResources.cs**

Utöka `// ── Common ──` blocket. Lägg sedan till ett nytt block:

```csharp
    public static string Common_OK       => Get(nameof(Common_OK));
    public static string Common_Save     => Get(nameof(Common_Save));
    public static string Common_Delete   => Get(nameof(Common_Delete));
    public static string Common_Add      => Get(nameof(Common_Add));
    public static string Common_Edit     => Get(nameof(Common_Edit));
    public static string Common_Done     => Get(nameof(Common_Done));
    public static string Common_Close    => Get(nameof(Common_Close));
    public static string Common_Back     => Get(nameof(Common_Back));
    public static string Common_Yes      => Get(nameof(Common_Yes));
    public static string Common_No       => Get(nameof(Common_No));
    public static string Common_Continue => Get(nameof(Common_Continue));
    public static string Common_Undo     => Get(nameof(Common_Undo));
    public static string Common_Loading  => Get(nameof(Common_Loading));
    public static string Common_Error    => Get(nameof(Common_Error));
    public static string Common_Confirm  => Get(nameof(Common_Confirm));

    // ── Tab titles ─────────────────────────────────────────────────────
    public static string Tab_Home    => Get(nameof(Tab_Home));
    public static string Tab_Train   => Get(nameof(Tab_Train));
    public static string Tab_History => Get(nameof(Tab_History));
    public static string Tab_Library => Get(nameof(Tab_Library));
    public static string Tab_Body    => Get(nameof(Tab_Body));
```

- [ ] **Step 4: Modifiera AppShell.xaml**

Lägg till `xmlns:loc="clr-namespace:LockIn.Resources.Strings"` på `<Shell>`-rotelementet. Byt sedan varje `Title="<svenska>"` på `ShellContent`-elementen:

```xml
<ShellContent Title="{loc:Localize Tab_Home}"    Icon="tab_home.png"    Route="HemPage"     ContentTemplate="{DataTemplate views:HemPage}" />
<ShellContent Title="{loc:Localize Tab_Train}"   Icon="tab_train.png"   Route="TrainPage"   ContentTemplate="{DataTemplate views:TrainPage}" />
<ShellContent Title="{loc:Localize Tab_History}" Icon="tab_history.png" Route="HistoryPage" ContentTemplate="{DataTemplate views:HistoryPage}" />
<ShellContent Title="{loc:Localize Tab_Library}" Icon="tab_library.png" Route="LibraryPage" ContentTemplate="{DataTemplate views:LibraryPage}" />
<ShellContent Title="{loc:Localize Tab_Body}"    Icon="ic_ruler.png"    Route="KroppPage"   ContentTemplate="{DataTemplate views:KroppPage}" />
```

OBS: `Shell`-rotens `Title="LockIn"` är app-namnet, lämna det som är.

- [ ] **Step 5: Bygg**

`dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` — exit 0, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add LockIn/AppShell.xaml \
        LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(i18n): translate AppShell tab titles + add Common catalog"
```

---

### Task 1: HemPage + HemViewModel

**Files:**
- Modify: `LockIn/Views/HemPage.xaml` (~24 strängar)
- Modify: `LockIn/ViewModels/HemViewModel.cs` (Toast / format-strängar, inkl. `BuildMotivationText`)
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Hem_`

**Speciellt:**
- `HemViewModel.BuildMotivationText` har 6 svenska format-strängar baserat på score-tier (>=100, >=75, etc.) — översätt alla 6 + skapa parameteriserade nycklar (`Hem_Motivation_Complete`, `_Strong`, `_Halfway`, `_Going`, `_Started`, `_New`).
- Streak-text kan ha "X dagars streak" vs "1 dags streak" → `Hem_StreakDays_One` / `_Many`.
- Pass kvar i veckan om "1 pass kvar" vs "X pass kvar" — pluralisera.
- "RECOVERY-RING", "STRAIN", "SLEEP" och liknande är på engelska redan eller versaler — verifiera i XAML.

Följ per-sida-processen ovan. Steps 1-10 i task-mallen.

- [ ] **Step 1**: `grep -nE '(Text|Placeholder|Title)="[A-ZÅÄÖa-zåäö0-9][^"]*"' LockIn/Views/HemPage.xaml` och `grep -nE '"[A-ZÅÄÖa-zåäö][^"]{2,}"' LockIn/ViewModels/HemViewModel.cs`
- [ ] **Step 2**: Definiera nyckelnamn per konvention. Inkludera de 6 motivationsvarianterna + streak-pluralisering.
- [ ] **Step 3**: Lägg till sv-värden i `AppResources.resx` under `// ── Hem ─` block.
- [ ] **Step 4**: Lägg till en-värden i `AppResources.en.resx` (samma nycklar). Översätt själv — använd common-phrases-tabellen.
- [ ] **Step 5**: Lägg till C# properties i `AppResources.cs`.
- [ ] **Step 6**: Lägg till `xmlns:loc` i HemPage.xaml om saknas. Byt strängar till `{loc:Localize ...}`.
- [ ] **Step 7**: Byt VM-strängar till `AppResources.X`. `BuildMotivationText` returnerar nu rätt nyckel baserat på score + `string.Format(nyckel, score)`.
- [ ] **Step 8**: Bygg — 0 errors.
- [ ] **Step 9**: Grep — endast `{loc:Localize ...}` / `AppResources.X` / tekniska strängar kvarstår.
- [ ] **Step 10**: Commit `feat(i18n): translate HemPage and HemViewModel`.

---

### Task 2: TrainPage + TrainViewModel

**Files:**
- Modify: `LockIn/Views/TrainPage.xaml` (~17 strängar)
- Modify: `LockIn/ViewModels/TrainViewModel.cs` (DisplayAlert/Toast om finns)
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Train_`

**Speciellt:**
- "STARTA PASS" / "FRITT PASS" / "DAGENS PASS" — kärna-CTA, viktig konsistens med PostWorkoutPage's "AVSLUTA PASS" (Task 7).
- Programsektioner ("Inget aktivt program", "Ditt aktiva program") — kolla pluralisering.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate TrainPage and TrainViewModel`.

---

### Task 3: HistoryPage + HistoryViewModel

**Files:**
- Modify: `LockIn/Views/HistoryPage.xaml` (~14 strängar)
- Modify: `LockIn/ViewModels/HistoryViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `History_`

**Speciellt:**
- Pill-knappar (Vecka/Månad/År eller liknande) — översätt konsekvent. Tab-pill-konsistens med LibraryPage.
- Tomma states ("Inga pass än", "Logga ditt första pass") — vanligt i flera sidor, men håll dessa under `History_` (inte `Common_`) — varje sida har sin egen tomma-state-formulering.
- Datum-format styrs av kultur (Plan 3) — inte denna task.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate HistoryPage and HistoryViewModel`.

---

### Task 4: LibraryPage + LibraryViewModel

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml` (~21 strängar)
- Modify: `LockIn/ViewModels/LibraryViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Library_`

**Speciellt:**
- 3 pill-tabbar ÖVNINGAR / MALLAR / PROGRAM — `Library_Tab_Exercises` / `_Templates` / `_Programs`.
- Sektion-rubriker "Skapa ny mall", "Skapa ny övning" — använd `Common_Add` + `Library_Template` / `Library_Exercise` om naturligt, ELLER skapa fulltext `Library_CreateTemplate` om det blir för bitvis. Bevara mening över bokstavlig återanvändning.
- "BIBLIOTEK" sticky-header — `Library_Title`.
- Filter-chips (Equipment-typer som "Skivstång", "Hantel" etc.) — om dessa är hårdkodade i ViewModel, det är teknisk-domän-data; lägg till nycklar `Library_Equipment_<typ>` ELLER skjut upp till Plan 3 om de kommer från enum. Verifiera i diff vad du hittar.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate LibraryPage and LibraryViewModel`.

---

### Task 5: KroppPage + KroppViewModel

**Files:**
- Modify: `LockIn/Views/KroppPage.xaml` (~21 strängar)
- Modify: `LockIn/ViewModels/KroppViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Kropp_`

**Speciellt:**
- "KROPP" → `Kropp_Title` ("BODY" på engelska).
- Mått-typer ("Bröst", "Midja", "Höft", "Lår", "Vad", "Arm", "Hals") — varje får en nyckel `Kropp_Measurement_<typ>`. Verifiera i XAML/VM hur de listas (kan vara enum eller stränglista).
- "Lägg till mått" → använd `Common_Add` + lokal kontextnyckel, ELLER skapa `Kropp_AddMeasurement` om frasen alltid kommer som ett helt.
- DisplayAlert-strängarna i `KroppViewModel.cs` rad ~170 ("Ta bort mått?", body med format, "Ta bort", "Avbryt").
- Vikt-rad (om finns) använder kanske `BodyWeight_*` — överlappar med Task 15.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate KroppPage and KroppViewModel`.

---

### Task 6: ActiveWorkoutPage + ActiveWorkoutViewModel

**Files:**
- Modify: `LockIn/Views/ActiveWorkoutPage.xaml` (~6 XAML-strängar — mycket bindas via VM)
- Modify: `LockIn/ViewModels/ActiveWorkoutViewModel.cs` (3 DisplayAlerts plus banner/toast-strängar)
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `ActiveWorkout_`

**Speciellt:**
- Detta är appens hjärta. Många strängar är dynamiska från ViewModel (set nr, vikt, reps). Översätt format-strängar varsamt.
- DisplayAlerts:
  - Rad 197: bekräfta-dialog när användaren tar bort övning. Format: `"Ta bort {0}? {1} loggade set försvinner."` (eller liknande efter undo-task) → `ActiveWorkout_RemoveTitle` / `_RemoveBodyWithLogs` / `_RemoveBodyEmpty` (use_One/Many för sets) + `Common_Delete` + `Common_Cancel`.
  - Rad 529: övningsinfo-popup (visar namn + beskrivning). Titel = `section.ExerciseName` (binda till data, ej översätt). Body = `section.ExerciseDescription` (svensk text från databasen — Plan 3 hanterar databasdata). Knapp = `Common_OK`.
  - Rad 556: confirmera-avsluta-pass eller liknande — översätt enligt vad du hittar.
- PR-banner och auto-progress-banner: dessa är XAML-strängar eller Text-bindningar från VM? Verifiera.
- "AVSLUTA PASS" CTA-knapp — `ActiveWorkout_FinishWorkout`.
- Vilotimer-relaterad text (om visas i XAML, ej notification — det är Plan 3).
- "SET" / "VIKT" / "REPS" kolumnrubriker — `ActiveWorkout_Col_Set` / `_Col_Weight` / `_Col_Reps`.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate ActiveWorkoutPage and ActiveWorkoutViewModel`.

---

### Task 7: PostWorkoutPage + PostWorkoutViewModel

**Files:**
- Modify: `LockIn/Views/PostWorkoutPage.xaml` (~13 strängar)
- Modify: `LockIn/ViewModels/PostWorkoutViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `PostWorkout_`

**Speciellt:**
- "BRA JOBBAT!" / "PASS KLART" celebration-text — översätt med tonen kvar ("GREAT WORK!" / "WORKOUT COMPLETE").
- Achievement-triggers — om popup visar achievement-namn här, det binder till data (Plan 3 hanterar achievement-strängar). XAML-skalet är denna task.
- Stats-sammanfattning ("Du lyfte X kg totalt", "Y set loggade") — pluralisera där "1 vs N" syns.
- "DELA" / "STÄNG" / "SE HISTORIK"-knappar.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate PostWorkoutPage and PostWorkoutViewModel`.

---

### Task 8: SessionDetailPage + SessionDetailViewModel

**Files:**
- Modify: `LockIn/Views/SessionDetailPage.xaml` (~9 strängar)
- Modify: `LockIn/ViewModels/SessionDetailViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `SessionDetail_`

**Speciellt:**
- Rubriker "Total vikt", "Antal set", "Längd", "Datum" — kolumnnamn för stats.
- DisplayAlert: "Ta bort pass?" / body / `Common_Delete` / `Common_Cancel`.
- Övningsnamn binds från data → ej översätt.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate SessionDetailPage and SessionDetailViewModel`.

---

### Task 9: ExercisePickerPage + ExercisePickerViewModel

**Files:**
- Modify: `LockIn/Views/ExercisePickerPage.xaml` (~5 XAML-strängar)
- Modify: `LockIn/ViewModels/ExercisePickerViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `ExercisePicker_`

**Speciellt:**
- Sökruta-placeholder "Sök övning..." → `ExercisePicker_SearchPlaceholder` / "Search exercise...".
- Filterchips för muskelgrupper — om bundna från enum, Plan 3 hanterar enum-översättningar. Verifiera.
- "Lägg till egen övning"-CTA → använd `Common_Add` ELLER `ExercisePicker_CreateCustom` om hel fras.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate ExercisePickerPage and ExercisePickerViewModel`.

---

### Task 10: TemplateEditPage + TemplateEditViewModel

**Files:**
- Modify: `LockIn/Views/TemplateEditPage.xaml` (~16 strängar)
- Modify: `LockIn/ViewModels/TemplateEditViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `TemplateEdit_`

**Speciellt:**
- Sektioner "Namn", "Övningar", "Spara mall".
- DisplayAlert: "Ta bort mall?" / "Spara ändringar?".
- Placeholder för mall-namn Entry.
- Knappar "Lägg till övning" — kan använda `Common_Add` + sub-nyckel ELLER fullt `TemplateEdit_AddExercise`.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate TemplateEditPage and TemplateEditViewModel`.

---

### Task 11: ExerciseProgressPage + ExerciseProgressViewModel

**Files:**
- Modify: `LockIn/Views/ExerciseProgressPage.xaml` (~16 strängar)
- Modify: `LockIn/ViewModels/ExerciseProgressViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `ExerciseProgress_`

**Speciellt:**
- Grafrubriker "1RM", "Volym", "Tonnage", "Max vikt" — översätt eller behåll förkortningar som de är (1RM, volume).
- Tidsperiod-pillar (1V, 1M, 3M, 1Å, eller liknande) — bevara förkortningar men nyckla dem: `ExerciseProgress_Range_Week` osv.
- Metadata-sektion (utrustning, primär muskelgrupp) — om data är enum-baserat, skjut upp värdena till Plan 3 och översätt bara LABELS här.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate ExerciseProgressPage and ExerciseProgressViewModel`.

---

### Task 12: CreateExercisePage + CreateExerciseViewModel

**Files:**
- Modify: `LockIn/Views/CreateExercisePage.xaml` (~11 strängar)
- Modify: `LockIn/ViewModels/CreateExerciseViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `CreateExercise_`

**Speciellt:**
- Form-fält ("Namn", "Muskelgrupp", "Utrustning", "Beskrivning") — labels.
- Placeholder-texter.
- "Spara" / "Avbryt" → `Common_Save` / `Common_Cancel`.
- Validation-meddelanden i VM ("Namn krävs", "Välj muskelgrupp") — om finns.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate CreateExercisePage and CreateExerciseViewModel`.

---

### Task 13: ProgramDetailPage + ProgramDetailViewModel

**Files:**
- Modify: `LockIn/Views/ProgramDetailPage.xaml` (~2 strängar)
- Modify: `LockIn/ViewModels/ProgramDetailViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `ProgramDetail_`

**Speciellt:**
- Liten sida. Programnamn och beskrivning binds från `WorkoutPrograms` data (Plan 3 hanterar beskrivning, namnet lämnas).
- "AKTIVERA PROGRAM" / "DEAKTIVERA" CTA-knappar.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate ProgramDetailPage and ProgramDetailViewModel`.

---

### Task 14: SettingsPage + SettingsViewModel

**Files:**
- Modify: `LockIn/Views/SettingsPage.xaml` (~24 strängar)
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Settings_`

**Speciellt:**
- Den största sidan tillsammans med HemPage.
- Sektioner: Användarprofil, Träningsmål, Notiser, Ljud, Vibration, Tema, Datahantering, Om appen, etc.
- Varje toggle/inställning har en label + ev. en beskrivning. Två nycklar per inställning: `Settings_<Setting>_Title` / `_Description`.
- "Rensa data"-DisplayAlert (om finns) — viktiga varningar.
- "Återställ"-knappar — använd `Common_Undo` ELLER skapa `Settings_Reset` om mer kontextuellt.
- Appversionsnummer binds från reflection — ej översätt.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate SettingsPage and SettingsViewModel`.

---

### Task 15: BodyWeightPage + BodyWeightViewModel

**Files:**
- Modify: `LockIn/Views/BodyWeightPage.xaml` (~6 strängar)
- Modify: `LockIn/ViewModels/BodyWeightViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `BodyWeight_`

**Speciellt:**
- "VIKT" rubrik → `BodyWeight_Title`.
- Entry-placeholder "Vikt (kg)" — `BodyWeight_EntryPlaceholder`. (kg-enhet är universell, behöll.)
- DisplayAlert rad ~79: "Ta bort vikt?" / format-body / `Common_Delete` / `Common_Cancel`.
- Datum-format styrs av kultur (Plan 3).

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate BodyWeightPage and BodyWeightViewModel`.

---

### Task 16: AchievementsPage + AchievementsViewModel

**Files:**
- Modify: `LockIn/Views/AchievementsPage.xaml` (~1 sträng — header)
- Modify: `LockIn/ViewModels/AchievementsViewModel.cs` (om har strängar)
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `Achievements_`

**Speciellt:**
- LITEN sida. Bara UI-kromen — achievement-namn och beskrivningar lagras i `AchievementService.cs` och översätts i Plan 3.
- "BEDRIFTER" / "PRESTATIONER" → `Achievements_Title` ("ACHIEVEMENTS").
- Tom-state ("Inga bedrifter än", "Logga ditt första pass för att låsa upp") — `Achievements_Empty_*`.
- "Låst" / "Upplåst" badge-text om finns → `Achievements_Locked` / `_Unlocked`.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate AchievementsPage UI chrome`.

---

### Task 17: PlateCalculatorPage + PlateCalculatorViewModel

**Files:**
- Modify: `LockIn/Views/PlateCalculatorPage.xaml` (~15 strängar)
- Modify: `LockIn/ViewModels/PlateCalculatorViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `PlateCalculator_`

**Speciellt:**
- "VIKTRÄKNARE" → `PlateCalculator_Title`.
- "Skivstångsvikt", "Önskad totalvikt" — input-labels.
- "Skivor per sida" → `PlateCalculator_PlatesPerSide`.
- Plate-storleksrad (20kg, 15kg, 10kg, 5kg, 2.5kg, 1.25kg) — siffror är språkneutrala, "kg" är universell. Ingen översättning för rena tal.
- Format-strängar för "2 × 20 kg + 1 × 5 kg" — Plan 3 hanterar kultur, denna task lämnar matematiken.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate PlateCalculatorPage and PlateCalculatorViewModel`.

---

### Task 18: ProgressPhotosPage + ProgressPhotosViewModel

**Files:**
- Modify: `LockIn/Views/ProgressPhotosPage.xaml` (~3 strängar)
- Modify: `LockIn/ViewModels/ProgressPhotosViewModel.cs`
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}`

**Nyckel-prefix:** `ProgressPhotos_`

**Speciellt:**
- "PROGRESSBILDER" → `ProgressPhotos_Title`.
- Knappar "Ta foto" / "Välj från bibliotek" → `ProgressPhotos_TakePhoto` / `_FromLibrary`.
- DisplayAlert "Ta bort foto?" — `ProgressPhotos_DeletePhotoTitle` + body + `Common_Delete` + `Common_Cancel`.
- Tom-state.

Följ per-sida-processen. Steps 1-10.

- [ ] **Step 1-10** per task-mall.
- [ ] Commit: `feat(i18n): translate ProgressPhotosPage and ProgressPhotosViewModel`.

---

### Task 19: App-wide audit + finishing

Sista task: hitta och åtgärda återstående hårdkodade strängar app-wide, samt finalisera Plan 2.

**Files:**
- Audit: alla `LockIn/Views/*.xaml`
- Audit: alla `LockIn/ViewModels/*.cs`
- Audit: alla `LockIn/Services/*.cs` och `LockIn/Controls/*.cs` (kan ha DisplayAlert)
- Modify: eventuella missade filer
- Modify: `LockIn/LockIn.csproj` (version bump)
- Modify: `LockIn/Resources/Strings/AppResources.{resx, en.resx, cs}` (om missade strängar hittas)

- [ ] **Step 1: Audit alla XAML-sidor**

```bash
for f in LockIn/Views/*.xaml LockIn/AppShell.xaml; do
  echo "=== $f ==="
  grep -nE '(Text|Placeholder|Title|ToolTip|AutomationId)="[A-ZÅÄÖa-zåäö][^"]{2,}"' "$f" | grep -v '{loc:Localize'
done
```

Förväntat: Endast tekniska bindningar (`{Binding ...}`) och numeriska/symboliska strängar. Inga svenska fraser.

- [ ] **Step 2: Audit alla ViewModels och Services för Display/Toast/string-strängar**

```bash
grep -rnE '"[A-ZÅÄÖa-zåäö][^"]{4,}"' LockIn/ViewModels/ LockIn/Services/ LockIn/Controls/ | \
  grep -v 'AppResources\.' | \
  grep -vE '"(startingstrength|fullbody|texasmethod|upperlower|531bbb|ppl|en|sv|kg|d MMM yyyy|F[0-9])"' | \
  grep -v '// '
```

Förväntat: Endast tekniska strängar (program-IDs, kultur-koder, format-specifiers). Inga svenska användarsynliga fraser.

- [ ] **Step 3: Åtgärda eventuella missade strängar**

För varje missad sträng:
- Lägg till nyckel i `AppResources.resx` + `.en.resx` + `.cs`
- Använd lämpligt prefix (det relevanta sidans, ELLER `Common_` om cross-cutting).
- Byt referenser i kod.

- [ ] **Step 4: MuscleGroup / Equipment enum-värden**

Om steg 1-2 visar att enum-värden (t.ex. `MuscleGroup.Chest.ToString()`) visas direkt i UI:t — det är teknisk data som Plan 3 ska översätta. Lämna oförändrat OM bara `.ToString()`-mönstret används.

Om det finns en HÅRDKODAD svensk lista (t.ex. `var muscleNames = new[] { "Bröst", "Rygg", ... }`) — det är UI-kod och denna task ska översätta den. Lägg till nycklar `MuscleGroup_Chest`, `MuscleGroup_Back` etc. (även om Plan 3 senare gör samma sak för achievement-data).

- [ ] **Step 5: Slutbygge**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED, 0 errors, INTE fler warnings än Plan 1's baseline (106 CS0618).

- [ ] **Step 6: Bump ApplicationVersion 53 → 54**

I `LockIn/LockIn.csproj`:
```xml
<ApplicationVersion>54</ApplicationVersion>
```

- [ ] **Step 7: Commit + slut-rapport**

```bash
git add -A
git commit -m "feat(i18n): plan 2 complete — UI text translated on all 18 pages

App-wide audit complete. ApplicationVersion bumped to 54.
Manual simulator verification (sv + en locale) pending on Mac."
```

- [ ] **Step 8: Sammanfatta för användaren**

I implementer-rapport: lista totalt antal nycklar i resx-filerna efter Plan 2 är klar, antal commits i denna plan, och uppskattat täckning av spec'ens acceptance criteria §7 punkter (utom de som hör till Plan 3 — statisk data + notiser).

---

## Verifierings-checklista (efter Task 19)

- [ ] `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` → 0 errors, ≤106 warnings
- [ ] App-wide grep efter `(Text|Placeholder|Title)="[A-ZÅÄÖa-zåäö]"` i alla XAML returnerar endast `{Binding ...}` / `{loc:Localize ...}` / tekniska/symboliska strängar
- [ ] App-wide grep efter `"[A-ZÅÄÖa-zåäö][^"]{4,}"` i alla `.cs` returnerar endast tekniska strängar (program-IDs, kultur-koder, format-specifiers, kommentarer)
- [ ] `AppResources.resx` och `.en.resx` har identiska key-set (verifiera med `diff <(grep '<data name' resx) <(grep '<data name' en.resx)`)
- [ ] `AppResources.cs` har en property för varje nyckel i resx
- [ ] `ApplicationVersion` i `LockIn.csproj` bumpad till 54
- [ ] Manuell simulator-verifiering (Mac) UTFÖRD eller schemalagd

---

## Vad som NOT ingår i Plan 2

För att hålla scope tight:

- **Övningsnamn** ("Bänkpress", "Marklyft") — stannar svenska. Spec §3.1.
- **Programnamn** — engelska redan ("Starting Strength", "Push/Pull/Legs"). Spec §3.2.
- **Programbeskrivningar** i `WorkoutPrograms.cs` — Plan 3.
- **Achievement-namn och -beskrivningar** i `AchievementService.cs` — Plan 3.
- **Notification-strängar** i `Plugin.LocalNotification`-anrop — Plan 3.
- **Tal- och datumformat** (`InvariantCulture` → `CurrentCulture`) — Plan 3.
- **Enum-värdens UI-visning** om det är via `.ToString()`-mönster — Plan 3.
- **Push till git** — användaren beslutar (per `feedback_git_push`).

---

## Risker och mitigering

| Risk | Mitigering |
|------|-----------|
| Nyckelnamn-kollisioner när 19 sidor lägger till keys samtidigt | Strikt `<Prefix>_`-konvention + reviewer kontrollerar duplikat per task. Final audit (Task 19) jämför resx + .en.resx key-sets. |
| Engelsk översättning blir längre och bryter layout | Manuell simulator-verifiering med engelskt locale efter planen. Implementer noterar misstänkta overflow-risker i sin rapport. |
| Implementer-subagent gör fri översättning som divergerar från common-phrases-tabellen | Reviewer-prompt instruerar att kontrollera tabellen + se varje task's diff mot tabellen. |
| `string.Format`-argumentordning swappas mellan språk | Reviewer kontrollerar att `{0}` och `{1}` representerar samma värden i båda språk. |
| Pluralisering missas | Final audit (Task 19) kör `grep` efter "pass", "set", "övning", "mått", "dagar" i resx. Alla nycklar där "1 vs N" finns ska ha `_One`/`_Many`. |
| Hand-skriven `AppResources.cs` blir ohanterligt med 500+ properties | Acceptera för Plan 2. Refactoring (kanske `T(string key)`-helper) noteras som uppföljning efter Plan 3. |
| Tab-titlar reagerar inte på locale-byte runtime | Acceptera krav på app-restart (spec §6). |
| ViewModel-bindade Text-properties (t.ex. `MotivationText` i HemViewModel) behöver omberäknas vid locale-byte | Spec accepterar app-restart för språkbyte (§1.4) — täcker även detta. |
