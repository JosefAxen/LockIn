using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;

namespace LockIn.ViewModels;

public partial class SettingsViewModel(DatabaseService db, IHealthService health) : ObservableObject
{
    [ObservableProperty] private bool _useKg = true;
    [ObservableProperty] private string _appVersion = "";
    [ObservableProperty] private bool _hapticEnabled = true;
    [ObservableProperty] private bool _soundEnabled = true;
    [ObservableProperty] private int _weeklyGoal = 4;
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private bool _healthKitSyncEnabled;

    public string WeeklyGoalDisplay =>
        string.Format(AppResources.Settings_WeeklyGoal_Format, WeeklyGoal);

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
    }

    partial void OnWeeklyGoalChanged(int value) =>
        OnPropertyChanged(nameof(WeeklyGoalDisplay));

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
}
