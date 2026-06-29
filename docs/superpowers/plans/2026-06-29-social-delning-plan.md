# Social delning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` to execute this plan task by task. Each task must be committed before proceeding to the next.

**Goal:** Rendera passammanfattning som 1080×1080 px PNG med SkiaSharp off-screen och dela via MAUI Share API. Knapp på PostWorkoutPage.

**Architecture:** `WorkoutShareService` (ny klass i Services/) bygger bilden med `SKBitmap`/`SKCanvas`. `PostWorkoutViewModel` exponerar `ShareWorkoutCommand` ([RelayCommand]). Dela-knapp läggs till i floating-zonen på PostWorkoutPage som en ikoncirkel till vänster om KLAR-knappen.

**Tech Stack:** .NET MAUI 10 iOS, SkiaSharp 3.116.1, MAUI Share API, CommunityToolkit.Mvvm, AppResources i18n

## Global Constraints
- Inga hårdkodade hex-strängar utanför DesignTokens.cs / Colors.xaml
- Inga hårdkodade UI-strängar — alla via AppResources
- Typsnitt: BebasNeue (rubriker/siffror), DMSansMedium (etiketter), DMSansRegular (brödtext)
- Design tokens i C#-kod: DesignTokens.cs (INTE Colors.xaml)
- Bilden alltid mörkt tema — ingen light-mode variant
- Temp-fil sparas till `FileSystem.CacheDirectory` (inte AppDataDirectory)
- Commit efter varje task

---

### Task 1: i18n — lägg till dela-strängar

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`

**Lägg till dessa rader i båda filerna (svenska resp. engelska värden):**

| Name | Värde (sv) | Värde (en) |
|------|------------|------------|
| `PostWorkout_Share` | `Dela` | `Share` |
| `PostWorkout_Share_Error` | `Kunde inte skapa bild` | `Could not create image` |
| `PostWorkout_Share_ImageTitle` | `LockIn pass` | `LockIn workout` |
| `Share_Footer_Tagline` | `Vana Strength` | `Vana Strength` |

Format för AppResources.resx:
```xml
<data name="PostWorkout_Share" xml:space="preserve">
  <value>Dela</value>
</data>
```

**Commit:** `feat(i18n): lägg till strängar för social delning`

---

### Task 2: ShareImageData record + WorkoutShareService

**Files:**
- Create: `LockIn/Services/WorkoutShareService.cs`

**Implementera exakt följande:**

```csharp
using SkiaSharp;

namespace LockIn.Services;

public record ShareImageData(
    string TemplateName,
    string DateDisplay,
    string Duration,
    string TotalVolume,
    string TotalSets,
    string PrCount,
    IReadOnlyList<(string Name, double Fraction)> TopMuscles
);

public class WorkoutShareService
{
    private const int Size = 1080;

    public async Task<string> CreateShareImageAsync(ShareImageData data)
    {
        return await Task.Run(() => RenderImage(data));
    }

    private static string RenderImage(ShareImageData data)
    {
        using var bitmap = new SKBitmap(Size, Size);
        using var canvas = new SKCanvas(bitmap);

        DrawBackground(canvas);
        DrawHeader(canvas, data);
        DrawHeroSection(canvas, data);
        DrawStatsSection(canvas, data);
        DrawMuscleSection(canvas, data);
        DrawFooter(canvas);

        var path = Path.Combine(FileSystem.CacheDirectory, "share_preview.png");
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = File.OpenWrite(path);
        encoded.SaveTo(stream);

        return path;
    }
    // ... (se metoddetaljerna nedan)
}
```

**Metoddetaljerna — implementera exakt dessa:**

`DrawBackground`: Fyll hela canvasen med `#141418`. Rita ett subtilt radialt gradient-overlay centrerat (transparent `#B8B8BC` → fullständigt transparent, radius 600 px, opacity 0.06) för djup.

`DrawHeader` (y: 0–200 px):
- Bakgrundsfärg: ingen extra — ingår i `#141418`
- Text "LOCKIN": BebasNeue, 96 pt, `#B8B8BC`, centrerad, baseline y=130
- Text "Strength Training": DMSansMedium, 22 pt, `#A2A2A2`, centrerad, baseline y=168
- Horisontell linje 2 px, `#252530`, y=196, x-marginal 80 px

`DrawHeroSection` (y: 200–480 px):
- `data.TemplateName` (versaler via `.ToUpperInvariant()`): BebasNeue, 72 pt, `#E2E8F0`, centrerad, baseline y=310
  - Om texten är bredare än 920 px → minska TextSize till 52 pt
- `data.DateDisplay + "  ·  " + data.Duration`: DMSansRegular, 26 pt, `#A2A2A2`, centrerad, baseline y=368

`DrawStatsSection` (y: 480–760 px):
- Tre kort med avrundade hörn (radius 20), jämnt fördelade med marginal 40 px på sidorna och 20 px mellanrum
- Kortbakgrund: `#222228`, kortbredd: (1080 - 80 - 40) / 3 ≈ 320 px, höjd: 220 px
- Kort 1 (Volym): siffra `data.TotalVolume` i `#38BDF8`, etikett "KG VOLYM" i `#A2A2A2`
- Kort 2 (Sets): siffra `data.TotalSets` i `#A78BFA`, etikett "SETS" i `#A2A2A2`
- Kort 3 (PR): siffra `data.PrCount` i `#FBBF24` (0 → `#A2A2A2`), etikett "PR" i `#A2A2A2`
- Siffrorna: BebasNeue, 88 pt, vertikal centering i kortet
- Etiketter: DMSansMedium, 20 pt, 10 px under siffrans baseline

`DrawMuscleSection` (y: 760–960 px):
- Visa max 3 muskler från `data.TopMuscles`
- Per muskel: namn (DMSansMedium, 22 pt, `#A2A2A2`) + horisontell bar + volymsindikator
- Bar: track `#2E2E36`, aktiv del `#B8B8BC`, höjd 6 px, avrundade ändar
- Barens bredd: 480 px, startx: 260 px (efter namnetiketten), y centrerat per rad
- Radavstånd: 50 px, första rad y: 800 px
- Namn vänsterjusterat, max 200 px brett

`DrawFooter` (y: 960–1080 px):
- Horisontell linje 2 px, `#252530`, y=964, x-marginal 80 px
- Text "VANA STRENGTH": DMSansMedium, 20 pt, `#A2A2A2`, vänsterjusterad, x=80, y=1010
- Text "lockin.app": DMSansRegular, 20 pt, `#A2A2A2`, högerjusterad, x=1000, y=1010

**Felhantering:** Hela `RenderImage` wrappad i try-catch — kasta vidare som `InvalidOperationException` med meddelande från `AppResources.PostWorkout_Share_Error`.

**Commit:** `feat(service): WorkoutShareService — SkiaSharp off-screen PNG-rendering`

---

### Task 3: PostWorkoutViewModel — ShareWorkoutCommand

**Files:**
- Modify: `LockIn/ViewModels/PostWorkoutViewModel.cs`

**Lägg till injection av `WorkoutShareService` i konstruktorn:**
```csharp
public partial class PostWorkoutViewModel(
    DatabaseService db,
    IHealthService health,
    PhotoService photos,
    ActiveWorkoutViewModel activeWorkout,
    WorkoutShareService share) : ObservableObject
```

**Lägg till [ObservableProperty] `_isSharing` (bool) ovanför befintliga properties.**

**Lägg till `ShareWorkoutCommand` efter `AddPhotoCommand`:**
```csharp
[RelayCommand]
private async Task ShareWorkoutAsync()
{
    if (IsSharing) return;
    IsSharing = true;
    try
    {
        var topMuscles = MuscleGroups
            .Take(3)
            .Select(m => (m.Name, m.ProgressFraction))
            .ToList();

        var imageData = new ShareImageData(
            TemplateName,
            _loadedSession?.StartedAt.ToString("d MMM yyyy") ?? "",
            Duration,
            TotalVolume,
            TotalSets,
            PrCount,
            topMuscles);

        var filePath = await share.CreateShareImageAsync(imageData);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = AppResources.PostWorkout_Share_ImageTitle,
            File  = new ShareFile(filePath, "image/png")
        });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[Share] Fel: {ex.Message}");
        await Shell.Current.DisplayAlertAsync(
            "", AppResources.PostWorkout_Share_Error, AppResources.Common_OK);
    }
    finally
    {
        IsSharing = false;
    }
}
```

**Registrera `WorkoutShareService` i MauiProgram.cs** som Transient:
```csharp
builder.Services.AddTransient<WorkoutShareService>();
```

**Commit:** `feat(vm): ShareWorkoutCommand i PostWorkoutViewModel`

---

### Task 4: PostWorkoutPage.xaml — dela-knapp

**Files:**
- Modify: `LockIn/Views/PostWorkoutPage.xaml`

**Ersätt den befintliga floating KLAR-knapp-blocket (ZIndex 23) med ett Grid som rymmer båda knapparna:**

```xml
<!-- Floating-zon: Dela-knapp + KLAR-knapp -->
<Grid ColumnDefinitions="56,16,*"
      HorizontalOptions="Fill"
      VerticalOptions="End"
      Margin="16,0,16,40"
      ZIndex="23">

    <!-- Dela-ikoncirkel -->
    <Border Grid.Column="0"
            BackgroundColor="{StaticResource ForgeSurface2}"
            StrokeShape="RoundRectangle 28"
            StrokeThickness="1"
            Stroke="{StaticResource ForgeBorder}"
            HeightRequest="56"
            WidthRequest="56">
        <Grid>
            <ActivityIndicator IsRunning="{Binding IsSharing}"
                               IsVisible="{Binding IsSharing}"
                               Color="{StaticResource ForgeAccent}"
                               WidthRequest="22" HeightRequest="22"
                               HorizontalOptions="Center" VerticalOptions="Center"/>
            <Image Source="ic_share.png"
                   IsVisible="{Binding IsSharing, Converter={StaticResource InverseBoolConverter}}"
                   WidthRequest="22" HeightRequest="22"
                   HorizontalOptions="Center" VerticalOptions="Center"/>
        </Grid>
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding ShareWorkoutCommand}"/>
        </Border.GestureRecognizers>
    </Border>

    <!-- KLAR-knapp (exakt befintlig, Grid.Column="2") -->
    <Border Grid.Column="2"
            BackgroundColor="{StaticResource ForgePrimary}"
            StrokeShape="RoundRectangle 26"
            StrokeThickness="0"
            HeightRequest="56"
            HorizontalOptions="Fill">
        <Label Text="{loc:Localize Common_Done}"
               TextColor="{StaticResource ForgePrimaryForeground}"
               FontFamily="BebasNeue"
               FontSize="20"
               CharacterSpacing="3"
               HorizontalOptions="Center"
               VerticalOptions="Center"/>
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding DoneCommand}" Tapped="OnKlarTapped"/>
        </Border.GestureRecognizers>
    </Border>

</Grid>
```

**Notera:** `ic_share.png` — om ikonen saknas, använd `ic_export.png` eller en generisk pil-upp-ikon. Kontrollera `LockIn/Resources/Images/` och välj lämplig befintlig ikon. Lägg INTE till `InverseBoolConverter` om den redan finns i sidan — kontrollera `ContentPage.Resources` först.

**Commit:** `feat(ui): dela-knapp på PostWorkoutPage`

---

### Task 5: Verifiering — bygg + smoke test

**Files:** Inga ändringar

**Steg:**
1. Kör `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` — ska kompilera utan fel
2. Kontrollera att `WorkoutShareService` är registrerad i DI-containern
3. Kontrollera att alla 4 i18n-nycklar finns i båda resx-filerna
4. Kontrollera att `ShareWorkoutCommand` genereras korrekt av CommunityToolkit.Mvvm (ingen syntax-felvarning)

**Commit:** `chore(build): bump ApplicationVersion +1 (social delning)`

---

## Designreferens — bildlayout (ASCII)

```
┌─────────────────────────────────┐  1080×1080 px
│           LOCKIN                │  y: 0–200   BebasNeue 96pt #B8B8BC
│       Strength Training         │             DMSans 22pt #A2A2A2
├─────────────────────────────────┤  y: 196     linje #252530
│                                 │
│      BÄNKPRESS & AXLAR          │  y: 200–480 BebasNeue 72pt #E2E8F0
│      28 jun 2026  ·  47m        │             DMSans 26pt #A2A2A2
│                                 │
├──────────┬──────────┬───────────┤  y: 480–760
│  12.4k   │   24     │     3     │  BebasNeue 88pt (blå/lila/gul)
│ KG VOLYM │  SETS    │    PR     │  DMSans 20pt #A2A2A2
├──────────┴──────────┴───────────┤
│  Bröst    ████████████░░░░  80% │  y: 760–960 max 3 muskler
│  Axlar    ████████░░░░░░░░  60% │
│  Triceps  █████░░░░░░░░░░░  40% │
├─────────────────────────────────┤  y: 964     linje #252530
│  VANA STRENGTH      lockin.app  │  y: 960–1080 DMSans 20pt #A2A2A2
└─────────────────────────────────┘
```
