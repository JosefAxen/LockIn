using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class WeeklyVolumeViewModel(IWeeklyVolumeService volumes) : ObservableObject
{
    public ObservableCollection<WeeklyVolumeRow> Rows { get; } = new();
    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var reports = await volumes.GetCurrentWeekVolumeAsync();
            Rows.Clear();
            foreach (var r in reports)
            {
                Rows.Add(new WeeklyVolumeRow
                {
                    DisplayName = MuscleDisplayName(r.Muscle),
                    Report      = r,
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
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

public class WeeklyVolumeRow
{
    public string DisplayName { get; set; } = "";
    public WeeklyVolumeReport Report { get; set; } = null!;

    public string SetsText => $"{Report.SetsThisWeek} / {Report.MRV}";
    public string RangeText => string.Format(AppResources.WeeklyVolume_Range_Format, Report.MEV, Report.MRV);
    public double ProgressFraction => Report.MRV > 0 ? Math.Min(1.0, (double)Report.SetsThisWeek / Report.MRV) : 0;

    public Color ZoneColor => Report.Zone switch
    {
        VolumeZone.UnderMEV => Color.FromArgb("#A2A2A2"),  // grå
        VolumeZone.InMAV    => Color.FromArgb("#4ADE80"),  // grön
        VolumeZone.NearMRV  => Color.FromArgb("#FBBF24"),  // gul
        VolumeZone.OverMRV  => Color.FromArgb("#FB7185"),  // röd
        _                   => Color.FromArgb("#A2A2A2"),
    };

    public string ZoneLabel => Report.Zone switch
    {
        VolumeZone.UnderMEV => AppResources.WeeklyVolume_Zone_UnderMEV,
        VolumeZone.InMAV    => AppResources.WeeklyVolume_Zone_InMAV,
        VolumeZone.NearMRV  => AppResources.WeeklyVolume_Zone_NearMRV,
        VolumeZone.OverMRV  => AppResources.WeeklyVolume_Zone_OverMRV,
        _                   => "",
    };
}
