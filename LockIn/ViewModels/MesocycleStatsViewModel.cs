using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

// RP full-klon Task 28: MesocycleStatsPage.
// Detaljerad stats-vy för aktiv eller vald mesocykel — vecko-översikt,
// volym-progression, RIR-mål-progression och deload-status.
// TODO: SkiaSharp volym-graf och joint pain heatmap skjutna till framtida polish-task.
public partial class MesocycleStatsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IMesocycleService _mesocycles;
    private readonly IWeeklyVolumeService _weekly;
    private readonly IVolumeProgressionService _volumeProgression;
    private int? _mesocycleId;

    [ObservableProperty] private string _mesocycleName = "";
    [ObservableProperty] private string _statusBadge = "";
    [ObservableProperty] private Color _statusBadgeColor = Colors.Gray;
    [ObservableProperty] private Color _statusBadgeTextColor = Colors.White;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _weekProgressText = "";
    [ObservableProperty] private double _weekProgressPercent;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasMesocycle;
    [ObservableProperty] private bool _isDeloadActive;
    [ObservableProperty] private string _deloadStatusText = "";
    [ObservableProperty] private bool _hasDeload;

    public ObservableCollection<WeekTile> Weeks { get; } = new();
    public ObservableCollection<VolumeBarRow> VolumeBars { get; } = new();
    public ObservableCollection<RirPoint> RirProgression { get; } = new();

    public bool HasNoMesocycle => !HasMesocycle && !IsBusy;

    partial void OnHasMesocycleChanged(bool value) => OnPropertyChanged(nameof(HasNoMesocycle));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(HasNoMesocycle));

    public MesocycleStatsViewModel(
        IMesocycleService mesocycles,
        IWeeklyVolumeService weekly,
        IVolumeProgressionService volumeProgression)
    {
        _mesocycles = mesocycles;
        _weekly = weekly;
        _volumeProgression = volumeProgression;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("mesocycleId", out var v) && v is not null
            && int.TryParse(v.ToString(), out var id) && id > 0)
            _mesocycleId = id;
        else
            _mesocycleId = null;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Mesocycle? meso;
            if (_mesocycleId is int id)
                meso = (await _mesocycles.GetDetailAsync(id))?.Meso;
            else
                meso = await _mesocycles.GetActiveAsync();

            HasMesocycle = meso is not null;
            if (meso is null)
            {
                Weeks.Clear();
                VolumeBars.Clear();
                RirProgression.Clear();
                MesocycleName = "";
                StatusBadge = "";
                WeekProgressText = "";
                WeekProgressPercent = 0;
                HasDeload = false;
                DeloadStatusText = "";
                IsDeloadActive = false;
                return;
            }
            _mesocycleId = meso.Id;

            var detail = await _mesocycles.GetDetailAsync(meso.Id);
            if (detail is null) return;

            MesocycleName = string.IsNullOrWhiteSpace(meso.Name) ? "—" : meso.Name;

            // Status räknas från cykelns egna datum (inte bara den aktiva cykeln), så att
            // en avslutad eller ersatt cykel aldrig visar "Vecka 0 av N".
            var elapsedDays = (DateTime.Today - meso.StartDate.Date).Days;
            bool isUpcoming = elapsedDays < 0;
            bool isFinished = !isUpcoming && (!meso.IsActive || elapsedDays >= meso.WeekCount * 7);
            IsRunning = !isUpcoming && !isFinished;
            int currentWeekNumber = IsRunning ? elapsedDays / 7 + 1 : 0;

            if (IsRunning)
            {
                StatusBadge = AppResources.MesoStats_Status_Active;
                StatusBadgeColor = Color.FromArgb("#4ADE80");
                StatusBadgeTextColor = DesignTokens.FabForeground;   // mörk text på grönt
            }
            else if (isUpcoming)
            {
                StatusBadge = AppResources.MesoStats_Status_Upcoming;
                StatusBadgeColor = Color.FromArgb("#71717A");
                StatusBadgeTextColor = Colors.White;
            }
            else
            {
                StatusBadge = AppResources.MesoStats_Status_Done;
                StatusBadgeColor = Color.FromArgb("#52525B");
                StatusBadgeTextColor = Colors.White;
            }

            if (IsRunning)
            {
                WeekProgressText = string.Format(
                    AppResources.MesoStats_WeekProgress_Format,
                    currentWeekNumber,
                    meso.WeekCount);
                WeekProgressPercent = meso.WeekCount > 0
                    ? Math.Clamp((double)currentWeekNumber / meso.WeekCount, 0.0, 1.0)
                    : 0;
            }
            else if (isUpcoming)
            {
                WeekProgressText = string.Format(AppResources.MesoStats_StartsOn_Format, meso.StartDate.ToString("d MMM yyyy"));
                WeekProgressPercent = 0;
            }
            else
            {
                WeekProgressText = string.Format(AppResources.MesoStats_Finished_Format, meso.WeekCount);
                WeekProgressPercent = 1;
            }

            Weeks.Clear();
            foreach (var w in detail.Weeks.OrderBy(x => x.WeekNumber))
            {
                Weeks.Add(new WeekTile
                {
                    WeekNumber = w.WeekNumber,
                    IsDeload = w.IsDeload,
                    IntensityPercent = w.IntensityPercent,
                    IsActive = w.WeekNumber == currentWeekNumber,
                });
            }

            // Volym — visas alltid för aktuell vecka
            VolumeBars.Clear();
            var volume = await _weekly.GetCurrentWeekVolumeAsync();
            foreach (var v in volume.OrderBy(v => v.Muscle))
            {
                VolumeBars.Add(new VolumeBarRow
                {
                    Muscle = v.Muscle,
                    MuscleName = v.Muscle.ToLocalizedLabel(),
                    SetsThisWeek = v.SetsThisWeek,
                    PlannedSets = v.PlannedSets,
                    MEV = v.MEV,
                    MRV = v.MRV,
                    Zone = v.Zone,
                });
            }

            // RIR-progression: linjärt 4 → 1 över accumulation-veckor, 4 vid deload
            RirProgression.Clear();
            for (int wk = 1; wk <= meso.WeekCount; wk++)
            {
                var weekEntry = detail.Weeks.FirstOrDefault(w => w.WeekNumber == wk);
                var isDeloadWeek = weekEntry?.IsDeload ?? false;
                int rir = isDeloadWeek ? 4 : CycleContextService.ComputeTargetRir(wk - 1, meso.WeekCount);
                RirProgression.Add(new RirPoint
                {
                    WeekNumber = wk,
                    TargetRir = rir,
                    IsCurrent = wk == currentWeekNumber,
                    IsDeload = isDeloadWeek,
                });
            }

            // Deload-status — bara meningsfull medan cykeln pågår
            var deloadWeek = IsRunning ? detail.Weeks.FirstOrDefault(w => w.IsDeload) : null;
            HasDeload = deloadWeek is not null;
            if (deloadWeek is not null)
            {
                IsDeloadActive = currentWeekNumber == deloadWeek.WeekNumber;
                var weeksUntilDeload = deloadWeek.WeekNumber - currentWeekNumber;
                DeloadStatusText = IsDeloadActive
                    ? AppResources.MesoStats_Deload_Active
                    : (weeksUntilDeload > 0
                        ? string.Format(AppResources.MesoStats_Deload_Upcoming_Format, weeksUntilDeload)
                        : AppResources.MesoStats_Deload_Passed);
            }
            else
            {
                IsDeloadActive = false;
                DeloadStatusText = "";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MesoStats] Laddning misslyckades: {ex}");
            if (Shell.Current?.CurrentPage is { } page)
                await page.DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync(AppRoutes.Back);

    // PeriodizationPage är en global route (Routing.RegisterRoute) — får inte prefixas med "//".
    [RelayCommand]
    private async Task OpenPeriodizationAsync()
        => await Shell.Current.GoToAsync(AppRoutes.Periodization);

}

public partial class WeekTile : ObservableObject
{
    public int WeekNumber { get; set; }
    public bool IsDeload { get; set; }
    public int IntensityPercent { get; set; }
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private bool _isSelected;

    public string Label => string.Format(AppResources.Periodization_Week_Label_Format, WeekNumber);
    public string IntensityDisplay =>
        string.Format(AppResources.MesoStats_Week_Intensity_Format, IntensityPercent);
    public string SubLabel => IsDeload
        ? AppResources.MesoStats_Week_Deload
        : IntensityDisplay;
}

public partial class VolumeBarRow : ObservableObject
{
    public MuscleGroup Muscle { get; set; }
    public string MuscleName { get; set; } = "";
    public int SetsThisWeek { get; set; }
    public int PlannedSets { get; set; }
    public int MEV { get; set; }
    public int MRV { get; set; }
    public VolumeZone Zone { get; set; }
    // Samma färger och etiketter som WeeklyVolume — zonen anges även i text, inte bara färg.
    public Color ZoneColor => WeeklyVolumeRow.ZoneColorFor(Zone);
    public string ZoneLabel => WeeklyVolumeRow.ZoneLabelFor(Zone);
    public string SetsDisplay => $"{SetsThisWeek} / {PlannedSets} sets";
    public string LandmarksDisplay => $"MEV {MEV} · MRV {MRV}";
    public double SetsProgress => MRV > 0
        ? Math.Clamp((double)SetsThisWeek / MRV, 0.0, 1.0)
        : 0;
}

public partial class RirPoint : ObservableObject
{
    public int WeekNumber { get; set; }
    public int TargetRir { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsDeload { get; set; }
    public string Label => string.Format(AppResources.Periodization_Week_Label_Format, WeekNumber);
    public string RirDisplay => TargetRir.ToString();
}
