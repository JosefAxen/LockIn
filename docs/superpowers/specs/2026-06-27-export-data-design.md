# Design Spec — Exportera träningsdata

**Datum:** 2026-06-27
**Sida:** SettingsPage → ny exportfunktion
**Motivering:** GDPR/DMA-krav + App Store Review-krav på dataportering + användarförtroende

---

## Bakgrund

LockIn lagrar all träningsdata lokalt i SQLite utan backup. Utan exportfunktion kan användaren inte ta med sin data vid enhetsbyte eller använda den i andra verktyg. FEATURES.md listar detta som "Hög" prioritet.

---

## Design

### Exportformat

**ZIP-fil** med två CSVer:

| Fil | Innehåll |
|-----|----------|
| `sessions.csv` | En rad per pass: SessionId, Date, StartTime, Duration_min, TemplateName, TotalSets, TotalVolumeKg, PRCount, Notes |
| `sets.csv` | En rad per set: SessionId, Date, ExerciseName, MuscleGroup, Equipment, SetNumber, SetType, WeightKg, Reps, RIR, IsPR, DurationSeconds |

Filnamn: `lockin_export_YYYY-MM-DD.zip`

**Motivering:** Två separata filer är lättare att analysera i Excel/Numbers än en flat denormaliserad fil. ZIP produceras med `System.IO.Compression.ZipArchive` (inbyggt i .NET 10).

### Arkitektur

**Nya filer:**
- `LockIn/Services/ExportService.cs` — genererar CSVer, skriver ZIP, returnerar filsökväg

**Ändrade filer:**
- `LockIn/Services/DatabaseService.cs` — ny metod `GetExportDataAsync()`
- `LockIn/ViewModels/SettingsViewModel.cs` — ny `[RelayCommand] ExportDataAsync()`
- `LockIn/Views/SettingsPage.xaml` — ny export-rad i Data-sektionen
- `LockIn/Resources/Strings/AppResources.resx` + `.en.resx` + `.cs` — 3 nya nycklar

---

## DatabaseService — ny metod

```csharp
public async Task<(List<ExportSessionRow> Sessions, List<ExportSetRow> Sets)> GetExportDataAsync()
```

**ExportSessionRow** (privat intern klass):
```
int SessionId, DateTime StartedAt, DateTime? CompletedAt,
string TemplateName, int TotalSets, double TotalVolumeKg, int PRCount, string Notes
```

**ExportSetRow** (privat intern klass):
```
int SessionId, DateTime SessionDate, string ExerciseName,
string MuscleGroup, string Equipment, int SetNumber, string SetType,
double WeightKg, int Reps, int RIR, bool IsPR, int DurationSeconds
```

Sessions-query: JOIN WorkoutSessions + WorkoutTemplates, GROUP BY session för att räkna TotalSets/Volume/PRCount.

Sets-query: JOIN WorkoutSessions + SessionExercises + LoggedSets + Exercises — en rad per set, ordnat per session + övning + setnummer.

---

## ExportService

```csharp
public class ExportService
{
    private readonly DatabaseService _db;
    
    public async Task<string> ExportAsync()
    // Returnerar full sökväg till ZIP-filen
    // Skriver till FileSystem.AppDataDirectory/exports/lockin_export_YYYY-MM-DD.zip
    // Skapar exports/-katalogen om den inte finns
    // Skriver sessions.csv och sets.csv inuti ZIPen
    // Returnerar filsökvägen
}
```

CSV-format: UTF-8 med BOM (för Excel-kompatibilitet), komma-separator, header-rad, DateTime i ISO 8601 (`yyyy-MM-ddTHH:mm:ss`), bool som `true`/`false`, enum som strängar (t.ex. "Normal", "Warmup").

Radavslut: `\r\n` (CRLF) för Windows/Excel-kompatibilitet.

---

## SettingsViewModel

```csharp
[RelayCommand]
private async Task ExportDataAsync()
{
    // 1. Anropa ExportService.ExportAsync()
    // 2. Share.RequestAsync(new ShareFileRequest { File = new ShareFile(path) })
    // 3. Toast vid fel
}
```

`ExportService` injiceras via konstruktor (registreras som Singleton i `MauiProgram.cs`).

---

## SettingsPage.xaml — ny rad

Ny rad i "Data"-sektionen, **ovanför** den befintliga "Rensa all data"-raden:

```
[⬆] Exportera träningsdata    >
```

Ikon: `share.fill` (SF Symbol). Stil: identisk med övriga settings-rader. Ingen färgmarkering — export är inte en destructive action.

---

## i18n-nycklar (3 nya)

| Nyckel | Svenska | Engelska |
|--------|---------|----------|
| `Settings_ExportData_Title` | Exportera träningsdata | Export Training Data |
| `Settings_ExportData_Subtitle` | Ladda ner all din historik som CSV | Download your full history as CSV |
| `Settings_ExportData_Error` | Exporten misslyckades | Export failed |

---

## Kantfall

| Kantfall | Hantering |
|----------|-----------|
| Ingen träningsdata alls | Exporterar tomma CSVer med bara header-rad |
| Diskfel vid skrivning | try-catch → Toast med `Settings_ExportData_Error` |
| Användaren avbryter Share-sheet | Share.RequestAsync kastar inte — tyst avbryt |
| Gammal export-fil finns kvar | Överskrivs (File.Create är truncate-on-open) |

---

## Vad som INTE ingår

- Datumfiltrering
- JSON-format
- Import-funktion
- Automatisk backup
