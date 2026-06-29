using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class PeriodizationViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<TrainingCycle> Cycles { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var cycles = await db.GetCyclesAsync();
        Cycles.Clear();
        foreach (var c in cycles) Cycles.Add(c);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task CreateCycleAsync()
        => await Shell.Current.GoToAsync("cycledetail",
            new Dictionary<string, object> { { "CycleId", 0 } });

    [RelayCommand]
    private async Task OpenCycleAsync(TrainingCycle cycle)
        => await Shell.Current.GoToAsync("cycledetail",
            new Dictionary<string, object> { { "CycleId", cycle.Id } });

    [RelayCommand]
    private async Task DeleteCycleAsync(TrainingCycle cycle)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.CycleDetail_Delete_Title,
            string.Format(AppResources.CycleDetail_Delete_Body_Format, cycle.Name),
            AppResources.Common_Delete,
            AppResources.Common_Cancel);
        if (!confirmed) return;
        await db.DeleteCycleAsync(cycle.Id);
        Cycles.Remove(cycle);
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(TrainingCycle cycle)
    {
        if (cycle.IsActive) return;
        await db.SetActiveCycleAsync(cycle.Id);
        await LoadAsync();
    }
}
