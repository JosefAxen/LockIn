using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn;
using LockIn.Data;
using LockIn.Models;
using LockIn.Resources.Strings;
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

    private static readonly Color _glassActiveBg   = DesignTokens.GlassActiveBg;
    private static readonly Color _glassActiveFg   = DesignTokens.GlassActiveFg;
    private static readonly Color _glassInactiveBg = DesignTokens.GlassInactiveBg;
    private static readonly Color _glassInactiveFg = DesignTokens.GlassInactiveFg;

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

    // Pagination per muskelgrupp: visa en åt gången, ladda nästa när användaren
    // scrollar nära botten. Resterande grupper väntar i _pendingGroups.
    private List<(string Title, List<Exercise> Items)> _pendingGroups = new();
    private const int InitialGroupsToShow = 1;

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
        MuscleChips.Add(new MuscleGroupChip { Label = AppResources.Library_Chip_All, MuscleGroup = null, IsSelected = true });
        foreach (var g in _allExercises.Select(e => e.MuscleGroup).Distinct().OrderBy(g => g.ToString()))
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

        var allDesired = source
            .GroupBy(e => e.MuscleGroup)
            .OrderBy(g => g.Key.ToString())
            .Select(g => (Title: MuscleGroupLabel(g.Key), Items: g.OrderBy(e => e.Name).ToList()))
            .ToList();

        // Visa endast första N grupper; resten ligger i kö tills användaren scrollar.
        var visibleCount = Math.Min(InitialGroupsToShow, allDesired.Count);
        var desired = allDesired.Take(visibleCount).ToList();
        _pendingGroups = allDesired.Skip(visibleCount).ToList();

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

            if (existingIdx < 0)
            {
                // Ny grupp: fyll övningarna INNAN gruppen läggs till Groups.
                // MAUI observerar inte gruppen förrän Groups.Add/Insert körs,
                // så Add-anropen har noll UI-kostnad. Groups.Add avfyrar ett
                // enda InsertSections-event med alla celler — 60× snabbare
                // än att lägga till en tom grupp och sedan infoga 60 rader var för sig.
                var g = new ExerciseGroup(title);
                foreach (var e in items) g.Add(e);
                if (i < Groups.Count) Groups.Insert(i, g);
                else Groups.Add(g);
                continue;  // gruppen är redan korrekt, hoppa till nästa
            }

            var group = Groups[existingIdx];
            if (existingIdx != i)
            {
                Groups.RemoveAt(existingIdx);
                Groups.Insert(i, group);
            }

            // Befintlig grupp: synka övningar i O(n) utan Clear() (undviker
            // Reset → ReloadData → tangentbord stängs).
            //   1. Ta bort övningar som inte matchar filtret (bakåt, O(n))
            //   2. Merge-infoga saknade övningar med tvåpekar-metod (O(n))
            // Båda group och items är sorterade på Name → rak in-order-pass.
            var keepSet = items.ToHashSet();
            for (int j = group.Count - 1; j >= 0; j--)
                if (!keepSet.Contains(group[j])) group.RemoveAt(j);

            int gi = 0;
            for (int ii = 0; ii < items.Count; ii++)
            {
                if (gi < group.Count && ReferenceEquals(group[gi], items[ii]))
                    gi++;
                else
                {
                    group.Insert(gi, items[ii]);
                    gi++;
                }
            }
        }
    }

    [RelayCommand]
    private void LoadMoreGroups()
    {
        if (_pendingGroups.Count == 0) return;
        var next = _pendingGroups[0];
        _pendingGroups.RemoveAt(0);

        var g = new ExerciseGroup(next.Title);
        foreach (var e in next.Items) g.Add(e);
        Groups.Add(g);
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
            AppResources.Library_DeleteTemplate_Title,
            string.Format(AppResources.Library_DeleteTemplate_Body_Format, template.Name),
            AppResources.Common_Delete, AppResources.Common_Cancel);
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
        MuscleGroup.Chest     => AppResources.Library_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Library_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Library_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Library_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Library_Muscle_Triceps,
        MuscleGroup.Forearms  => AppResources.Library_Muscle_Forearms,
        MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Library_Muscle_Core,
        MuscleGroup.FullBody  => AppResources.Library_Muscle_FullBody,
        _                     => AppResources.Library_Muscle_Other
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

public class ExerciseGroup(string title) : ObservableCollection<Exercise>
{
    public string Title { get; } = title;
}
