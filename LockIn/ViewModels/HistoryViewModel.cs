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
    [ObservableProperty] private bool _hasNoSessions;
    [ObservableProperty] private string _calendarTitle = "";

    private List<SessionSummaryRow> _allSessions = new();

    // Calendar state
    public int CalendarYear { get; private set; } = DateTime.Today.Year;
    public int CalendarMonth { get; private set; } = DateTime.Today.Month;
    public int SelectedCalendarDay { get; private set; } = 0;
    public HashSet<int> TrainedDays { get; private set; } = new();

    public event Action? CalendarChanged;

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
        SelectedCalendarDay = 0;
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
        await RefreshCalendarAsync();
        ApplyFilterSort();
        IsLoading = false;
    }

    private async Task RefreshCalendarAsync()
    {
        TrainedDays = await db.GetTrainedDaysInMonthAsync(CalendarYear, CalendarMonth);
        var culture = new System.Globalization.CultureInfo("sv-SE");
        CalendarTitle = new DateTime(CalendarYear, CalendarMonth, 1)
            .ToString("MMMM yyyy", culture).ToUpper();
        CalendarChanged?.Invoke();
    }

    [RelayCommand]
    private async Task PrevMonthAsync()
    {
        var dt = new DateTime(CalendarYear, CalendarMonth, 1).AddMonths(-1);
        CalendarYear = dt.Year;
        CalendarMonth = dt.Month;
        SelectedCalendarDay = 0;
        await RefreshCalendarAsync();
        ApplyFilterSort();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        var dt = new DateTime(CalendarYear, CalendarMonth, 1).AddMonths(1);
        CalendarYear = dt.Year;
        CalendarMonth = dt.Month;
        SelectedCalendarDay = 0;
        await RefreshCalendarAsync();
        ApplyFilterSort();
    }

    public void SelectCalendarDay(int day)
    {
        SelectedCalendarDay = SelectedCalendarDay == day ? 0 : day;
        ApplyFilterSort();
        CalendarChanged?.Invoke();
    }

    private void ApplyFilterSort()
    {
        var today = DateTime.Today;
        IEnumerable<SessionSummaryRow> filtered;

        if (SelectedCalendarDay > 0)
        {
            filtered = _allSessions.Where(s =>
                s.StartedAt.Year == CalendarYear &&
                s.StartedAt.Month == CalendarMonth &&
                s.StartedAt.Day == SelectedCalendarDay);
        }
        else
        {
            filtered = SelectedPeriod switch
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
        }

        var sorted = SelectedSort == 1
            ? filtered.OrderByDescending(s => s.TotalVolume)
            : filtered.OrderByDescending(s => s.StartedAt);

        Sessions.Clear();
        foreach (var s in sorted)
            Sessions.Add(s);

        HasNoSessions = Sessions.Count == 0;
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
