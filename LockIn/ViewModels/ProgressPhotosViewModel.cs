using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class ProgressPhotosViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<PhotoRow> Photos { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var all = await db.GetAllPhotosAsync();
        Photos.Clear();
        foreach (var p in all)
            Photos.Add(new PhotoRow(p));
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PhotoRow row)
    {
        var confirmed = await Shell.Current.DisplayAlert("Ta bort foto", "Ta bort det här fotot?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeletePhotoAsync(row.Photo);
        Photos.Remove(row);
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
