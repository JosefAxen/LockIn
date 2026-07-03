using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

// RP full-klon Task 19: PeriodizationPage — mesocykel-hub.
// Ersätter gamla TrainingCycle-baserade VM som raderades i Task 3.
public partial class PeriodizationViewModel : ObservableObject
{
    private readonly IMesocycleService _mesocycles;

    public ObservableCollection<MesocycleRow> Rows { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasNoCycles;

    public PeriodizationViewModel(IMesocycleService mesocycles)
    {
        _mesocycles = mesocycles;
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _mesocycles.ListAsync();

            Rows.Clear();
            var today = DateTime.Today;

            // Sortera aktiva först, sedan nyaste startdatum
            foreach (var m in list
                .OrderByDescending(m => m.IsActive)
                .ThenByDescending(m => m.StartDate))
            {
                var daysSinceStart = (today - m.StartDate.Date).Days;
                var weekNumber = (daysSinceStart / 7) + 1;
                if (weekNumber < 1) weekNumber = 1;
                if (weekNumber > m.WeekCount) weekNumber = m.WeekCount;

                var progress = m.WeekCount > 0
                    ? (double)weekNumber / m.WeekCount
                    : 0.0;

                Rows.Add(new MesocycleRow
                {
                    Id = m.Id,
                    Name = string.IsNullOrWhiteSpace(m.Name) ? "—" : m.Name,
                    WeekCount = m.WeekCount,
                    IsActive = m.IsActive,
                    StartDate = m.StartDate,
                    CurrentWeek = weekNumber,
                    ProgressPercent = progress,
                });
            }

            HasNoCycles = Rows.Count == 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenMesoBuilderAsync()
        => await Shell.Current.GoToAsync(AppRoutes.MesoBuilder);

    [RelayCommand]
    private async Task OpenTemplatePickerAsync()
        => await Shell.Current.GoToAsync(AppRoutes.TemplatePicker);

    [RelayCommand]
    private async Task OpenMesocycleAsync(MesocycleRow row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync(AppRoutes.MesocycleStatsFor(row.Id));
    }

    [RelayCommand]
    private async Task DeleteMesocycleAsync(MesocycleRow row)
    {
        if (row is null) return;
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;

        var confirm = await page.DisplayAlert(
            AppResources.Periodization_Delete_Confirm_Title,
            string.Format(AppResources.Periodization_Delete_Confirm_Body_Format, row.Name),
            AppResources.Periodization_Delete_Confirm_Yes,
            AppResources.Periodization_Delete_Confirm_No);

        if (!confirm) return;

        await _mesocycles.DeleteAsync(row.Id);
        await RefreshAsync();
    }
}

public partial class MesocycleRow : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int WeekCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public int CurrentWeek { get; set; }
    public double ProgressPercent { get; set; }

    public string StartDateDisplay => StartDate.ToString("d MMM yyyy");
    public string WeeksDisplay =>
        string.Format(AppResources.Periodization_WeeksCount_Format, WeekCount);
    public string ProgressDisplay => IsActive
        ? string.Format(AppResources.Periodization_Week_Format, CurrentWeek, WeekCount)
        : "";
}
