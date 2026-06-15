using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class LibraryViewModel(DatabaseService db) : ObservableObject
{
    // ── Tab control ────────────────────────────────────────────────────────

    [ObservableProperty] private int _selectedTab = 0;

    public bool ShowExercises => SelectedTab == 0;
    public bool ShowTemplates => SelectedTab == 1;
    public bool ShowPrograms  => SelectedTab == 2;
    public bool ShowActionButton => SelectedTab < 2;

    private static Color ActiveTabBg  => TabColorHelper.ActiveBg;
    private static Color ActiveTabFg  => TabColorHelper.ActiveFg;
    private static Color InactiveTabBg => TabColorHelper.InactiveBg;
    private static Color InactiveTabFg => TabColorHelper.InactiveFg;

    public Color Tab0Bg => SelectedTab == 0 ? ActiveTabBg  : InactiveTabBg;
    public Color Tab1Bg => SelectedTab == 1 ? ActiveTabBg  : InactiveTabBg;
    public Color Tab2Bg => SelectedTab == 2 ? ActiveTabBg  : InactiveTabBg;
    public Color Tab0Fg => SelectedTab == 0 ? ActiveTabFg  : InactiveTabFg;
    public Color Tab1Fg => SelectedTab == 1 ? ActiveTabFg  : InactiveTabFg;
    public Color Tab2Fg => SelectedTab == 2 ? ActiveTabFg  : InactiveTabFg;

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(ShowExercises));
        OnPropertyChanged(nameof(ShowTemplates));
        OnPropertyChanged(nameof(ShowPrograms));
        OnPropertyChanged(nameof(ShowActionButton));
        OnPropertyChanged(nameof(Tab0Bg)); OnPropertyChanged(nameof(Tab0Fg));
        OnPropertyChanged(nameof(Tab1Bg)); OnPropertyChanged(nameof(Tab1Fg));
        OnPropertyChanged(nameof(Tab2Bg)); OnPropertyChanged(nameof(Tab2Fg));
        if (value == 1) _ = LoadTemplatesAsync();
    }

    [RelayCommand]
    private void SelectTab(int tab) => SelectedTab = tab;

    [RelayCommand]
    private async Task ActionButtonAsync()
    {
        if (SelectedTab == 0) await AddCustomExerciseAsync();
        else if (SelectedTab == 1) await NewTemplateAsync();
    }

    // ── Exercises tab ──────────────────────────────────────────────────────

    private List<Exercise> _allExercises = [];
    private MuscleGroup? _selectedMuscleGroup;

    public ObservableCollection<ExerciseGroup> Groups { get; } = new();
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
        foreach (var g in _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => g.ToString()))
            MuscleChips.Add(new MuscleGroupChip { Label = MuscleGroupLabel(g), MuscleGroup = g });

        ApplyFilter();
        if (SelectedTab == 1) await LoadTemplatesAsync();
        RefreshTabColors();
        IsLoading = false;
    }

    private void RefreshTabColors()
    {
        OnPropertyChanged(nameof(Tab0Bg)); OnPropertyChanged(nameof(Tab0Fg));
        OnPropertyChanged(nameof(Tab1Bg)); OnPropertyChanged(nameof(Tab1Fg));
        OnPropertyChanged(nameof(Tab2Bg)); OnPropertyChanged(nameof(Tab2Fg));
    }

    [RelayCommand]
    private void SelectMuscleChip(MuscleGroupChip chip)
    {
        foreach (var c in MuscleChips) c.IsSelected = false;
        chip.IsSelected = true;
        _selectedMuscleGroup = chip.MuscleGroup;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim().ToLowerInvariant();
        var source = _allExercises.AsEnumerable();
        if (_selectedMuscleGroup.HasValue)
            source = source.Where(e => e.MuscleGroup == _selectedMuscleGroup.Value);
        if (!string.IsNullOrEmpty(q))
            source = source.Where(e => e.Name.ToLowerInvariant().Contains(q));

        Groups.Clear();
        foreach (var group in source.GroupBy(e => e.MuscleGroup).OrderBy(g => g.Key.ToString()))
        {
            var g = new ExerciseGroup(MuscleGroupLabel(group.Key));
            foreach (var e in group.OrderBy(e => e.Name))
                g.Add(e);
            Groups.Add(g);
        }
    }

    [RelayCommand]
    private async Task OpenExerciseProgressAsync(Exercise exercise)
    {
        await Shell.Current.GoToAsync(nameof(ExerciseProgressPage), new Dictionary<string, object>
        {
            { "ExerciseId", exercise.Id }
        });
    }

    [RelayCommand]
    private async Task AddCustomExerciseAsync()
        => await Shell.Current.GoToAsync(nameof(CreateExercisePage));

    // ── Templates tab ──────────────────────────────────────────────────────

    public ObservableCollection<WorkoutTemplate> Templates { get; } = new();

    private async Task LoadTemplatesAsync()
    {
        var templates = await db.GetTemplatesAsync();
        Templates.Clear();
        foreach (var t in templates) Templates.Add(t);
    }

    [RelayCommand]
    private async Task NewTemplateAsync()
    {
        await Shell.Current.GoToAsync(nameof(TemplateEditPage), new Dictionary<string, object>
        {
            { "TemplateId", 0 }
        });
    }

    [RelayCommand]
    private async Task EditTemplateAsync(WorkoutTemplate template)
    {
        await Shell.Current.GoToAsync(nameof(TemplateEditPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
        });
    }

    [RelayCommand]
    private async Task StartFromTemplateAsync(WorkoutTemplate template)
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
        });
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(WorkoutTemplate template)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort mall", $"Ta bort \"{template.Name}\"?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteTemplateAsync(template);
        Templates.Remove(template);
    }

    // ── Programs tab ───────────────────────────────────────────────────────

    public IReadOnlyList<WorkoutProgram> Programs { get; } = WorkoutPrograms.All;

    [RelayCommand]
    private async Task OpenProgramAsync(WorkoutProgram program)
    {
        await Shell.Current.GoToAsync(nameof(ProgramDetailPage), new Dictionary<string, object>
        {
            { "ProgramId", program.Id }
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => "Bröst",
        MuscleGroup.Back      => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps    => "Biceps",
        MuscleGroup.Triceps   => "Triceps",
        MuscleGroup.Forearms  => "Underarmar",
        MuscleGroup.Legs      => "Ben",
        MuscleGroup.Core      => "Core",
        MuscleGroup.FullBody  => "Helkropp",
        _                     => "Övrigt"
    };
}

public class ExerciseGroup(string title) : ObservableCollection<Exercise>
{
    public string Title { get; } = title;
}
