using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class HistoryViewModel(DatabaseService db) : ObservableObject
{
    private List<Exercise> _allExercises = new();

    public ObservableCollection<Exercise> FilteredExercises { get; } = new();
    public ObservableCollection<ExerciseHistoryRow> HistoryRows { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Exercise? _selectedExercise;
    [ObservableProperty] private bool _isLoading;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedExerciseChanged(Exercise? value) => _ = LoadHistoryAsync(value);

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
        FilteredExercises.Clear();
        foreach (var e in _allExercises.Where(e =>
            string.IsNullOrEmpty(q) || e.Name.ToLowerInvariant().Contains(q)))
            FilteredExercises.Add(e);
    }

    [RelayCommand]
    private void SelectExercise(Exercise exercise) => SelectedExercise = exercise;

    private async Task LoadHistoryAsync(Exercise? exercise)
    {
        if (exercise is null) return;
        IsLoading = true;
        HistoryRows.Clear();
        var rows = await db.GetBestSetPerSessionForExerciseAsync(exercise.Id);
        foreach (var (date, weight, reps, epley, isPR) in rows)
        {
            HistoryRows.Add(new ExerciseHistoryRow
            {
                Date = date.ToString("d MMM yyyy"),
                SetDisplay = $"{weight} kg × {reps}",
                Epley1RM = $"{epley:F0} kg",
                IsPR = isPR
            });
        }
        IsLoading = false;
    }
}

public class ExerciseHistoryRow
{
    public string Date { get; set; } = "";
    public string SetDisplay { get; set; } = "";
    public string Epley1RM { get; set; } = "";
    public bool IsPR { get; set; }
}
