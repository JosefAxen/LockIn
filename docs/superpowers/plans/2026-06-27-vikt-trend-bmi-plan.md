# Vikt-trend och BMI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Visa vikttrend (senaste 30 dagarna) och BMI i BodyWeightPage. Lägg till höjd-inmatning i SettingsPage.

**Architecture:** `AppSettings` får ett nytt `HeightCm`-fält (int, default 0). `BodyWeightViewModel` beräknar BMI och trend i `LoadAsync`. `BodyWeightPage.xaml` visar dem i hero-kortet. `SettingsViewModel` + `SettingsPage.xaml` får ett nytt höjd-fält med samma mönster som befintliga `EditWeeklyGoalCommand`.

**Tech Stack:** sqlite-net-pcl (idempotent ALTER TABLE migration), CommunityToolkit.Mvvm, .NET MAUI 10 XAML

## Global Constraints

- Inga hårdkodade hex-strängar utanför `DesignTokens.cs` och `Colors.xaml`
- i18n: alla UI-strängar i `AppResources.resx` + `.en.resx` + `AppResources.cs`
- `[ObservableProperty]` på fält, `[RelayCommand]` på `async Task`-metoder
- Migration-pattern: `try { ALTER TABLE } catch when (message.Contains("duplicate column")) { }`
- `_allEntries` i BodyWeightViewModel är sorterad **nyast först** (index 0 = senaste)
- Commit efter varje task

---

### Task 1: AppSettings + migration + i18n-strängar

**Files:**
- Modify: `LockIn/Models/AppSettings.cs`
- Modify: `LockIn/Services/DatabaseService.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Produces: `AppSettings.HeightCm` (int property, default 0)
- Produces: i18n-nycklar för Task 2–4 (se steg nedan)

- [ ] **Step 1: Lägg till HeightCm i AppSettings.cs**

Lägg till efter `UserName`-raden:

```csharp
public int HeightCm { get; set; } = 0;
```

- [ ] **Step 2: Lägg till migration i DatabaseService.cs**

I `InitCoreAsync()`, direkt efter den sista befintliga `ALTER TABLE AppSettings`-raden (den för `MorningRecoveryPct`), lägg till:

```csharp
try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HeightCm INTEGER NOT NULL DEFAULT 0"); }
catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
```

- [ ] **Step 3: Lägg till i18n-strängar i AppResources.resx (svenska)**

Lägg till alfabetiskt i `AppResources.resx`:

```xml
<data name="BodyWeight_Bmi" xml:space="preserve">
  <value>BMI</value>
</data>
<data name="BodyWeight_BmiCategory_Normal" xml:space="preserve">
  <value>Normal</value>
</data>
<data name="BodyWeight_BmiCategory_Obese" xml:space="preserve">
  <value>Fetma</value>
</data>
<data name="BodyWeight_BmiCategory_Overweight" xml:space="preserve">
  <value>Övervikt</value>
</data>
<data name="BodyWeight_BmiCategory_Underweight" xml:space="preserve">
  <value>Undervikt</value>
</data>
<data name="BodyWeight_Trend_Down" xml:space="preserve">
  <value>↓ {0} kg / 30 dgr</value>
</data>
<data name="BodyWeight_Trend_Stable" xml:space="preserve">
  <value>→ Stabil</value>
</data>
<data name="BodyWeight_Trend_Up" xml:space="preserve">
  <value>↑ {0} kg / 30 dgr</value>
</data>
<data name="Settings_Height_Format" xml:space="preserve">
  <value>{0} cm</value>
</data>
<data name="Settings_Height_Prompt_Body" xml:space="preserve">
  <value>Ange din längd i centimeter</value>
</data>
<data name="Settings_Height_Prompt_Title" xml:space="preserve">
  <value>Din längd</value>
</data>
<data name="Settings_Height_Title" xml:space="preserve">
  <value>Längd</value>
</data>
```

- [ ] **Step 4: Lägg till samma strängar i AppResources.en.resx (engelska)**

```xml
<data name="BodyWeight_Bmi" xml:space="preserve">
  <value>BMI</value>
</data>
<data name="BodyWeight_BmiCategory_Normal" xml:space="preserve">
  <value>Normal</value>
</data>
<data name="BodyWeight_BmiCategory_Obese" xml:space="preserve">
  <value>Obese</value>
</data>
<data name="BodyWeight_BmiCategory_Overweight" xml:space="preserve">
  <value>Overweight</value>
</data>
<data name="BodyWeight_BmiCategory_Underweight" xml:space="preserve">
  <value>Underweight</value>
</data>
<data name="BodyWeight_Trend_Down" xml:space="preserve">
  <value>↓ {0} kg / 30 days</value>
</data>
<data name="BodyWeight_Trend_Stable" xml:space="preserve">
  <value>→ Stable</value>
</data>
<data name="BodyWeight_Trend_Up" xml:space="preserve">
  <value>↑ {0} kg / 30 days</value>
</data>
<data name="Settings_Height_Format" xml:space="preserve">
  <value>{0} cm</value>
</data>
<data name="Settings_Height_Prompt_Body" xml:space="preserve">
  <value>Enter your height in centimeters</value>
</data>
<data name="Settings_Height_Prompt_Title" xml:space="preserve">
  <value>Your height</value>
</data>
<data name="Settings_Height_Title" xml:space="preserve">
  <value>Height</value>
</data>
```

- [ ] **Step 5: Lägg till wrapper-properties i AppResources.cs**

```csharp
public static string BodyWeight_Bmi => Get(nameof(BodyWeight_Bmi));
public static string BodyWeight_BmiCategory_Normal => Get(nameof(BodyWeight_BmiCategory_Normal));
public static string BodyWeight_BmiCategory_Obese => Get(nameof(BodyWeight_BmiCategory_Obese));
public static string BodyWeight_BmiCategory_Overweight => Get(nameof(BodyWeight_BmiCategory_Overweight));
public static string BodyWeight_BmiCategory_Underweight => Get(nameof(BodyWeight_BmiCategory_Underweight));
public static string BodyWeight_Trend_Down => Get(nameof(BodyWeight_Trend_Down));
public static string BodyWeight_Trend_Stable => Get(nameof(BodyWeight_Trend_Stable));
public static string BodyWeight_Trend_Up => Get(nameof(BodyWeight_Trend_Up));
public static string Settings_Height_Format => Get(nameof(Settings_Height_Format));
public static string Settings_Height_Prompt_Body => Get(nameof(Settings_Height_Prompt_Body));
public static string Settings_Height_Prompt_Title => Get(nameof(Settings_Height_Prompt_Title));
public static string Settings_Height_Title => Get(nameof(Settings_Height_Title));
```

- [ ] **Step 6: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep "error CS"
```

Förväntat: ingen output (inga fel).

- [ ] **Step 7: Commit**

```bash
git add LockIn/Models/AppSettings.cs LockIn/Services/DatabaseService.cs LockIn/Resources/Strings/AppResources.resx LockIn/Resources/Strings/AppResources.en.resx LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(body): add HeightCm to AppSettings, migration, and i18n strings"
```

---

### Task 2: BodyWeightViewModel — BMI + trend

**Files:**
- Modify: `LockIn/ViewModels/BodyWeightViewModel.cs`

**Interfaces:**
- Consumes: `DatabaseService.GetAppSettingsAsync()` (befintlig)
- Consumes: `AppSettings.HeightCm` (Task 1)
- Consumes: `AppResources.BodyWeight_*` (Task 1)
- Produces: `BmiText`, `BmiCategory`, `HasBmi`, `WeightTrend`, `HasTrend`

**Context:**
`GetAppSettingsAsync()` returnerar `AppSettings` — är en befintlig DatabaseService-metod.
`_allEntries` är sorterad nyast-först (index 0 = senaste posten).
`DesignTokens.AccentTeal` och `DesignTokens.AccentGreen` existerar i `LockIn/DesignTokens.cs`.

- [ ] **Step 1: Lägg till nya ObservableProperty-fält**

Lägg till efter `[ObservableProperty] private IReadOnlyList<ChartPoint> _chartPoints = [];`:

```csharp
[ObservableProperty] private string _bmiText    = "–";
[ObservableProperty] private string _bmiCategory = "";
[ObservableProperty] private bool   _hasBmi;
[ObservableProperty] private string _weightTrend = "";
[ObservableProperty] private bool   _hasTrend;
```

- [ ] **Step 2: Lägg till BMI + trend-beräkning i LoadAsync**

Direkt efter `if (HasData) { ... }` (chart-beräkningsblocket), men fortfarande innanför `LoadAsync`, lägg till:

```csharp
// BMI
var settings = await db.GetAppSettingsAsync();
if (settings.HeightCm > 0 && HasData)
{
    var latestKg = (double)_allEntries[0].WeightKg;
    var heightM  = settings.HeightCm / 100.0;
    var bmi      = latestKg / (heightM * heightM);
    BmiText     = bmi.ToString("F1");
    BmiCategory = bmi switch
    {
        < 18.5 => AppResources.BodyWeight_BmiCategory_Underweight,
        < 25.0 => AppResources.BodyWeight_BmiCategory_Normal,
        < 30.0 => AppResources.BodyWeight_BmiCategory_Overweight,
        _      => AppResources.BodyWeight_BmiCategory_Obese,
    };
    HasBmi = true;
}
else
{
    BmiText     = "–";
    BmiCategory = "";
    HasBmi      = false;
}

// Trend
var thirtyDaysAgo = DateTime.Now.AddDays(-30);
var recent = _allEntries
    .Where(e => e.LoggedAt >= thirtyDaysAgo)
    .OrderBy(e => e.LoggedAt)
    .ToList();
if (recent.Count >= 2)
{
    var delta    = (double)(recent.Last().WeightKg - recent.First().WeightKg);
    var absDelta = Math.Abs(delta);
    WeightTrend = absDelta < 0.2
        ? AppResources.BodyWeight_Trend_Stable
        : delta > 0
            ? string.Format(AppResources.BodyWeight_Trend_Up,   absDelta.ToString("F1", CultureInfo.InvariantCulture))
            : string.Format(AppResources.BodyWeight_Trend_Down, absDelta.ToString("F1", CultureInfo.InvariantCulture));
    HasTrend = true;
}
else
{
    WeightTrend = "";
    HasTrend    = false;
}
```

- [ ] **Step 3: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep "error CS"
```

Förväntat: ingen output.

- [ ] **Step 4: Commit**

```bash
git add LockIn/ViewModels/BodyWeightViewModel.cs
git commit -m "feat(body): compute BMI and weight trend in BodyWeightViewModel"
```

---

### Task 3: BodyWeightPage.xaml — visa BMI + trend

**Files:**
- Modify: `LockIn/Views/BodyWeightPage.xaml`

**Interfaces:**
- Consumes: `BmiText`, `BmiCategory`, `HasBmi`, `WeightTrend`, `HasTrend` (Task 2)

**Context:**
Hero-kortet i BodyWeightPage.xaml ser ut så här (förkortat):
```xml
<Border Style="{StaticResource CardFrame}" Padding="24,20">
    <VerticalStackLayout Spacing="4" HorizontalOptions="Center">
        <Label Text="{Binding LatestWeight}"
               FontSize="56" FontAttributes="Bold" .../>
        <Label Text="{Binding LatestDate}" Style="{StaticResource MutedLabel}" .../>
        <Label Text="{loc:Localize BodyWeight_NoData}" ...
               IsVisible="{Binding HasData, Converter={x:StaticResource InvertedBoolConverter}}"/>
    </VerticalStackLayout>
</Border>
```

Lägg till BMI-rad och trend-rad innanför `VerticalStackLayout`, efter `LatestDate`-labeln.

- [ ] **Step 1: Lägg till BMI-rad och trend i hero-kortet**

Hitta raden:
```xml
                        <Label Text="{loc:Localize BodyWeight_NoData}"
```

Lägg till EFTER `LatestDate`-labeln men INNAN `BodyWeight_NoData`-labeln:

```xml
                        <!-- BMI -->
                        <HorizontalStackLayout Spacing="6" HorizontalOptions="Center"
                                               IsVisible="{Binding HasBmi}">
                            <Label Text="{loc:Localize BodyWeight_Bmi}"
                                   Style="{StaticResource MutedLabel}" FontSize="13"/>
                            <Label Text="{Binding BmiText}"
                                   FontFamily="DMSansMedium" FontSize="13"
                                   TextColor="{AppThemeBinding Light={StaticResource LightAccent}, Dark={StaticResource ForgeAccent}}"/>
                            <Label Text="{Binding BmiCategory}"
                                   Style="{StaticResource MutedLabel}" FontSize="13"/>
                        </HorizontalStackLayout>

                        <!-- Trend -->
                        <Label Text="{Binding WeightTrend}"
                               Style="{StaticResource MutedLabel}"
                               FontSize="13"
                               HorizontalOptions="Center"
                               IsVisible="{Binding HasTrend}"/>
```

- [ ] **Step 2: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep "error CS"
```

Förväntat: ingen output.

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/BodyWeightPage.xaml
git commit -m "feat(body): show BMI and weight trend in BodyWeightPage hero card"
```

---

### Task 4: SettingsViewModel + SettingsPage.xaml — höjd-fält

**Files:**
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`
- Modify: `LockIn/Views/SettingsPage.xaml`

**Interfaces:**
- Consumes: `AppSettings.HeightCm` (Task 1)
- Consumes: `AppResources.Settings_Height_*` (Task 1)

**Context:**
Befintligt mönster för `EditWeeklyGoalAsync` i SettingsViewModel:
```csharp
[RelayCommand]
private async Task EditWeeklyGoalAsync()
{
    var result = await Shell.Current.DisplayPromptAsync(
        AppResources.Settings_EditWeeklyGoal_Title,
        AppResources.Settings_EditWeeklyGoal_Body,
        keyboard: Keyboard.Numeric,
        initialValue: WeeklyGoal.ToString(),
        maxLength: 1);
    if (!int.TryParse(result, out var goal) || goal < 1 || goal > 7) return;
    WeeklyGoal = goal;
    var settings = await db.GetAppSettingsAsync();
    settings.WeeklyWorkoutGoal = goal;
    await db.SaveAppSettingsAsync(settings);
}
```

SettingsPage.xaml — befintlig rad för WeeklyGoal (Träning-sektion):
```xml
<Border Style="{StaticResource CardFrame}" Padding="16,14">
    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
        <Border Grid.Column="0" BackgroundColor="{StaticResource ForgeAccentOrangeDim}" ...>
            <views:AppIcon Source="ic_calendar.png" .../>
        </Border>
        <StackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
            <Label Text="{loc:Localize Settings_WeeklyGoal_Title}" .../>
            <Label Text="{Binding WeeklyGoalDisplay}" Style="{StaticResource MutedLabel}"/>
        </StackLayout>
        <Path Grid.Column="2" Style="{StaticResource ForwardChevron}"/>
    </Grid>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding EditWeeklyGoalCommand}"/>
    </Border.GestureRecognizers>
</Border>
```

Höjd-raden ska ligga i Profile-sektionen (efter UserName-raden), med samma struktur.

`ForgeAccentBlueDim` — kontrollera om det finns i Colors.xaml. Om inte, använd `ForgeAccentCoralDim` eller `ForgeSurface2` som bakgrund på ikontorget.

- [ ] **Step 1: Lägg till HeightCm + HeightDisplay i SettingsViewModel**

Lägg till efter `_userName`:

```csharp
[ObservableProperty] private int _heightCm;

public string HeightDisplay =>
    HeightCm > 0
        ? string.Format(AppResources.Settings_Height_Format, HeightCm)
        : "–";
```

- [ ] **Step 2: Ladda HeightCm i LoadAsync**

Lägg till i `LoadAsync()` efter `UserName = settings.UserName ?? "";`:

```csharp
HeightCm = settings.HeightCm;
```

- [ ] **Step 3: Lägg till OnHeightCmChanged för att uppdatera HeightDisplay**

```csharp
partial void OnHeightCmChanged(int value)
    => OnPropertyChanged(nameof(HeightDisplay));
```

- [ ] **Step 4: Lägg till EditHeightCommand**

```csharp
[RelayCommand]
private async Task EditHeightAsync()
{
    var result = await Shell.Current.DisplayPromptAsync(
        AppResources.Settings_Height_Prompt_Title,
        AppResources.Settings_Height_Prompt_Body,
        keyboard: Keyboard.Numeric,
        initialValue: HeightCm > 0 ? HeightCm.ToString() : "",
        maxLength: 3);
    if (!int.TryParse(result, out var cm) || cm < 100 || cm > 250) return;
    HeightCm = cm;
    var settings = await db.GetAppSettingsAsync();
    settings.HeightCm = cm;
    await db.SaveAppSettingsAsync(settings);
}
```

- [ ] **Step 5: Lägg till höjd-rad i SettingsPage.xaml (Profile-sektionen)**

Hitta UserName-raden (sista `</Border>` i Profile-sektionen, dvs. direkt före `<!-- Weekly goal -->`-kommentaren). Lägg till efter UserName-Border:

```xml
<!-- Height -->
<Border Style="{StaticResource CardFrame}" Padding="16,14">
    <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
        <Border Grid.Column="0" BackgroundColor="{StaticResource ForgeSurface2}"
                StrokeShape="RoundRectangle 10" StrokeThickness="0"
                WidthRequest="38" HeightRequest="38" VerticalOptions="Center">
            <Label Text="📏" FontSize="18"
                   HorizontalOptions="Center" VerticalOptions="Center"/>
        </Border>
        <StackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
            <Label Text="{loc:Localize Settings_Height_Title}" FontFamily="DMSansMedium" FontSize="15"/>
            <Label Text="{Binding HeightDisplay}" Style="{StaticResource MutedLabel}"/>
        </StackLayout>
        <Path Grid.Column="2" Style="{StaticResource ForwardChevron}"/>
    </Grid>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding EditHeightCommand}"/>
    </Border.GestureRecognizers>
</Border>
```

OBS: `ForgeSurface2` — kontrollera att denna nyckel finns i `Colors.xaml`. Om den inte finns, använd `ForgeSurface` eller `ForgeElevated` istället.

- [ ] **Step 6: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep "error CS"
```

Förväntat: ingen output.

- [ ] **Step 7: Commit**

```bash
git add LockIn/ViewModels/SettingsViewModel.cs LockIn/Views/SettingsPage.xaml
git commit -m "feat(settings): add height field for BMI calculation"
```
