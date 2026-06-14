using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class BodyWeightViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private string _latestWeight = "–";
    [ObservableProperty] private string _latestDate = "";
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private IReadOnlyList<ChartPoint> _chartPoints = [];

    public ObservableCollection<BodyWeightEntry> RecentEntries { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        var entries = await db.GetBodyWeightEntriesAsync();
        HasData = entries.Count > 0;

        if (HasData)
        {
            var latest = entries[0];
            LatestWeight = $"{latest.WeightKg:F1} kg";
            LatestDate   = latest.LoggedAt.ToString("d MMM yyyy");

            ChartPoints = entries
                .OrderBy(e => e.LoggedAt)
                .Select(e => new ChartPoint(e.LoggedAt, (double)e.WeightKg))
                .ToList();
        }

        RecentEntries.Clear();
        foreach (var e in entries.Take(10))
            RecentEntries.Add(e);

        IsLoading = false;
    }

    [RelayCommand]
    private async Task LogWeightAsync()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            "Logga vikt", "Ange din kroppsvikt i kg:",
            keyboard: Keyboard.Numeric, placeholder: "t.ex. 80.5");

        if (string.IsNullOrWhiteSpace(result)) return;
        if (!decimal.TryParse(result.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var kg) || kg <= 0) return;

        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = DateTime.Now, WeightKg = kg });
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(BodyWeightEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort", $"Ta bort {entry.WeightKg} kg ({entry.LoggedAt:d MMM})?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteBodyWeightEntryAsync(entry);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
