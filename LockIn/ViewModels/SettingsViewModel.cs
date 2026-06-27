using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;

namespace LockIn.ViewModels;

public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export, NotificationService notifications) : ObservableObject
{
    [ObservableProperty] private bool _useKg = true;
    [ObservableProperty] private string _appVersion = "";
    [ObservableProperty] private bool _hapticEnabled = true;
    [ObservableProperty] private bool _soundEnabled = true;
    [ObservableProperty] private int _weeklyGoal = 4;
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private int _heightCm;
    [ObservableProperty] private bool _healthKitSyncEnabled;
    [ObservableProperty] private int _reminderDays;
    [ObservableProperty] private int _reminderTimeMinutes;

    public string WeeklyGoalDisplay =>
        string.Format(AppResources.Settings_WeeklyGoal_Format, WeeklyGoal);

    public string HeightDisplay =>
        HeightCm > 0
            ? string.Format(AppResources.Settings_Height_Format, HeightCm)
            : "–";

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

    public async Task LoadAsync()
    {
        var settings = await db.GetAppSettingsAsync();
        UseKg      = settings.WeightUnit == WeightUnit.Kg;
        WeeklyGoal = settings.WeeklyWorkoutGoal > 0 ? settings.WeeklyWorkoutGoal : 4;
        HapticEnabled        = Preferences.Default.Get("haptic_enabled", true);
        SoundEnabled         = Preferences.Default.Get("sound_enabled", true);
        HealthKitSyncEnabled = Preferences.Default.Get("healthkit_sync_enabled", false);
        AppVersion = AppInfo.VersionString;
        UserName   = settings.UserName ?? "";
        HeightCm   = settings.HeightCm;
        ReminderDays        = settings.ReminderDays;
        ReminderTimeMinutes = settings.ReminderTimeMinutes;
    }

    partial void OnWeeklyGoalChanged(int value) =>
        OnPropertyChanged(nameof(WeeklyGoalDisplay));

    partial void OnHeightCmChanged(int value) =>
        OnPropertyChanged(nameof(HeightDisplay));

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

    partial void OnUseKgChanged(bool value) => _ = SaveSettingsAsync();

    partial void OnHapticEnabledChanged(bool value) =>
        Preferences.Default.Set("haptic_enabled", value);

    partial void OnSoundEnabledChanged(bool value) =>
        Preferences.Default.Set("sound_enabled", value);

    partial void OnHealthKitSyncEnabledChanged(bool value)
    {
        Preferences.Default.Set("healthkit_sync_enabled", value);
        if (value) _ = health.RequestPermissionsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        var settings = await db.GetAppSettingsAsync();
        settings.WeightUnit = UseKg ? WeightUnit.Kg : WeightUnit.Lbs;
        await db.SaveAppSettingsAsync(settings);
    }

    [RelayCommand]
    private async Task EditUserNameAsync()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.Settings_EditName_Title,
            AppResources.Settings_EditName_Body,
            initialValue: UserName,
            maxLength: 30);
        if (result is null) return;
        var trimmed = result.Trim();
        if (trimmed.Length == 0) return;
        UserName = trimmed;
        var settings = await db.GetAppSettingsAsync();
        settings.UserName = trimmed;
        await db.SaveAppSettingsAsync(settings);
    }

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

    [RelayCommand]
    private async Task ToggleReminderDayAsync(int bitIndex)
    {
        ReminderDays ^= (1 << bitIndex);
        await SaveAndRescheduleAsync();
    }

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

    [RelayCommand]
    private async Task OpenHealthSettingsAsync()
        => await Launcher.OpenAsync("x-apple-health://");

    [RelayCommand]
    private async Task OpenBodyWeightAsync() =>
        await Shell.Current.GoToAsync(nameof(BodyWeightPage));

    [RelayCommand]
    private async Task OpenProgressPhotosAsync() =>
        await Shell.Current.GoToAsync(nameof(ProgressPhotosPage));

    [RelayCommand]
    private async Task ClearAllDataAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.Settings_ClearData_Button,
            AppResources.Settings_ClearData_Body,
            AppResources.Settings_ClearData_Confirm,
            AppResources.Common_Cancel);
        if (!confirmed) return;

        await db.DeleteAllDataAsync();
        await Toast.Make(AppResources.Settings_ClearData_Toast).Show();
    }

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
}
