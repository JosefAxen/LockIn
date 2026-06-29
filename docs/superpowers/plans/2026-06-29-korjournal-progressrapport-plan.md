# Körjournal / Progressrapport — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lägga till en dela-knapp (↗) på `ExerciseProgressPage` som renderar och delar en 1080×1350 px PNG med övningens 1RM-progression via SkiaSharp och iOS Share Sheet.

**Architecture:** En ny `ProgressReportService` renderar bilden off-screen i `Task.Run` (samma mönster som `WorkoutShareService`). `ExerciseProgressViewModel` får ett `ShareProgressCommand` som bygger `ProgressReportData` från redan inläst data och anropar tjänsten. Ingen ny DB-metod behövs.

**Tech Stack:** SkiaSharp 3.116.1, MAUI Share API (`Share.Default.RequestAsync`), CommunityToolkit.Mvvm 8.4.2.

## Global Constraints

- Target: `net10.0-ios`, .NET MAUI 10
- SkiaSharp text-API: `SKPaint`-baserat — `TextSize`, `TextAlign`, `GetFontMetrics`, `DrawText(string, x, y, SKPaint)`. INTE `SKFont`.
- Bildstorlek: exakt 1080×1350 px (`Width = 1080`, `Height = 1350`)
- Typsnitt laddas via `LoadTypeface(string familyName)` (identisk med WorkoutShareService — kopiera hela metoden)
- PNG-fil sparas med `File.Create(path)` (INTE `OpenWrite`) till `Path.Combine(FileSystem.CacheDirectory, "progress_report.png")`
- Färger (hex, INTE SKColors.*): background `#141418`, surface `#222228`, accent `#FF6B2C`, text `#E2E8F0`, muted `#A2A2A2`, blue `#38BDF8`, divider `#252530`
- `Share.Default.RequestAsync(new ShareFileRequest { Title = AppResources.ExerciseProgress_Share_ImageTitle, File = new ShareFile(path, "image/png") })`
- Inga hårdkodade strängar utanför .resx
- Inget testprojekt — verifiera via `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`

---

## Fil-översikt

| Åtgärd | Fil |
|--------|-----|
| Skapa | `LockIn/Services/ProgressReportService.cs` |
| Ändra | `LockIn/Resources/Strings/AppResources.resx` |
| Ändra | `LockIn/Resources/Strings/AppResources.en.resx` |
| Ändra | `LockIn/Resources/Strings/AppResources.cs` |
| Ändra | `LockIn/ViewModels/ExerciseProgressViewModel.cs` |
| Ändra | `LockIn/Views/ExerciseProgressPage.xaml` |
| Ändra | `LockIn/MauiProgram.cs` |

---

### Task 1: ProgressReportService

**Files:**
- Create: `LockIn/Services/ProgressReportService.cs`

**Interfaces:**
- Produces: `public record ProgressReportData(...)`, `public class ProgressReportService` med `public Task<string> CreateReportImageAsync(ProgressReportData data)`
- Consumed by: Task 2 (`ExerciseProgressViewModel`)

---

- [ ] **Steg 1: Skapa filen med hela implementationen**

```csharp
// LockIn/Services/ProgressReportService.cs
using LockIn.Models;
using SkiaSharp;

namespace LockIn.Services;

public record ProgressReportData(
    string ExerciseName,
    string MuscleGroupLabel,
    IReadOnlyList<ChartPoint> History,
    int TotalSessions
);

public class ProgressReportService
{
    private const int Width  = 1080;
    private const int Height = 1350;

    public async Task<string> CreateReportImageAsync(ProgressReportData data)
        => await Task.Run(() => RenderImage(data));

    private static string RenderImage(ProgressReportData data)
    {
        try
        {
            using var bitmap = new SKBitmap(Width, Height);
            using var canvas = new SKCanvas(bitmap);

            DrawBackground(canvas);
            DrawHeader(canvas);
            DrawExerciseInfo(canvas, data);
            DrawChart(canvas, data);
            DrawStats(canvas, data);
            DrawFooter(canvas, data);

            var path = Path.Combine(FileSystem.CacheDirectory, "progress_report.png");
            using var image   = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            using var stream  = File.Create(path);
            encoded.SaveTo(stream);

            return path;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to render progress report image.", ex);
        }
    }

    // ─── Typeface helper (identisk med WorkoutShareService) ───────────────────

    private static SKTypeface LoadTypeface(string familyName)
    {
        var tf = SKTypeface.FromFamilyName(familyName);
        if (tf != null && tf.FamilyName != "Unknown")
            return tf;

        var fileName = familyName switch
        {
            "BebasNeue"    => "BebasNeue-Regular.ttf",
            "DMSansMedium" => "DMSans-Medium.ttf",
            _              => "DMSans-Regular.ttf"
        };

        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
            return SKTypeface.FromStream(stream) ?? SKTypeface.Default;
        }
        catch
        {
            return SKTypeface.Default;
        }
    }

    // ─── DrawBackground ───────────────────────────────────────────────────────

    private static void DrawBackground(SKCanvas canvas)
    {
        canvas.Clear(SKColor.Parse("#141418"));

        var center = new SKPoint(Width / 2f, Height / 2f);
        using var shader = SKShader.CreateRadialGradient(
            center, 700f,
            new[] { SKColor.Parse("#B8B8BC").WithAlpha((byte)(255 * 0.05f)), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Shader = shader };
        canvas.DrawRect(0, 0, Width, Height, paint);
    }

    // ─── DrawHeader (y: 0–140) ────────────────────────────────────────────────

    private static void DrawHeader(SKCanvas canvas)
    {
        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfBebas,  TextSize  = 80f,
            Color       = SKColor.Parse("#B8B8BC"),   TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText("VANA STRENGTH", 60f, 100f, paint);
        }

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfMedium, TextSize  = 24f,
            Color       = SKColor.Parse("#FF6B2C"),   TextAlign = SKTextAlign.Right
        })
        {
            canvas.DrawText("PROGRESS", Width - 60f, 100f, paint);
        }

        using var linePaint = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#252530"),
            StrokeWidth = 2f,   Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(60f, 130f, Width - 60f, 130f, linePaint);
    }

    // ─── DrawExerciseInfo (y: 140–340) ────────────────────────────────────────

    private static void DrawExerciseInfo(SKCanvas canvas, ProgressReportData data)
    {
        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        var nameUpper = data.ExerciseName.ToUpperInvariant();

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfBebas,          TextSize  = 80f,
            Color       = SKColor.Parse("#E2E8F0"),            TextAlign = SKTextAlign.Left
        })
        {
            if (paint.MeasureText(nameUpper) > 960f)
                paint.TextSize = 52f;
            canvas.DrawText(nameUpper, 60f, 260f, paint);
        }

        var subLabel = data.MuscleGroupLabel.ToUpperInvariant()
                       + " · " + data.TotalSessions + " SESSIONER";
        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfMedium, TextSize  = 26f,
            Color       = SKColor.Parse("#A2A2A2"),   TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText(subLabel, 60f, 310f, paint);
        }
    }

    // ─── DrawChart (y: 340–780) ───────────────────────────────────────────────

    private static void DrawChart(SKCanvas canvas, ProgressReportData data)
    {
        const float chartLeft   = 60f;
        const float chartRight  = 1020f;
        const float chartTop    = 360f;
        const float chartBottom = 760f;
        const float chartW      = chartRight - chartLeft;
        const float chartH      = chartBottom - chartTop;

        var history = data.History;
        if (history.Count == 0) return;

        double minVal = history.Min(p => p.Value);
        double maxVal = history.Max(p => p.Value);
        double range  = maxVal - minVal < 1 ? 1 : maxVal - minVal;

        float XAt(int i)      => chartLeft + (history.Count > 1 ? (float)i / (history.Count - 1) * chartW : chartW / 2f);
        float YAt(double v)   => chartBottom - (float)((v - minVal) / range) * chartH;

        // Fill area
        using (var fillPath = new SKPath())
        {
            fillPath.MoveTo(XAt(0), chartBottom);
            for (int i = 0; i < history.Count; i++)
                fillPath.LineTo(XAt(i), YAt(history[i].Value));
            fillPath.LineTo(XAt(history.Count - 1), chartBottom);
            fillPath.Close();

            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#FF6B2C").WithAlpha(40),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawPath(fillPath, fillPaint);
        }

        // Line
        using (var linePath = new SKPath())
        {
            linePath.MoveTo(XAt(0), YAt(history[0].Value));
            for (int i = 1; i < history.Count; i++)
                linePath.LineTo(XAt(i), YAt(history[i].Value));

            using var linePaint = new SKPaint
            {
                IsAntialias = true, Color = SKColor.Parse("#FF6B2C"),
                StrokeWidth = 3f, Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(linePath, linePaint);
        }

        // Last point dot
        float lastX = XAt(history.Count - 1);
        float lastY = YAt(history[history.Count - 1].Value);
        using (var dotPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FF6B2C"), Style = SKPaintStyle.Fill })
            canvas.DrawCircle(lastX, lastY, 10f, dotPaint);

        // X-axis labels (max 5)
        using var tfRegular = LoadTypeface("DMSansRegular");
        using var lblPaint = new SKPaint
        {
            IsAntialias = true, Typeface = tfRegular, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Center
        };
        int step = Math.Max(1, (history.Count - 1) / 4);
        for (int i = 0; i < history.Count; i += step)
            canvas.DrawText(history[i].Date.ToString("MMM d"), XAt(i), chartBottom + 40f, lblPaint);
    }

    // ─── DrawStats (y: 820–1050) ──────────────────────────────────────────────

    private static void DrawStats(SKCanvas canvas, ProgressReportData data)
    {
        const float cardY  = 820f;
        const float cardH  = 200f;
        const float margin = 60f;
        const float gap    = 20f;
        float cardW = (Width - margin * 2f - gap) / 2f;

        double bestRm   = data.History.Count > 0 ? data.History.Max(p => p.Value) : 0;
        string bestRmStr = bestRm > 0 ? $"{bestRm:F0}" : "–";

        string improvStr = "–";
        if (data.History.Count >= 2)
        {
            double first = data.History[0].Value;
            double last  = data.History[data.History.Count - 1].Value;
            double pct   = (last - first) / first * 100;
            improvStr = pct >= 0 ? $"+{pct:F0}%" : $"{pct:F0}%";
        }

        (string Value, string Label, string Color)[] cards =
        [
            (bestRmStr, "BÄSTA 1RM",    "#FF6B2C"),
            (improvStr, "FÖRBÄTTRING",  "#38BDF8")
        ];

        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        for (int i = 0; i < 2; i++)
        {
            float x = margin + i * (cardW + gap);

            using (var bgPaint = new SKPaint
            {
                IsAntialias = true, Color = SKColor.Parse("#222228"), Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(x, cardY, x + cardW, cardY + cardH), 20f, 20f), bgPaint);
            }

            float cx = x + cardW / 2f;

            using var numPaint = new SKPaint
            {
                IsAntialias = true, Typeface = tfBebas, TextSize = 88f,
                Color = SKColor.Parse(cards[i].Color), TextAlign = SKTextAlign.Center
            };
            numPaint.GetFontMetrics(out var fm);
            float numY   = cardY + (cardH - (fm.Descent - fm.Ascent)) / 2f - fm.Ascent - 15f;
            canvas.DrawText(cards[i].Value, cx, numY, numPaint);

            using var lblPaint2 = new SKPaint
            {
                IsAntialias = true, Typeface = tfMedium, TextSize = 22f,
                Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(cards[i].Label, cx, numY + fm.Descent + 14f, lblPaint2);
        }
    }

    // ─── DrawFooter (y: 1050–1350) ────────────────────────────────────────────

    private static void DrawFooter(SKCanvas canvas, ProgressReportData data)
    {
        using var linePaint = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#252530"),
            StrokeWidth = 2f, Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(60f, 1070f, Width - 60f, 1070f, linePaint);

        using var tfRegular = LoadTypeface("DMSansRegular");
        using var tfMedium  = LoadTypeface("DMSansMedium");

        string dateRange = data.History.Count >= 2
            ? $"{data.History[0].Date:MMM d, yyyy} → {data.History[data.History.Count - 1].Date:MMM d, yyyy}"
            : data.History.Count == 1
                ? data.History[0].Date.ToString("MMM d, yyyy")
                : "";

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface = tfRegular, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText(dateRange, 60f, 1130f, paint);
        }

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface = tfMedium, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText("VANA STRENGTH", 60f, 1200f, paint);
        }
    }
}
```

- [ ] **Steg 2: Verifiera kompilering**

```
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED (inga ProgressReportService-fel).

- [ ] **Steg 3: Committa**

```bash
git add LockIn/Services/ProgressReportService.cs
git commit -m "feat(progress): lägg till ProgressReportService med SkiaSharp-rendering"
```

---

### Task 2: i18n + ExerciseProgressViewModel + DI

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`
- Modify: `LockIn/ViewModels/ExerciseProgressViewModel.cs`
- Modify: `LockIn/MauiProgram.cs`

**Interfaces:**
- Consumes: `ProgressReportService.CreateReportImageAsync`, `ProgressReportData` från Task 1
- Produces: `ShareProgressCommand` (RelayCommand, CanExecute = false om ChartPoints tom)

---

- [ ] **Steg 1: Lägg till i18n-nycklar i AppResources.resx**

Öppna `LockIn/Resources/Strings/AppResources.resx`. Lägg till dessa rader precis EFTER `PostWorkout_Share_ImageTitle`-raden:

```xml
  <data name="ExerciseProgress_Share_Error" xml:space="preserve">
    <value>Kunde inte skapa bild</value>
  </data>
  <data name="ExerciseProgress_Share_ImageTitle" xml:space="preserve">
    <value>LockIn progression</value>
  </data>
```

- [ ] **Steg 2: Lägg till samma nycklar i AppResources.en.resx**

Öppna `LockIn/Resources/Strings/AppResources.en.resx`. Lägg till samma rader med engelska värden:

```xml
  <data name="ExerciseProgress_Share_Error" xml:space="preserve">
    <value>Failed to create image</value>
  </data>
  <data name="ExerciseProgress_Share_ImageTitle" xml:space="preserve">
    <value>LockIn progress</value>
  </data>
```

- [ ] **Steg 3: Lägg till properties i AppResources.cs**

Öppna `LockIn/Resources/Strings/AppResources.cs`. Hitta raden `public static string PostWorkout_Share_ImageTitle`. Lägg till direkt EFTER de befintliga PostWorkout_Share*-raderna:

```csharp
    public static string ExerciseProgress_Share_Error      => Get(nameof(ExerciseProgress_Share_Error));
    public static string ExerciseProgress_Share_ImageTitle => Get(nameof(ExerciseProgress_Share_ImageTitle));
```

- [ ] **Steg 4: Uppdatera ExerciseProgressViewModel**

Öppna `LockIn/ViewModels/ExerciseProgressViewModel.cs`. Gör dessa ändringar:

**4a. Uppdatera using-direktiv** — lägg till `using CommunityToolkit.Maui.Alerts;` om den saknas.

**4b. Uppdatera konstruktorsignaturen** — ändra från:

```csharp
public partial class ExerciseProgressViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
```

till:

```csharp
public partial class ExerciseProgressViewModel(DatabaseService db, ProgressReportService report) : ObservableObject, IQueryAttributable
```

**4c. Lägg till nya fields och properties** direkt efter `[ObservableProperty] private bool _hasMetadata;`:

```csharp
    [ObservableProperty] private bool _isSharing;

    private int _sessionCount;
```

**4d. Lägg till `partial void OnIsSharingChanged`** direkt efter `private Exercise? _exercise;`:

```csharp
    partial void OnIsSharingChanged(bool value)     => ShareProgressCommand.NotifyCanExecuteChanged();
    partial void OnChartPointsChanged(IReadOnlyList<ChartPoint> value) => ShareProgressCommand.NotifyCanExecuteChanged();
```

**4e. Uppdatera `LoadAsync`** — lägg till `_sessionCount = history.Count;` direkt EFTER `HasData = history.Count > 0;`:

```csharp
        HasData = history.Count > 0;
        _sessionCount = history.Count;           // ← lägg till denna rad
```

**4f. Lägg till `ShareProgressCommand`** direkt EFTER `SaveNotesAsync`-metoden:

```csharp
    private bool CanShareProgress() => !IsSharing && ChartPoints.Count > 0;

    [RelayCommand(CanExecute = nameof(CanShareProgress))]
    private async Task ShareProgressAsync()
    {
        if (IsSharing) return;
        IsSharing = true;
        try
        {
            var data = new ProgressReportData(
                ExerciseName,
                MuscleGroupName,
                ChartPoints,
                _sessionCount);

            var path = await report.CreateReportImageAsync(data);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.ExerciseProgress_Share_ImageTitle,
                File  = new ShareFile(path, "image/png")
            });
        }
        catch
        {
            await Shell.Current.DisplayAlert(
                null,
                AppResources.ExerciseProgress_Share_Error,
                AppResources.Common_OK);
        }
        finally
        {
            IsSharing = false;
        }
    }
```

- [ ] **Steg 5: Lägg till DI-registrering i MauiProgram.cs**

Öppna `LockIn/MauiProgram.cs`. Hitta raden med `builder.Services.AddTransient<WorkoutShareService>()`. Lägg till direkt EFTER den:

```csharp
builder.Services.AddTransient<ProgressReportService>();
```

- [ ] **Steg 6: Verifiera kompilering**

```
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED.

- [ ] **Steg 7: Committa**

```bash
git add LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs \
        LockIn/ViewModels/ExerciseProgressViewModel.cs \
        LockIn/MauiProgram.cs
git commit -m "feat(progress): ShareProgressCommand + i18n + DI-registrering"
```

---

### Task 3: ExerciseProgressPage.xaml — dela-knapp

**Files:**
- Modify: `LockIn/Views/ExerciseProgressPage.xaml`

**Interfaces:**
- Consumes: `ShareProgressCommand` från Task 2
- Produces: En ↗-knapp i headerns tredje kolumn, disabled under delning

---

- [ ] **Steg 1: Utöka header-kolumnerna**

Öppna `LockIn/Views/ExerciseProgressPage.xaml`. Hitta raden:

```xml
        <Grid Grid.Row="0" Padding="16,56,16,8" ColumnDefinitions="48,*">
```

Ändra `ColumnDefinitions="48,*"` till `ColumnDefinitions="48,*,48"`.

- [ ] **Steg 2: Lägg till dela-knappen som tredje kolumn**

Lägg till följande Border direkt EFTER `</StackLayout>`-taggen (StackLayout med övningsnamn), och FÖRE `</Grid>`:

```xml
            <Border Grid.Column="2"
                    WidthRequest="36" HeightRequest="36"
                    StrokeShape="Ellipse"
                    BackgroundColor="#17FFFFFF"
                    Stroke="#29FFFFFF"
                    StrokeThickness="0.5"
                    VerticalOptions="Center"
                    HorizontalOptions="End"
                    Opacity="{Binding IsSharing, Converter={x:StaticResource InvertedBoolConverter}}">
                <Label Text="↗"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"
                       FontSize="16"
                       HorizontalOptions="Center"
                       VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ShareProgressCommand}"/>
                </Border.GestureRecognizers>
            </Border>
```

**Obs:** `InvertedBoolConverter` är redan registrerad i sidan som `x:Key="InvertedBoolConverter"` — använd `x:StaticResource`.

- [ ] **Steg 3: Verifiera kompilering**

```
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat: BUILD SUCCEEDED.

- [ ] **Steg 4: Committa**

```bash
git add LockIn/Views/ExerciseProgressPage.xaml
git commit -m "feat(progress): lägg till dela-knapp i ExerciseProgressPage-header"
```

---

## Self-Review

**Spec coverage:**
- ✅ `ProgressReportService.cs` med `ProgressReportData` record — Task 1
- ✅ 1080×1350 px — `Width = 1080, Height = 1350` i Task 1
- ✅ Header (VANA STRENGTH + PROGRESS), exercise info, chart, stats, footer — Task 1
- ✅ Line chart med fill, linje, sista punkt som cirkel, datum-labels — Task 1
- ✅ Stats: bästa 1RM + förbättring % — Task 1
- ✅ `PNG sparas med File.Create` till CacheDirectory — Task 1
- ✅ `ExerciseProgress_Share_Error` + `ExerciseProgress_Share_ImageTitle` i18n — Task 2
- ✅ `_sessionCount` backing field satt i LoadAsync — Task 2
- ✅ `ShareProgressCommand` med `CanExecute` (ChartPoints.Count > 0) — Task 2
- ✅ `AddTransient<ProgressReportService>()` i MauiProgram.cs — Task 2
- ✅ ↗-knapp i header (tredje kolumn) — Task 3
- ✅ Knappens opacity 0.5 under delning via IsSharing — Task 3

**Placeholder scan:** Inga TBD, TODO eller oangivna delar.

**Type consistency:**
- `ProgressReportData` skapad i Task 1, konsumeras i Task 2 — namnen matchar
- `ChartPoint(DateTime Date, double Value)` — `history[i].Value` används korrekt i DrawChart
- `CreateReportImageAsync(ProgressReportData)` → `Task<string>` — matchar i Task 2
