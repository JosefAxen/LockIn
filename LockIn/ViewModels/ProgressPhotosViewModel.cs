using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class ProgressPhotosViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<PhotoRow> Photos { get; } = new();
    public ObservableCollection<PhotoGroup> PhotoGroups { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var all = await db.GetAllPhotosAsync();
        Photos.Clear();
        PhotoGroups.Clear();

        var byMonth = all
            .GroupBy(p => new DateTime(p.TakenAt.Year, p.TakenAt.Month, 1))
            .OrderByDescending(g => g.Key);

        foreach (var g in byMonth)
        {
            var monthKey = g.Key.ToString("MMMM yyyy").ToUpper();
            var group = new PhotoGroup(monthKey);
            foreach (var p in g.OrderByDescending(x => x.TakenAt))
            {
                var row = new PhotoRow(p);
                Photos.Add(row);
                group.Add(row);
            }
            PhotoGroups.Add(group);
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PhotoRow row)
    {
        var confirmed = await Shell.Current.DisplayAlert("Ta bort foto", "Ta bort det här fotot?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeletePhotoAsync(row.Photo);
        Photos.Remove(row);
        var group = PhotoGroups.FirstOrDefault(g => g.Contains(row));
        if (group != null)
        {
            group.Remove(row);
            if (group.Count == 0) PhotoGroups.Remove(group);
        }
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}

public class PhotoGroup(string monthKey) : ObservableCollection<PhotoRow>
{
    public string MonthKey { get; } = monthKey;
}
