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
        FilteredExercises.Clear();
        foreach (var e in _allExercises.Where(e =>
            string.IsNullOrEmpty(q) || e.Name.ToLowerInvariant().Contains(q)))
            FilteredExercises.Add(e);
    }

    [RelayCommand]
    private async Task SelectExerciseAsync(Exercise exercise)
    {
        CallbackAction?.Invoke(exercise);
        await Shell.Current.GoToAsync("..");
    }
}
