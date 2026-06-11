using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class KroppViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private int _selectedTab = 0;
    [ObservableProperty] private bool _isLoading;

    // VIKT
    public ObservableCollection<BodyWeightEntry> WeightEntries { get; } = new();
    [ObservableProperty] private string _latestWeight = "—";
    [ObservableProperty] private string _latestWeightDate = "";
    [ObservableProperty] private bool _hasWeightData;
    public BodyWeightDrawable WeightChartDrawable { get; } = new();
    public event Action? ChartInvalidated;

    // KROPP
    public ObservableCollection<BodyCompositionEntry> CompositionEntries { get; } = new();
    [ObservableProperty] private bool _hasCompositionData;
    [ObservableProperty] private string _latestWaist = "—";
    [ObservableProperty] private string _latestChest = "—";
    [ObservableProperty] private string _latestHip = "—";
    [ObservableProperty] private string _latestArm = "—";
    [ObservableProperty] private string _latestThigh = "—";

    // HEATMAP
    public List<HeatmapTile> HeatmapTiles { get; } = new();
    public event Action? HeatmapReady;

    // Tab state
    public bool IsViktTab => SelectedTab == 0;
    public bool IsKroppTab => SelectedTab == 1;
    public bool IsHeatmapTab => SelectedTab == 2;

    public Color Tab0Bg => SelectedTab == 0 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#111111");
    public Color Tab0Fg => SelectedTab == 0 ? Colors.White : Color.FromArgb("#505058");
    public Color Tab1Bg => SelectedTab == 1 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#111111");
    public Color Tab1Fg => SelectedTab == 1 ? Colors.White : Color.FromArgb("#505058");
    public Color Tab2Bg => SelectedTab == 2 ? Color.FromArgb("#FF5A1F") : Color.FromArgb("#111111");
    public Color Tab2Fg => SelectedTab == 2 ? Colors.White : Color.FromArgb("#505058");

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(Tab0Bg)); OnPropertyChanged(nameof(Tab0Fg));
        OnPropertyChanged(nameof(Tab1Bg)); OnPropertyChanged(nameof(Tab1Fg));
        OnPropertyChanged(nameof(Tab2Bg)); OnPropertyChanged(nameof(Tab2Fg));
        OnPropertyChanged(nameof(IsViktTab));
        OnPropertyChanged(nameof(IsKroppTab));
        OnPropertyChanged(nameof(IsHeatmapTab));
    }

    [RelayCommand]
    private void SelectTab(int tab) => SelectedTab = tab;

    public async Task LoadAsync()
    {
        IsLoading = true;
        await LoadWeightDataAsync();
        await LoadCompositionDataAsync();
        await LoadHeatmapAsync();
        IsLoading = false;
    }

    private async Task LoadWeightDataAsync()
    {
        var entries = await db.GetBodyWeightEntriesAsync();
        HasWeightData = entries.Count > 0;

        WeightEntries.Clear();
        foreach (var e in entries.Take(15))
            WeightEntries.Add(e);

        if (HasWeightData)
        {
            var latest = entries[0];
            LatestWeight = $"{latest.WeightKg:F1} kg";
            LatestWeightDate = latest.LoggedAt.ToString("d MMM yyyy");
            WeightChartDrawable.Points = entries.OrderBy(e => e.LoggedAt)
                .Select(e => (e.LoggedAt, (double)e.WeightKg))
                .ToList();
        }
        ChartInvalidated?.Invoke();
    }

    private async Task LoadCompositionDataAsync()
    {
        var entries = await db.GetBodyCompositionEntriesAsync();
        HasCompositionData = entries.Count > 0;

        CompositionEntries.Clear();
        foreach (var e in entries.Take(10))
            CompositionEntries.Add(e);

        if (HasCompositionData)
        {
            var l = entries[0];
            LatestWaist = l.WaistCm.HasValue ? $"{l.WaistCm:F1}" : "—";
            LatestChest = l.ChestCm.HasValue ? $"{l.ChestCm:F1}" : "—";
            LatestHip   = l.HipCm.HasValue   ? $"{l.HipCm:F1}"   : "—";
            LatestArm   = l.ArmCm.HasValue   ? $"{l.ArmCm:F1}"   : "—";
            LatestThigh = l.ThighCm.HasValue ? $"{l.ThighCm:F1}" : "—";
        }
    }

    private async Task LoadHeatmapAsync()
    {
        var scores = await db.GetMuscleScoresAsync();
        HeatmapTiles.Clear();

        var muscles = new (MuscleGroup mg, string name)[]
        {
            (MuscleGroup.Chest,    "BRÖST"),
            (MuscleGroup.Back,     "RYGG"),
            (MuscleGroup.Shoulders,"AXLAR"),
            (MuscleGroup.Biceps,   "BICEPS"),
            (MuscleGroup.Triceps,  "TRICEPS"),
            (MuscleGroup.Legs,     "BEN"),
            (MuscleGroup.Core,     "CORE"),
        };

        foreach (var (mg, name) in muscles)
        {
            var score = scores.TryGetValue(mg, out var s) ? s : 0.0;
            var t = score / 10.0;
            Color tileColor;
            Color textColor;
            if (score < 0.05)
            {
                tileColor = Color.FromArgb("#141414");
                textColor = Color.FromArgb("#505058");
            }
            else
            {
                var r = (int)(26 + 229 * t);
                var g = (int)(26 + 64 * t);
                var b = (int)(26 + 5 * t);
                tileColor = Color.FromRgb(r, g, b);
                textColor = t > 0.55 ? Colors.White : Color.FromArgb("#D09080");
            }

            HeatmapTiles.Add(new HeatmapTile
            {
                Name = name,
                Score = score,
                TileColor = tileColor,
                TextColor = textColor,
            });
        }
        HeatmapReady?.Invoke();
    }

    [RelayCommand]
    private async Task LogWeightAsync()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            "Logga vikt", "Ange din kroppsvikt i kg:",
            keyboard: Keyboard.Numeric, placeholder: "t.ex. 80.5");
        if (string.IsNullOrWhiteSpace(result)) return;
        if (!decimal.TryParse(result.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var kg) || kg <= 0) return;

        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { WeightKg = kg, LoggedAt = DateTime.Now });
        await LoadWeightDataAsync();
    }

    [RelayCommand]
    private async Task DeleteWeightEntryAsync(BodyWeightEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort", $"Ta bort {entry.WeightKg} kg ({entry.LoggedAt:d MMM})?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteBodyWeightEntryAsync(entry);
        await LoadWeightDataAsync();
    }

    [RelayCommand]
    private async Task LogCompositionAsync()
    {
        var entry = new BodyCompositionEntry { LoggedAt = DateTime.Now };

        var w = await Shell.Current.DisplayPromptAsync("Midja", "Midjemått i cm (valfritt):", keyboard: Keyboard.Numeric, placeholder: "cm");
        if (w != null && decimal.TryParse(w.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var wv) && wv > 0) entry.WaistCm = wv;

        var c = await Shell.Current.DisplayPromptAsync("Bröst", "Bröstkorgsmått i cm (valfritt):", keyboard: Keyboard.Numeric, placeholder: "cm");
        if (c != null && decimal.TryParse(c.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var cv) && cv > 0) entry.ChestCm = cv;

        var h = await Shell.Current.DisplayPromptAsync("Höft", "Höftmått i cm (valfritt):", keyboard: Keyboard.Numeric, placeholder: "cm");
        if (h != null && decimal.TryParse(h.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var hv) && hv > 0) entry.HipCm = hv;

        var a = await Shell.Current.DisplayPromptAsync("Armar", "Armmått i cm (valfritt):", keyboard: Keyboard.Numeric, placeholder: "cm");
        if (a != null && decimal.TryParse(a.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var av) && av > 0) entry.ArmCm = av;

        var th = await Shell.Current.DisplayPromptAsync("Lår", "Lårmått i cm (valfritt):", keyboard: Keyboard.Numeric, placeholder: "cm");
        if (th != null && decimal.TryParse(th.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var tv) && tv > 0) entry.ThighCm = tv;

        if (entry.WaistCm.HasValue || entry.ChestCm.HasValue || entry.HipCm.HasValue
            || entry.ArmCm.HasValue || entry.ThighCm.HasValue)
        {
            await db.SaveBodyCompositionEntryAsync(entry);
            await LoadCompositionDataAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteCompositionEntryAsync(BodyCompositionEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort", $"Ta bort mätning ({entry.LoggedAt:d MMM yyyy})?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteBodyCompositionEntryAsync(entry);
        await LoadCompositionDataAsync();
    }

    [RelayCommand]
    private async Task OpenSettingsAsync() =>
        await Shell.Current.GoToAsync(nameof(SettingsPage));
}

public class HeatmapTile
{
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public Color TileColor { get; set; } = Colors.Transparent;
    public Color TextColor { get; set; } = Colors.White;
    public string ScoreText => Score >= 0.05 ? Score.ToString("F1") : "—";
}
