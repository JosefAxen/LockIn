using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using static LockIn.Services.DatabaseService;

namespace LockIn.ViewModels;

public partial class HistoryViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<SessionSummaryRow> Sessions { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPeriod = 0;
    [ObservableProperty] private int _selectedSort = 0;

    private List<SessionSummaryRow> _allSessions = new();

    // Period tab colors
    public Color Period0Bg => SelectedPeriod == 0 ? Color.FromArgb("#1AFF5A1F") : Color.FromArgb("#1A1A1A");
    public Color Period0Fg => SelectedPeriod == 0 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#A0A0A8");
    public Color Period1Bg => SelectedPeriod == 1 ? Color.FromArgb("#1AFF5A1F") : Color.FromArgb("#1A1A1A");
    public Color Period1Fg => SelectedPeriod == 1 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#A0A0A8");
    public Color Period2Bg => SelectedPeriod == 2 ? Color.FromArgb("#1AFF5A1F") : Color.FromArgb("#1A1A1A");
    public Color Period2Fg => SelectedPeriod == 2 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#A0A0A8");

    // Sort tab colors
    public Color Sort0Bg => SelectedSort == 0 ? Color.FromArgb("#1AFF5A1F") : Color.FromArgb("#1A1A1A");
    public Color Sort0Fg => SelectedSort == 0 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#A0A0A8");
    public Color Sort1Bg => SelectedSort == 1 ? Color.FromArgb("#1AFF5A1F") : Color.FromArgb("#1A1A1A");
    public Color Sort1Fg => SelectedSort == 1 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#A0A0A8");

    partial void OnSelectedPeriodChanged(int value)
    {
        OnPropertyChanged(nameof(Period0Bg)); OnPropertyChanged(nameof(Period0Fg));
        OnPropertyChanged(nameof(Period1Bg)); OnPropertyChanged(nameof(Period1Fg));
        OnPropertyChanged(nameof(Period2Bg)); OnPropertyChanged(nameof(Period2Fg));
        ApplyFilterSort();
    }

    partial void OnSelectedSortChanged(int value)
    {
        OnPropertyChanged(nameof(Sort0Bg)); OnPropertyChanged(nameof(Sort0Fg));
        OnPropertyChanged(nameof(Sort1Bg)); OnPropertyChanged(nameof(Sort1Fg));
        ApplyFilterSort();
    }

    [RelayCommand]
    private void SelectPeriod(int period) => SelectedPeriod = period;

    [RelayCommand]
    private void SelectSort(int sort) => SelectedSort = sort;

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allSessions = await db.GetCompletedSessionsAsync();
        ApplyFilterSort();
        IsLoading = false;
    }

    private void ApplyFilterSort()
    {
        var today = DateTime.Today;
        IEnumerable<SessionSummaryRow> filtered = SelectedPeriod switch
        {
            1 => _allSessions.Where(s =>
            {
                var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
                var monday = today.AddDays(-daysFromMonday);
                return s.StartedAt.Date >= monday;
            }),
            2 => _allSessions.Where(s => s.StartedAt.Year == today.Year && s.StartedAt.Month == today.Month),
            _ => _allSessions
        };

        var sorted = SelectedSort == 1
            ? filtered.OrderByDescending(s => s.TotalVolume)
            : filtered.OrderByDescending(s => s.StartedAt);

        Sessions.Clear();
        foreach (var s in sorted)
            Sessions.Add(s);
    }

    [RelayCommand]
    private async Task OpenSessionAsync(SessionSummaryRow session)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailPage), new Dictionary<string, object>
        {
            { "Session", session }
        });
    }
}
