using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;
using static LockIn.Services.DatabaseService;

namespace LockIn.ViewModels;

public partial class SessionDetailViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    public ObservableCollection<SessionExerciseGroup> ExerciseGroups { get; } = new();
    public ObservableCollection<PhotoRow> Photos { get; } = new();

    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _dateDisplay = "";
    [ObservableProperty] private string _durationDisplay = "";
    [ObservableProperty] private string _volumeDisplay = "";
    [ObservableProperty] private int _prCountValue;
    [ObservableProperty] private string _sessionNotes = "";
    [ObservableProperty] private bool _hasNotes;
    [ObservableProperty] private bool _isLoading;

    private int _sessionId;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Session", out var val) && val is SessionSummaryRow session)
            _ = LoadAsync(session);
    }

    private async Task LoadAsync(SessionSummaryRow session)
    {
        IsLoading = true;
        _sessionId = session.Id;
        TemplateName = session.TemplateName;
        DateDisplay = session.StartedAt.ToString("d MMM yyyy");
        PrCountValue = session.PRCount;
        VolumeDisplay = $"{session.TotalVolume:F0} kg";
        SessionNotes = session.Notes ?? "";
        HasNotes = !string.IsNullOrWhiteSpace(session.Notes);

        if (session.CompletedAt.HasValue)
        {
            var dur = session.CompletedAt.Value - session.StartedAt;
            DurationDisplay = dur.TotalHours >= 1
                ? $"{(int)dur.TotalHours}t {dur.Minutes}m"
                : $"{(int)dur.TotalMinutes}m";
        }

        var rows = await db.GetSessionExerciseDetailsAsync(session.Id);
        ExerciseGroups.Clear();
        foreach (var group in rows.GroupBy(r => r.ExerciseName))
        {
            var g = new SessionExerciseGroup(group.Key);
            foreach (var set in group)
                g.Add(set);
            ExerciseGroups.Add(g);
        }

        await RefreshPhotosAsync();
        IsLoading = false;
    }

    private async Task RefreshPhotosAsync()
    {
        var photos = await db.GetPhotosForSessionAsync(_sessionId);
        Photos.Clear();
        foreach (var p in photos)
            Photos.Add(new PhotoRow(p));
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (_sessionId == 0) return;

        var action = await Shell.Current.DisplayActionSheetAsync("Lägg till foto", "Avbryt", null, "Ta foto", "Välj från bibliotek");
        var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        Directory.CreateDirectory(dir);

        try
        {
            if (action == "Ta foto")
            {
                var file = await MediaPicker.Default.CapturePhotoAsync();
                if (file is not null)
                    await SavePhotoFileAsync(file, dir);
            }
            else if (action == "Välj från bibliotek")
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                if (files is not null)
                    foreach (var file in files)
                        if (file is not null)
                            await SavePhotoFileAsync(file, dir);
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Photos] Fel vid fotoval: {ex.Message}"); return; }

        await RefreshPhotosAsync();
    }

    private async Task SavePhotoFileAsync(FileResult file, string dir)
    {
        var destPath = Path.Combine(dir, $"session_{_sessionId}_{Guid.NewGuid():N}.jpg");
        using var stream = await file.OpenReadAsync();
        using var dest = File.Create(destPath);
        await stream.CopyToAsync(dest);
        try
        {
            await db.SavePhotoAsync(new WorkoutPhoto
            {
                SessionId = _sessionId,
                FilePath = destPath,
                TakenAt = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Photos] DB-fel, tar bort orphan: {ex.Message}");
            try { File.Delete(destPath); } catch { }
            throw;
        }
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PhotoRow row)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync("Ta bort foto", "Ta bort det här fotot?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeletePhotoAsync(row.Photo);
        Photos.Remove(row);
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}

public class SessionExerciseGroup(string exerciseName) : ObservableCollection<SessionExerciseDetailRow>
{
    public string ExerciseName { get; } = exerciseName;
}
