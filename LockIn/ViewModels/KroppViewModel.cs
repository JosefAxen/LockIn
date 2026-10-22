using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LockIn.ViewModels;

public partial class KroppViewModel(DatabaseService db, IBodyRepository bodyRepo) : ObservableObject
{
    [ObservableProperty] private int _selectedTab = 0;
    [ObservableProperty] private bool _isLoading;

    // VIKT
    public ObservableCollection<BodyWeightEntry> WeightEntries { get; } = new();
    [ObservableProperty] private string _latestWeight = "—";
    [ObservableProperty] private string _latestWeightDate = "";
    [ObservableProperty] private bool _hasWeightData;
    [ObservableProperty] private IReadOnlyList<ChartPoint> _weightChartPoints = [];
    [ObservableProperty] private string _weightChartAccessibilityLabel = "";

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

    // Vald flik: ljus text på indikatorn (blått/lila på #525260 gav bara 3,6:1 resp. 2,8:1).
    public Color Tab0Bg => SelectedTab == 0 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab0Fg => SelectedTab == 0 ? DesignTokens.GlassActiveFg : _segTextDim;
    public Color Tab1Bg => SelectedTab == 1 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab1Fg => SelectedTab == 1 ? DesignTokens.GlassActiveFg : _segTextDim;
    public Color Tab2Bg => SelectedTab == 2 ? _segPillActive : DesignTokens.GlassInactiveBg;
    public Color Tab2Fg => SelectedTab == 2 ? DesignTokens.GlassActiveFg : _segTextDim;

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
        var entries = await bodyRepo.GetWeightEntriesAsync();
        HasWeightData = entries.Count > 0;

        WeightEntries.Clear();
        foreach (var e in entries.Take(15))
            WeightEntries.Add(e);

        if (HasWeightData)
        {
            var latest = entries[0];
            LatestWeight = $"{latest.WeightKg.ToString("0.##")} kg";
            LatestWeightDate = latest.LoggedAt.ToString("d MMM yyyy");
            WeightChartPoints = entries.OrderBy(e => e.LoggedAt)
                .Select(e => new ChartPoint(e.LoggedAt, (double)e.WeightKg))
                .ToList();
            WeightChartAccessibilityLabel = string.Format(AppResources.Kropp_WeightChart_A11y,
                entries.Count,
                entries[^1].WeightKg.ToString("0.##"),
                latest.WeightKg.ToString("0.##"));
        }
    }

    private async Task LoadCompositionDataAsync()
    {
        var entries = await bodyRepo.GetCompositionEntriesAsync();
        HasCompositionData = entries.Count > 0;

        CompositionEntries.Clear();
        foreach (var e in entries.Take(10))
            CompositionEntries.Add(e);

        if (HasCompositionData)
        {
            var l = entries[0];
            LatestWaist = l.WaistCm?.ToString("0.##") ?? "—";
            LatestChest = l.ChestCm?.ToString("0.##") ?? "—";
            LatestHip   = l.HipCm?.ToString("0.##")   ?? "—";
            LatestArm   = l.ArmCm?.ToString("0.##")   ?? "—";
            LatestThigh = l.ThighCm?.ToString("0.##") ?? "—";
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
                TextColor = HeatmapTile.TextOn(DesignTokens.HeatmapTile(t)),
            });
        }
        HeatmapReady?.Invoke();
    }

    [RelayCommand]
    private async Task LogWeightAsync()
    {
        var (cancelled, kg) = await PromptPositiveDecimalAsync(
            AppResources.Kropp_LogWeight_Title, AppResources.Kropp_LogWeight_Body,
            AppResources.Common_Save, AppResources.Kropp_LogWeight_Placeholder);
        if (cancelled || kg is null) return;

        try
        {
            await bodyRepo.SaveWeightEntryAsync(new BodyWeightEntry { WeightKg = kg.Value, LoggedAt = DateTime.Now });
            await LoadWeightDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Kropp] LogWeightAsync failed: {ex}");
            await Shell.Current.DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }

    /// <summary>
    /// Frågar efter ett positivt decimaltal ("82,5" och "82.5" fungerar). Ogiltig inmatning
    /// (t.ex. "80 kg") ger samma fråga igen med ett felmeddelande och texten kvar.
    /// Cancelled = användaren tryckte Avbryt. Value = null betyder tomt fält.
    /// </summary>
    private static async Task<(bool Cancelled, decimal? Value)> PromptPositiveDecimalAsync(
        string title, string message, string accept, string placeholder)
    {
        var body = message;
        var initial = "";
        while (true)
        {
            var input = await Shell.Current.DisplayPromptAsync(
                title, body, accept, AppResources.Common_Cancel,
                placeholder, keyboard: Keyboard.Numeric, initialValue: initial);
            if (input is null) return (true, null);
            if (string.IsNullOrWhiteSpace(input)) return (false, null);
            if (decimal.TryParse(input.Trim().Replace(',', '.'), NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var value) && value > 0)
                return (false, value);

            body = $"{AppResources.Kropp_InvalidNumber}\n\n{message}";
            initial = input;
        }
    }

    [RelayCommand]
    private async Task DeleteWeightEntryAsync(BodyWeightEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.Common_Delete,
            string.Format(AppResources.Kropp_DeleteWeight_Body_Format, entry.WeightKg.ToString("0.##"), entry.LoggedAt.ToString("d MMM")),
            AppResources.Common_Delete, AppResources.Common_Cancel);
        if (!confirmed) return;
        await bodyRepo.DeleteWeightEntryAsync(entry);
        await LoadWeightDataAsync();
    }

    [RelayCommand]
    private async Task LogCompositionAsync()
    {
        var entry = new BodyCompositionEntry { LoggedAt = DateTime.Now };

        var steps = new (string Title, string Body, Action<decimal> Set)[]
        {
            (AppResources.Kropp_Measurement_Waist,  AppResources.Kropp_Prompt_Waist_Body,  v => entry.WaistCm = v),
            (AppResources.Kropp_Measurement_Chest,  AppResources.Kropp_Prompt_Chest_Body,  v => entry.ChestCm = v),
            (AppResources.Kropp_Measurement_Hips,   AppResources.Kropp_Prompt_Hips_Body,   v => entry.HipCm = v),
            (AppResources.Kropp_Measurement_Arms,   AppResources.Kropp_Prompt_Arms_Body,   v => entry.ArmCm = v),
            (AppResources.Kropp_Measurement_Thighs, AppResources.Kropp_Prompt_Thighs_Body, v => entry.ThighCm = v),
        };

        for (int i = 0; i < steps.Length; i++)
        {
            var (title, body, set) = steps[i];
            var accept = i == steps.Length - 1 ? AppResources.Common_Save : AppResources.Common_Continue;
            var (cancelled, value) = await PromptPositiveDecimalAsync(title, body, accept, "cm");
            // Avbryt avbryter hela flödet — inget sparas. Tomt fält hoppar över måttet.
            if (cancelled) return;
            if (value.HasValue) set(value.Value);
        }

        if (entry.WaistCm.HasValue || entry.ChestCm.HasValue || entry.HipCm.HasValue
            || entry.ArmCm.HasValue || entry.ThighCm.HasValue)
        {
            try
            {
                await bodyRepo.SaveCompositionEntryAsync(entry);
                await LoadCompositionDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Kropp] LogCompositionAsync failed: {ex}");
                await Shell.Current.DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
            }
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
        await bodyRepo.DeleteCompositionEntryAsync(entry);
        await LoadCompositionDataAsync();
    }

    [RelayCommand]
    private async Task OpenSettingsAsync() =>
        await Shell.Current.GoToAsync(AppRoutes.Settings);
}

public class HeatmapTile
{
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public Color TileColor { get; set; } = Colors.Transparent;
    public Color TextColor { get; set; } = Colors.White;
    public string FrequencyText { get; set; } = "";
    public string ScoreText => Score >= 0.05 ? Score.ToString("F1") : "—";

    /// <summary>Textfärgen med bäst kontrast mot rutan: mörk på de ljusgröna rutorna, annars vit.</summary>
    public static Color TextOn(Color tile)
    {
        static double Ch(float v) => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        static double Lum(Color c) => 0.2126 * Ch(c.Red) + 0.7152 * Ch(c.Green) + 0.0722 * Ch(c.Blue);

        double bg = Lum(tile);
        double onWhite = 1.05 / (bg + 0.05);
        double onDark  = (bg + 0.05) / (Lum(DesignTokens.FabForeground) + 0.05);
        return onDark > onWhite ? DesignTokens.FabForeground : Colors.White;
    }
}
