using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(CallbackAction), "CallbackAction")]
public partial class ExercisePickerViewModel(DatabaseService db) : ObservableObject
{
    private List<Exercise> _allExercises = new();

    public Action<Exercise>? CallbackAction { get; set; }

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
        _allExercises = await db.GetExercisesAsync();

        MuscleChips.Clear();
        MuscleChips.Add(new MuscleGroupChip { Label = AppResources.Library_Chip_All, MuscleGroup = null, IsSelected = true });
        var groups = _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => g.ToString());
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

        ApplyFilter();
        IsLoading = false;
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
                                    || e.SwedishName.ToLowerInvariant().Contains(q));

        var desired = source.OrderBy(e => e.Name).ToList();

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
        CallbackAction?.Invoke(row.Exercise);
        await Shell.Current.GoToAsync("..");
    }

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => AppResources.Library_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Library_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Library_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Library_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Library_Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Library_Muscle_Core,
        MuscleGroup.FullBody  => AppResources.Library_Muscle_FullBody,
        _                     => AppResources.Library_Muscle_Other
    };

    private static Color GetMuscleColor(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => Color.FromArgb("#FB7185"),
        MuscleGroup.Back      => Color.FromArgb("#38BDF8"),
        MuscleGroup.Shoulders => Color.FromArgb("#A78BFA"),
        MuscleGroup.Biceps    => Color.FromArgb("#4ADE80"),
        MuscleGroup.Triceps   => Color.FromArgb("#FBBF24"),
        MuscleGroup.Legs      => Color.FromArgb("#F97316"),
        MuscleGroup.Core      => Color.FromArgb("#EC4899"),
        MuscleGroup.FullBody  => Color.FromArgb("#EF4444"),
        _                     => Color.FromArgb("#52525E"),
    };

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
    public string Name => Exercise.Name;
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
