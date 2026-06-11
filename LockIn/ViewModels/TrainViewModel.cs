using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class TrainViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<WorkoutTemplate> Templates { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNoTemplates;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var templates = await db.GetTemplatesAsync();
        Templates.Clear();
        foreach (var t in templates)
            Templates.Add(t);
        HasNoTemplates = Templates.Count == 0;
        IsLoading = false;
    }

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
}
