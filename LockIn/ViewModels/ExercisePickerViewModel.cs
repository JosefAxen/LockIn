using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(CallbackAction), "CallbackAction")]
[QueryProperty(nameof(MuscleFilterQuery), "muscle")]
public partial class ExercisePickerViewModel(IExerciseRepository exerciseRepo, IExerciseRecommendationService recommend) : ObservableObject
{
    private List<Exercise> _allExercises = new();

    public Action<Exercise>? CallbackAction { get; set; }
    public string? MuscleFilterQuery { get; set; }

    public ObservableCollection<ExercisePickerRow> FilteredExercises { get; } = new();
    public ObservableCollection<MuscleGroupChip> MuscleChips { get; } = new();
    public ObservableCollection<EquipmentChip> EquipmentChips { get; } = new();
    private EquipmentType? _selectedEquipment;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;

    private CancellationTokenSource? _searchCts;

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        Task.Delay(300, token).ContinueWith(
            _ => MainThread.BeginInvokeOnMainThread(ApplyFilter),
            token,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allExercises = await exerciseRepo.GetAllAsync();

            MuscleChips.Clear();
            MuscleChips.Add(new MuscleGroupChip { Label = AppResources.Library_Chip_All, MuscleGroup = null, IsSelected = true });
            // Etiketterna täcker alla muskelgrupper (även underarmar, quads m.fl.) — annars blir det flera "Övrigt"-chips
            var groups = _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => MuscleGroupLabel(g));
            foreach (var g in groups)
                MuscleChips.Add(new MuscleGroupChip { Label = MuscleGroupLabel(g), MuscleGroup = g });

            EquipmentChips.Clear();
            EquipmentChips.Add(new EquipmentChip { Label = AppResources.Library_Chip_All, Equipment = null, IsSelected = true });
            foreach (var eq in _allExercises
                .Select(e => e.Equipment)
                .Where(e => e != EquipmentType.Other)
                .Distinct()
                .OrderBy(e => e.ToString()))
            {
                EquipmentChips.Add(new EquipmentChip { Label = EquipmentTypeLabel(eq), Equipment = eq });
            }
            _selectedEquipment = null;

            ApplyMuscleQueryFilter();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task ShowErrorAsync(Exception ex, string context)
    {
        System.Diagnostics.Debug.WriteLine($"[ExercisePicker] {context}: {ex}");
        try { await CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.Common_ErrorRetry).Show(); }
        catch { /* felvisningen får inte själv fälla sidan */ }
    }

    private void ApplyMuscleQueryFilter()
    {
        if (string.IsNullOrEmpty(MuscleFilterQuery)) return;
        MuscleGroup? target = null;
        if (Enum.TryParse<MuscleGroup>(MuscleFilterQuery, out var byName)) target = byName;
        else if (int.TryParse(MuscleFilterQuery, out var byInt) && Enum.IsDefined(typeof(MuscleGroup), byInt))
            target = (MuscleGroup)byInt;
        if (target is null) return;
        var chip = MuscleChips.FirstOrDefault(c => c.MuscleGroup == target);
        if (chip is null) return;
        foreach (var c in MuscleChips) c.IsSelected = false;
        chip.IsSelected = true;
    }

    [RelayCommand]
    private async Task RecommendAsync()
    {
        try
        {
            var selectedMuscle = MuscleChips.FirstOrDefault(c => c.IsSelected)?.MuscleGroup;
            if (selectedMuscle is null)
            {
                await CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.ExercisePicker_Recommend_NoMuscle).Show();
                return;
            }
            var picks = await recommend.RecommendForMuscleAsync(
                selectedMuscle.Value,
                MusclePriorityLevel.Grow,
                new List<int>(),
                0, 0);
            if (picks.Count == 0)
            {
                await CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.ExercisePicker_Recommend_None).Show();
                return;
            }
            var top3 = picks.Take(3).ToList();
            var options = top3.Select(e => e.DisplayName).ToArray();
            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            var choice = await page.DisplayActionSheet(
                AppResources.ExercisePicker_Recommend_Title,
                AppResources.ExercisePicker_Recommend_Cancel,
                null,
                options);
            if (string.IsNullOrEmpty(choice) || choice == AppResources.ExercisePicker_Recommend_Cancel) return;
            var picked = top3.FirstOrDefault(e => e.DisplayName == choice);
            if (picked is null) return;
            CallbackAction?.Invoke(picked);
            await Shell.Current!.GoToAsync(AppRoutes.Back);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "Recommend"); }
    }

    [RelayCommand]
    private void SelectChip(MuscleGroupChip chip)
    {
        foreach (var c in MuscleChips) c.IsSelected = false;
        chip.IsSelected = true;
        ApplyFilter();
    }

    [RelayCommand]
    private void SelectEquipmentChip(EquipmentChip chip)
    {
        foreach (var c in EquipmentChips) c.IsSelected = false;
        chip.IsSelected = true;
        _selectedEquipment = chip.Equipment;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim().ToLowerInvariant();
        var selected = MuscleChips.FirstOrDefault(c => c.IsSelected);
        var source = _allExercises.AsEnumerable();

        if (selected?.MuscleGroup is MuscleGroup mg)
            source = source.Where(e => e.MuscleGroup == mg);
        if (_selectedEquipment.HasValue)
            source = source.Where(e => e.Equipment == _selectedEquipment.Value);
        if (!string.IsNullOrEmpty(q))
            source = source.Where(e => e.Name.ToLowerInvariant().Contains(q)
                                    || e.SwedishName.ToLowerInvariant().Contains(q)
                                    || e.DisplayName.ToLowerInvariant().Contains(q));

        var desired = source.OrderBy(e => e.DisplayName).ToList();

        // Aldrig Clear() — det avfyrar CollectionChanged.Reset → iOS ReloadData()
        // → Entry förlorar focus → tangentbordet stängs.
        // Bakåtpass tar bort rader som filtrerats bort, sedan merge-infoga nya.
        var keepIds = desired.Select(e => e.Id).ToHashSet();
        for (int i = FilteredExercises.Count - 1; i >= 0; i--)
            if (!keepIds.Contains(FilteredExercises[i].Exercise.Id))
                FilteredExercises.RemoveAt(i);

        int fi = 0;
        foreach (var ex in desired)
        {
            if (fi < FilteredExercises.Count && FilteredExercises[fi].Exercise.Id == ex.Id)
                fi++;
            else
            {
                FilteredExercises.Insert(fi, new ExercisePickerRow(ex, MuscleGroupLabel(ex.MuscleGroup), GetMuscleColor(ex.MuscleGroup)));
                fi++;
            }
        }
    }

    [RelayCommand]
    private async Task SelectExerciseAsync(ExercisePickerRow row)
    {
        try
        {
            CallbackAction?.Invoke(row.Exercise);
            await Shell.Current.GoToAsync(AppRoutes.Back);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "SelectExercise"); }
    }

    // Samma etikett som i resten av appen — täcker alla MuscleGroup-värden
    private static string MuscleGroupLabel(MuscleGroup mg) => mg.ToLocalizedLabel();

    // Samma färger som passets sektioner; #52525E för "Övrigt" gav 2,6:1 som textfärg
    private static Color GetMuscleColor(MuscleGroup mg) => DesignTokens.MuscleColor(mg);

    private static string EquipmentTypeLabel(EquipmentType e) => e switch
    {
        EquipmentType.Barbell      => AppResources.Library_Equipment_Barbell,
        EquipmentType.Dumbbell     => AppResources.Library_Equipment_Dumbbell,
        EquipmentType.Cable        => AppResources.Library_Equipment_Cable,
        EquipmentType.Machine      => AppResources.Library_Equipment_Machine,
        EquipmentType.BodyOnly     => AppResources.Library_Equipment_Bodyweight,
        EquipmentType.EZBar        => AppResources.Library_Equipment_EZBar,
        EquipmentType.Kettlebell   => AppResources.Library_Equipment_Kettlebell,
        EquipmentType.Bands        => AppResources.Library_Equipment_Bands,
        EquipmentType.FoamRoll     => AppResources.Library_Equipment_FoamRoll,
        EquipmentType.MedicineBall => AppResources.Library_Equipment_MedicineBall,
        _                          => AppResources.Library_Equipment_Other
    };
}

public class ExercisePickerRow
{
    public Exercise Exercise { get; }
    public string Name => Exercise.DisplayName;
    public bool IsCustom => Exercise.IsCustom;
    public string MuscleLabel { get; }
    public Color MuscleColor { get; }
    public string EquipmentLabel { get; }

    public ExercisePickerRow(Exercise exercise, string muscleLabel, Color muscleColor)
    {
        Exercise = exercise;
        MuscleLabel = muscleLabel;
        MuscleColor = muscleColor;
        EquipmentLabel = EquipmentTypeLabel(exercise.Equipment);
    }

    private static string EquipmentTypeLabel(EquipmentType e) => e switch
    {
        EquipmentType.Barbell      => AppResources.Library_Equipment_Barbell,
        EquipmentType.Dumbbell     => AppResources.Library_Equipment_Dumbbell,
        EquipmentType.Cable        => AppResources.Library_Equipment_Cable,
        EquipmentType.Machine      => AppResources.Library_Equipment_Machine,
        EquipmentType.BodyOnly     => AppResources.Library_Equipment_Bodyweight,
        EquipmentType.EZBar        => AppResources.Library_Equipment_EZBar,
        EquipmentType.Kettlebell   => AppResources.Library_Equipment_Kettlebell,
        EquipmentType.Bands        => AppResources.Library_Equipment_Bands,
        EquipmentType.FoamRoll     => AppResources.Library_Equipment_FoamRoll,
        EquipmentType.MedicineBall => AppResources.Library_Equipment_MedicineBall,
        _                          => ""
    };
}

public partial class MuscleGroupChip : ObservableObject
{
    public string Label { get; set; } = "";
    public MuscleGroup? MuscleGroup { get; set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Color _background = DesignTokens.ChipInactiveBg;
    [ObservableProperty] private Color _foreground = DesignTokens.ChipInactiveFg;

    partial void OnIsSelectedChanged(bool value)
    {
        Background = value ? DesignTokens.ChipActiveBg : DesignTokens.ChipInactiveBg;
        Foreground = value ? DesignTokens.ChipActiveFg : DesignTokens.ChipInactiveFg;
    }
}

public partial class EquipmentChip : ObservableObject
{
    public string Label { get; set; } = "";
    public EquipmentType? Equipment { get; set; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Color _background = DesignTokens.ChipInactiveBg;
    [ObservableProperty] private Color _foreground = DesignTokens.ChipInactiveFg;

    partial void OnIsSelectedChanged(bool value)
    {
        Background = value ? DesignTokens.ChipActiveBg : DesignTokens.ChipInactiveBg;
        Foreground = value ? DesignTokens.ChipActiveFg : DesignTokens.ChipInactiveFg;
    }
}
