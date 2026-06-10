using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class SettingsViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private bool _useKg = true;
    [ObservableProperty] private string _appVersion = "";

    public async Task LoadAsync()
    {
        var settings = await db.GetSettingsAsync();
        UseKg = settings.WeightUnit == WeightUnit.Kg;
        AppVersion = AppInfo.VersionString;
    }

    partial void OnUseKgChanged(bool value) => _ = SaveSettingsAsync();

    private async Task SaveSettingsAsync()
    {
        var settings = await db.GetSettingsAsync();
        settings.WeightUnit = UseKg ? WeightUnit.Kg : WeightUnit.Lbs;
        await db.SaveSettingsAsync(settings);
    }

    [RelayCommand]
    private async Task ClearAllDataAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Rensa all data",
            "All träningsdata, mallar och historik tas bort permanent. Övningsbiblioteket återställs.",
            "Rensa", "Avbryt");
        if (!confirmed) return;

        await db.DeleteAllDataAsync();
        await Shell.Current.DisplayAlert("Klart", "All data har rensats.", "OK");
    }
}
