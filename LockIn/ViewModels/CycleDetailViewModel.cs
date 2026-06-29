using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class CycleWeekRow(int weekNumber) : ObservableObject
{
    public int WeekNumber { get; } = weekNumber;
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private int _intensityPercent = 75;
    public ObservableCollection<CycleSessionRow> Sessions { get; } = new();
}

public partial class CycleSessionRow : ObservableObject
{
    public int DayOfWeek { get; init; }
    public string DayName => AppResources.ResourceManager.GetString($"Day_{DayOfWeek}") ?? $"Dag {DayOfWeek}";
    [ObservableProperty] private int _templateId;
    [ObservableProperty] private string _templateName = "";
    public bool HasTemplate => TemplateId > 0;
    partial void OnTemplateIdChanged(int value) => OnPropertyChanged(nameof(HasTemplate));
}

public partial class CycleDetailViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    private int _cycleId;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private int _weekCount = 4;
    [ObservableProperty] private string _pageTitle = "";
    public ObservableCollection<CycleWeekRow> Weeks { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _cycleId = query.TryGetValue("CycleId", out var id) ? (int)id : 0;
        PageTitle = _cycleId == 0
            ? AppResources.CycleDetail_Title_New
            : AppResources.CycleDetail_Title_Edit;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_cycleId == 0)
        {
            RebuildWeeks();
            return;
        }
        var cycles = await db.GetCyclesAsync();
        var cycle = cycles.FirstOrDefault(c => c.Id == _cycleId);
        if (cycle is null) return;
        Name = cycle.Name;
        StartDate = cycle.StartDate;
        _weekCount = cycle.WeekCount;
        OnPropertyChanged(nameof(WeekCount));
        var weeks = await db.GetCycleWeeksAsync(_cycleId);
        Weeks.Clear();
        foreach (var w in weeks)
        {
            var row = new CycleWeekRow(w.WeekNumber) { Label = w.Label, IntensityPercent = w.IntensityPercent };
            var sessions = await db.GetCycleSessionsAsync(w.Id);
            var templates = await db.GetTemplatesAsync();
            for (int d = 0; d < 7; d++)
            {
                var s = sessions.FirstOrDefault(x => x.DayOfWeek == d);
                var tName = s?.TemplateId > 0
                    ? templates.FirstOrDefault(t => t.Id == s.TemplateId)?.Name ?? ""
                    : "";
                row.Sessions.Add(new CycleSessionRow
                {
                    DayOfWeek = d,
                    TemplateId = s?.TemplateId ?? 0,
                    TemplateName = tName
                });
            }
            Weeks.Add(row);
        }
    }

    partial void OnWeekCountChanged(int value) => RebuildWeeks();

    private void RebuildWeeks()
    {
        while (Weeks.Count > WeekCount) Weeks.RemoveAt(Weeks.Count - 1);
        while (Weeks.Count < WeekCount)
        {
            int n = Weeks.Count + 1;
            var row = new CycleWeekRow(n) { IntensityPercent = DefaultIntensity(n, WeekCount) };
            for (int d = 0; d < 7; d++)
                row.Sessions.Add(new CycleSessionRow { DayOfWeek = d });
            Weeks.Add(row);
        }
    }

    private static int DefaultIntensity(int weekNum, int total) => weekNum == total
        ? 60   // Deload
        : 70 + (weekNum - 1) * (20 / Math.Max(total - 1, 1));

    [RelayCommand]
    private void IncrementWeeks() { if (WeekCount < 16) WeekCount++; }

    [RelayCommand]
    private void DecrementWeeks() { if (WeekCount > 1) WeekCount--; }

    [RelayCommand]
    private async Task PickTemplateAsync(CycleSessionRow session)
    {
        var templates = await db.GetTemplatesAsync();
        var names = templates.Select(t => t.Name).ToArray();
        var noneOption = AppResources.CycleDetail_NoTemplate;
        var all = names.Prepend(noneOption).ToArray();
        var picked = await Shell.Current.DisplayActionSheet(
            AppResources.CycleDetail_PickTemplate_Title,
            AppResources.CycleDetail_PickTemplate_Cancel,
            null,
            all);
        if (picked == null || picked == AppResources.CycleDetail_PickTemplate_Cancel) return;
        if (picked == noneOption)
        {
            session.TemplateId = 0;
            session.TemplateName = "";
        }
        else
        {
            var t = templates.First(x => x.Name == picked);
            session.TemplateId = t.Id;
            session.TemplateName = t.Name;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        var cycle = new TrainingCycle
        {
            Id = _cycleId,
            Name = Name.Trim(),
            StartDate = StartDate,
            WeekCount = WeekCount
        };
        var weeks = Weeks.Select((r, i) => new CycleWeek
        {
            WeekNumber = i + 1,
            IntensityPercent = r.IntensityPercent,
            Label = r.Label
        }).ToList();
        var sessionsByWeek = Weeks.Select(r =>
            r.Sessions.Select(s => new CycleSession
            {
                DayOfWeek = s.DayOfWeek,
                TemplateId = s.TemplateId,
                SortOrder = s.DayOfWeek
            }).ToList()).ToList();
        await db.SaveCycleAsync(cycle, weeks, sessionsByWeek);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
