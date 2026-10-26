using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class MuscleVolumeViewModel(IMesocycleRepository mesocyclesRepo) : ObservableObject
{
    public ObservableCollection<MuscleVolumeRow> Rows { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Rows.Clear();
            var all = await mesocyclesRepo.GetLandmarksAsync();
            var order = new[]
            {
                MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
                MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Legs,
                MuscleGroup.Core, MuscleGroup.Forearms,
            };
            foreach (var mg in order)
            {
                var entry = all.FirstOrDefault(l => l.MuscleGroup == mg)
                    ?? new MuscleVolumeLandmarks { MuscleGroup = mg, MEV = 8, MRV = 18 };
                Rows.Add(new MuscleVolumeRow
                {
                    Muscle = mg,
                    DisplayName = MuscleDisplayName(mg),
                    MevText = entry.MEV.ToString(),
                    MrvText = entry.MRV.ToString(),
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Ogiltiga rader sparas inte — och användaren får veta vilka.
        // Giltigt: heltal > 0 och MRV ≥ MEV (MRV justeras inte i tysthet).
        var notSaved = new List<string>();
        try
        {
            foreach (var row in Rows)
            {
                if (!int.TryParse(row.MevText?.Trim(), out var mev) || mev <= 0
                    || !int.TryParse(row.MrvText?.Trim(), out var mrv) || mrv <= 0
                    || mrv < mev)
                {
                    notSaved.Add(row.DisplayName);
                    continue;
                }
                await mesocyclesRepo.SaveLandmarksAsync(new MuscleVolumeLandmarks
                {
                    MuscleGroup = row.Muscle,
                    MEV = mev,
                    MRV = mrv,
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MuscleVolume] Kunde inte spara: {ex}");
            await Toast.Make(AppResources.Common_ErrorRetry).Show();
            return;
        }

        if (notSaved.Count == 0)
        {
            await Toast.Make(AppResources.MuscleVolume_Saved_Toast).Show();
            return;
        }

        if (Shell.Current?.CurrentPage is { } page)
            await page.DisplayAlert(
                AppResources.MuscleVolume_Invalid_Title,
                string.Format(AppResources.MuscleVolume_Invalid_Body_Format, string.Join(", ", notSaved)),
                AppResources.Common_OK);
    }

    private static string MuscleDisplayName(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => AppResources.Library_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Library_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Library_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Library_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Library_Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Library_Muscle_Core,
        MuscleGroup.Forearms  => AppResources.Library_Muscle_Forearms,
        _                     => AppResources.Library_Muscle_Other,
    };
}

public partial class MuscleVolumeRow : ObservableObject
{
    public MuscleGroup Muscle { get; set; }
    public string DisplayName { get; set; } = "";
    [ObservableProperty] private string _mevText = "";
    [ObservableProperty] private string _mrvText = "";
}
