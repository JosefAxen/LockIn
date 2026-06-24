using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class BodyWeightViewModel(DatabaseService db) : ObservableObject
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
            LatestDate   = latest.LoggedAt.ToString("d MMM yyyy");

            ChartPoints = _allEntries
                .OrderBy(e => e.LoggedAt)
                .Select(e => new ChartPoint(e.LoggedAt, (double)e.WeightKg))
                .ToList();
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

        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = DateTime.Now, WeightKg = kg });
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(BodyWeightEntry entry)
    {
        var body = string.Format(AppResources.BodyWeight_DeleteBody_Format,
            $"{entry.WeightKg:F1}", entry.LoggedAt.ToString("d MMM"));
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
