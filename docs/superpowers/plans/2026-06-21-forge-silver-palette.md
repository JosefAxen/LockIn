# Forge Silver Palette Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Byt ut LockIn:s primära gröna brand-färg (#4ADE80) mot en metallisk silver-palett som matchar app-ikonen och splash-screenens "L"-logga. Behåll amber och lila som sekundära accents (för Strain respektive Recovery), gör hela appen mer Bevel-konsekvent och premium. Lätt att backa helt om det inte passar.

**Architecture:** Color-tokens definieras på två ställen — `Resources/Styles/Colors.xaml` (XAML) och `DesignTokens.cs` (C#). Övriga 33 förekomster av `#4ADE80` är hårdkodade hex i ViewModels/Controls/XAML och måste hittas manuellt. Vi behåller `ForgeSuccess` som grön (semantisk "success" för completed states, set-checkmark, calendar trained-day har egen behandling). Vi behåller också muskelgrupp-färgen för Biceps som grön (informationsdesign, inte brand). Alla brand-CTAs (FRITT PASS, SPARA, AKTIVERA, AVSLUTA) byts till silver gradient med glow shadow.

**Tech Stack:** .NET MAUI 10 XAML, SkiaSharp 3.116.1 (för Controls/), CommunityToolkit.Mvvm.

## Global Constraints

- Inga nya NuGet-paket
- Bevara `ForgeSuccess` (semantisk grön) och Biceps muskelfärg som grön
- Endast brand-relaterade gröna byts
- Plattform: `net10.0-ios`
- Bump `ApplicationVersion` 15 → 16 innan slutpush
- Build verifiering: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` ska ge 0 errors per task

---

## Rollback-strategi

Tre lager av säkerhetsnät — om du inte gillar Forge Silver kan vi backa hela ändringen säkert.

**Lager 1 — Git tag som baseline:**
Innan Task 1 körs skapas en annoterad git-tag `forge-green-v15` på nuvarande master HEAD. Detta gör nuvarande tillstånd (grön palett, version 15) bevarat och enkelt att referera till.

```bash
git tag -a forge-green-v15 -m "Baseline innan Forge Silver-rollout" e5b28e8
git push origin forge-green-v15
```

**Lager 2 — Logiska commits:**
Alla ändringar görs i 5 tematiska commits (en per task), inte i en stor klump. Detta gör revert lättare och feature-flag-style cherry-pick möjligt om vi vill behålla DELAR.

**Lager 3 — One-command rollback:**
Om du säger "backa Forge Silver" kör jag:
```bash
git revert --no-edit <task1-hash>..<task5-hash>
```
Detta skapar EN revert-commit som backar alla 5 silver-commits. ApplicationVersion bumpas, pushas, TestFlight bygger version 17 med Forge Green tillbaka. Historik är intakt — inget destruktivt.

**Snabb återställning via tag (alternativ):**
```bash
git reset --hard forge-green-v15
git push --force origin master
```
Det här är destruktivt och raderar Silver-commits från remote. Använd bara om du är 100% säker att Silver aldrig ska prövas igen.

---

## Filöversikt

**Källkods-tokens (single source of truth, måste ändras först):**
- `LockIn/Resources/Styles/Colors.xaml` — XAML-färger
- `LockIn/DesignTokens.cs` — C#-färger

**Styles & app-shell:**
- `LockIn/Resources/Styles/Styles.xaml` — 2 hårdkodade Shadow Brush #4ADE80
- `LockIn/AppShell.xaml` — Använder StaticResource ForgeAccent (ändras automatiskt)

**SkiaSharp Controls:**
- `LockIn/Controls/AtmosphericBackgroundView.cs` — 2 hårdkodade `#284ADE80`/`#1C4ADE80` glow

**XAML-sidor med hårdkodad #4ADE80 (5 platser):**
- `LockIn/Views/TrainPage.xaml:318` — FAB shadow
- `LockIn/Views/TemplateEditPage.xaml:72` — Save pill shadow
- `LockIn/Views/PostWorkoutPage.xaml:28` — Celebration gradient
- `LockIn/Views/HemPage.xaml:276` — SparklineView StrokeColor
- `LockIn/Views/ExerciseProgressPage.xaml:57` — LineChartView StrokeColor

**ViewModels med hårdkodad #4ADE80 (chip/state-färger, ej muskel):**
- `LockIn/ViewModels/TrainViewModel.cs:164` — chip selected
- `LockIn/ViewModels/OnboardingViewModel.cs:153` — chip selection
- `LockIn/ViewModels/KroppViewModel.cs:49` — Tab2 selected text
- `LockIn/ViewModels/Converters.cs:28, 49, 58` — diverse state converters
- `LockIn/Views/BoolToColorConverter.cs:11` — bool→color

**ViewModels att BEHÅLLA som gröna (Biceps muskel-symbol, informationsdesign):**
- `LockIn/ViewModels/WorkoutExerciseSection.cs:28`
- `LockIn/ViewModels/ExercisePickerViewModel.cs:89`
- `LockIn/ViewModels/CreateExerciseViewModel.cs:55`
- `LockIn/ViewModels/MuscleGroupConverters.cs:16`

**Att BEHÅLLA grön (semantisk success):**
- `LoggedSetRow.cs:25` Rir-färg när Rir ≥ 0 (rep är registrerat = success)
- `HistoryPage.xaml.cs:103` Calendar trained day → byts till silver istället

---

## Färg-paletten Forge Silver

| Token | Forge Green (gammalt) | Forge Silver (nytt) | Roll |
|-------|----------------------|---------------------|------|
| `ForgeAccent` | `#4ADE80` | `#B8B8BC` | Primär brand, ringar, ikoner, accent text |
| `ForgePrimary` | `#006239` | `#3E3E44` | Primary button bakgrund (mörk silver) |
| `ForgePrimaryForeground` | `#DDE8E3` | `#FAFAFA` | Text på primary button |
| `ForgeAccentDim` | `#1F4ADE80` | `#1FB8B8BC` | Subtil silver tint på cards/states |
| `ForgeAccentGlow` | `#0F4ADE80` | `#0FB8B8BC` | Lite glow på borders |
| `LightAccent` | `#72E3AD` | `#9E9EA6` | Light-mode silver |
| `LightPrimary` | `#006239` | `#3E3E44` | Light-mode dark silver |
| `LightPrimaryForeground` | `#1E2723` | `#1E1E22` | Light-mode dark text |
| `ForgeSuccess` | `#4ADE80` | `#4ADE80` (BEHÅLL) | Semantisk success för completed states |
| `ForgeAccentAmber` | `#FBBF24` | `#FACC15` (förstärks) | Strain, PR, kalorier-accent |
| `ForgeAccentPurple` | `#A78BFA` | `#A78BFA` (behåll) | Recovery, aktiv tid-accent |

CalTrained-tokens i DesignTokens ändras också till silver för konsekvens.

Shadow brushes på FAB/CTAs blir silver `rgba(184,184,188,0.45)` istället för grön glow.

---

## Task 0: Skapa baseline-tag innan något ändras

**Files:** N/A (git-operation)

- [ ] **Step 1: Skapa annoterad tag på nuvarande HEAD**

```bash
git tag -a forge-green-v15 -m "Baseline innan Forge Silver — sista grön-commit"
```

- [ ] **Step 2: Pusha tagg till remote**

```bash
git push origin forge-green-v15
```

Expected: tag `forge-green-v15` finns både lokalt och på GitHub.

- [ ] **Step 3: Verifiera tag**

```bash
git tag -l "forge-green-v15"
```

Expected output: `forge-green-v15`

---

## Task 1: Uppdatera färg-tokens (Colors.xaml + DesignTokens.cs)

**Files:**
- Modify: `LockIn/Resources/Styles/Colors.xaml:14-22`
- Modify: `LockIn/Resources/Styles/Colors.xaml:63-67`
- Modify: `LockIn/DesignTokens.cs:18-19`
- Modify: `LockIn/DesignTokens.cs:32`
- Modify: `LockIn/DesignTokens.cs:38-43`

**Interfaces:**
- Produces: nya färgvärden för `ForgeAccent`, `ForgePrimary`, `ForgePrimaryForeground`, `ForgeAccentDim`, `ForgeAccentGlow`, `LightAccent`, `LightPrimary`, `LightPrimaryForeground` (XAML); `Accent`, `Primary`, `ChipActiveBg`, `CalTrained*` (C#).

- [ ] **Step 1: Uppdatera Colors.xaml — dark palette**

Hitta block på rad 13-22:
```xml
<!-- Accent: ring / highlight color — used for text, indicators, progress bars -->
<Color x:Key="ForgeAccent">#4ADE80</Color>
<!-- Primary: button background in dark mode -->
<Color x:Key="ForgePrimary">#006239</Color>
<!-- Text on primary buttons in dark mode -->
<Color x:Key="ForgePrimaryForeground">#DDE8E3</Color>

<!-- Tinted accent surfaces -->
<Color x:Key="ForgeAccentDim">#1F4ADE80</Color>
<Color x:Key="ForgeAccentGlow">#0F4ADE80</Color>
```

Ersätt med:
```xml
<!-- Accent: ring / highlight color — silver, matchar logo -->
<Color x:Key="ForgeAccent">#B8B8BC</Color>
<!-- Primary: button background in dark mode (dark silver) -->
<Color x:Key="ForgePrimary">#3E3E44</Color>
<!-- Text on primary buttons in dark mode -->
<Color x:Key="ForgePrimaryForeground">#FAFAFA</Color>

<!-- Tinted accent surfaces -->
<Color x:Key="ForgeAccentDim">#1FB8B8BC</Color>
<Color x:Key="ForgeAccentGlow">#0FB8B8BC</Color>
```

- [ ] **Step 2: Uppdatera Colors.xaml — light palette**

Hitta block på rad 62-67:
```xml
<Color x:Key="LightAccent">#72E3AD</Color>
<Color x:Key="LightPrimary">#006239</Color>
<Color x:Key="LightPrimaryForeground">#1E2723</Color>
```

Ersätt med:
```xml
<Color x:Key="LightAccent">#9E9EA6</Color>
<Color x:Key="LightPrimary">#3E3E44</Color>
<Color x:Key="LightPrimaryForeground">#1E1E22</Color>
```

- [ ] **Step 3: Uppdatera DesignTokens.cs Accent/Primary**

Hitta rad 18-19:
```csharp
public static readonly Color Accent  = Color.FromArgb("#4ADE80");
public static readonly Color Primary = Color.FromArgb("#006239");
```

Ersätt med:
```csharp
public static readonly Color Accent  = Color.FromArgb("#B8B8BC");
public static readonly Color Primary = Color.FromArgb("#3E3E44");
```

- [ ] **Step 4: Uppdatera ChipActiveBg**

Hitta rad 32:
```csharp
public static readonly Color ChipActiveBg  = Color.FromArgb("#006239");
```

Ersätt med:
```csharp
public static readonly Color ChipActiveBg  = Color.FromArgb("#3E3E44");
```

- [ ] **Step 5: Uppdatera CalTrained* (HistoryPage calendar)**

Hitta rad 38-40:
```csharp
public static readonly Color CalTrainedStroke = Color.FromArgb("#4ADE80");
public static readonly Color CalTrainedFill   = Color.FromArgb("#1A4ADE80");
public static readonly Color CalTrainedText   = Color.FromArgb("#4ADE80");
```

Ersätt med:
```csharp
public static readonly Color CalTrainedStroke = Color.FromArgb("#B8B8BC");
public static readonly Color CalTrainedFill   = Color.FromArgb("#1AB8B8BC");
public static readonly Color CalTrainedText   = Color.FromArgb("#FAFAFA");
```

- [ ] **Step 6: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add LockIn/Resources/Styles/Colors.xaml LockIn/DesignTokens.cs
git commit -m "feat(palette): Forge Silver — primär brand grå istället för grön"
```

---

## Task 2: Uppdatera Styles.xaml och SkiaSharp Controls

**Files:**
- Modify: `LockIn/Resources/Styles/Styles.xaml:85`
- Modify: `LockIn/Resources/Styles/Styles.xaml:140`
- Modify: `LockIn/Controls/AtmosphericBackgroundView.cs:23`
- Modify: `LockIn/Controls/AtmosphericBackgroundView.cs:33`

**Interfaces:**
- Konsumerar: ny silver-palett från Task 1.

- [ ] **Step 1: Uppdatera Styles.xaml Shadow Brushes**

Hitta rad 85:
```xml
<Shadow Brush="#4ADE80" Offset="0,4" Radius="20" Opacity="0.28" />
```

Ersätt med:
```xml
<Shadow Brush="#B8B8BC" Offset="0,4" Radius="20" Opacity="0.28" />
```

Hitta rad 140:
```xml
<Shadow Brush="#4ADE80" Offset="0,4" Radius="20" Opacity="0.15" />
```

Ersätt med:
```xml
<Shadow Brush="#B8B8BC" Offset="0,4" Radius="20" Opacity="0.15" />
```

- [ ] **Step 2: Uppdatera AtmosphericBackgroundView glow**

Hitta rad 22-24:
```csharp
new SKPoint(w * 0.5f, h * 0.17f),
w * 0.65f,
new[] { SKColor.Parse("#284ADE80"), SKColors.Transparent },
```

Ersätt `#284ADE80` med `#28B8B8BC` (silver med samma alpha):
```csharp
new[] { SKColor.Parse("#28B8B8BC"), SKColors.Transparent },
```

Hitta rad 32-34:
```csharp
new SKPoint(w * 0.5f, h * 0.12f),
w * 0.3f,
new[] { SKColor.Parse("#1C4ADE80"), SKColors.Transparent },
```

Ersätt `#1C4ADE80` med `#1CB8B8BC`:
```csharp
new[] { SKColor.Parse("#1CB8B8BC"), SKColors.Transparent },
```

- [ ] **Step 3: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Resources/Styles/Styles.xaml LockIn/Controls/AtmosphericBackgroundView.cs
git commit -m "feat(palette): silver shadow brushes + atmospheric glow"
```

---

## Task 3: Uppdatera hårdkodade #4ADE80 i XAML-sidor

**Files:**
- Modify: `LockIn/Views/TrainPage.xaml:318`
- Modify: `LockIn/Views/TemplateEditPage.xaml:72`
- Modify: `LockIn/Views/PostWorkoutPage.xaml:28`
- Modify: `LockIn/Views/HemPage.xaml:276`
- Modify: `LockIn/Views/ExerciseProgressPage.xaml:57`

**Interfaces:**
- Konsumerar: silver-färgvärden.

- [ ] **Step 1: TrainPage FAB shadow**

Hitta rad 318:
```xml
<Shadow Brush="#4ADE80" Offset="0,6" Radius="28" Opacity="0.45"/>
```

Ersätt med:
```xml
<Shadow Brush="#B8B8BC" Offset="0,6" Radius="28" Opacity="0.45"/>
```

- [ ] **Step 2: TemplateEditPage Save pill shadow**

Hitta rad 72:
```xml
<Shadow Brush="#4ADE80" Offset="0,3" Radius="14" Opacity="0.4"/>
```

Ersätt med:
```xml
<Shadow Brush="#B8B8BC" Offset="0,3" Radius="14" Opacity="0.4"/>
```

- [ ] **Step 3: PostWorkoutPage celebration gradient**

Hitta rad 28:
```xml
<GradientStop Color="#1A4ADE80" Offset="0.0"/>
```

Ersätt med:
```xml
<GradientStop Color="#1AB8B8BC" Offset="0.0"/>
```

- [ ] **Step 4: HemPage SparklineView StrokeColor**

Hitta rad 276:
```xml
<controls:SparklineView Values="{Binding StepsValues}" StrokeColor="#4ADE80" HeightRequest="38" BackgroundColor="Transparent"/>
```

Ersätt med:
```xml
<controls:SparklineView Values="{Binding StepsValues}" StrokeColor="#B8B8BC" HeightRequest="38" BackgroundColor="Transparent"/>
```

- [ ] **Step 5: ExerciseProgressPage LineChartView StrokeColor**

Hitta rad 57:
```xml
<controls:LineChartView Points="{Binding ChartPoints}" StrokeColor="#4ADE80" HeightRequest="200" BackgroundColor="Transparent"/>
```

Ersätt med:
```xml
<controls:LineChartView Points="{Binding ChartPoints}" StrokeColor="#B8B8BC" HeightRequest="200" BackgroundColor="Transparent"/>
```

- [ ] **Step 6: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add LockIn/Views/TrainPage.xaml LockIn/Views/TemplateEditPage.xaml LockIn/Views/PostWorkoutPage.xaml LockIn/Views/HemPage.xaml LockIn/Views/ExerciseProgressPage.xaml
git commit -m "feat(palette): silver shadows + charts + celebration gradient"
```

---

## Task 4: Uppdatera ViewModel chip/state-färger (ej muskel)

**Files:**
- Modify: `LockIn/ViewModels/TrainViewModel.cs:164`
- Modify: `LockIn/ViewModels/OnboardingViewModel.cs:153`
- Modify: `LockIn/ViewModels/KroppViewModel.cs:49`
- Modify: `LockIn/ViewModels/Converters.cs:28`
- Modify: `LockIn/ViewModels/Converters.cs:49`
- Modify: `LockIn/ViewModels/Converters.cs:58`
- Modify: `LockIn/Views/BoolToColorConverter.cs:11`
- Modify: `LockIn/Views/HistoryPage.xaml.cs:103`

**Interfaces:**
- Konsumerar: silver-färgvärden.

**Viktigt — Biceps muskelfärg ska INTE ändras:**
Följande rader använder `#4ADE80` för Biceps-muskelgruppen och ska BEHÅLLAS:
- `WorkoutExerciseSection.cs:28`
- `ExercisePickerViewModel.cs:89`
- `CreateExerciseViewModel.cs:55`
- `MuscleGroupConverters.cs:16`

**Viktigt — LoggedSetRow Rir-färg:**
`LoggedSetRow.cs:25` använder `#4ADE80` när Rir ≥ 0 (registrerat värde). Detta är semantiskt "success" och behålls grönt.

- [ ] **Step 1: TrainViewModel chip selected**

Hitta rad 164:
```csharp
? Color.FromArgb("#4ADE80")
```

Verifiera kontext (måste vara chip-selection):
```bash
grep -n -B2 -A2 "4ADE80" LockIn/ViewModels/TrainViewModel.cs
```

Ersätt `"#4ADE80"` på rad 164 med `"#B8B8BC"`.

- [ ] **Step 2: OnboardingViewModel chip background**

Hitta rad 153:
```csharp
private static Color SelBg  => Color.FromArgb("#4ADE80");
```

Ersätt med:
```csharp
private static Color SelBg  => Color.FromArgb("#B8B8BC");
```

- [ ] **Step 3: KroppViewModel Tab2 selected text**

Hitta rad 49:
```csharp
public Color Tab2Fg => SelectedTab == 2 ? Color.FromArgb("#4ADE80") : _segTextDim;
```

Ersätt med:
```csharp
public Color Tab2Fg => SelectedTab == 2 ? Color.FromArgb("#FAFAFA") : _segTextDim;
```

(Använder ljus silver för text-läsbarhet på Tab2-selected.)

- [ ] **Step 4: Converters.cs rad 28**

Hitta rad 26-28 (kontrollera kontext):
```bash
grep -n -B3 -A2 "4ADE80" LockIn/ViewModels/Converters.cs
```

På rad 28, ersätt `"#4ADE80"` med `"#B8B8BC"`.

- [ ] **Step 5: Converters.cs rad 49**

På rad 49, ersätt `"#4ADE80"` med `"#B8B8BC"`.

- [ ] **Step 6: Converters.cs rad 58**

På rad 58, ersätt `"#4ADE80"` med `"#B8B8BC"`.

- [ ] **Step 7: BoolToColorConverter (set complete checkmark bg)**

Hitta rad 11 i `LockIn/Views/BoolToColorConverter.cs`:
```csharp
? Color.FromArgb("#4ADE80")
```

Detta är checkmark-bakgrunden när set är completed. Här BEHÅLLER vi grönt eftersom det är semantisk "completed/success". **Ingen ändring i denna fil.**

- [ ] **Step 8: HistoryPage.xaml.cs calendar fill**

Hitta rad 103:
```csharp
BackgroundColor = (trained || selected) ? Color.FromArgb("#006239") : Colors.Transparent,
```

Ersätt med:
```csharp
BackgroundColor = (trained || selected) ? Color.FromArgb("#3E3E44") : Colors.Transparent,
```

(Mörk silver för calendar-fyllning, matchar nya ForgePrimary.)

- [ ] **Step 9: Verifiera build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 10: Commit**

```bash
git add LockIn/ViewModels/TrainViewModel.cs LockIn/ViewModels/OnboardingViewModel.cs LockIn/ViewModels/KroppViewModel.cs LockIn/ViewModels/Converters.cs LockIn/Views/HistoryPage.xaml.cs
git commit -m "feat(palette): silver chip/state-färger i viewmodels"
```

---

## Task 5: Version bump + push (slutgiltig deploy)

**Files:**
- Modify: `LockIn/LockIn.csproj:34`

- [ ] **Step 1: Bumpa ApplicationVersion 15 → 16**

Hitta rad 34:
```xml
<ApplicationVersion>15</ApplicationVersion>
```

Ersätt med:
```xml
<ApplicationVersion>16</ApplicationVersion>
```

- [ ] **Step 2: Verifiera full build**

Run: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Error\(s\)" | grep -v warning`

Expected: `    0 Error(s)`

- [ ] **Step 3: Committa version bump**

```bash
git add LockIn/LockIn.csproj docs/superpowers/plans/2026-06-21-forge-silver-palette.md
git commit -m "chore: bump ApplicationVersion till 16 (Forge Silver)"
```

- [ ] **Step 4: Push**

```bash
git push origin master
```

Expected: push lyckas, GitHub Actions triggar TestFlight-bygge med Forge Silver.

---

## Verifiering

Bygget kompilerar utan errors per task. Visuell verifiering sker på TestFlight (Mac/iOS-simulator inte tillgänglig i denna session):

| Vy | Vad ska vara silver | Vad ska BEHÅLLAS grön |
|----|---------------------|----------------------|
| HemPage | IDAG-banner accent, ringar (silver active arc), Steg-stat färg, sparkline | Set-checkmark om visas (success) |
| TrainPage | FRITT PASS-FAB (silver gradient + silver glow) | – |
| ActiveWorkoutPage | Rest timer progress bar, RIR ≥ 0 indikator (bör fortfarande vara grön = success) | RIR-color (LoggedSetRow Rir ≥ 0) |
| PostWorkoutPage | Celebration gradient topp (subtil silver) | – |
| HistoryPage | Calendar trained-day fyllning (mörk silver), period-pill text | – |
| LibraryPage | Pill-segment selected state | Biceps muskelfärg på övningar |
| KroppPage | KROPP-tab-text (silver), heatmap progress-bars (kvarstår i färg per muskel) | Biceps-muskel om finns i heatmap |
| TemplateEditPage | SPARA-pill (nu silver gradient med silver glow), AUTO-PROGRESSION switch OnColor | – |
| ExerciseProgressPage | EST. 1RM PROGRESSION linje (silver) | – |
| AppShell tab bar | Selected tab icon + title (silver) | – |

## Spec coverage

| Krav | Task |
|------|------|
| Grå primär (silver) ersätter grön brand-färg | Task 1 |
| Gul/amber som secondary (Strain/PR) | Behålls befintlig (#FBBF24/#FACC15) — ingen ändring krävs |
| Lila som secondary (Recovery) | Behålls befintlig (#A78BFA) — ingen ändring krävs |
| Premium, Bevel-känsla | Task 1+2 (silver shadows + atmospheric) |
| Matcha logga | Task 1 (silver = samma gradient som L:et i appicon.svg) |
| Lätt att backa | Task 0 (tag) + tematiska commits (Tasks 1-4) |
| Bevara success-grön (set complete) | Task 4 (BoolToColorConverter oförändrad) |
| Bevara Biceps muskel-grön | Inte ändrad — listad som explicit "BEHÅLL" |
| TestFlight-deploy | Task 5 |
