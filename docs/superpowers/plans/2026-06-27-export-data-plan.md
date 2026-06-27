# Exportera träningsdata — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lägg till en "Exportera träningsdata"-knapp i SettingsPage som genererar en ZIP med två CSVer (sessions.csv + sets.csv) och delar den via iOS Share Sheet.

**Architecture:** Ny `ExportService` (singleton) hanterar CSV-generering och ZIP-skapande via `System.IO.Compression`. `SettingsViewModel` får ett `ExportDataCommand` som anropar `ExportService.ExportAsync()` och sedan `Share.Default.RequestAsync()`. UI-rad läggs till i SettingsPage's "DATAHANTERING"-sektion.

**Tech Stack:** .NET MAUI 10, net10.0-ios, sqlite-net-pcl, System.IO.Compression (inbyggt), Microsoft.Maui.ApplicationModel.DataTransfer.Share, CommunityToolkit.Maui (Toast), CommunityToolkit.Mvvm ([RelayCommand])

## Global Constraints

- Target: `net10.0-ios` — inget Android-beroende
- Namespaces: `LockIn.Services` för ExportService, `LockIn.ViewModels` för SettingsViewModel
- CSV: UTF-8 med BOM (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)`), CRLF-radavslut (`\r\n`), komma-separator
- ZIP-sökväg: `Path.Combine(FileSystem.AppDataDirectory, "exports", $"lockin_export_{DateTime.Now:yyyy-MM-dd}.zip")`
- SQLite table-namn (exakta): `WorkoutSessions`, `WorkoutTemplates`, `SessionExercises`, `LoggedSets`, `Exercises`
- Enum-värden lagras som int i SQLite; konvertera till strängar i ExportService
- Följ exakt samma kardinalitets-mönster som andra settings-rader i SettingsPage.xaml (se BodyWeight-raden, linjerna 210–230)
- Ingen datumfiltrering — exportera ALL historik
- Inga nya NuGet-paket — `System.IO.Compression` ingår i .NET 10 BCL

---

### Task 1: DatabaseService — GetExportDataAsync

**Files:**
- Modify: `LockIn/Services/DatabaseService.cs` (lägg till i slutet, före sista `}`)

**Interfaces:**
- Produces: `DatabaseService.GetExportDataAsync() → Task<(List<ExportSessionRow> Sessions, List<ExportSetRow> Sets)>` — används av Task 2

- [ ] **Steg 1: Lägg till inre klasser och metod i DatabaseService.cs**

Lägg till precis före den sista `}` i klassen (rad ~1161):

```csharp
// ── Export ─────────────────────────────────────────────────────────────────

private class ExportSessionRow
{
    public int SessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string TemplateName { get; set; } = "";
    public string Notes { get; set; } = "";
    public int TotalSets { get; set; }
    public double TotalVolumeKg { get; set; }
    public int PRCount { get; set; }
}

private class ExportSetRow
{
    public int SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public string ExerciseName { get; set; } = "";
    public int MuscleGroup { get; set; }
    public int Equipment { get; set; }
    public int SetNumber { get; set; }
    public int SetType { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int RIR { get; set; }
    public bool IsPR { get; set; }
    public int DurationSeconds { get; set; }
}

public async Task<(List<ExportSessionRow> Sessions, List<ExportSetRow> Sets)> GetExportDataAsync()
{
    await InitAsync();

    var sessions = await _db.QueryAsync<ExportSessionRow>(@"
        SELECT
            ws.Id                                                    AS SessionId,
            ws.StartedAt,
            ws.CompletedAt,
            COALESCE(wt.Name, '')                                    AS TemplateName,
            ws.Notes,
            COUNT(ls.Id)                                             AS TotalSets,
            COALESCE(SUM(CAST(ls.WeightKg AS REAL) * ls.Reps), 0.0) AS TotalVolumeKg,
            COALESCE(SUM(ls.IsPR), 0)                                AS PRCount
        FROM WorkoutSessions ws
        LEFT JOIN WorkoutTemplates wt ON wt.Id = ws.TemplateId
        LEFT JOIN SessionExercises se ON se.SessionId = ws.Id
        LEFT JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
        WHERE ws.CompletedAt IS NOT NULL
        GROUP BY ws.Id
        ORDER BY ws.StartedAt");

    var sets = await _db.QueryAsync<ExportSetRow>(@"
        SELECT
            ws.Id           AS SessionId,
            ws.StartedAt    AS SessionDate,
            e.Name          AS ExerciseName,
            e.MuscleGroup,
            e.Equipment,
            ls.SetNumber,
            ls.SetType,
            CAST(ls.WeightKg AS REAL) AS WeightKg,
            ls.Reps,
            ls.RIR,
            ls.IsPR,
            ls.DurationSeconds
        FROM LoggedSets ls
        JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
        JOIN WorkoutSessions ws ON ws.Id = se.SessionId
        JOIN Exercises e ON e.Id = se.ExerciseId
        WHERE ws.CompletedAt IS NOT NULL
        ORDER BY ws.StartedAt, se.OrderIndex, ls.SetNumber");

    return (sessions, sets);
}
```

- [ ] **Steg 2: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.` 0 errors.

- [ ] **Steg 3: Commit**

```bash
git add LockIn/Services/DatabaseService.cs
git commit -m "feat(export): add GetExportDataAsync to DatabaseService"
```

---

### Task 2: ExportService

**Files:**
- Create: `LockIn/Services/ExportService.cs`

**Interfaces:**
- Consumes: `DatabaseService.GetExportDataAsync()` (Task 1)
- Produces: `ExportService.ExportAsync() → Task<string>` — returnerar full sökväg till ZIP-filen; används av Task 3

- [ ] **Steg 1: Skapa LockIn/Services/ExportService.cs**

```csharp
using System.IO.Compression;
using System.Text;

namespace LockIn.Services;

public class ExportService(DatabaseService db)
{
    public async Task<string> ExportAsync()
    {
        var (sessions, sets) = await db.GetExportDataAsync();

        var exportsDir = Path.Combine(FileSystem.AppDataDirectory, "exports");
        Directory.CreateDirectory(exportsDir);

        var zipPath = Path.Combine(exportsDir,
            $"lockin_export_{DateTime.Now:yyyy-MM-dd}.zip");

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        // sessions.csv
        var sessionsEntry = zip.CreateEntry("sessions.csv");
        using (var writer = new StreamWriter(sessionsEntry.Open(),
                   new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(
                "SessionId,Date,StartTime,DurationMinutes,TemplateName," +
                "TotalSets,TotalVolumeKg,PRCount,Notes");

            foreach (var s in sessions)
            {
                var dur = s.CompletedAt.HasValue
                    ? (int)(s.CompletedAt.Value - s.StartedAt).TotalMinutes
                    : 0;
                writer.WriteLine(string.Join(",",
                    s.SessionId,
                    s.StartedAt.ToString("yyyy-MM-dd"),
                    s.StartedAt.ToString("HH:mm:ss"),
                    dur,
                    Csv(s.TemplateName),
                    s.TotalSets,
                    s.TotalVolumeKg.ToString("F1"),
                    s.PRCount,
                    Csv(s.Notes)));
            }
        }

        // sets.csv
        var setsEntry = zip.CreateEntry("sets.csv");
        using (var writer = new StreamWriter(setsEntry.Open(),
                   new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.NewLine = "\r\n";
            writer.WriteLine(
                "SessionId,SessionDate,ExerciseName,MuscleGroup,Equipment," +
                "SetNumber,SetType,WeightKg,Reps,RIR,IsPR,DurationSeconds");

            foreach (var r in sets)
            {
                writer.WriteLine(string.Join(",",
                    r.SessionId,
                    r.SessionDate.ToString("yyyy-MM-dd"),
                    Csv(r.ExerciseName),
                    MuscleGroupName(r.MuscleGroup),
                    EquipmentName(r.Equipment),
                    r.SetNumber,
                    SetTypeName(r.SetType),
                    r.WeightKg.ToString("F2"),
                    r.Reps,
                    r.RIR,
                    r.IsPR ? "true" : "false",
                    r.DurationSeconds));
            }
        }

        return zipPath;
    }

    private static string Csv(string? value)
    {
        if (value is null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string MuscleGroupName(int v) => v switch
    {
        0 => "Chest", 1 => "Back", 2 => "Shoulders", 3 => "Biceps",
        4 => "Triceps", 5 => "Legs", 6 => "Core", 7 => "FullBody",
        8 => "Other", 9 => "Forearms", _ => "Unknown"
    };

    private static string EquipmentName(int v) => v switch
    {
        0 => "Other", 1 => "Barbell", 2 => "Dumbbell", 3 => "Cable",
        4 => "Machine", 5 => "BodyOnly", 6 => "EZBar", 7 => "Kettlebell",
        8 => "Bands", 9 => "FoamRoll", 10 => "MedicineBall", _ => "Unknown"
    };

    private static string SetTypeName(int v) => v switch
    {
        0 => "Normal", 1 => "Warmup", 2 => "Time", 3 => "Dropset", _ => "Normal"
    };
}
```

- [ ] **Steg 2: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.` 0 errors.

- [ ] **Steg 3: Commit**

```bash
git add LockIn/Services/ExportService.cs
git commit -m "feat(export): add ExportService with CSV/ZIP generation"
```

---

### Task 3: i18n + MauiProgram + SettingsViewModel

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`
- Modify: `LockIn/MauiProgram.cs`
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`

**Interfaces:**
- Consumes: `ExportService.ExportAsync()` (Task 2)
- Produces: `ExportDataCommand` på `SettingsViewModel` — binds till Task 4

- [ ] **Steg 1: Lägg till 3 nycklar i AppResources.resx**

Lägg till efter `Settings_ClearData_Toast`-raden (rad ~619):

```xml
  <!-- Export -->
  <data name="Settings_ExportData_Title" xml:space="preserve"><value>Exportera träningsdata</value></data>
  <data name="Settings_ExportData_Subtitle" xml:space="preserve"><value>Ladda ner all din historik som CSV</value></data>
  <data name="Settings_ExportData_Error" xml:space="preserve"><value>Exporten misslyckades</value></data>
```

- [ ] **Steg 2: Lägg till samma 3 nycklar i AppResources.en.resx**

Lägg till på exakt samma relativa position (efter Settings_ClearData_Toast):

```xml
  <!-- Export -->
  <data name="Settings_ExportData_Title" xml:space="preserve"><value>Export Training Data</value></data>
  <data name="Settings_ExportData_Subtitle" xml:space="preserve"><value>Download your full history as CSV</value></data>
  <data name="Settings_ExportData_Error" xml:space="preserve"><value>Export failed</value></data>
```

- [ ] **Steg 3: Lägg till 3 wrapper-properties i AppResources.cs**

Lägg till efter `Settings_WeeklyGoal_Format`-raden (rad ~470):

```csharp
    public static string Settings_ExportData_Title    => Get(nameof(Settings_ExportData_Title));
    public static string Settings_ExportData_Subtitle => Get(nameof(Settings_ExportData_Subtitle));
    public static string Settings_ExportData_Error    => Get(nameof(Settings_ExportData_Error));
```

- [ ] **Steg 4: Registrera ExportService som Singleton i MauiProgram.cs**

Lägg till efter `builder.Services.AddTransient<PhotoService>();` (rad 35):

```csharp
        builder.Services.AddSingleton<ExportService>();
```

- [ ] **Steg 5: Injicera ExportService i SettingsViewModel och lägg till ExportDataCommand**

Ändra rad 11 (primärkonstruktorn) från:

```csharp
public partial class SettingsViewModel(DatabaseService db, IHealthService health) : ObservableObject
```

till:

```csharp
public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export) : ObservableObject
```

Lägg till nytt kommando direkt efter `ClearAllDataAsync`-metoden (efter rad 117):

```csharp
    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            var path = await export.ExportAsync();
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.Settings_ExportData_Title,
                File  = new ShareFile(path, "application/zip")
            });
        }
        catch
        {
            await Toast.Make(AppResources.Settings_ExportData_Error).Show();
        }
    }
```

- [ ] **Steg 6: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.` 0 errors.

- [ ] **Steg 7: Commit**

```bash
git add LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs \
        LockIn/MauiProgram.cs \
        LockIn/ViewModels/SettingsViewModel.cs
git commit -m "feat(export): i18n keys, DI registration, ExportDataCommand in SettingsViewModel"
```

---

### Task 4: SettingsPage.xaml — export-rad

**Files:**
- Modify: `LockIn/Views/SettingsPage.xaml`

**Interfaces:**
- Consumes: `ExportDataCommand` från SettingsViewModel (Task 3)

- [ ] **Steg 1: Lägg till export-rad i SettingsPage.xaml**

Hitta raden `<!-- Danger zone -->` (rad ~253). Lägg till följande Border **direkt ovanför** den raden (dvs. efter `</Border>` för ProgressPhotos, rad ~251):

```xml
                <!-- Export data -->
                <Border Style="{StaticResource CardFrame}" Padding="16,14">
                    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                        <Border Grid.Column="0" BackgroundColor="{StaticResource ForgeAccentBlueDim}"
                                StrokeShape="RoundRectangle 10" StrokeThickness="0"
                                WidthRequest="38" HeightRequest="38" VerticalOptions="Center">
                            <Label Text="📤" FontSize="18"
                                   HorizontalOptions="Center" VerticalOptions="Center"/>
                        </Border>
                        <StackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                            <Label Text="{loc:Localize Settings_ExportData_Title}"
                                   FontFamily="DMSansMedium" FontSize="15"/>
                            <Label Text="{loc:Localize Settings_ExportData_Subtitle}"
                                   Style="{StaticResource MutedLabel}"/>
                        </StackLayout>
                        <Path Grid.Column="2" Style="{StaticResource ForwardChevron}"/>
                    </Grid>
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding ExportDataCommand}"/>
                    </Border.GestureRecognizers>
                </Border>

```

- [ ] **Steg 2: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.` 0 errors.

- [ ] **Steg 3: Commit**

```bash
git add LockIn/Views/SettingsPage.xaml
git commit -m "feat(export): add export-row to SettingsPage"
```

---

## Spec Coverage Check

| Krav | Task |
|------|------|
| ZIP med sessions.csv + sets.csv | Task 2 |
| UTF-8 BOM, CRLF, komma-separator | Task 2 |
| CSV-escaping av fält med komma/citattecken | Task 2 |
| Enum → sträng-konvertering | Task 2 |
| FileSystem.AppDataDirectory/exports/ | Task 2 |
| Share.Default.RequestAsync | Task 3 |
| Toast vid fel | Task 3 |
| 3 i18n-nycklar sv + en | Task 3 |
| ExportService singleton i DI | Task 3 |
| Settings_ExportData_Title/Subtitle/Error wrappers | Task 3 |
| Export-rad i DATAHANTERING-sektion, ovanför Rensa | Task 4 |
| ForwardChevron + MutedLabel (matchar sidans stil) | Task 4 |
| Ingen datumfiltrering | Task 1–2 |
