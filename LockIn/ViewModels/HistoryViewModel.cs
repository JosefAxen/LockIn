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

    // Pagination: visa 5 i taget, ladda 5 till när användaren scrollar nära botten.
    private List<SessionSummaryRow> _filteredSessions = new();
    private const int SessionsPerPage = 5;

    // Calendar state
    public int CalendarYear { get; private set; } = DateTime.Today.Year;
    public int CalendarMonth { get; private set; } = DateTime.Today.Month;
    public int SelectedCalendarDay { get; private set; } = 0;
    public HashSet<int> TrainedDays { get; private set; } = new();

    public event Action? CalendarChanged;

    private static readonly Color _glassActiveBg  = Color.FromArgb("#2BFFFFFF");
    private static readonly Color _glassActiveFg  = Color.FromArgb("#E2E8F0");
    private static readonly Color _glassInactiveBg = Colors.Transparent;
    private static readonly Color _glassInactiveFg = Color.FromArgb("#80FFFFFF");

    // Period tab colors
    public Color Period0Bg => SelectedPeriod == 0 ? _glassActiveBg : _glassInactiveBg;
    public Color Period0Fg => SelectedPeriod == 0 ? _glassActiveFg : _glassInactiveFg;
    public Color Period1Bg => SelectedPeriod == 1 ? _glassActiveBg : _glassInactiveBg;
    public Color Period1Fg => SelectedPeriod == 1 ? _glassActiveFg : _glassInactiveFg;
    public Color Period2Bg => SelectedPeriod == 2 ? _glassActiveBg : _glassInactiveBg;
    public Color Period2Fg => SelectedPeriod == 2 ? _glassActiveFg : _glassInactiveFg;

    // Sort tab colors
    public Color Sort0Bg => SelectedSort == 0 ? _glassActiveBg : _glassInactiveBg;
    public Color Sort0Fg => SelectedSort == 0 ? _glassActiveFg : _glassInactiveFg;
    public Color Sort1Bg => SelectedSort == 1 ? _glassActiveBg : _glassInactiveBg;
    public Color Sort1Fg => SelectedSort == 1 ? _glassActiveFg : _glassInactiveFg;

    partial void OnSelectedPeriodChanged(int value)
    {
        OnPropertyChanged(nameof(Period0Bg)); OnPropertyChanged(nameof(Period0Fg));
        OnPropertyChanged(nameof(Period1Bg)); OnPropertyChanged(nameof(Period1Fg));
        OnPropertyChanged(nameof(Period2Bg)); OnPropertyChanged(nameof(Period2Fg));
        SelectedCalendarDay = 0;
        CalendarChanged?.Invoke();
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

    [RelayCommand]
    private async Task OpenAchievementsAsync() =>
        await Shell.Current.GoToAsync(nameof(AchievementsPage));

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allSessions = await db.GetCompletedSessionsAsync();
        await RefreshCalendarAsync();
        ApplyFilterSort();
        RefreshTabColors();
        IsLoading = false;
    }

    private void RefreshTabColors()
    {
        OnPropertyChanged(nameof(Period0Bg)); OnPropertyChanged(nameof(Period0Fg));
        OnPropertyChanged(nameof(Period1Bg)); OnPropertyChanged(nameof(Period1Fg));
        OnPropertyChanged(nameof(Period2Bg)); OnPropertyChanged(nameof(Period2Fg));
        OnPropertyChanged(nameof(Sort0Bg)); OnPropertyChanged(nameof(Sort0Fg));
        OnPropertyChanged(nameof(Sort1Bg)); OnPropertyChanged(nameof(Sort1Fg));
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

        _filteredSessions = sorted.ToList();
        Sessions.Clear();
        foreach (var s in _filteredSessions.Take(SessionsPerPage))
            Sessions.Add(s);

        HasNoSessions = _filteredSessions.Count == 0;
    }

    [RelayCommand]
    public void LoadMoreSessions()
    {
        if (Sessions.Count >= _filteredSessions.Count) return;
        var next = _filteredSessions.Skip(Sessions.Count).Take(SessionsPerPage).ToList();
        foreach (var s in next) Sessions.Add(s);
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
