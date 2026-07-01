using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class MuscleVolumeViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<MuscleVolumeRow> Rows { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Rows.Clear();
            var all = await db.GetMuscleVolumeLandmarksAsync();
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
        foreach (var row in Rows)
        {
            if (!int.TryParse(row.MevText, out var mev) || mev <= 0) continue;
            if (!int.TryParse(row.MrvText, out var mrv) || mrv <= 0) continue;
            if (mrv < mev) mrv = mev; // enforce mrv >= mev
            await db.SaveMuscleVolumeLandmarksAsync(new MuscleVolumeLandmarks
            {
                MuscleGroup = row.Muscle,
                MEV = mev,
                MRV = mrv,
            });
        }
        await Toast.Make(AppResources.MuscleVolume_Saved_Toast).Show();
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
