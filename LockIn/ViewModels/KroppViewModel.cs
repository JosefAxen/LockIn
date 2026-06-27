using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Models;
using LockIn.Resources.Strings;
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
    [ObservableProperty] private IReadOnlyList<ChartPoint> _weightChartPoints = [];

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

    private static readonly Color _segPillActive = DesignTokens.GlassActiveBg;
    private static readonly Color _segTextDim    = DesignTokens.GlassInactiveFg;

    public Color Tab0Bg => SelectedTab == 0 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab0Fg => SelectedTab == 0 ? DesignTokens.AccentBlue   : _segTextDim;
    public Color Tab1Bg => SelectedTab == 1 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab1Fg => SelectedTab == 1 ? DesignTokens.AccentPurple : _segTextDim;
    public Color Tab2Bg => SelectedTab == 2 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab2Fg => SelectedTab == 2 ? DesignTokens.PrimaryForeground : _segTextDim;

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
            WeightChartPoints = entries.OrderBy(e => e.LoggedAt)
                .Select(e => new ChartPoint(e.LoggedAt, (double)e.WeightKg))
                .ToList();
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
            (MuscleGroup.Chest,    AppResources.Train_Muscle_Chest),
            (MuscleGroup.Back,     AppResources.Train_Muscle_Back),
            (MuscleGroup.Shoulders,AppResources.Train_Muscle_Shoulders),
            (MuscleGroup.Biceps,   AppResources.Train_Muscle_Biceps),
            (MuscleGroup.Triceps,  AppResources.Train_Muscle_Triceps),
            (MuscleGroup.Legs,     AppResources.Train_Muscle_Legs),
            (MuscleGroup.Core,     AppResources.Train_Muscle_Core),
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
            AppResources.Kropp_LogWeight_Title, AppResources.Kropp_LogWeight_Body,
            keyboard: Keyboard.Numeric, placeholder: AppResources.Kropp_LogWeight_Placeholder);
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
            AppResources.Common_Delete,
            string.Format(AppResources.Kropp_DeleteWeight_Body_Format, $"{entry.WeightKg} kg", entry.LoggedAt.ToString("d MMM")),
            AppResources.Common_Delete, AppResources.Common_Cancel);
        if (!confirmed) return;
        await db.DeleteBodyWeightEntryAsync(entry);
        await LoadWeightDataAsync();
    }

    [RelayCommand]
    private async Task LogCompositionAsync()
    {
        var entry = new BodyCompositionEntry { LoggedAt = DateTime.Now };

        var w = await Shell.Current.DisplayPromptAsync(AppResources.Kropp_Measurement_Waist, AppResources.Kropp_Prompt_Waist_Body, keyboard: Keyboard.Numeric, placeholder: "cm");
        if (w != null && decimal.TryParse(w.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var wv) && wv > 0) entry.WaistCm = wv;

        var c = await Shell.Current.DisplayPromptAsync(AppResources.Kropp_Measurement_Chest, AppResources.Kropp_Prompt_Chest_Body, keyboard: Keyboard.Numeric, placeholder: "cm");
        if (c != null && decimal.TryParse(c.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var cv) && cv > 0) entry.ChestCm = cv;

        var h = await Shell.Current.DisplayPromptAsync(AppResources.Kropp_Measurement_Hips, AppResources.Kropp_Prompt_Hips_Body, keyboard: Keyboard.Numeric, placeholder: "cm");
        if (h != null && decimal.TryParse(h.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var hv) && hv > 0) entry.HipCm = hv;

        var a = await Shell.Current.DisplayPromptAsync(AppResources.Kropp_Measurement_Arms, AppResources.Kropp_Prompt_Arms_Body, keyboard: Keyboard.Numeric, placeholder: "cm");
        if (a != null && decimal.TryParse(a.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var av) && av > 0) entry.ArmCm = av;

        var th = await Shell.Current.DisplayPromptAsync(AppResources.Kropp_Measurement_Thighs, AppResources.Kropp_Prompt_Thighs_Body, keyboard: Keyboard.Numeric, placeholder: "cm");
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
            AppResources.Common_Delete,
            string.Format(AppResources.Kropp_DeleteMeasurement_Body_Format, entry.LoggedAt.ToString("d MMM yyyy")),
            AppResources.Common_Delete, AppResources.Common_Cancel);
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
    public string FrequencyText { get; set; } = "";
    public string ScoreText => Score >= 0.05 ? Score.ToString("F1") : "—";
}
