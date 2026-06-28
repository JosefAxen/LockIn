using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LockIn.ViewModels;

public partial class BodyWeightViewModel(DatabaseService db, IHealthService health) : ObservableObject
{
    private const int PageSize = 10;
    private List<BodyWeightEntry> _allEntries = [];
    private int _displayedCount;

    [ObservableProperty] private string _latestWeight = "–";
    [ObservableProperty] private string _latestDate = "";
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasMore;
    [ObservableProperty] private IReadOnlyList<ChartPoint> _chartPoints = [];
    [ObservableProperty] private string _bmiText    = "–";
    [ObservableProperty] private string _bmiCategory = "";
    [ObservableProperty] private bool   _hasBmi;
    [ObservableProperty] private string _weightTrend = "";
    [ObservableProperty] private bool   _hasTrend;

    public ObservableCollection<BodyWeightEntry> RecentEntries { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allEntries = await db.GetBodyWeightEntriesAsync();
        HasData = _allEntries.Count > 0;

        if (HasData)
        {
            var latest = _allEntries[0];
            LatestWeight = $"{latest.WeightKg:F1} kg";
            LatestDate   = latest.LoggedAt.ToString("d MMM yyyy", CultureInfo.CurrentUICulture);

            ChartPoints = _allEntries
                .OrderBy(e => e.LoggedAt)
                .Select(e => new ChartPoint(e.LoggedAt, (double)e.WeightKg))
                .ToList();
        }

        // BMI
        var settings = await db.GetAppSettingsAsync();
        if (settings.HeightCm > 0 && HasData)
        {
            var latestKg = (double)_allEntries[0].WeightKg;
            var heightM  = settings.HeightCm / 100.0;
            var bmi      = latestKg / (heightM * heightM);
            BmiText     = bmi.ToString("F1");
            BmiCategory = bmi switch
            {
                < 18.5 => AppResources.BodyWeight_BmiCategory_Underweight,
                < 25.0 => AppResources.BodyWeight_BmiCategory_Normal,
                < 30.0 => AppResources.BodyWeight_BmiCategory_Overweight,
                _      => AppResources.BodyWeight_BmiCategory_Obese,
            };
            HasBmi = true;
        }
        else
        {
            BmiText     = "–";
            BmiCategory = "";
            HasBmi      = false;
        }

        // Trend
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var recent = _allEntries
            .Where(e => e.LoggedAt >= thirtyDaysAgo)
            .OrderBy(e => e.LoggedAt)
            .ToList();
        if (recent.Count >= 2)
        {
            var delta    = (double)(recent.Last().WeightKg - recent.First().WeightKg);
            var absDelta = Math.Abs(delta);
            WeightTrend = absDelta < 0.2
                ? AppResources.BodyWeight_Trend_Stable
                : delta > 0
                    ? string.Format(AppResources.BodyWeight_Trend_Up,   absDelta.ToString("F1", CultureInfo.InvariantCulture))
                    : string.Format(AppResources.BodyWeight_Trend_Down, absDelta.ToString("F1", CultureInfo.InvariantCulture));
            HasTrend = true;
        }
        else
        {
            WeightTrend = "";
            HasTrend    = false;
        }

        _displayedCount = Math.Min(PageSize, _allEntries.Count);
        RecentEntries.Clear();
        foreach (var e in _allEntries.Take(_displayedCount))
            RecentEntries.Add(e);
        HasMore = _allEntries.Count > _displayedCount;

        IsLoading = false;
    }

    [RelayCommand]
    private void LoadMore()
    {
        var next = Math.Min(_displayedCount + PageSize, _allEntries.Count);
        for (int i = _displayedCount; i < next; i++)
            RecentEntries.Add(_allEntries[i]);
        _displayedCount = next;
        HasMore = _allEntries.Count > _displayedCount;
    }

    [RelayCommand]
    private async Task LogWeightAsync()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.BodyWeight_Prompt_Title,
            AppResources.BodyWeight_Prompt_Body,
            keyboard: Keyboard.Numeric,
            placeholder: AppResources.BodyWeight_EntryPlaceholder);

        if (string.IsNullOrWhiteSpace(result)) return;
        if (!decimal.TryParse(result.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var kg) || kg <= 0) return;

        var loggedAt = DateTime.Now;
        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = loggedAt, WeightKg = kg });
        try { await health.SaveBodyMassAsync(kg, loggedAt); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[BodyWeight] HealthKit sync misslyckades: {ex.Message}"); }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(BodyWeightEntry entry)
    {
        var body = string.Format(AppResources.BodyWeight_DeleteBody_Format,
            $"{entry.WeightKg:F1}", entry.LoggedAt.ToString("d MMM", CultureInfo.CurrentUICulture));
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.Common_Delete, body,
            AppResources.Common_Delete, AppResources.Common_Cancel);
        if (!confirmed) return;
        await db.DeleteBodyWeightEntryAsync(entry);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
