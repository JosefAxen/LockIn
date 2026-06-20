using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class PostWorkoutViewModel(DatabaseService db, IHealthService health) : ObservableObject
{
    [ObservableProperty] private int _sessionId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _duration = "";
    [ObservableProperty] private string _totalVolume = "";
    [ObservableProperty] private string _totalSets = "";
    [ObservableProperty] private string _prCount = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _isLoading;

    private WorkoutSession? _loadedSession;

    public ObservableCollection<MuscleGroupRow> MuscleGroups { get; } = new();
    public ObservableCollection<PRRow> PRs { get; } = new();
    public ObservableCollection<NewAchievementRow> NewAchievements { get; } = new();
    public ObservableCollection<PhotoRow> Photos { get; } = new();

    partial void OnSessionIdChanged(int value) => _ = LoadAsync(value);

    private async Task LoadAsync(int sessionId)
    {
        IsLoading = true;

        var sessions = await db.GetSessionsAsync();
        var session = sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session is null) { IsLoading = false; return; }

        _loadedSession = session;
        Notes = session.Notes ?? "";

        var templates = await db.GetTemplatesAsync();
        TemplateName = templates.FirstOrDefault(t => t.Id == session.TemplateId)?.Name ?? "";

        if (session.CompletedAt.HasValue)
        {
            var elapsed = session.CompletedAt.Value - session.StartedAt;
            Duration = $"{(int)elapsed.TotalMinutes}m";
        }

        var muscleVolume = await db.GetSessionVolumeByMuscleGroupAsync(sessionId);
        var allSets = await GetAllSessionSetsAsync(sessionId);

        var totalVol = allSets.Sum(s => s.WeightKg * s.Reps);
        TotalVolume = totalVol >= 1000
            ? $"{totalVol / 1000:F1}k"
            : totalVol.ToString("F0");
        TotalSets = allSets.Count.ToString();

        MuscleGroups.Clear();
        if (muscleVolume.Any())
        {
            var maxVol = muscleVolume.Values.Max(v => v.Volume);
            foreach (var kv in muscleVolume.OrderByDescending(kv => kv.Value.Volume))
            {
                MuscleGroups.Add(new MuscleGroupRow
                {
                    Name = MuscleGroupName(kv.Key),
                    Volume = kv.Value.Volume,
                    Sets = kv.Value.Sets,
                    ProgressFraction = maxVol > 0 ? (double)(kv.Value.Volume / maxVol) : 0
                });
            }
        }

        var prSets = await db.GetPRsForSessionAsync(sessionId);
        PrCount = prSets.Count.ToString();
        PRs.Clear();
        foreach (var ps in prSets)
        {
            var se = await db.GetSessionExercisesAsync(sessionId);
            var seRow = se.FirstOrDefault(x => x.Id == ps.SessionExerciseId);
            var exercise = seRow != null ? await db.GetExerciseAsync(seRow.ExerciseId) : null;
            PRs.Add(new PRRow
            {
                ExerciseName = exercise?.Name ?? "",
                Display = $"{ps.WeightKg} kg × {ps.Reps}",
                Epley1RM = $"Est. 1RM {PRService.CalculateEpley1RM(ps.WeightKg, ps.Reps):F0} kg"
            });
        }

        // Check achievements
        await CheckAchievementsAsync(session, allSets);

        // Sync to Apple Health if enabled
        if (session.CompletedAt.HasValue &&
            Preferences.Default.Get("healthkit_sync_enabled", false))
        {
            var durationMinutes = (session.CompletedAt.Value - session.StartedAt).TotalMinutes;
            var activeKcal = durationMinutes * 6.0; // ~6 kcal/min for strength training
            _ = health.SaveWorkoutAsync(session.StartedAt, session.CompletedAt.Value, activeKcal)
                .ContinueWith(t => System.Diagnostics.Debug.WriteLine($"[HealthKit] Sync misslyckades: {t.Exception?.GetBaseException().Message}"),
                              TaskContinuationOptions.OnlyOnFaulted);
        }

        // Load photos
        await RefreshPhotosAsync();

        IsLoading = false;
    }

    private async Task CheckAchievementsAsync(WorkoutSession session, List<LoggedSet> allSets)
    {
        var newlyUnlocked = new List<AchievementService.AchievementDef>();

        async Task TryUnlock(AchievementId id)
        {
            if (await db.UnlockAchievementAsync(id))
            {
                var def = AchievementService.Get(id);
                if (def is not null) newlyUnlocked.Add(def);
            }
        }

        var totalSessions = await db.GetTotalCompletedSessionCountAsync();
        if (totalSessions >= 1)   await TryUnlock(AchievementId.FirstWorkout);
        if (totalSessions >= 5)   await TryUnlock(AchievementId.Sessions5);
        if (totalSessions >= 10)  await TryUnlock(AchievementId.Sessions10);
        if (totalSessions >= 25)  await TryUnlock(AchievementId.Sessions25);
        if (totalSessions >= 50)  await TryUnlock(AchievementId.Sessions50);
        if (totalSessions >= 100) await TryUnlock(AchievementId.Sessions100);

        var weekStreak = await db.GetCurrentWeekStreakAsync();
        if (weekStreak >= 2)  await TryUnlock(AchievementId.WeekStreak1);
        if (weekStreak >= 4)  await TryUnlock(AchievementId.WeekStreak4);
        if (weekStreak >= 12) await TryUnlock(AchievementId.WeekStreak12);

        var totalPRs = await db.GetTotalPRCountAsync();
        if (totalPRs >= 1)  await TryUnlock(AchievementId.FirstPR);
        if (totalPRs >= 10) await TryUnlock(AchievementId.PR10);
        if (totalPRs >= 50) await TryUnlock(AchievementId.PR50);

        var totalVolume = await db.GetTotalVolumeAsync();
        if (totalVolume >= 100000)  await TryUnlock(AchievementId.TotalVolume100k);
        if (totalVolume >= 500000)  await TryUnlock(AchievementId.TotalVolume500k);
        if (totalVolume >= 1000000) await TryUnlock(AchievementId.TotalVolume1M);

        if (await db.GetAllMuscleGroupsThisWeekAsync())
            await TryUnlock(AchievementId.AllMuscleGroups);

        if (session.CompletedAt.HasValue)
        {
            var duration = session.CompletedAt.Value - session.StartedAt;
            if (duration.TotalMinutes > 90) await TryUnlock(AchievementId.LongSession);
            if (session.StartedAt.Hour < 7)  await TryUnlock(AchievementId.EarlyBird);
            if (session.StartedAt.Hour >= 21) await TryUnlock(AchievementId.NightOwl);
        }

        // Custom exercise check
        var exercises = await db.GetExercisesAsync();
        if (exercises.Any(e => e.IsCustom))
            await TryUnlock(AchievementId.FirstCustomExercise);

        NewAchievements.Clear();
        foreach (var def in newlyUnlocked)
            NewAchievements.Add(new NewAchievementRow { Emoji = def.Emoji, Title = def.Title });
    }

    private async Task RefreshPhotosAsync()
    {
        if (_loadedSession is null) return;
        var photos = await db.GetPhotosForSessionAsync(_loadedSession.Id);
        Photos.Clear();
        foreach (var p in photos)
            Photos.Add(new PhotoRow(p));
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (_loadedSession is null) return;

        var action = await Shell.Current.DisplayActionSheetAsync("Lägg till foto", "Avbryt", null, "Ta foto", "Välj från bibliotek");
        var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        Directory.CreateDirectory(dir);

        try
        {
            if (action == "Ta foto")
            {
                var file = await MediaPicker.Default.CapturePhotoAsync();
                if (file is not null)
                    await SavePhotoFileAsync(file, _loadedSession.Id, dir);
            }
            else if (action == "Välj från bibliotek")
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                if (files is not null)
                    foreach (var file in files)
                        await SavePhotoFileAsync(file, _loadedSession.Id, dir);
            }
        }
        catch { return; }

        await RefreshPhotosAsync();
    }

    private async Task SavePhotoFileAsync(FileResult file, int sessionId, string dir)
    {
        var destPath = Path.Combine(dir, $"session_{sessionId}_{Guid.NewGuid():N}.jpg");
        using var stream = await file.OpenReadAsync();
        using var dest = File.Create(destPath);
        await stream.CopyToAsync(dest);
        try
        {
            await db.SavePhotoAsync(new WorkoutPhoto
            {
                SessionId = sessionId,
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

    private async Task<List<LoggedSet>> GetAllSessionSetsAsync(int sessionId)
    {
        var ses = await db.GetSessionExercisesAsync(sessionId);
        var result = new List<LoggedSet>();
        foreach (var se in ses)
        {
            var sets = await db.GetSetsForSessionExerciseAsync(se.Id);
            result.AddRange(sets);
        }
        return result;
    }

    [RelayCommand]
    private async Task DoneAsync()
    {
        if (_loadedSession is not null)
        {
            _loadedSession.Notes = Notes;
            await db.SaveSessionAsync(_loadedSession);
        }
        await Shell.Current.GoToAsync("//TrainPage");
    }

    private static string MuscleGroupName(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest => "Bröst",
        MuscleGroup.Back => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps => "Biceps",
        MuscleGroup.Triceps => "Triceps",
        MuscleGroup.Legs => "Ben",
        MuscleGroup.Core => "Core",
        MuscleGroup.FullBody => "Helkropp",
        _ => "Övrigt"
    };
}

public class MuscleGroupRow
{
    public string Name { get; set; } = "";
    public decimal Volume { get; set; }
    public int Sets { get; set; }
    public double ProgressFraction { get; set; }
    public string VolumeDisplay => $"{Volume:F0} kg";
    public string SetsDisplay => $"{Sets} set";
}

public class PRRow
{
    public string ExerciseName { get; set; } = "";
    public string Display { get; set; } = "";
    public string Epley1RM { get; set; } = "";
}

public class NewAchievementRow
{
    public string Emoji { get; set; } = "";
    public string Title { get; set; } = "";
}

public class PhotoRow(WorkoutPhoto photo)
{
    public WorkoutPhoto Photo { get; } = photo;
    public string FilePath => Photo.FilePath;
    public ImageSource Source => ImageSource.FromFile(Photo.FilePath);
    public string DateDisplay => Photo.TakenAt.ToString("d MMM HH:mm");
    public string MonthYear => Photo.TakenAt.ToString("MMMM yyyy").ToUpper();
}
