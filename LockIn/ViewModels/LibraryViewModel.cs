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

    private static readonly Color _glassActiveBg   = Color.FromArgb("#2BFFFFFF");
    private static readonly Color _glassActiveFg   = Color.FromArgb("#E2E8F0");
    private static readonly Color _glassInactiveBg = Colors.Transparent;
    private static readonly Color _glassInactiveFg = Color.FromArgb("#80FFFFFF");

    private static Color ActiveTabBg   => _glassActiveBg;
    private static Color ActiveTabFg   => _glassActiveFg;
    private static Color InactiveTabBg => _glassInactiveBg;
    private static Color InactiveTabFg => _glassInactiveFg;

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
        MuscleChips.Add(new MuscleGroupChip { Label = "ALLA", MuscleGroup = null, IsSelected = true });
        foreach (var g in _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => g.ToString()))
            MuscleChips.Add(new MuscleGroupChip { Label = MuscleGroupLabel(g), MuscleGroup = g });

        EquipmentChips.Clear();
        EquipmentChips.Add(new EquipmentChip { Label = "ALLA", Equipment = null, IsSelected = true });
        foreach (var eq in _allExercises
            .Select(e => e.Equipment)
            .Where(e => e != EquipmentType.Other)
            .Distinct()
            .OrderBy(e => e.ToString()))
        {
            EquipmentChips.Add(new EquipmentChip { Label = EquipmentTypeLabel(eq), Equipment = eq });
        }

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
        var source = _allExercises.AsEnumerable();
        if (_selectedMuscleGroup.HasValue)
            source = source.Where(e => e.MuscleGroup == _selectedMuscleGroup.Value);
        if (_selectedEquipment.HasValue)
            source = source.Where(e => e.Equipment == _selectedEquipment.Value);
        if (!string.IsNullOrEmpty(q))
            source = source.Where(e => e.Name.ToLowerInvariant().Contains(q)
                                    || e.SwedishName.ToLowerInvariant().Contains(q));

        var desired = source
            .GroupBy(e => e.MuscleGroup)
            .OrderBy(g => g.Key.ToString())
            .Select(g => (Title: MuscleGroupLabel(g.Key), Items: g.OrderBy(e => e.Name).ToList()))
            .ToList();

        // Remove groups no longer in result (backward to preserve indices).
        // Uses individual RemoveAt — never Clear() — so CollectionView fires
        // DeleteSections rather than ReloadData(), preserving keyboard focus.
        for (int i = Groups.Count - 1; i >= 0; i--)
            if (desired.All(d => d.Title != Groups[i].Title))
                Groups.RemoveAt(i);

        for (int i = 0; i < desired.Count; i++)
        {
            var (title, items) = desired[i];

            int existingIdx = -1;
            for (int k = i; k < Groups.Count; k++)
                if (Groups[k].Title == title) { existingIdx = k; break; }

            ExerciseGroup group;
            if (existingIdx < 0)
            {
                group = new ExerciseGroup(title);
                if (i < Groups.Count) Groups.Insert(i, group);
                else Groups.Add(group);
            }
            else
            {
                group = Groups[existingIdx];
                if (existingIdx != i)
                {
                    Groups.RemoveAt(existingIdx);
                    Groups.Insert(i, group);
                }
            }

            // Sync items within group using individual Remove/Insert —
            // no Clear() — so inner ObservableCollection fires DeleteItems/
            // InsertItems rather than ReloadSections, which preserves focus.
            var toKeep = items.ToHashSet();
            for (int j = group.Count - 1; j >= 0; j--)
                if (!toKeep.Contains(group[j])) group.RemoveAt(j);
            for (int j = 0; j < items.Count; j++)
            {
                int cur = group.IndexOf(items[j]);
                if (cur < 0) group.Insert(j, items[j]);
                else if (cur != j) { group.RemoveAt(cur); group.Insert(j, items[j]); }
            }
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

    private static string EquipmentTypeLabel(EquipmentType e) => e switch
    {
        EquipmentType.Barbell      => "SKIVSTÅNG",
        EquipmentType.Dumbbell     => "HANTEL",
        EquipmentType.Cable        => "KABEL",
        EquipmentType.Machine      => "MASKIN",
        EquipmentType.BodyOnly     => "KROPPSVIKT",
        EquipmentType.EZBar        => "EZ-STÅNG",
        EquipmentType.Kettlebell   => "KETTLEBELL",
        EquipmentType.Bands        => "BAND",
        EquipmentType.FoamRoll     => "FOAM ROLL",
        EquipmentType.MedicineBall => "MEDICINBOLL",
        _                          => "ÖVRIGT"
    };
}

public class ExerciseGroup(string title) : ObservableCollection<Exercise>
{
    public string Title { get; } = title;
}
