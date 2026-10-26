using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using static LockIn.Services.DatabaseService;

namespace LockIn.ViewModels;

public partial class SessionDetailViewModel(ISessionRepository sessionRepo, PhotoService photos, IBodyRepository bodyRepo) : ObservableObject, IQueryAttributable
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

    private static async Task ShowErrorAsync(Exception ex, string context)
    {
        System.Diagnostics.Debug.WriteLine($"[SessionDetail] {context}: {ex}");
        try
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        catch { /* felvisningen får inte själv fälla sidan */ }
    }

    private async Task LoadAsync(SessionSummaryRow session)
    {
        IsLoading = true;
        try
        {
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

            var rows = await sessionRepo.GetSessionExerciseDetailsAsync(session.Id);
            ExerciseGroups.Clear();
            foreach (var group in rows.GroupBy(r => r.ExerciseName))
            {
                // Svenskt visningsnamn (SwedishName kommer från SQL, övriga via översättningstabellen)
                var g = new SessionExerciseGroup(SwedishExerciseNames.Display(group.Key));
                foreach (var set in group)
                    g.Add(set);
                ExerciseGroups.Add(g);
            }

            await RefreshPhotosAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "LoadAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshPhotosAsync()
    {
        var photos = await bodyRepo.GetPhotosForSessionAsync(_sessionId);
        Photos.Clear();
        foreach (var p in photos)
            Photos.Add(new PhotoRow(p));
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (_sessionId == 0) return;

        try
        {
            var action = await Shell.Current.DisplayActionSheetAsync(AppResources.SessionDetail_AddPhoto_Title, AppResources.Common_Cancel, null, AppResources.SessionDetail_Photo_TakePhoto, AppResources.SessionDetail_Photo_PickLibrary);
            var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
            Directory.CreateDirectory(dir);

            if (action == AppResources.SessionDetail_Photo_TakePhoto)
            {
                var file = await MediaPicker.Default.CapturePhotoAsync();
                if (file is not null)
                    await photos.SaveAsync(file, _sessionId, dir);
            }
            else if (action == AppResources.SessionDetail_Photo_PickLibrary)
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                if (files is not null)
                    foreach (var file in files)
                        if (file is not null)
                            await photos.SaveAsync(file, _sessionId, dir);
            }

            await RefreshPhotosAsync();
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "AddPhoto"); }
    }

    [RelayCommand]
    private async Task DeletePhotoAsync(PhotoRow row)
    {
        try
        {
            var confirmed = await Shell.Current.DisplayAlertAsync(AppResources.SessionDetail_DeletePhoto_Title, AppResources.SessionDetail_DeletePhoto_Body, AppResources.Common_Delete, AppResources.Common_Cancel);
            if (!confirmed) return;
            await bodyRepo.DeletePhotoAsync(row.Photo);
            Photos.Remove(row);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "DeletePhoto"); }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SessionDetail] GoBack: {ex}"); }
    }
}

public class SessionExerciseGroup(string exerciseName) : ObservableCollection<SessionExerciseDetailRow>
{
    public string ExerciseName { get; } = exerciseName;
}

/// <summary>Vikt med svenskt decimalkomma och utan onödiga nollor (82,5 / 85), eller sekunder för tidsset.</summary>
public class SetWeightDisplayConverter : IValueConverter
{
    public static readonly SetWeightDisplayConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is SessionExerciseDetailRow row
            ? row.SetType == SetType.Time ? $"{row.DurationSeconds}s" : row.WeightKg.ToString("0.##")
            : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

/// <summary>RIR för historiska set. Negativt värde (null i DB) = inte loggad → "–".</summary>
public class RirDisplayConverter : IValueConverter
{
    public static readonly RirDisplayConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int rir && rir >= 0 ? rir.ToString() : "–";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
