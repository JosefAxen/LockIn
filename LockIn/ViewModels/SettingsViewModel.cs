using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;

namespace LockIn.ViewModels;

public partial class SettingsViewModel(DatabaseService db, IHealthService health, ExportService export, NotificationService notifications, ISettingsRepository settingsRepo) : ObservableObject
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
    [ObservableProperty] private string _reminderLabels = "";
    // Bunden till TimePicker (ersätter fritext HH:MM)
    [ObservableProperty] private TimeSpan? _reminderTime;
    private bool _isLoadingReminderTime;

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

    private string[] ParseLabels() =>
        ReminderLabels.Split(',').Concat(Enumerable.Repeat("", 7)).Take(7).ToArray();

    public string Day0Label => ParseLabels()[0];
    public string Day1Label => ParseLabels()[1];
    public string Day2Label => ParseLabels()[2];
    public string Day3Label => ParseLabels()[3];
    public string Day4Label => ParseLabels()[4];
    public string Day5Label => ParseLabels()[5];
    public string Day6Label => ParseLabels()[6];

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

    // VoiceOver: dagens namn + om påminnelsen är på/av (valet syns annars bara som färg)
    public string Day0A11y => DayA11y(0);
    public string Day1A11y => DayA11y(1);
    public string Day2A11y => DayA11y(2);
    public string Day3A11y => DayA11y(3);
    public string Day4A11y => DayA11y(4);
    public string Day5A11y => DayA11y(5);
    public string Day6A11y => DayA11y(6);
    public string Day0LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(0));
    public string Day1LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(1));
    public string Day2LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(2));
    public string Day3LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(3));
    public string Day4LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(4));
    public string Day5LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(5));
    public string Day6LabelA11y => string.Format(AppResources.Settings_Reminders_LabelA11y_Format, DayName(6));

    private string DayA11y(int bit) => string.Format(
        (ReminderDays & (1 << bit)) != 0 ? AppResources.Settings_Reminders_DayOn_Format : AppResources.Settings_Reminders_DayOff_Format,
        DayName(bit));

    private static string DayName(int bit) => bit switch
    {
        0 => AppResources.Settings_Reminders_Day_0,
        1 => AppResources.Settings_Reminders_Day_1,
        2 => AppResources.Settings_Reminders_Day_2,
        3 => AppResources.Settings_Reminders_Day_3,
        4 => AppResources.Settings_Reminders_Day_4,
        5 => AppResources.Settings_Reminders_Day_5,
        _ => AppResources.Settings_Reminders_Day_6,
    };

    public async Task LoadAsync()
    {
        var settings = await settingsRepo.GetAsync();
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
        ReminderLabels = settings.ReminderLabels ?? "";

        _isLoadingReminderTime = true;
        ReminderTime = TimeSpan.FromMinutes(ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480);
        _isLoadingReminderTime = false;
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
        OnPropertyChanged(nameof(Day0A11y)); OnPropertyChanged(nameof(Day1A11y));
        OnPropertyChanged(nameof(Day2A11y)); OnPropertyChanged(nameof(Day3A11y));
        OnPropertyChanged(nameof(Day4A11y)); OnPropertyChanged(nameof(Day5A11y));
        OnPropertyChanged(nameof(Day6A11y));
        OnPropertyChanged(nameof(ReminderDisplay));
    }

    partial void OnReminderTimeMinutesChanged(int value)
    {
        OnPropertyChanged(nameof(ReminderTimeDisplay));
        OnPropertyChanged(nameof(ReminderDisplay));
    }

    // TimePicker → spara och schemalägg om (inte vid laddning)
    partial void OnReminderTimeChanged(TimeSpan? value)
    {
        if (_isLoadingReminderTime || value is not TimeSpan t) return;
        var mins = (int)t.TotalMinutes;
        if (mins == ReminderTimeMinutes) return;
        ReminderTimeMinutes = mins;
        _ = SaveAndRescheduleSafeAsync();
    }

    private async Task SaveAndRescheduleSafeAsync()
    {
        try
        {
            await SaveAndRescheduleAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Kunde inte spara påminnelsetid: {ex}");
            await Toast.Make(AppResources.Common_ErrorRetry).Show();
        }
    }

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
        var settings = await settingsRepo.GetAsync();
        settings.WeightUnit = UseKg ? WeightUnit.Kg : WeightUnit.Lbs;
        await settingsRepo.SaveAsync(settings);
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
        var settings = await settingsRepo.GetAsync();
        settings.UserName = trimmed;
        await settingsRepo.SaveAsync(settings);
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
        var settings = await settingsRepo.GetAsync();
        settings.WeeklyWorkoutGoal = goal;
        await settingsRepo.SaveAsync(settings);
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
        var settings = await settingsRepo.GetAsync();
        settings.HeightCm = cm;
        await settingsRepo.SaveAsync(settings);
    }

    [RelayCommand]
    private async Task ToggleReminderDayAsync(int bitIndex)
    {
        ReminderDays ^= (1 << bitIndex);
        await SaveAndRescheduleAsync();
    }

    private async Task SaveAndRescheduleAsync()
    {
        var settings = await settingsRepo.GetAsync();
        settings.ReminderDays        = ReminderDays;
        settings.ReminderTimeMinutes = ReminderTimeMinutes;
        settings.ReminderLabels      = ReminderLabels;
        await settingsRepo.SaveAsync(settings);

        var mins = ReminderTimeMinutes > 0 ? ReminderTimeMinutes : 480;
        if (ReminderDays == 0)
            notifications.CancelReminders();
        else
            notifications.ScheduleReminders(ReminderDays, mins, ParseLabels());
    }

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

    [RelayCommand]
    private async Task OpenHealthSettingsAsync()
        => await Launcher.OpenAsync("x-apple-health://");

    [RelayCommand]
    private async Task OpenBodyWeightAsync() =>
        await Shell.Current.GoToAsync(AppRoutes.BodyWeight);

    [RelayCommand]
    private async Task OpenProgressPhotosAsync() =>
        await Shell.Current.GoToAsync(AppRoutes.ProgressPhotos);

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
