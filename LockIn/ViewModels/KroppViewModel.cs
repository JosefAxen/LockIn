using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LockIn;
using LockIn.Models;
using LockIn.Services;
using SkiaSharp;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class KroppViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private int _selectedTab = 0;
    [ObservableProperty] private bool _isLoading;

    private static readonly SKColor ChartBlue = new(0x6E, 0xA8, 0xDC);

    // VIKT
    public ObservableCollection<BodyWeightEntry> WeightEntries { get; } = new();
    [ObservableProperty] private string _latestWeight = "—";
    [ObservableProperty] private string _latestWeightDate = "";
    [ObservableProperty] private bool _hasWeightData;
    [ObservableProperty] private ISeries[] _weightSeries = [];
    [ObservableProperty] private Axis[] _weightXAxes = [];
    public Axis[] WeightYAxes { get; } =
    [
        new Axis { LabelsPaint = null, TicksPaint = null,
                   SeparatorsPaint = new SolidColorPaint(new SKColor(0x2A, 0x2A, 0x2A)) }
    ];

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

    public Color Tab0Bg => SelectedTab == 0 ? TabColorHelper.ActiveBg : TabColorHelper.InactiveBg;
    public Color Tab0Fg => SelectedTab == 0 ? TabColorHelper.ActiveFg : TabColorHelper.InactiveFg;
    public Color Tab1Bg => SelectedTab == 1 ? TabColorHelper.ActiveBg : TabColorHelper.InactiveBg;
    public Color Tab1Fg => SelectedTab == 1 ? TabColorHelper.ActiveFg : TabColorHelper.InactiveFg;
    public Color Tab2Bg => SelectedTab == 2 ? TabColorHelper.ActiveBg : TabColorHelper.InactiveBg;
    public Color Tab2Fg => SelectedTab == 2 ? TabColorHelper.ActiveFg : TabColorHelper.InactiveFg;

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
        RefreshTabColors();
        IsLoading = false;
    }

    private void RefreshTabColors()
    {
        OnPropertyChanged(nameof(Tab0Bg)); OnPropertyChanged(nameof(Tab0Fg));
        OnPropertyChanged(nameof(Tab1Bg)); OnPropertyChanged(nameof(Tab1Fg));
        OnPropertyChanged(nameof(Tab2Bg)); OnPropertyChanged(nameof(Tab2Fg));
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
            var pts = entries.OrderBy(e => e.LoggedAt)
                .Select(e => new DateTimePoint(e.LoggedAt, (double)e.WeightKg))
                .ToArray();
            WeightSeries =
            [
                new LineSeries<DateTimePoint>
                {
                    Values         = pts,
                    Stroke         = new SolidColorPaint(ChartBlue, 2),
                    Fill           = new SolidColorPaint(ChartBlue.WithAlpha(30)),
                    GeometryFill   = new SolidColorPaint(ChartBlue),
                    GeometryStroke = new SolidColorPaint(new SKColor(0x12, 0x12, 0x12), 1.5f),
                    GeometrySize   = 8,
                    LineSmoothness = 0.3,
                }
            ];
            WeightXAxes =
            [
                new DateTimeAxis(TimeSpan.FromDays(1), d => d.ToString("d/M"))
                {
                    TextSize        = 9,
                    LabelsPaint     = new SolidColorPaint(new SKColor(0x88, 0x88, 0x88)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(0x2A, 0x2A, 0x2A)),
                    TicksPaint      = null,
                }
            ];
        }
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
            HeatmapTiles.Add(new HeatmapTile
            {
                Name = name,
                Score = score,
                TileColor = DesignTokens.HeatmapTile(t),
                TextColor = DesignTokens.HeatmapText(t),
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
