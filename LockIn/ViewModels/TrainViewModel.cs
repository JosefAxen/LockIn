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

    public async Task LoadAsync()
    {
        IsLoading = true;
        var templates = await db.GetTemplatesAsync();
        Templates.Clear();
        foreach (var t in templates)
            Templates.Add(t);
        IsLoading = false;
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
    private async Task StartWorkoutAsync(WorkoutTemplate template)
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
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
    private async Task DeleteTemplateAsync(WorkoutTemplate template)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort mall",
            $"Ta bort \"{template.Name}\"?",
            "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteTemplateAsync(template);
        Templates.Remove(template);
    }
}
