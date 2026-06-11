using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(CallbackAction), "CallbackAction")]
public partial class ExercisePickerViewModel(DatabaseService db) : ObservableObject
{
    private List<Exercise> _allExercises = new();

    public Action<Exercise>? CallbackAction { get; set; }

    public ObservableCollection<Exercise> FilteredExercises { get; } = new();
    public ObservableCollection<MuscleGroupChip> MuscleChips { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allExercises = await db.GetExercisesAsync();

        MuscleChips.Clear();
        MuscleChips.Add(new MuscleGroupChip { Label = "ALLA", MuscleGroup = null, IsSelected = true });
        var groups = _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => g.ToString());
        foreach (var g in groups)
            MuscleChips.Add(new MuscleGroupChip { Label = MuscleGroupLabel(g), MuscleGroup = g });

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

    private void ApplyFilter()
    {
        var q = SearchText.Trim().ToLowerInvariant();
        var selected = MuscleChips.FirstOrDefault(c => c.IsSelected);
        var source = _allExercises.AsEnumerable();

        if (selected?.MuscleGroup is MuscleGroup mg)
            source = source.Where(e => e.MuscleGroup == mg);
        if (!string.IsNullOrEmpty(q))
            source = source.Where(e => e.Name.ToLowerInvariant().Contains(q));

        FilteredExercises.Clear();
        foreach (var e in source.OrderBy(e => e.Name))
            FilteredExercises.Add(e);
    }

    [RelayCommand]
    private async Task SelectExerciseAsync(Exercise exercise)
    {
        CallbackAction?.Invoke(exercise);
        await Shell.Current.GoToAsync("..");
    }

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => "Bröst",
        MuscleGroup.Back      => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps    => "Biceps",
        MuscleGroup.Triceps   => "Triceps",
        MuscleGroup.Legs      => "Ben",
        MuscleGroup.Core      => "Core",
        MuscleGroup.FullBody  => "Helkropp",
        _                     => "Övrigt"
    };
}

public partial class MuscleGroupChip : ObservableObject
{
    public string Label { get; set; } = "";
    public MuscleGroup? MuscleGroup { get; set; }
    [ObservableProperty] private bool _isSelected;

    private static readonly Color SelectedBg   = Color.FromArgb("#FF5A1F");
    private static readonly Color UnselectedBg = Color.FromArgb("#1A1A1A");
    private static readonly Color SelectedFg   = Color.FromArgb("#FFFFFF");
    private static readonly Color UnselectedFg = Color.FromArgb("#A0A0A8");

    public Color Background => IsSelected ? SelectedBg : UnselectedBg;
    public Color Foreground => IsSelected ? SelectedFg : UnselectedFg;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(Foreground));
    }
}
