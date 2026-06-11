using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class TrainViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<ProgramGroup> ProgramGroups { get; } = new();
    public ObservableCollection<WorkoutTemplate> FreeTemplates { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNoTemplates;
    [ObservableProperty] private bool _hasProgramGroups;
    [ObservableProperty] private int _weekCount;
    [ObservableProperty] private int _streak;
    [ObservableProperty] private int _totalCount;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var templates = await db.GetTemplatesAsync();

        // Group by program
        ProgramGroups.Clear();
        var grouped = templates
            .Where(t => t.ProgramId != null)
            .GroupBy(t => t.ProgramId!);

        foreach (var group in grouped)
        {
            var program = WorkoutPrograms.All.FirstOrDefault(p => p.Id == group.Key);
            var pg = new ProgramGroup
            {
                ProgramId = group.Key,
                ProgramName = program?.Name ?? group.Key
            };
            foreach (var t in group) pg.Templates.Add(t);
            ProgramGroups.Add(pg);
        }

        // Free templates (no program)
        FreeTemplates.Clear();
        foreach (var t in templates.Where(t => t.ProgramId == null))
            FreeTemplates.Add(t);

        HasProgramGroups = ProgramGroups.Count > 0;
        HasNoTemplates = ProgramGroups.Count == 0 && FreeTemplates.Count == 0;

        WeekCount = await db.GetSessionCountThisWeekAsync();
        Streak = await db.GetCurrentStreakAsync();
        TotalCount = await db.GetTotalCompletedSessionCountAsync();

        IsLoading = false;
    }

    [RelayCommand]
    private void ToggleProgramGroup(ProgramGroup group) => group.IsExpanded = !group.IsExpanded;

    [RelayCommand]
    private async Task StartWorkoutAsync(WorkoutTemplate template)
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
        });
    }

    [RelayCommand]
    private async Task StartFreeWorkoutAsync()
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", 0 }
        });
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(WorkoutTemplate template)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort mall", $"Ta bort \"{template.Name}\"?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteTemplateAsync(template);
        FreeTemplates.Remove(template);
        // Also remove from program groups if present
        foreach (var pg in ProgramGroups)
            pg.Templates.Remove(template);
    }
}
