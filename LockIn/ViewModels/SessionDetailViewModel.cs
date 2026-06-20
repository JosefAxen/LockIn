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
        var action = await Shell.Current.DisplayActionSheetAsync("Lägg till foto", "Avbryt", null, "Ta foto", "Välj från bibliotek");
        FileResult? file = null;

        try
        {
            if (action == "Ta foto")
                file = await MediaPicker.Default.CapturePhotoAsync();
            else if (action == "Välj från bibliotek")
                file = (await MediaPicker.Default.PickPhotosAsync())?.FirstOrDefault();
        }
        catch { return; }

        if (file is null) return;

        var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        Directory.CreateDirectory(dir);
        var destPath = Path.Combine(dir, $"session_{_sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

        using var stream = await file.OpenReadAsync();
        using var dest = File.Create(destPath);
        await stream.CopyToAsync(dest);

        await db.SavePhotoAsync(new WorkoutPhoto
        {
            SessionId = _sessionId,
            FilePath = destPath,
            TakenAt = DateTime.Now
        });

        await RefreshPhotosAsync();
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
