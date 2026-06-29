# Körjournal / Progressrapport — Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Dela övningsprogression som en 1080×1350 PNG-bild via iOS Share Sheet, tillgänglig från ExerciseProgressPage.

**Architecture:** En ny `ProgressReportService` (same pattern as WorkoutShareService) renderar bilden off-screen med SkiaSharp. ExerciseProgressViewModel får ett nytt `ShareProgressCommand` som anropar tjänsten med redan inläst data och öppnar Share Sheet. Ingen ny DB-metod behövs.

**Tech Stack:** SkiaSharp 3.116.1, MAUI Share API, CommunityToolkit.Mvvm, existing `ProgressReportService` pattern from `WorkoutShareService`.

---

## Global Constraints

- Target: `net10.0-ios`, .NET MAUI 10
- SkiaSharp: `SKPaint`-baserat text-API (TextSize, TextAlign, DrawText(string, x, y, SKPaint)), INTE SKFont
- Bildstorlek: exakt 1080×1350 px
- Typsnitt: ladda via `SKTypeface.FromFamilyName("BebasNeue")` + `SKTypeface.FromFamilyName("DM Sans")` — fallback till `SKTypeface.Default`
- Färger: `ForgeBackground = 0xFF08060C`, `ForgeAccent = 0xFFFF6B2C`, `ForgeSurface = 0xFF1A1625`, `ForgeMuted = 0xFF8B8A9B`, `ForgeText = 0xFFE8E6F0`
- Fil sparas till `FileSystem.CacheDirectory + "/progress_report.png"` (File.Create, inte OpenWrite)
- Dela via `Share.Default.RequestAsync(new ShareFileRequest { Title = "...", File = new ShareFile(path) })`
- Inga hårdkodade strängar utanför .resx
- Ny i18n-nyckel: `ExerciseProgress_Share_Title` = "DELA" / "SHARE"
- `[RelayCommand(CanExecute = nameof(CanShare))]` med `private bool CanShare() => !IsSharing && ChartPoints.Count > 0`
- DI: `AddTransient<ProgressReportService>()` i MauiProgram.cs
- Committa varje task separat

---

## Data Model

```csharp
public record ProgressReportData(
    string ExerciseName,
    string MuscleGroupLabel,
    IReadOnlyList<(DateTime Date, double Epley1RM)> History,
    double BestEpley1RM,
    DateTime BestDate,
    int TotalSessions
);
```

---

## Image Layout (1080×1350 px)

```
┌─────────────────────────────────┐  y=0
│  VANA STRENGTH     [PROGRESS]   │  Header (y: 0–140)
├─────────────────────────────────┤  y=140
│                                 │
│  BÄNKPRESS                      │  Exercise name (BebasNeue 72px)
│  BRÖST · 12 SESSIONER           │  Sub-label (DM Sans 28px, muted)
│                                 │
├─────────────────────────────────┤  y=340
│                                 │
│   [LINE CHART 1RM-TREND]        │  Chart (y: 340–780)
│    datum på x-axeln             │
│                                 │
├─────────────────────────────────┤  y=780
│  ┌─────────┐  ┌─────────┐       │
│  │ 142 KG  │  │  +18%   │       │  Stats cards (y: 780–1050)
│  │BÄSTA 1RM│  │FÖRBÄTTR │       │
│  └─────────┘  └─────────┘       │
│                                 │
├─────────────────────────────────┤  y=1050
│  2024-01-15 → 2024-03-10        │  Datumspann (DM Sans muted)
│  VANA STRENGTH                  │  Footer (y: 1050–1350)
└─────────────────────────────────┘  y=1350
```

**Chartdetaljer:**
- Polyline av `History`-punkter, normaliserat till (y: 340–760, x: 60–1020)
- Fyllt område under linjen med semi-transparent accent (`0x33FF6B2C`)
- Sista punkten: liten cirkel (r=12) i `ForgeAccent`
- X-axis labels: varannan punkt (eller max 6) visar "Jan 15" format
- Y-axis: minvärde – maxvärde med marginal (+10%)

**Stats cards:**
- Vänster: bästa 1RM i `ForgeAccent` (BebasNeue 64px) + "BÄSTA 1RM" (muted 26px)
- Höger: procentuell förbättring (första→sista 1RM) med `+N%`-format, fallback "–" om <2 sessioner

---

## Komponenter

### 1. `ProgressReportService.cs` (nytt)
- `public record ProgressReportData(...)`
- `public class ProgressReportService`
  - `public Task<string> CreateReportImageAsync(ProgressReportData data)` — kör `Task.Run(() => RenderImage(data))`
  - `private static string RenderImage(ProgressReportData data)` — all SkiaSharp-rendering

### 2. `ExerciseProgressViewModel.cs` (ändra)
- Lägg till `ProgressReportService report` i konstruktorparametrar (primärkonstruktor)
- `[ObservableProperty] private bool _isSharing;`
- `[RelayCommand(CanExecute = nameof(CanShare))] private async Task ShareProgressAsync()`
  - Bygger `ProgressReportData` från `ExerciseName`, `ChartPoints`, `_bestEpley1RM`, `_bestDate`, `TotalSessions`
  - Anropar `report.CreateReportImageAsync(data)`
  - `Share.Default.RequestAsync(new ShareFileRequest { ... })`
  - try/finally med `IsSharing` guard

### 3. `ExerciseProgressPage.xaml` (ändra)
- Lägg till dela-knapp (↗-ikon) i headern, bredvid tillbaka-knappen
- `Command="{Binding ShareProgressCommand}"`, `IsEnabled="{Binding IsSharing, Converter={x:Static InverseBoolConverter.Instance}}"` (eller `CanExecute`)

### 4. i18n
- `ExerciseProgress_Share_Title` = "DELA" / "SHARE"

### 5. `MauiProgram.cs`
- `builder.Services.AddTransient<ProgressReportService>()`

---

## Error Handling

- SkiaSharp-rendering wrappas i try/catch; vid fel visas Toast ("Kunde inte skapa bild")
- Om `History.Count == 0`: `CanShare()` returnerar false — knappen disabled
- `IsSharing`-guard förhindrar dubbeltapp

---

## Befintliga filer att studera

- `LockIn/Services/WorkoutShareService.cs` — SkiaSharp-rendering-mönster att följa exakt
- `LockIn/ViewModels/ExerciseProgressViewModel.cs` — property-namn att återanvända
- `LockIn/Views/ExerciseProgressPage.xaml` — header-struktur att utöka
