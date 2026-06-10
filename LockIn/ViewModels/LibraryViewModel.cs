using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class LibraryViewModel(DatabaseService db) : ObservableObject
{
    private List<Exercise> _allExercises = new();

    public ObservableCollection<ExerciseGroup> Groups { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allExercises = await db.GetExercisesAsync();
        ApplyFilter();
        IsLoading = false;
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(q)
            ? _allExercises
            : _allExercises.Where(e => e.Name.ToLowerInvariant().Contains(q)).ToList();

        Groups.Clear();
        foreach (var group in filtered.GroupBy(e => e.MuscleGroup).OrderBy(g => g.Key.ToString()))
        {
            var g = new ExerciseGroup(MuscleGroupName(group.Key));
            foreach (var e in group.OrderBy(e => e.Name))
                g.Add(e);
            Groups.Add(g);
        }
    }

    [RelayCommand]
    private async Task AddCustomExerciseAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "Ny övning", "Namn på övningen:", "Lägg till", "Avbryt");
        if (string.IsNullOrWhiteSpace(name)) return;

        var exercise = new Exercise
        {
            Name = name.Trim(),
            IsCustom = true,
            DefaultRestSeconds = 120,
            MuscleGroup = MuscleGroup.Other
        };
        await db.SaveExerciseAsync(exercise);
        _allExercises = await db.GetExercisesAsync();
        ApplyFilter();
    }

    private static string MuscleGroupName(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest => "Bröst",
        MuscleGroup.Back => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps => "Biceps",
        MuscleGroup.Triceps => "Triceps",
        MuscleGroup.Legs => "Ben",
        MuscleGroup.Core => "Core",
        MuscleGroup.FullBody => "Helkropp",
        _ => "Övrigt"
    };
}

public class ExerciseGroup(string title) : ObservableCollection<Exercise>
{
    public string Title { get; } = title;
}
