# Vila-dag-schemaläggning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Utöka befintliga träningspåminnelser med per-dag-etiketter (t.ex. "Bendag") som visas i iOS-notisens brödtext och som en liten text under varje vald dag-chip i SettingsPage.

**Architecture:** `AppSettings` får ett nytt `ReminderLabels`-fält (kommaseparerad sträng med 7 slots). `NotificationService.ScheduleReminders` tar ett extra `string[]`-argument och väljer etikett-format om labeln är satt. `SettingsViewModel` exponerar `Day0Label`..`Day6Label` och ett `EditReminderLabelCommand(int)` via `DisplayPromptAsync`. `SettingsPage.xaml` wrapping varje chip i en `VerticalStackLayout` med en liten tappbar label-rad under.

**Tech Stack:** .NET MAUI 10 iOS, sqlite-net-pcl, UserNotifications, CommunityToolkit.Mvvm 8.4.2

## Global Constraints

- Alla färger via `StaticResource` (XAML) eller `DesignTokens.*` (C#) — inga hårdkodade hex
- `[RelayCommand]` på `async Task`-metoder — aldrig på `void`
- i18n: alla UI-strängar via `AppResources` + `.resx` + `.en.resx` + `AppResources.cs`
- `#if IOS` skyddar all `UserNotifications`-kod i `NotificationService`
- Idempotent SQLite-migration via try-catch på `"duplicate column"` (case-insensitive)
- Commit efter varje task

---

### Task 1: AppSettings + DB-migration + i18n

**Files:**
- Modify: `LockIn/Models/AppSettings.cs`
- Modify: `LockIn/Services/DatabaseService.cs` (runt rad 74)
- Modify: `LockIn/Resources/Strings/AppResources.resx` (efter rad 868, före `</root>`)
- Modify: `LockIn/Resources/Strings/AppResources.en.resx` (samma position)
- Modify: `LockIn/Resources/Strings/AppResources.cs` (efter rad 527)

**Interfaces:**
- Produces: `AppSettings.ReminderLabels` (string, default `""`)
- Produces: i18n-nycklarna `Settings_Reminders_Label_Prompt_Title`, `Settings_Reminders_Label_Prompt_Body`, `Notification_Reminder_Body_Label_Format`

- [ ] **Step 1: Lägg till ReminderLabels i AppSettings.cs**

Öppna `LockIn/Models/AppSettings.cs`. Lägg till direkt efter `public int ReminderTimeMinutes { get; set; } = 0;` (rad 28):

```csharp
public string ReminderLabels { get; set; } = "";
```

Filen ser nu ut:
```csharp
public int ReminderDays        { get; set; } = 0;
public int ReminderTimeMinutes { get; set; } = 0;
public string ReminderLabels   { get; set; } = "";
```

- [ ] **Step 2: Lägg till DB-migration i DatabaseService.cs**

Öppna `LockIn/Services/DatabaseService.cs`. Hitta de senaste `ALTER TABLE AppSettings`-migreringsblocken (runt rad 72–75). Lägg till direkt efter det sista blocket:

```csharp
try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderLabels TEXT NOT NULL DEFAULT ''"); }
catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
```

- [ ] **Step 3: Lägg till i18n-nycklar i AppResources.resx (svenska)**

Öppna `LockIn/Resources/Strings/AppResources.resx`. Hitta `<!-- Notifications -->` (runt rad 848). Lägg till under `Notification_Reminder_Title`:

```xml
  <data name="Notification_Reminder_Body_Label_Format" xml:space="preserve"><value>Dags med {0}! Öppna appen och sätt igång 💪</value></data>
```

Hitta `Settings_Section_Reminders` (runt rad 868). Lägg till direkt efter den:

```xml
  <data name="Settings_Reminders_Label_Prompt_Title" xml:space="preserve"><value>Dagsetikett</value></data>
  <data name="Settings_Reminders_Label_Prompt_Body" xml:space="preserve"><value>Kort namn för träningsdagen (t.ex. "Bendag") — lämna tomt för att ta bort</value></data>
```

- [ ] **Step 4: Lägg till samma nycklar i AppResources.en.resx (engelska)**

Exakt samma positioner i den engelska filen:

Under `Notification_Reminder_Title`:
```xml
  <data name="Notification_Reminder_Body_Label_Format" xml:space="preserve"><value>Time for {0}! Open the app and get going 💪</value></data>
```

Efter `Settings_Section_Reminders`:
```xml
  <data name="Settings_Reminders_Label_Prompt_Title" xml:space="preserve"><value>Day label</value></data>
  <data name="Settings_Reminders_Label_Prompt_Body" xml:space="preserve"><value>Short name for this training day (e.g. "Leg day") — leave empty to remove</value></data>
```

- [ ] **Step 5: Lägg till C#-properties i AppResources.cs**

Öppna `LockIn/Resources/Strings/AppResources.cs`.

Efter `Notification_RestTimer_Body_Format` (runt rad 82), lägg till:
```csharp
    public static string Notification_Reminder_Body_Label_Format => Get(nameof(Notification_Reminder_Body_Label_Format));
```

Efter `Settings_Reminders_Title` (runt rad 527), lägg till:
```csharp
    public static string Settings_Reminders_Label_Prompt_Title => Get(nameof(Settings_Reminders_Label_Prompt_Title));
    public static string Settings_Reminders_Label_Prompt_Body  => Get(nameof(Settings_Reminders_Label_Prompt_Body));
```

- [ ] **Step 6: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```
Förväntat: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add LockIn/Models/AppSettings.cs LockIn/Services/DatabaseService.cs LockIn/Resources/Strings/AppResources.resx LockIn/Resources/Strings/AppResources.en.resx LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(reminders): AppSettings.ReminderLabels, DB-migration och i18n-nycklar"
```

---

### Task 2: NotificationService + SettingsViewModel

**Files:**
- Modify: `LockIn/Services/NotificationService.cs`
- Modify: `LockIn/ViewModels/SettingsViewModel.cs`

**Interfaces:**
- Consumes: `AppSettings.ReminderLabels` (string), i18n-nycklar från Task 1
- Produces: `NotificationService.ScheduleReminders(int daysBitmask, int timeMinutes, string[] labels)`
- Produces: `SettingsViewModel.Day0Label`..`Day6Label` (string computed properties)
- Produces: `SettingsViewModel.EditReminderLabelCommand` (RelayCommand, int-parameter)

- [ ] **Step 1: Uppdatera ScheduleReminders i NotificationService.cs**

Ersätt hela `ScheduleReminders`-metoden med en ny signatur som tar ett `string[] labels`-argument. Den befintliga signaturen är `public void ScheduleReminders(int daysBitmask, int timeMinutes)`.

Ny metod (ersätter helt):

```csharp
public void ScheduleReminders(int daysBitmask, int timeMinutes, string[] labels)
{
    CancelReminders();
#if IOS
    var h = timeMinutes / 60;
    var m = timeMinutes % 60;
    var iOSWeekdays = new[] { 2, 3, 4, 5, 6, 7, 1 };
    for (var bit = 0; bit < 7; bit++)
    {
        if ((daysBitmask & (1 << bit)) == 0) continue;
        var label = labels.Length > bit ? labels[bit].Trim() : "";
        var body  = label.Length > 0
            ? string.Format(AppResources.Notification_Reminder_Body_Label_Format, label)
            : AppResources.Notification_Reminder_Body;
        var content = new UNMutableNotificationContent
        {
            Title = AppResources.Notification_Reminder_Title,
            Body  = body,
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

- [ ] **Step 2: Lägg till _reminderLabels i SettingsViewModel.cs**

Öppna `LockIn/ViewModels/SettingsViewModel.cs`. Lägg till det nya fältet efter `_reminderTimeMinutes` (rad 23):

```csharp
[ObservableProperty] private string _reminderLabels = "";
```

- [ ] **Step 3: Lägg till ParseLabels-hjälpmetod och Day0Label..Day6Label**

Lägg till dessa computed properties och hjälpmetod direkt efter `ReminderDisplay`-egenskapen (efter klosande `}` för `ReminderDisplay`, runt rad 63):

```csharp
private string[] ParseLabels() =>
    ReminderLabels.Split(',').Concat(Enumerable.Repeat("", 7)).Take(7).ToArray();

public string Day0Label => ParseLabels()[0];
public string Day1Label => ParseLabels()[1];
public string Day2Label => ParseLabels()[2];
public string Day3Label => ParseLabels()[3];
public string Day4Label => ParseLabels()[4];
public string Day5Label => ParseLabels()[5];
public string Day6Label => ParseLabels()[6];
```

- [ ] **Step 4: Lägg till OnReminderLabelsChanged partial**

Lägg till direkt efter `OnReminderTimeMinutesChanged` (runt rad 118):

```csharp
partial void OnReminderLabelsChanged(string value)
{
    OnPropertyChanged(nameof(Day0Label));
    OnPropertyChanged(nameof(Day1Label));
    OnPropertyChanged(nameof(Day2Label));
    OnPropertyChanged(nameof(Day3Label));
    OnPropertyChanged(nameof(Day4Label));
    OnPropertyChanged(nameof(Day5Label));
    OnPropertyChanged(nameof(Day6Label));
}
```

- [ ] **Step 5: Ladda ReminderLabels i LoadAsync**

I `LoadAsync`-metoden (runt rad 94), lägg till direkt efter `ReminderTimeMinutes = settings.ReminderTimeMinutes;`:

```csharp
ReminderLabels = settings.ReminderLabels ?? "";
```

- [ ] **Step 6: Uppdatera SaveAndRescheduleAsync**

Ersätt hela `SaveAndRescheduleAsync`-metoden (runt rad 221–233):

```csharp
private async Task SaveAndRescheduleAsync()
{
    var settings = await db.GetAppSettingsAsync();
    settings.ReminderDays        = ReminderDays;
    settings.ReminderTimeMinutes = ReminderTimeMinutes;
    settings.ReminderLabels      = ReminderLabels;
    await db.SaveAppSettingsAsync(settings);

    var mins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
    if (ReminderDays == 0)
        notifications.CancelReminders();
    else
        notifications.ScheduleReminders(ReminderDays, mins, ParseLabels());
}
```

- [ ] **Step 7: Lägg till EditReminderLabelAsync-kommando**

Lägg till direkt efter `EditReminderTimeAsync`-metoden (runt rad 219):

```csharp
[RelayCommand]
private async Task EditReminderLabelAsync(int bitIndex)
{
    var labels  = ParseLabels();
    var current = labels[bitIndex];
    var result  = await Shell.Current.DisplayPromptAsync(
        AppResources.Settings_Reminders_Label_Prompt_Title,
        AppResources.Settings_Reminders_Label_Prompt_Body,
        initialValue: current,
        maxLength: 12);
    if (result is null) return;
    labels[bitIndex] = result.Trim();
    ReminderLabels   = string.Join(",", labels);
    await SaveAndRescheduleAsync();
}
```

- [ ] **Step 8: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```
Förväntat: `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add LockIn/Services/NotificationService.cs LockIn/ViewModels/SettingsViewModel.cs
git commit -m "feat(reminders): per-dag-etiketter i notiskropp + EditReminderLabelCommand"
```

---

### Task 3: SettingsPage.xaml — label under dag-chip

**Files:**
- Modify: `LockIn/Views/SettingsPage.xaml` (dag-chips-blocket, rad 175–232)

**Interfaces:**
- Consumes: `Day0Label`..`Day6Label` (string), `EditReminderLabelCommand` (int-param) från Task 2
- Consumes: `ToggleReminderDayCommand` (int-param) — befintlig, oförändrad

- [ ] **Step 1: Ersätt dag-chip-blocket i SettingsPage.xaml**

Hitta hela `<HorizontalStackLayout Spacing="6" HorizontalOptions="Center" Padding="0,0,0,12">...</HorizontalStackLayout>` (rad 175–232). Ersätt det med nedanstående block. Varje dag-chip wrappas i en `VerticalStackLayout` med en liten tappbar etikett-label under.

Befintligt block att ersätta (rad 175–232):
```xml
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
```

Nytt block:
```xml
                        <HorizontalStackLayout Spacing="6" HorizontalOptions="Center" Padding="0,0,0,12">
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day0Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_0}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day0Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 0}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day0Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 0}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day1Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_1}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day1Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 1}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day1Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 1}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day2Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_2}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day2Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 2}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day2Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 2}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day3Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_3}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day3Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 3}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day3Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 3}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day4Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_4}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day4Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 4}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day4Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 4}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day5Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_5}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day5Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 5}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day5Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 5}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                            <VerticalStackLayout Spacing="2" WidthRequest="36">
                                <Border BackgroundColor="{Binding Day6Bg}" StrokeShape="RoundRectangle 8" StrokeThickness="0"
                                        WidthRequest="36" HeightRequest="36">
                                    <Label Text="{loc:Localize Settings_Reminders_Day_6}" FontFamily="DMSansMedium" FontSize="11"
                                           TextColor="{Binding Day6Fg}" HorizontalOptions="Center" VerticalOptions="Center"/>
                                    <Border.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding ToggleReminderDayCommand}" CommandParameter="{x:Int32 6}"/>
                                    </Border.GestureRecognizers>
                                </Border>
                                <Label Text="{Binding Day6Label}" FontFamily="DMSansRegular" FontSize="8"
                                       TextColor="{StaticResource ForgeAccent}" HorizontalOptions="Center"
                                       LineBreakMode="TailTruncation">
                                    <Label.GestureRecognizers>
                                        <TapGestureRecognizer Command="{Binding EditReminderLabelCommand}" CommandParameter="{x:Int32 6}"/>
                                    </Label.GestureRecognizers>
                                </Label>
                            </VerticalStackLayout>
                        </HorizontalStackLayout>
```

- [ ] **Step 2: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```
Förväntat: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add LockIn/Views/SettingsPage.xaml
git commit -m "feat(reminders): dag-etikett-rad under chips i SettingsPage"
```
