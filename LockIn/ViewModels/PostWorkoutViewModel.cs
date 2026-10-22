using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class PostWorkoutViewModel(
    DatabaseService db,
    IHealthService health,
    PhotoService photos,
    ActiveWorkoutViewModel activeWorkout,
    WorkoutShareService share,
    ISettingsRepository settingsRepo,
    IBodyRepository bodyRepo,
    IExerciseRepository exerciseRepo,
    ISessionRepository sessionRepo,
    ICycleContextService cycleContext) : ObservableObject
{
    [ObservableProperty] private int _sessionId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _duration = "";
    [ObservableProperty] private string _totalVolume = "";
    [ObservableProperty] private string _totalSets = "";
    [ObservableProperty] private string _prCount = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSharing;
    [ObservableProperty] private bool _hasMesocycle;

    // true när passet är avgjort (sparat eller ångrat) — inget mer ska hända med det
    private bool _committed;
    // Spärr mot dubbeltryck på KLAR / Ångra
    private bool _isLeaving;
    private WorkoutSession? _loadedSession;

    public ObservableCollection<MuscleGroupRow> MuscleGroups { get; } = new();
    public ObservableCollection<PRRow> PRs { get; } = new();
    public ObservableCollection<NewAchievementRow> NewAchievements { get; } = new();
    public ObservableCollection<PhotoRow> Photos { get; } = new();

    partial void OnSessionIdChanged(int value)
    {
        _committed = false;
        _ = LoadAsync(value);
    }

    private static async Task ShowErrorAsync(Exception ex, string context)
    {
        System.Diagnostics.Debug.WriteLine($"[PostWorkout] {context}: {ex}");
        try
        {
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        catch { /* felvisningen får inte själv fälla sidan */ }
    }

    private async Task LoadAsync(int sessionId)
    {
        IsLoading = true;
        try
        {
            var session = await sessionRepo.GetSessionAsync(sessionId);
            if (session is null) return;

            _loadedSession = session;
            Notes = session.Notes ?? "";

            // Passnamnet finns inte på WorkoutSession — hämta det från det pågående passet
            TemplateName = activeWorkout.TemplateName;

            // Mesocykelkortet ("DU AVANCERAR …") visas bara när en cykel faktiskt är aktiv
            HasMesocycle = await cycleContext.GetCurrentAsync() is not null;

            {
                var elapsed = session.CompletedAt.HasValue
                    ? session.CompletedAt.Value - session.StartedAt
                    : DateTime.Now - session.StartedAt;
                Duration = $"{(int)elapsed.TotalMinutes}m";
            }

            var muscleTask = sessionRepo.GetSessionVolumeByMuscleGroupAsync(sessionId);
            var setsTask   = sessionRepo.GetAllSetsForSessionAsync(sessionId);
            await Task.WhenAll(muscleTask, setsTask);
            var muscleVolume = muscleTask.Result;
            var allSets = setsTask.Result;

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
                        Name = kv.Key.ToLocalizedLabel(),
                        Volume = kv.Value.Volume,
                        Sets = kv.Value.Sets,
                        ProgressFraction = maxVol > 0 ? (double)(kv.Value.Volume / maxVol) : 0
                    });
                }
            }

            var prSets = await sessionRepo.GetPRsForSessionAsync(sessionId);
            PrCount = prSets.Count.ToString();
            PRs.Clear();
            if (prSets.Count > 0)
            {
                var seTask  = sessionRepo.GetSessionExercisesAsync(sessionId);
                var exTask  = exerciseRepo.GetAllAsync();
                await Task.WhenAll(seTask, exTask);
                var seById      = seTask.Result.ToDictionary(se => se.Id);
                var exerciseById = exTask.Result.ToDictionary(e => e.Id);
                foreach (var ps in prSets)
                {
                    seById.TryGetValue(ps.SessionExerciseId, out var seRow);
                    var exercise = seRow is not null && exerciseById.TryGetValue(seRow.ExerciseId, out var ex) ? ex : null;
                    PRs.Add(new PRRow
                    {
                        ExerciseName = exercise?.DisplayName ?? "",
                        Display = $"{ps.WeightKg:0.##} kg × {ps.Reps}",
                        Epley1RM = string.Format(AppResources.PostWorkout_Epley1RM_Format, $"{PRService.CalculateEpley1RM(ps.WeightKg, ps.Reps):F0}")
                    });
                }
            }

            // Meriter kontrolleras efter att passet sparats som avslutat (sker när AVSLUTA PASS
            // bekräftas), så att "Första passet" m.fl. räknar med det här passet.
            await CheckAchievementsAsync(session, allSets);

            // HealthKit-sync sker i DoneAsync när passet släpps — en gång per pass.

            // Load photos
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

    private async Task CheckAchievementsAsync(WorkoutSession session, List<LoggedSet> allSets)
    {
        var newlyUnlocked = new List<AchievementService.AchievementDef>();

        async Task TryUnlock(AchievementId id)
        {
            if (await settingsRepo.UnlockAchievementAsync(id))
            {
                var def = AchievementService.Get(id);
                if (def is not null) newlyUnlocked.Add(def);
            }
        }

        var totalSessions = await sessionRepo.GetTotalCompletedCountAsync();
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
        var exercises = await exerciseRepo.GetAllAsync();
        if (exercises.Any(e => e.IsCustom))
            await TryUnlock(AchievementId.FirstCustomExercise);

        NewAchievements.Clear();
        foreach (var def in newlyUnlocked)
            NewAchievements.Add(new NewAchievementRow { Emoji = def.Emoji, Title = def.Title, Description = def.Description });
    }

    private async Task RefreshPhotosAsync()
    {
        if (_loadedSession is null) return;
        var photos = await bodyRepo.GetPhotosForSessionAsync(_loadedSession.Id);
        Photos.Clear();
        foreach (var p in photos)
            Photos.Add(new PhotoRow(p));
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        if (_loadedSession is null) return;

        try
        {
            var action = await Shell.Current.DisplayActionSheetAsync(
                AppResources.PostWorkout_AddPhoto_Title,
                AppResources.Common_Cancel, null,
                AppResources.PostWorkout_Photo_TakePhoto,
                AppResources.PostWorkout_Photo_PickLibrary);
            var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
            Directory.CreateDirectory(dir);

            if (action == AppResources.PostWorkout_Photo_TakePhoto)
            {
                var file = await MediaPicker.Default.CapturePhotoAsync();
                if (file is not null)
                    await photos.SaveAsync(file, _loadedSession.Id, dir);
            }
            else if (action == AppResources.PostWorkout_Photo_PickLibrary)
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                if (files is not null)
                    foreach (var file in files)
                        if (file is not null)
                            await photos.SaveAsync(file, _loadedSession.Id, dir);
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
            var confirmed = await Shell.Current.DisplayAlertAsync(
                AppResources.PostWorkout_DeletePhoto_Title,
                AppResources.PostWorkout_DeletePhoto_Body,
                AppResources.Common_Delete,
                AppResources.Common_Cancel);
            if (!confirmed) return;
            await bodyRepo.DeletePhotoAsync(row.Photo);
            Photos.Remove(row);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "DeletePhoto"); }
    }

    [RelayCommand]
    private async Task ShareWorkoutAsync()
    {
        if (IsSharing) return;
        IsSharing = true;
        try
        {
            var topMuscles = MuscleGroups
                .Take(3)
                .Select(m => (m.Name, m.ProgressFraction))
                .ToList();

            var imageData = new ShareImageData(
                TemplateName,
                _loadedSession?.StartedAt.ToString("d MMM yyyy") ?? "",
                Duration,
                TotalVolume,
                TotalSets,
                PrCount,
                topMuscles);

            var filePath = await share.CreateShareImageAsync(imageData);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.PostWorkout_Share_ImageTitle,
                File  = new ShareFile(filePath, "image/png")
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Share] Fel: {ex.Message}");
            await Shell.Current.DisplayAlert(
                "", AppResources.PostWorkout_Share_Error, AppResources.Common_OK);
        }
        finally
        {
            IsSharing = false;
        }
    }

    // Pushas ovanpå efter-passet-sidan. Sidan sparar inte passet vid push (se PostWorkoutPage).
    [RelayCommand]
    private async Task ViewMesocycleStatsAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.MesocycleStats); }
        catch (Exception ex) { await ShowErrorAsync(ex, "ViewMesocycleStats"); }
    }

    [RelayCommand]
    private async Task UndoFinishAsync()
    {
        if (_isLeaving) return;
        _isLeaving = true;
        try
        {
            if (await activeWorkout.ResumeFromPostWorkoutAsync(SessionId))
            {
                _committed = true;  // markera som "hanterad" — ingen commit
                await Shell.Current.GoToAsync(AppRoutes.Back);
            }
            else
            {
                // Passet är redan sparat och släppt — det finns inget pass att återgå till
                _committed = true;
                await Shell.Current.Navigation.PopToRootAsync(false);
                await Shell.Current.GoToAsync(AppRoutes.TrainTab);
            }
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "UndoFinish"); }
        finally { _isLeaving = false; }
    }

    [RelayCommand]
    private async Task DoneAsync()
    {
        if (_isLeaving) return;
        _isLeaving = true;
        try
        {
            var committedNow = await CommitIfNotDoneAsync();
            // Redan sparat (t.ex. efter ett tidigare misslyckat försök) — anteckningarna ska ändå med
            if (!committedNow)
                await SaveNotesAsync();

            // Sync till Apple Health efter commit — CompletedAt är satt. Bara en gång per pass.
            if (committedNow && _loadedSession is not null &&
                Preferences.Default.Get("healthkit_sync_enabled", false))
            {
                var completedAt = _loadedSession.CompletedAt ?? DateTime.Now;
                var durationMinutes = (completedAt - _loadedSession.StartedAt).TotalMinutes;
                var activeKcal = durationMinutes * 6.0; // ~6 kcal/min för styrketräning
                _ = health.SaveWorkoutAsync(_loadedSession.StartedAt, completedAt, activeKcal)
                    .ContinueWith(t => System.Diagnostics.Debug.WriteLine($"[HealthKit] Sync misslyckades: {t.Exception?.GetBaseException().Message}"),
                                  TaskContinuationOptions.OnlyOnFaulted);
            }

            await Shell.Current.Navigation.PopToRootAsync(false);
            await Shell.Current.GoToAsync(AppRoutes.TrainTab);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "Done"); }
        finally { _isLeaving = false; }
    }

    /// <summary>Släpper passet med anteckningarna. True om det sparades nu, false om det redan var avgjort.</summary>
    public async Task<bool> CommitIfNotDoneAsync()
    {
        if (_committed) return false;
        _committed = true;
        return await activeWorkout.CommitFinishAsync(SessionId, Notes);
    }

    private async Task SaveNotesAsync()
    {
        // Läs om passet — den laddade kopian kan ha inaktuellt CompletedAt
        var session = await sessionRepo.GetSessionAsync(SessionId);
        if (session is null || session.Notes == Notes) return;
        session.Notes = Notes;
        await sessionRepo.SaveSessionAsync(session);
    }

    /// <summary>
    /// Sidan lämnades utan KLAR eller Ångra (svep bakåt, systemnavigering). Tillbaka till
    /// passet betyder ångra; allt annat sparar passet med anteckningarna.
    /// </summary>
    public async Task HandleLeftPageAsync(bool returnedToWorkout)
    {
        if (_committed || _isLeaving) return;
        try
        {
            if (returnedToWorkout && await activeWorkout.ResumeFromPostWorkoutAsync(SessionId))
            {
                _committed = true;
                return;
            }
            // Passet är redan släppt (eller ett nytt har startats) — spara åtminstone anteckningarna
            if (!await CommitIfNotDoneAsync())
                await SaveNotesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostWorkout] HandleLeftPage: {ex}");
        }
    }
}

public class MuscleGroupRow
{
    public string Name { get; set; } = "";
    public decimal Volume { get; set; }
    public int Sets { get; set; }
    public double ProgressFraction { get; set; }
    public string VolumeDisplay => string.Format(AppResources.PostWorkout_VolumeDisplay_Format, $"{Volume:F0}");
    public string SetsDisplay   => string.Format(AppResources.PostWorkout_SetsDisplay_Format, Sets);
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
    public string Description { get; set; } = "";
}

public class PhotoRow(WorkoutPhoto photo)
{
    public WorkoutPhoto Photo { get; } = photo;
    public string FilePath => Photo.FilePath;
    public ImageSource Source => ImageSource.FromFile(Photo.FilePath);
    public string DateDisplay => Photo.TakenAt.ToString("d MMM HH:mm");
    public string MonthYear => Photo.TakenAt.ToString("MMMM yyyy").ToUpper();
}
