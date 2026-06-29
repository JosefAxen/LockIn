# Träningspåminnelser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Schemalagda notiser för träningsdagar — användaren väljer vilka veckodagar och vilken tid, appen schemalägger `UNCalendarNotificationTrigger`-notiser som upprepas varje vald dag.

**Architecture:**
- `AppSettings` får två nya fält: `ReminderDays` (int bitmask, bit 0=Mån..bit 6=Sön) och `ReminderTimeMinutes` (int, minuter från midnatt, 0 = ej satt → behandlas som 480 = 08:00).
- `NotificationService` utökas med `ScheduleReminders(int daysBitmask, int timeMinutes)` och `CancelReminders()` via `UNCalendarNotificationTrigger` med `repeats: true`.
- `SettingsViewModel` injicerar `NotificationService`, exponerar dag-chip-färger + `ReminderDisplay` + kommandon för toggle och tidsredigering.
- `SettingsPage.xaml` får ett nytt "Påminnelser"-section med ett kompakt kort innehållandes 7 klickbara dagchips + en tidsrad.

**Tech Stack:** .NET MAUI 10 iOS, `UserNotifications` (UNCalendarNotificationTrigger), CommunityToolkit.Mvvm, sqlite-net-pcl

## Global Constraints

- Alla färger via `StaticResource` (XAML) eller `DesignTokens.*` (C#) — inga hårdkodade hex
- `[RelayCommand]` på `async Task`-metoder
- i18n: alla UI-strängar via `AppResources` + `.resx` + `.en.resx` + `AppResources.cs`
- `#if IOS` skyddar all `UserNotifications`-kod
- Idempotent SQLite-migration via try-catch på "duplicate column"
- Commit efter varje task

---

### Task 1: AppSettings + DatabaseService migration + AppResources i18n

**Files:**
- Modify: `LockIn/Models/AppSettings.cs`
- Modify: `LockIn/Services/DatabaseService.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Produces: `AppSettings.ReminderDays` (int, default 0) och `AppSettings.ReminderTimeMinutes` (int, default 0)
- Produces: i18n-nycklar för Task 3

- [ ] **Step 1: Lägg till fält i AppSettings.cs**

Efter `MorningRecoveryPct`, lägg till:

```csharp
public int ReminderDays        { get; set; } = 0;
public int ReminderTimeMinutes { get; set; } = 0;
```

- [ ] **Step 2: Lägg till migration i DatabaseService.cs (InitCoreAsync)**

Hitta det sista `ALTER TABLE AppSettings`-blocket i `InitCoreAsync`. Lägg till direkt efter:

```csharp
try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderDays INTEGER NOT NULL DEFAULT 0"); }
catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderTimeMinutes INTEGER NOT NULL DEFAULT 0"); }
catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
```

- [ ] **Step 3: Lägg till i18n-strängar i AppResources.resx (svenska)**

Lägg till dessa strängar alfabetiskt bland Settings_-nycklar och Notification_-nycklar:

```xml
<data name="Notification_Reminder_Body" xml:space="preserve">
  <value>Dags att träna! Öppna appen och sätt igång 💪</value>
</data>
<data name="Notification_Reminder_Title" xml:space="preserve">
  <value>LockIn · Påminnelse</value>
</data>
<data name="Settings_Reminders_Day_0" xml:space="preserve">
  <value>Mån</value>
</data>
<data name="Settings_Reminders_Day_1" xml:space="preserve">
  <value>Tis</value>
</data>
<data name="Settings_Reminders_Day_2" xml:space="preserve">
  <value>Ons</value>
</data>
<data name="Settings_Reminders_Day_3" xml:space="preserve">
  <value>Tor</value>
</data>
<data name="Settings_Reminders_Day_4" xml:space="preserve">
  <value>Fre</value>
</data>
<data name="Settings_Reminders_Day_5" xml:space="preserve">
  <value>Lör</value>
</data>
<data name="Settings_Reminders_Day_6" xml:space="preserve">
  <value>Sön</value>
</data>
<data name="Settings_Reminders_Off" xml:space="preserve">
  <value>Av</value>
</data>
<data name="Settings_Reminders_Time_Label" xml:space="preserve">
  <value>Tid</value>
</data>
<data name="Settings_Reminders_TimeInvalid" xml:space="preserve">
  <value>Ogiltig tid — ange HH:MM (t.ex. 08:00)</value>
</data>
<data name="Settings_Reminders_TimePrompt_Body" xml:space="preserve">
  <value>Ange önskad tid i formatet HH:MM</value>
</data>
<data name="Settings_Reminders_TimePrompt_Title" xml:space="preserve">
  <value>Påminnelsetid</value>
</data>
<data name="Settings_Reminders_Title" xml:space="preserve">
  <value>Träningspåminnelser</value>
</data>
<data name="Settings_Section_Reminders" xml:space="preserve">
  <value>Påminnelser</value>
</data>
```

- [ ] **Step 4: Lägg till i18n-strängar i AppResources.en.resx (engelska)**

```xml
<data name="Notification_Reminder_Body" xml:space="preserve">
  <value>Time to train! Open the app and get going 💪</value>
</data>
<data name="Notification_Reminder_Title" xml:space="preserve">
  <value>LockIn · Reminder</value>
</data>
<data name="Settings_Reminders_Day_0" xml:space="preserve">
  <value>Mon</value>
</data>
<data name="Settings_Reminders_Day_1" xml:space="preserve">
  <value>Tue</value>
</data>
<data name="Settings_Reminders_Day_2" xml:space="preserve">
  <value>Wed</value>
</data>
<data name="Settings_Reminders_Day_3" xml:space="preserve">
  <value>Thu</value>
</data>
<data name="Settings_Reminders_Day_4" xml:space="preserve">
  <value>Fri</value>
</data>
<data name="Settings_Reminders_Day_5" xml:space="preserve">
  <value>Sat</value>
</data>
<data name="Settings_Reminders_Day_6" xml:space="preserve">
  <value>Sun</value>
</data>
<data name="Settings_Reminders_Off" xml:space="preserve">
  <value>Off</value>
</data>
<data name="Settings_Reminders_Time_Label" xml:space="preserve">
  <value>Time</value>
</data>
<data name="Settings_Reminders_TimeInvalid" xml:space="preserve">
  <value>Invalid time — enter HH:MM (e.g. 08:00)</value>
</data>
<data name="Settings_Reminders_TimePrompt_Body" xml:space="preserve">
  <value>Enter desired time in HH:MM format</value>
</data>
<data name="Settings_Reminders_TimePrompt_Title" xml:space="preserve">
  <value>Reminder Time</value>
</data>
<data name="Settings_Reminders_Title" xml:space="preserve">
  <value>Workout Reminders</value>
</data>
<data name="Settings_Section_Reminders" xml:space="preserve">
  <value>Reminders</value>
</data>
```

- [ ] **Step 5: Lägg till wrapper-properties i AppResources.cs**

Lägg till (alfabetiskt):

```csharp
public static string Notification_Reminder_Body   => Get(nameof(Notification_Reminder_Body));
public static string Notification_Reminder_Title  => Get(nameof(Notification_Reminder_Title));
public static string Settings_Reminders_Day_0     => Get(nameof(Settings_Reminders_Day_0));
public static string Settings_Reminders_Day_1     => Get(nameof(Settings_Reminders_Day_1));
public static string Settings_Reminders_Day_2     => Get(nameof(Settings_Reminders_Day_2));
public static string Settings_Reminders_Day_3     => Get(nameof(Settings_Reminders_Day_3));
public static string Settings_Reminders_Day_4     => Get(nameof(Settings_Reminders_Day_4));
public static string Settings_Reminders_Day_5     => Get(nameof(Settings_Reminders_Day_5));
public static string Settings_Reminders_Day_6     => Get(nameof(Settings_Reminders_Day_6));
public static string Settings_Reminders_Off       => Get(nameof(Settings_Reminders_Off));
public static string Settings_Reminders_Time_Label        => Get(nameof(Settings_Reminders_Time_Label));
public static string Settings_Reminders_TimeInvalid       => Get(nameof(Settings_Reminders_TimeInvalid));
public static string Settings_Reminders_TimePrompt_Body   => Get(nameof(Settings_Reminders_TimePrompt_Body));
public static string Settings_Reminders_TimePrompt_Title  => Get(nameof(Settings_Reminders_TimePrompt_Title));
public static string Settings_Reminders_Title             => Get(nameof(Settings_Reminders_Title));
public static string Settings_Section_Reminders           => Get(nameof(Settings_Section_Reminders));
```

- [ ] **Step 6: Bygg och verifiera**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add LockIn/Models/AppSettings.cs LockIn/Services/DatabaseService.cs \
        LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(reminders): add AppSettings fields, migration, and i18n strings"
```

---

### Task 2: NotificationService — ScheduleReminders + CancelReminders

**Files:**
- Modify: `LockIn/Services/NotificationService.cs`

**Interfaces:**
- Produces: `ScheduleReminders(int daysBitmask, int timeMinutes)` — schemalägger `UNCalendarNotificationTrigger` för varje aktiv dag
- Produces: `CancelReminders()` — tar bort alla 7 möjliga reminder-notiser

**Context:**

`NotificationService` (iOS-guardad med `#if IOS`):
```csharp
private const string TimerId = "rest_timer";
public async Task RequestPermissionAsync() { ... }
public void ScheduleTimer(int seconds, string exerciseName) { ... }
public void CancelTimer() { ... }
```

iOS weekday-mappning (NSDateComponents): Sunday=1, Monday=2, Tuesday=3, Wednesday=4, Thursday=5, Friday=6, Saturday=7.

Bitmask-till-iOS-weekday-array (index = bitIndex, värde = iOS weekday):
```csharp
int[] iOSWeekdays = new[] { 2, 3, 4, 5, 6, 7, 1 }; // bit0=Mån=2 .. bit6=Sön=1
```

Notification-identifierare: `"reminder_wd_1"` .. `"reminder_wd_7"` (suffixet är iOS-weekday-talet).

- [ ] **Step 1: Lägg till ScheduleReminders-metod**

Lägg till direkt efter `CancelTimer()`:

```csharp
public void ScheduleReminders(int daysBitmask, int timeMinutes)
{
    CancelReminders();
#if IOS
    var h = timeMinutes / 60;
    var m = timeMinutes % 60;
    var iOSWeekdays = new[] { 2, 3, 4, 5, 6, 7, 1 };
    for (var bit = 0; bit < 7; bit++)
    {
        if ((daysBitmask & (1 << bit)) == 0) continue;
        var content = new UNMutableNotificationContent
        {
            Title = AppResources.Notification_Reminder_Title,
            Body  = AppResources.Notification_Reminder_Body,
            Sound = UNNotificationSound.Default
        };
        var components = new NSDateComponents
        {
            Hour    = h,
            Minute  = m,
            Weekday = iOSWeekdays[bit]
        };
        var trigger = UNCalendarNotificationTrigger.CreateTrigger(components, repeats: true);
        var id      = $"reminder_wd_{iOSWeekdays[bit]}";
        var request = UNNotificationRequest.FromIdentifier(id, content, trigger);
        UNUserNotificationCenter.Current.AddNotificationRequest(request, null);
    }
#endif
}
```

- [ ] **Step 2: Lägg till CancelReminders-metod**

Lägg till direkt efter ScheduleReminders:

```csharp
public void CancelReminders()
{
#if IOS
    var ids = new[] { "reminder_wd_1", "reminder_wd_2", "reminder_wd_3",
                      "reminder_wd_4", "reminder_wd_5", "reminder_wd_6", "reminder_wd_7" };
    UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
#endif
}
```

- [ ] **Step 3: Bygg och verifiera**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add LockIn/Services/NotificationService.cs
git commit -m "feat(reminders): add ScheduleReminders and CancelReminders to NotificationService"
```

---

### Task 3: SettingsViewModel + SettingsPage.xaml

**Files:**
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`
- Modify: `LockIn/Views/SettingsPage.xaml`

**Interfaces:**
- Consumes: `AppSettings.ReminderDays` och `AppSettings.ReminderTimeMinutes` (Task 1)
- Consumes: alla `AppResources.Settings_Reminders_*` wrapper-properties (Task 1)
- Consumes: `NotificationService.ScheduleReminders(int, int)` och `.CancelReminders()` (Task 2)

**Context:**

Nuvarande SettingsViewModel-konstruktor:
```csharp
public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export) : ObservableObject
```

Tillgängliga design-tokens (C#):
- `DesignTokens.Accent` — silver (aktiv dag)
- `DesignTokens.Surface2` — mörk bakgrund (inaktiv dag)
- `DesignTokens.FabForeground` — mörk text på silver
- `DesignTokens.Text` — normal textfärg

Tillgängliga XAML-resurser:
- `ForgeSuccessDim` (#1F4ADE80) — grön bakgrund för ikon
- `ForgeSuccess` (#4ADE80) — grön ikon-text
- `ForgeSurface2` — avdelarfärg
- `CardFrame` — border-stil för kort
- `SectionLabel` — etikettsstil för sektionsrubriker
- `ForwardChevron` — Path-stil för ">"
- `MutedLabel` — grå text-stil

Nuvarande struktur i SettingsPage.xaml (sektioner):
1. Profile (Settings_Section_Profile)
2. Training (Settings_Section_Training) ← **lägg till Reminders-sektionen EFTER detta block**
3. Units (Settings_Section_Units)
4. RestTimer (Settings_Section_RestTimer)
5. Health (Settings_Section_Health)
6. Data (Settings_Section_Data)

Exakt placering: Lägg till Reminders-XAML direkt efter den avslutande `</Border>` för Training-sektionens enda kort (veckomålskortet), precis innan `<Label Text="{loc:Localize Settings_Section_Units}"`.

- [ ] **Step 1: Lägg till NotificationService i SettingsViewModel-konstruktorn**

Ändra:
```csharp
public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export) : ObservableObject
```
Till:
```csharp
public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export, NotificationService notifications) : ObservableObject
```

- [ ] **Step 2: Lägg till observable properties för ReminderDays och ReminderTimeMinutes**

Lägg till bland befintliga `[ObservableProperty]`-fält:

```csharp
[ObservableProperty] private int _reminderDays;
[ObservableProperty] private int _reminderTimeMinutes;
```

- [ ] **Step 3: Lägg till computed properties**

Lägg till efter HeightDisplay-propertyn:

```csharp
public string ReminderTimeDisplay
{
    get
    {
        var mins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
        return $"{mins / 60:D2}:{mins % 60:D2}";
    }
}

public string ReminderDisplay
{
    get
    {
        if (ReminderDays == 0) return AppResources.Settings_Reminders_Off;
        var dayLabels = new[]
        {
            AppResources.Settings_Reminders_Day_0,
            AppResources.Settings_Reminders_Day_1,
            AppResources.Settings_Reminders_Day_2,
            AppResources.Settings_Reminders_Day_3,
            AppResources.Settings_Reminders_Day_4,
            AppResources.Settings_Reminders_Day_5,
            AppResources.Settings_Reminders_Day_6,
        };
        var active = Enumerable.Range(0, 7)
            .Where(i => (ReminderDays & (1 << i)) != 0)
            .Select(i => dayLabels[i]);
        var mins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
        return $"{string.Join(", ", active)} · {mins / 60:D2}:{mins % 60:D2}";
    }
}

// Day chip colors (bit 0=Mån .. bit 6=Sön)
public Color Day0Bg => (ReminderDays & (1 << 0)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day1Bg => (ReminderDays & (1 << 1)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day2Bg => (ReminderDays & (1 << 2)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day3Bg => (ReminderDays & (1 << 3)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day4Bg => (ReminderDays & (1 << 4)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day5Bg => (ReminderDays & (1 << 5)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day6Bg => (ReminderDays & (1 << 6)) != 0 ? DesignTokens.Accent   : DesignTokens.Surface2;
public Color Day0Fg => (ReminderDays & (1 << 0)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day1Fg => (ReminderDays & (1 << 1)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day2Fg => (ReminderDays & (1 << 2)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day3Fg => (ReminderDays & (1 << 3)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day4Fg => (ReminderDays & (1 << 4)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day5Fg => (ReminderDays & (1 << 5)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
public Color Day6Fg => (ReminderDays & (1 << 6)) != 0 ? DesignTokens.FabForeground : DesignTokens.Text;
```

- [ ] **Step 4: Lägg till OnReminderDaysChanged och OnReminderTimeMinutesChanged partials**

```csharp
partial void OnReminderDaysChanged(int value)
{
    OnPropertyChanged(nameof(Day0Bg)); OnPropertyChanged(nameof(Day0Fg));
    OnPropertyChanged(nameof(Day1Bg)); OnPropertyChanged(nameof(Day1Fg));
    OnPropertyChanged(nameof(Day2Bg)); OnPropertyChanged(nameof(Day2Fg));
    OnPropertyChanged(nameof(Day3Bg)); OnPropertyChanged(nameof(Day3Fg));
    OnPropertyChanged(nameof(Day4Bg)); OnPropertyChanged(nameof(Day4Fg));
    OnPropertyChanged(nameof(Day5Bg)); OnPropertyChanged(nameof(Day5Fg));
    OnPropertyChanged(nameof(Day6Bg)); OnPropertyChanged(nameof(Day6Fg));
    OnPropertyChanged(nameof(ReminderDisplay));
}

partial void OnReminderTimeMinutesChanged(int value)
{
    OnPropertyChanged(nameof(ReminderTimeDisplay));
    OnPropertyChanged(nameof(ReminderDisplay));
}
```

- [ ] **Step 5: Läs in ReminderDays och ReminderTimeMinutes i LoadAsync**

I `LoadAsync`, efter `HeightCm = settings.HeightCm;`, lägg till:

```csharp
ReminderDays        = settings.ReminderDays;
ReminderTimeMinutes = settings.ReminderTimeMinutes;
```

- [ ] **Step 6: Lägg till ToggleReminderDayCommand**

```csharp
[RelayCommand]
private async Task ToggleReminderDayAsync(int bitIndex)
{
    ReminderDays ^= (1 << bitIndex);
    await SaveAndRescheduleAsync();
}
```

- [ ] **Step 7: Lägg till EditReminderTimeCommand**

```csharp
[RelayCommand]
private async Task EditReminderTimeAsync()
{
    var currentMins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
    var current = $"{currentMins / 60:D2}:{currentMins % 60:D2}";
    var result = await Shell.Current.DisplayPromptAsync(
        AppResources.Settings_Reminders_TimePrompt_Title,
        AppResources.Settings_Reminders_TimePrompt_Body,
        keyboard: Keyboard.Default,
        initialValue: current,
        maxLength: 5);
    if (result is null) return;
    var parts = result.Trim().Split(':');
    if (parts.Length != 2
        || !int.TryParse(parts[0], out var h) || h < 0 || h > 23
        || !int.TryParse(parts[1], out var m) || m < 0 || m > 59)
    {
        await Shell.Current.DisplayAlert(null, AppResources.Settings_Reminders_TimeInvalid, "OK");
        return;
    }
    ReminderTimeMinutes = h * 60 + m;
    await SaveAndRescheduleAsync();
}
```

- [ ] **Step 8: Lägg till SaveAndRescheduleAsync**

```csharp
private async Task SaveAndRescheduleAsync()
{
    var settings = await db.GetAppSettingsAsync();
    settings.ReminderDays        = ReminderDays;
    settings.ReminderTimeMinutes = ReminderTimeMinutes;
    await db.SaveAppSettingsAsync(settings);

    var mins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
    if (ReminderDays == 0)
        notifications.CancelReminders();
    else
        notifications.ScheduleReminders(ReminderDays, mins);
}
```

- [ ] **Step 9: Lägg till Reminders-sektion i SettingsPage.xaml**

Hitta raden:
```xml
<Label Text="{loc:Localize Settings_Section_Units}" Style="{StaticResource SectionLabel}" Margin="4,0"/>
```

Lägg till detta block PRECIS INNAN den raden:

```xml
<!-- Reminders section -->
<Label Text="{loc:Localize Settings_Section_Reminders}" Style="{StaticResource SectionLabel}" Margin="4,0"/>
<Border Style="{StaticResource CardFrame}" Padding="0">
    <VerticalStackLayout Spacing="0">
        <!-- Header row -->
        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12" Padding="16,14,16,10">
            <Border Grid.Column="0" BackgroundColor="{StaticResource ForgeSuccessDim}"
                    StrokeShape="RoundRectangle 10" StrokeThickness="0"
                    WidthRequest="38" HeightRequest="38" VerticalOptions="Center">
                <Label Text="🔔" FontSize="18" HorizontalOptions="Center" VerticalOptions="Center"/>
            </Border>
            <StackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                <Label Text="{loc:Localize Settings_Reminders_Title}" FontFamily="DMSansMedium" FontSize="15"/>
                <Label Text="{Binding ReminderDisplay}" Style="{StaticResource MutedLabel}"/>
            </StackLayout>
        </Grid>
        <!-- Day chips -->
        <HorizontalStackLayout Spacing="6" HorizontalOptions="Center" Padding="0,0,0,12">
            <Border BackgroundColor="{Binding Day0Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_0}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day0Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 0}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day1Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_1}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day1Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 1}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day2Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_2}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day2Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 2}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day3Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_3}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day3Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 3}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day4Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_4}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day4Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 4}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day5Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_5}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day5Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 5}"/>
                </Border.GestureRecognizers>
            </Border>
            <Border BackgroundColor="{Binding Day6Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                    WidthRequest="36" HeightRequest="36">
                <Label Text="{loc:Localize Settings_Reminders_Day_6}" FontFamily="DMSansMedium" FontSize="11"
                       TextColor="{Binding Day6Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 6}"/>
                </Border.GestureRecognizers>
            </Border>
        </HorizontalStackLayout>
        <!-- Divider -->
        <BoxView HeightRequest="1" BackgroundColor="{StaticResource ForgeSurface2}"/>
        <!-- Time row -->
        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8" Padding="16,12">
            <Label Grid.Column="0" Text="{loc:Localize Settings_Reminders_Time_Label}"
                   FontFamily="DMSansMedium" FontSize="14" VerticalOptions="Center"/>
            <Label Grid.Column="1" Text="{Binding ReminderTimeDisplay}"
                   Style="{StaticResource MutedLabel}" VerticalOptions="Center"/>
            <Grid.GestureRecognizers>
                <TapGestureRecognizer Command="{Binding EditReminderTimeCommand}"/>
            </Grid.GestureRecognizers>
        </Grid>
    </VerticalStackLayout>
</Border>
```

- [ ] **Step 10: Bygg och verifiera**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 11: Commit**

```bash
git add LockIn/ViewModels/SettingsViewModel.cs LockIn/Views/SettingsPage.xaml
git commit -m "feat(reminders): add day chips and time picker to SettingsPage"
```
