using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class ProgressPhotosViewModel(DatabaseService db, IBodyRepository bodyRepo) : ObservableObject
{
    public ObservableCollection<PhotoRow> Photos { get; } = new();
    public ObservableCollection<PhotoGroup> PhotoGroups { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var all = await bodyRepo.GetAllPhotosAsync();
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PhotoRow row)
    {
        try
        {
            var confirmed = await Shell.Current.DisplayAlert(
                AppResources.ProgressPhotos_DeletePhotoTitle,
                AppResources.ProgressPhotos_DeletePhotoBody,
                AppResources.Common_Delete,
                AppResources.Common_Cancel);
            if (!confirmed) return;
            await bodyRepo.DeletePhotoAsync(row.Photo);
            Photos.Remove(row);
            var group = PhotoGroups.FirstOrDefault(g => g.Contains(row));
            if (group != null)
            {
                group.Remove(row);
                if (group.Count == 0) PhotoGroups.Remove(group);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProgressPhotos] DeletePhoto: {ex}");
            try { await Shell.Current.DisplayAlert("", AppResources.Common_ErrorRetry, AppResources.Common_OK); }
            catch { /* felvisningen får inte själv fälla sidan */ }
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProgressPhotos] GoBack: {ex}"); }
    }
}

public class PhotoGroup(string monthKey) : ObservableCollection<PhotoRow>
{
    public string MonthKey { get; } = monthKey;
}
