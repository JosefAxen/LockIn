using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LockIn.ViewModels;

public partial class ActiveWorkoutViewModel(DatabaseService db, PRService pr, RestTimerService timer, ISoundService sound, ActiveWorkoutStateService state, NotificationService notifications, IWeightProgressionService weightProgression, ICycleContextService cycleContext, IMesocycleService mesocycles, ISettingsRepository settingsRepo, IExerciseRepository exerciseRepo, ITemplateRepository templateRepo, ISessionRepository sessionRepo)
    : ObservableObject, IQueryAttributable
{
    private WorkoutSession? _session;
    private int _templateId;           // lokalt spårad, WorkoutSession har inte TemplateId längre
    private CycleContext? _activeCycle;
    private WorkoutExerciseSection? _activeTimerSection;
    private WorkoutExerciseSection? _currentTimerSection;
    private DateTime _startTime;
    private CancellationTokenSource? _clockCts;
    private readonly Dictionary<int, HashSet<int>> _supersetRound = new();
    private readonly object _supersetLock = new();
    private readonly HashSet<MuscleGroup> _feedbackShownFor = new();
    private readonly HashSet<int> _jointPainReportedFor = new();
    // Serialiserar alla DB-skrivningar för set (logga, ändra, ångra, ta bort) så att
    // snabba tryck inte ger dubbletter eller skriver över varandra i fel ordning.
    private readonly SemaphoreSlim _setLock = new(1, 1);
    // Scroll-mål som begärdes medan sidan inte lyssnade (t.ex. övning tillagd från väljaren)
    private int? _pendingScrollId;

    [ObservableProperty] private string _templateName = AppResources.ActiveWorkout_FreeWorkout;
    [ObservableProperty] private string _elapsedTime = "0:00";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasPR;
    [ObservableProperty] private string _prMessage = "";
    [ObservableProperty] private bool _hasAutoProgress;
    [ObservableProperty] private string _autoProgressMessage = "";
    [ObservableProperty] private bool _showRirHint;
    [ObservableProperty] private bool _isDeloadWeek;
    [ObservableProperty] private string _weekDayHeader = "";
    [ObservableProperty] private string _weekDaySubHeader = "";
    [ObservableProperty] private bool _hasMesocycle;
    [ObservableProperty] private bool _showStartWeightHint;

    public event EventHandler? PRScored;
    public event EventHandler<int>? ScrollToSectionRequested;

    public ObservableCollection<WorkoutExerciseSection> Exercises { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int templateId = 0;
        if (query.TryGetValue("TemplateId", out var val) && val is int id)
            templateId = id;
        // Navigating back to an ongoing workout (e.g. via "PASS PÅGÅR" banner, or back from
        // the post-workout page) — no TemplateId in query. Never start a new workout then.
        if ((state.IsActive || _session is not null) && !query.ContainsKey("TemplateId"))
            return;
        // Return early only when navigating back from a sub-page during the same ongoing workout
        if (_session is not null && _templateId == templateId)
            return;
        _ = LoadAsync(templateId);
    }

    private async Task LoadAsync(int templateId)
    {
        IsLoading = true;
        try
        {
            Exercises.Clear();
            HasPR = false;
            PrMessage = "";
            HasAutoProgress = false;
            AutoProgressMessage = "";

            var settings = await settingsRepo.GetAsync();
            ShowRirHint = !settings.HasSeenRirHint;

            _activeCycle = await cycleContext.GetCurrentAsync();
            IsDeloadWeek = _activeCycle?.IsDeload ?? false;
            HasMesocycle = _activeCycle is not null;

            if (templateId != 0)
            {
                var template = await templateRepo.GetAsync(templateId);
                TemplateName = template?.Name ?? AppResources.ActiveWorkout_FreeWorkout;
            }

            if (_activeCycle is not null)
            {
                WeekDayHeader = string.Format(AppResources.ActiveWorkout_Week_Day_Format,
                    _activeCycle.WeekNumber, await ComputeDayNumberAsync());
                WeekDaySubHeader = string.Format(AppResources.ActiveWorkout_DayLabel_Format,
                    ComputeDayName(), TemplateName);
            }
            else
            {
                WeekDayHeader = AppResources.ActiveWorkout_FreeWorkout;
                WeekDaySubHeader = TemplateName;
            }

            // Skapa sessionen EFTER att övningar laddats, för att undvika orphan sessions
            // vid avbrutet pass (t.ex. Back-gesture innan SessionExercises skapas)
            _templateId = templateId;
            _session = new WorkoutSession
            {
                StartedAt = DateTime.Now,
            };
            await sessionRepo.SaveSessionAsync(_session);
            _startTime = _session.StartedAt;

            if (templateId != 0)
            {
                // ADR-001 PR 4: batch-hämta hela template-strukturen i 3 queries (löser N+1).
                var structure = await templateRepo.GetStructureAsync(templateId);
                if (structure is not null)
                {
                    int sortIndex = 0;
                    foreach (var day in structure.Days)
                    {
                        if (!structure.SlotsByDayId.TryGetValue(day.Id, out var slots)) continue;
                        foreach (var slot in slots)
                        {
                            if (!structure.ExercisesBySlotId.TryGetValue(slot.Id, out var exs)) continue;
                            foreach (var te in exs)
                            {
                                var exercise = await exerciseRepo.GetAsync(te.ExerciseId);
                                if (exercise is null) continue;
                                await AddExerciseSectionAsync(exercise, sortIndex++, 3, te.TargetRepsMin,
                                    0m, te.DefaultRestSeconds,
                                    te.TargetRepsMin, te.TargetRepsMax, 2.5m,
                                    0, null, te.Id);
                            }
                        }
                    }
                }
            }

            // Blå info-banner: första passet någonsin (inga prev-sets alls) och användaren har inte sett hint
            if (!settings.HasSeenStartWeightHint
                && Exercises.All(s => string.IsNullOrEmpty(s.PrevSessionSummary)))
            {
                ShowStartWeightHint = true;
            }

            StartClock();
            state.Activate();
        }
        catch (Exception ex)
        {
            // Om sessionen hann sparas i DB men laddningen misslyckades — ta bort den,
            // annars blir den ett tomt pass i historiken.
            if (_session is not null)
                _ = DeleteSessionSafeAsync(_session.Id);
            _session = null;
            Exercises.Clear();
            await ShowErrorAsync(ex, "LoadAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<WorkoutExerciseSection> AddExerciseSectionAsync(
        Exercise exercise, int orderIndex, int sets = 3, int reps = 8,
        decimal targetWeight = 0, int restSeconds = 90,
        int targetRepsMin = 0, int targetRepsMax = 0, decimal weightIncrementKg = 2.5m,
        int autoProgressMode = 0, int? supersetGroupId = null, int templateExerciseId = 0,
        bool scrollIntoView = false)
    {
        // Sidan kan ligga kvar efter att passet avslutats — lägg aldrig till övningar utan session
        var session = _session ?? throw new InvalidOperationException("Inget aktivt pass.");

        var se = new SessionExercise
        {
            SessionId = session.Id,
            ExerciseId = exercise.Id,
            OrderIndex = orderIndex,
            SupersetGroupId = supersetGroupId
        };
        await sessionRepo.SaveSessionExerciseAsync(se);

        var section = new WorkoutExerciseSection
        {
            SessionExerciseId = se.Id,
            ExerciseId = exercise.Id,
            TemplateExerciseId = templateExerciseId,
            ExerciseName = exercise.DisplayName,
            ExerciseDescription = exercise.Description ?? "",
            DefaultRestSeconds = restSeconds,
            RestSeconds = restSeconds,
            TargetReps = reps,
            TargetRepsMin = targetRepsMin,
            TargetRepsMax = targetRepsMax,
            WeightIncrementKg = weightIncrementKg,
            AutoProgressMode = autoProgressMode,
            SupersetGroupId = supersetGroupId,
            MuscleGroup = exercise.MuscleGroup,
            TargetRir = _activeCycle?.TargetRir ?? -1,
        };

        // Priority-tag från mesocykeln (Emphasize/Grow/Maintain)
        if (_activeCycle is not null)
        {
            var prio = await mesocycles.GetPriorityAsync(_activeCycle.MesocycleId, exercise.MuscleGroup);
            section.Priority = prio?.PriorityLevel ?? MusclePriorityLevel.Grow;
        }

        // Rekommenderat-RIR-hint under set-raderna
        if (section.HasTargetRir)
        {
            section.RecommendedRirHintText = string.Format(
                AppResources.ActiveWorkout_RecommendedRir_Format, section.TargetRir);
            section.ShowRecommendedRirHint = true;
        }

        var prevSets = await sessionRepo.GetLastSessionSetsAsync(exercise.Id, session.Id);

        string prevSummary = "";
        if (prevSets.Count > 0)
        {
            var parts = prevSets
                .Where(s => s.SetType != SetType.Warmup)
                .Select(s => s.SetType == SetType.Time && s.DurationSeconds > 0
                    ? $"{s.DurationSeconds}s"
                    : s.WeightKg > 0
                        ? $"{Kg(s.WeightKg)}×{s.Reps}"
                        : s.Reps > 0
                            ? $"{s.Reps}r"
                            : null)
                .Where(p => p is not null)
                .ToList();
            prevSummary = parts.Count > 0 ? "Förra: " + string.Join(" · ", parts) : "";
        }
        section.PrevSessionSummary = prevSummary;

        // RP-baserad vikt-suggestion när template inte äger progression
        WeightSuggestion? rpSuggestion = null;
        if (!(autoProgressMode > 0 && targetWeight > 0))
        {
            rpSuggestion = await weightProgression.SuggestNextWeightAsync(exercise.Id, session.Id, reps);
            if (rpSuggestion is not null)
                section.WeightSuggestionReason = rpSuggestion.Reason;
        }

        for (int s = 1; s <= sets; s++)
        {
            var prev = prevSets.ElementAtOrDefault(s - 1);

            // Progression äger hints när aktiv, annars RP-suggestion från förra passet, sist senaste sessions vikt
            var weightHint = autoProgressMode > 0 && targetWeight > 0
                ? Kg(targetWeight)
                : rpSuggestion is not null
                    ? Kg(rpSuggestion.WeightKg)
                    : (prev is { WeightKg: > 0 } ? Kg(prev.WeightKg) : "");
            var repsHint = autoProgressMode > 0
                ? reps.ToString()
                : prev?.SetType == SetType.Time && prev.DurationSeconds > 0
                    ? prev.DurationSeconds.ToString()
                    : (prev is { Reps: > 0 } ? prev.Reps.ToString() : "");

            section.Sets.Add(new LoggedSetRow
            {
                SessionExerciseId = se.Id,
                ExerciseId = exercise.Id,
                SetNumber = s,
                WeightText = "",
                RepsText = "",
                PrevWeightHint = weightHint,
                PrevRepsHint = repsHint,
                TargetReps = reps,
                TargetRir = _activeCycle?.TargetRir ?? -1,
            });
        }

        Exercises.Add(section);
        if (scrollIntoView)
            RequestScrollTo(section.SessionExerciseId);
        return section;
    }

    public Task<WorkoutExerciseSection> AddExerciseFromPickerAsync(Exercise exercise)
        => AddExerciseSectionAsync(exercise, Exercises.Count, scrollIntoView: true);

    /// <summary>Svensk visning av vikt utan onödiga nollor: 82,5 / 85.</summary>
    private static string Kg(decimal weightKg) => weightKg.ToString("0.##");

    private static bool TryParseDecimal(string? text, out decimal value) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'),
            NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private void RequestScrollTo(int sessionExerciseId)
    {
        var handler = ScrollToSectionRequested;
        if (handler is null)
        {
            _pendingScrollId = sessionExerciseId;
            return;
        }
        handler.Invoke(this, sessionExerciseId);
    }

    /// <summary>Hämtar (och nollställer) ett scroll-mål som begärdes medan sidan inte lyssnade.</summary>
    public int? TakePendingScroll()
    {
        var id = _pendingScrollId;
        _pendingScrollId = null;
        return id;
    }

    private static async Task ShowErrorAsync(Exception ex, string context)
    {
        System.Diagnostics.Debug.WriteLine($"[ActiveWorkout] {context}: {ex}");
        try { await Toast.Make(AppResources.Common_ErrorRetry).Show(); }
        catch { /* Toast får inte fälla passet */ }
    }

    [RelayCommand]
    private async Task DismissRirHintAsync()
    {
        ShowRirHint = false;
        try
        {
            var settings = await settingsRepo.GetAsync();
            settings.HasSeenRirHint = true;
            await settingsRepo.SaveAsync(settings);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "DismissRirHint"); }
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        try
        {
            await Shell.Current.GoToAsync(AppRoutes.ExercisePicker, new Dictionary<string, object>
            {
                { "CallbackAction", new Action<Exercise>(async ex =>
                {
                    try
                    {
                        await AddExerciseSectionAsync(ex, Exercises.Count, scrollIntoView: true);
                    }
                    catch (Exception e) { await ShowErrorAsync(e, "AddExercise"); }
                })}
            });
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "AddExercise"); }
    }

    [RelayCommand]
    private async Task RemoveExerciseAsync(WorkoutExerciseSection section)
    {
        try
        {
            var loggedCount = section.Sets.Count(s => s.IsCompleted);
            var detail = loggedCount > 0
                ? string.Format(AppResources.ActiveWorkout_RemoveExercise_Body_Logs, section.ExerciseName, loggedCount)
                : string.Format(AppResources.ActiveWorkout_RemoveExercise_Body_Empty, section.ExerciseName);
            var ok = await Shell.Current.DisplayAlert(
                AppResources.ActiveWorkout_RemoveExercise_Title,
                detail,
                AppResources.Common_Delete, AppResources.Common_Cancel);
            if (!ok) return;
            await sessionRepo.DeleteSessionExerciseWithSetsAsync(section.SessionExerciseId);
            Exercises.Remove(section);
            RefreshPRBanner();
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "RemoveExercise"); }
    }

    [RelayCommand]
    private async Task ChangeRestTimeAsync(WorkoutExerciseSection section)
    {
        try
        {
            var result = await Shell.Current.DisplayPromptAsync(
                AppResources.ActiveWorkout_RestTime_Title,
                string.Format(AppResources.ActiveWorkout_RestTime_Body_Format, section.ExerciseName),
                initialValue: section.RestSeconds.ToString(),
                keyboard: Keyboard.Numeric);

            if (int.TryParse(result, out var secs) && secs > 0)
                section.RestSeconds = secs;
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "ChangeRestTime"); }
    }

    [RelayCommand]
    private void AddSet(WorkoutExerciseSection section)
    {
        var last = section.Sets.LastOrDefault();
        section.Sets.Add(new LoggedSetRow
        {
            SessionExerciseId = section.SessionExerciseId,
            ExerciseId = section.ExerciseId,
            SetNumber = section.Sets.Count + 1,
            WeightText = last?.WeightText ?? "",
            RepsText = last?.RepsText ?? "8",
            TargetReps = section.TargetReps,
            TargetRir = section.TargetRir,
            IsFreshlyAdded = true
        });
    }

    /// <summary>
    /// "− SET": sista raden tas bort. Är den loggad krävs bekräftelse, eftersom setet
    /// då också raderas ur databasen och historiken.
    /// </summary>
    public async Task<bool> ConfirmRemoveLastSetAsync(WorkoutExerciseSection section)
    {
        if (section.Sets.Count <= 1) return false;
        var last = section.Sets[^1];
        if (!last.IsCompleted) return true;
        try
        {
            return await Shell.Current.DisplayAlert(
                AppResources.ActiveWorkout_RemoveLoggedSet_Title,
                string.Format(AppResources.ActiveWorkout_RemoveLoggedSet_Body_Format, last.SetNumber, section.ExerciseName),
                AppResources.Common_Delete, AppResources.Common_Cancel);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "ConfirmRemoveLastSet");
            return false;
        }
    }

    /// <summary>Tar bort sista raden (och dess LoggedSet i databasen). False om det misslyckades.</summary>
    public async Task<bool> RemoveLastSetAsync(WorkoutExerciseSection section)
    {
        if (section.Sets.Count <= 1) return false;
        var last = section.Sets[^1];
        await _setLock.WaitAsync();
        try
        {
            if (last.LoggedSetId > 0)
                await sessionRepo.DeleteLoggedSetAsync(last.LoggedSetId);
            last.LoggedSetId = 0;
            section.Sets.Remove(last);
            RefreshPRBanner();
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "RemoveLastSet");
            return false;
        }
        finally
        {
            _setLock.Release();
        }
    }

    [RelayCommand]
    private async Task DeleteSetAsync(LoggedSetRow set)
    {
        var section = Exercises.FirstOrDefault(e => e.Sets.Contains(set));
        if (section is null || section.Sets.Count <= 1) return;
        await _setLock.WaitAsync();
        try
        {
            if (set.LoggedSetId > 0)
                await sessionRepo.DeleteLoggedSetAsync(set.LoggedSetId);
            set.LoggedSetId = 0;
            section.Sets.Remove(set);
            for (int i = 0; i < section.Sets.Count; i++)
                section.Sets[i].SetNumber = i + 1;
            RefreshPRBanner();
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "DeleteSet"); }
        finally { _setLock.Release(); }
    }

    [RelayCommand]
    private void ChangeSetType(LoggedSetRow set)
    {
        if (set.IsCompleted) return;
        set.SetType = set.SetType switch
        {
            SetType.Normal  => SetType.Warmup,
            SetType.Warmup  => SetType.Time,
            SetType.Time    => SetType.Dropset,
            SetType.Dropset => SetType.Normal,
            _               => SetType.Normal
        };

        if (set.SetType == SetType.Dropset)
            SuggestDropWeight(set);
    }

    private void SuggestDropWeight(LoggedSetRow set)
    {
        var section = Exercises.FirstOrDefault(s => s.Sets.Contains(set));
        if (section == null) return;
        var idx = section.Sets.IndexOf(set);
        if (idx <= 0) return;
        var prev = section.Sets[idx - 1];
        var baseStr = !string.IsNullOrEmpty(prev.WeightText) ? prev.WeightText : prev.PrevWeightHint;
        if (string.IsNullOrEmpty(baseStr)) return;
        // "82,5" och "82.5" ska båda bli 82,5 — aldrig NumberStyles.Any (då blir "82,5" 825)
        if (!TryParseDecimal(baseStr, out var baseW) || baseW <= 0) return;
        var suggested = Math.Round(baseW * 0.8m / 2.5m) * 2.5m;
        set.WeightText = Kg(suggested);
    }

    /// <summary>Returnerar ett felmeddelande om raden saknar giltiga reps/sekunder, annars null.</summary>
    private static string? ParseSet(LoggedSetRow set, out decimal weight, out int reps, out int durationSeconds)
    {
        reps = 0;
        durationSeconds = 0;
        if (!TryParseDecimal(set.WeightText, out weight) || weight < 0)
            weight = 0;

        if (set.SetType == SetType.Time)
        {
            if (!int.TryParse(set.RepsText, out durationSeconds) || durationSeconds <= 0)
                return AppResources.ActiveWorkout_Toast_EnterDuration;
        }
        else if (!int.TryParse(set.RepsText, out reps) || reps <= 0)
        {
            return AppResources.ActiveWorkout_Toast_EnterReps;
        }
        return null;
    }

    private async Task<bool> EvaluatePRAsync(LoggedSetRow set, decimal weight, int reps) =>
        set.SetType == SetType.Normal && weight > 0 && reps > 0
        && await pr.IsPRAsync(set.ExerciseId, weight, reps, set.LoggedSetId, _session?.Id ?? 0);

    private LoggedSet ToLoggedSet(LoggedSetRow set, decimal weight, int reps, int durationSeconds, bool isPR) => new()
    {
        Id = set.LoggedSetId,
        SessionExerciseId = set.SessionExerciseId,
        SetNumber = set.SetNumber,
        WeightKg = weight,
        Reps = reps,
        // Ingen RIR angiven → null, så att coachen inte tolkar det som RIR 0
        RIR = set.Rir >= 0 ? set.Rir : null,
        LoggedAt = set.LoggedAt,
        IsPR = isPR,
        SetType = set.SetType,
        DurationSeconds = durationSeconds
    };

    // Samtidiga körningar tillåts så att ✓ på nästa rad inte ignoreras medan föregående sparas;
    // IsSaving per rad + _setLock förhindrar dubbletter och håller ordningen.
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task CompleteSetAsync(LoggedSetRow set)
    {
        if (set.IsSaving) return;   // dubbeltryck på ✓
        set.IsSaving = true;
        await _setLock.WaitAsync();
        try
        {
            if (set.IsCompleted)
            {
                // ✓ på ett klart set: ändrade värden sparas (även om fältet nyss lämnades
                // och sparade sig självt), annars växlar setet tillbaka till ej klart.
                var justEdited = DateTime.Now - set.LastEditSavedAt < TimeSpan.FromSeconds(1.5);
                if (set.HasUnsavedEdits)
                    await SaveEditedSetCoreAsync(set);
                else if (!justEdited)
                    await UncompleteSetCoreAsync(set);
                return;
            }

            await LogSetCoreAsync(set);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "CompleteSet"); }
        finally
        {
            _setLock.Release();
            set.IsSaving = false;
        }
    }

    private async Task LogSetCoreAsync(LoggedSetRow set)
    {
        // Auto-fill från hint om fälten är tomma
        if (string.IsNullOrWhiteSpace(set.WeightText) && !string.IsNullOrWhiteSpace(set.PrevWeightHint))
            set.WeightText = set.PrevWeightHint;
        if (string.IsNullOrWhiteSpace(set.RepsText) && !string.IsNullOrWhiteSpace(set.PrevRepsHint))
            set.RepsText = set.PrevRepsHint;

        var error = ParseSet(set, out var weight, out var reps, out var durationSeconds);
        if (error is not null)
        {
            await Toast.Make(error).Show();
            return;
        }

        var isPR = await EvaluatePRAsync(set, weight, reps);

        set.LoggedAt = DateTime.Now;
        var loggedSet = ToLoggedSet(set, weight, reps, durationSeconds, isPR);
        await sessionRepo.SaveLoggedSetAsync(loggedSet);

        // Spara Id:t så att senare ändringar uppdaterar samma rad i stället för att infoga en dubblett
        set.LoggedSetId = loggedSet.Id;
        set.SavedWeightText = set.WeightText;
        set.SavedRepsText = set.RepsText;
        set.IsCompleted = true;
        set.IsPR = isPR;

        if (isPR)
            AnnouncePR(set, weight, reps);

        // Timer och superset-navigering
        if (set.SetType is SetType.Normal or SetType.Time)
            HandlePostSetTimer(set.SessionExerciseId);

        CheckAutoProgression(set.SessionExerciseId);

        // Auto-scroll till nästa övning när alla sets i sektionen är klara
        var completedSection = Exercises.FirstOrDefault(e => e.SessionExerciseId == set.SessionExerciseId);
        if (completedSection is not null && completedSection.Sets.All(s => s.IsCompleted))
        {
            var nextIdx = Exercises.IndexOf(completedSection) + 1;
            if (nextIdx < Exercises.Count)
                RequestScrollTo(Exercises[nextIdx].SessionExerciseId);

            // RP-clone Task 26: trigga MuscleFeedbackSheet när sista övningen i muskelgruppen är klar
            _ = MaybeShowMuscleFeedbackAsync(completedSection);
        }
    }

    private async Task UncompleteSetCoreAsync(LoggedSetRow set)
    {
        if (set.LoggedSetId > 0)
            await sessionRepo.DeleteLoggedSetAsync(set.LoggedSetId);
        set.LoggedSetId = 0;
        set.IsCompleted = false;
        set.IsPR = false;
        RefreshPRBanner();
    }

    /// <summary>
    /// Sparar ändrad vikt/reps på ett redan loggat set (anropas när fältet lämnas).
    /// Uppdaterar befintlig LoggedSet — infogar aldrig en ny.
    /// </summary>
    public async Task CommitSetEditsAsync(LoggedSetRow set)
    {
        if (!set.HasUnsavedEdits) return;
        await _setLock.WaitAsync();
        try
        {
            if (set.HasUnsavedEdits)
            {
                await SaveEditedSetCoreAsync(set);
                set.LastEditSavedAt = DateTime.Now;
            }
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "CommitSetEdits"); }
        finally { _setLock.Release(); }
    }

    /// <summary>RIR-växlaren: – → 0 → 1 → 2 → 3 → 4+ → –. Ett loggat set sparas direkt.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task CycleRirAsync(LoggedSetRow set)
    {
        set.CycleRir();   // haptik ges av tryck-animationen i vyn
        if (!set.IsCompleted || set.LoggedSetId == 0) return;
        await _setLock.WaitAsync();
        try
        {
            if (set.IsCompleted && set.LoggedSetId > 0)
                await SaveEditedSetCoreAsync(set);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "CycleRir"); }
        finally { _setLock.Release(); }
    }

    private async Task SaveEditedSetCoreAsync(LoggedSetRow set)
    {
        var error = ParseSet(set, out var weight, out var reps, out var durationSeconds);
        if (error is not null)
        {
            // Ogiltigt värde på ett loggat set — återställ till det som är sparat
            set.WeightText = set.SavedWeightText;
            set.RepsText = set.SavedRepsText;
            await Toast.Make(error).Show();
            return;
        }

        var wasPR = set.IsPR;
        var isPR = await EvaluatePRAsync(set, weight, reps);
        await sessionRepo.SaveLoggedSetAsync(ToLoggedSet(set, weight, reps, durationSeconds, isPR));

        set.SavedWeightText = set.WeightText;
        set.SavedRepsText = set.RepsText;
        set.IsPR = isPR;

        if (isPR && !wasPR)
            AnnouncePR(set, weight, reps);
        else if (!isPR && wasPR)
            RefreshPRBanner();
    }

    private void AnnouncePR(LoggedSetRow set, decimal weight, int reps)
    {
        HasPR = true;
        var name = GetExerciseName(set.SessionExerciseId);
        var est = PRService.CalculateEpley1RM(weight, reps);
        PrMessage = string.Format(AppResources.ActiveWorkout_PR_Message_Format, name, Kg(weight), reps, est.ToString("F0"));
        PRScored?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>PR-bannern visas bara så länge något loggat set i passet faktiskt är PR.</summary>
    private void RefreshPRBanner()
    {
        if (Exercises.Any(e => e.Sets.Any(s => s.IsPR))) return;
        HasPR = false;
        PrMessage = "";
    }

    private async Task MaybeShowMuscleFeedbackAsync(WorkoutExerciseSection completedSection)
    {
        if (_session is null) return;
        if (_feedbackShownFor.Contains(completedSection.MuscleGroup)) return;
        if (completedSection.Sets.Count == 0) return;
        if (completedSection.Sets.Any(s => !s.IsCompleted)) return;

        // Finns senare övning med samma muskelgrupp?
        var idx = Exercises.IndexOf(completedSection);
        if (idx < 0) return;
        var laterExists = Exercises.Skip(idx + 1).Any(e => e.MuscleGroup == completedSection.MuscleGroup);
        if (laterExists) return;

        _feedbackShownFor.Add(completedSection.MuscleGroup);

        try
        {
            var services = Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext?.Services;
            if (services is null) return;
            var vm = services.GetRequiredService<MuscleFeedbackSheetViewModel>();
            vm.Reset();
            vm.SessionId = _session.Id;
            vm.Muscle = completedSection.MuscleGroup;
            vm.MuscleName = completedSection.MuscleGroup.ToLocalizedLabel();
            vm.LastSessionExerciseId = completedSection.SessionExerciseId;
            vm.ShowJointPain = !_jointPainReportedFor.Contains(completedSection.SessionExerciseId);

            var sheet = new MuscleFeedbackSheet(vm);
            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            await page.ShowPopupAsync(sheet);
        }
        catch (Exception ex)
        {
            // Sheet-fel ska inte krascha pass
            System.Diagnostics.Debug.WriteLine($"[ActiveWorkout] MuscleFeedbackSheet: {ex}");
        }
    }

    private void HandlePostSetTimer(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section?.SupersetGroupId is not int groupId)
        {
            StartTimerForSection(sessionExerciseId);
            return;
        }

        bool allDone;
        WorkoutExerciseSection? next;
        lock (_supersetLock)
        {
            if (!_supersetRound.TryGetValue(groupId, out var done))
            {
                done = new HashSet<int>();
                _supersetRound[groupId] = done;
            }
            done.Add(sessionExerciseId);

            var groupSections = Exercises.Where(e => e.SupersetGroupId == groupId).ToList();
            allDone = done.Count >= groupSections.Count;
            if (allDone)
                _supersetRound.Remove(groupId);
            next = allDone ? null : groupSections.FirstOrDefault(e => !done.Contains(e.SessionExerciseId));
        }

        if (allDone)
            StartTimerForSection(sessionExerciseId);
        else if (next != null)
            RequestScrollTo(next.SessionExerciseId);
    }

    private void CheckAutoProgression(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section is null || section.AutoProgressMode == 0) return;

        var normalSets = section.Sets.Where(s => s.SetType == SetType.Normal).ToList();
        if (normalSets.Count == 0 || !normalSets.All(s => s.IsCompleted)) return;

        var currentTarget = section.TargetReps;
        if (currentTarget <= 0) return;

        bool allHitTarget = normalSets.All(s =>
            int.TryParse(s.RepsText, out var r) && r >= currentTarget);
        if (!allHitTarget) return;

        var maxWeight = normalSets
            .Select(s => TryParseDecimal(s.WeightText, out var w) ? w : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        if (maxWeight <= 0) return;

        bool hitsWeightThreshold = section.TargetRepsMax > 0 && currentTarget >= section.TargetRepsMax;
        if (hitsWeightThreshold)
        {
            var increment = section.WeightIncrementKg > 0 ? section.WeightIncrementKg : 2.5m;
            var resetReps = section.TargetRepsMin > 0 ? section.TargetRepsMin : currentTarget;
            AutoProgressMessage = string.Format(AppResources.ActiveWorkout_AutoProgress_WeightUp_Format, section.ExerciseName, Kg(maxWeight + increment), resetReps);
        }
        else
        {
            AutoProgressMessage = string.Format(AppResources.ActiveWorkout_AutoProgress_RepsUp_Format, currentTarget + 1, Kg(maxWeight));
        }
        HasAutoProgress = true;
    }

    private string GetExerciseName(int sessionExerciseId) =>
        Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId)?.ExerciseName ?? "";

    private void StartTimerForSection(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section is null) return;

        if (_activeTimerSection != null) _activeTimerSection.IsTimerActive = false;
        _activeTimerSection = section;

        timer.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;

        _currentTimerSection = section;
        timer.Tick += OnTimerTick;
        timer.Completed += OnTimerCompleted;

        timer.Start(section.RestSeconds);
        notifications.ScheduleTimer(section.RestSeconds, section.ExerciseName);
        section.IsTimerActive = true;
        section.TimerSecondsRemaining = section.RestSeconds;
        section.TimerProgress = 1.0;
    }

    private void OnTimerTick(int secs)
    {
        var section = _currentTimerSection;
        if (section is null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            section.TimerSecondsRemaining = secs;
            section.TimerProgress = section.RestSeconds > 0
                ? (double)secs / section.RestSeconds : 0;
            section.RefreshTimerDisplay();
        });
    }

    private void OnTimerCompleted()
    {
        notifications.CancelTimer();
        var section = _currentTimerSection;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AnimationHelper.Haptic(HapticFeedbackType.LongPress);
            if (Preferences.Default.Get("sound_enabled", true))
                sound.PlayTimerComplete();
            if (section != null) section.IsTimerActive = false;
            _activeTimerSection = null;
            _currentTimerSection = null;
        });
    }

    public void RefreshTimerState()
    {
        var section = _currentTimerSection;
        if (section is null || !timer.IsRunning) return;
        var secs = timer.SecondsRemaining;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            section.TimerSecondsRemaining = secs;
            section.TimerProgress = section.RestSeconds > 0
                ? (double)secs / section.RestSeconds : 0;
            section.RefreshTimerDisplay();
            if (secs <= 0)
            {
                section.IsTimerActive = false;
                _activeTimerSection = null;
                _currentTimerSection = null;
            }
        });
    }

    private void StopRestTimer()
    {
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();
        if (_activeTimerSection != null) _activeTimerSection.IsTimerActive = false;
        _activeTimerSection = null;
        _currentTimerSection = null;
    }

    private void ForceDeactivateCore()
    {
        _clockCts?.Cancel();
        StopRestTimer();
        state.Deactivate();
        Exercises.Clear();
        _supersetRound.Clear();
        _feedbackShownFor.Clear();
        _jointPainReportedFor.Clear();
        _pendingScrollId = null;
        TemplateName = AppResources.ActiveWorkout_FreeWorkout;
        ElapsedTime = "0:00";
        HasPR = false;
        PrMessage = "";
        HasAutoProgress = false;
        AutoProgressMessage = "";
    }

    public void ForceDeactivate()
    {
        // Ett pass utan loggade set ska inte hamna i historiken — ta bort det i stället för att stänga det
        if (_session is not null && !Exercises.Any(s => s.Sets.Any(r => r.IsCompleted)))
            _ = DeleteSessionSafeAsync(_session.Id);
        _session = null;
        notifications.CancelTimer();
        ForceDeactivateCore();
    }

    private async Task DeleteSessionSafeAsync(int sessionId)
    {
        try { await sessionRepo.DeleteSessionAsync(sessionId); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ActiveWorkout] Kunde inte ta bort tomt pass: {ex}"); }
    }

    // Dag i mesocykel-veckan = antal avslutade pass sedan veckans start + 1
    private async Task<int> ComputeDayNumberAsync()
    {
        if (_activeCycle is null) return 1;
        var meso = await mesocycles.GetActiveAsync();
        if (meso is null) return 1;
        var weekStart = meso.StartDate.Date.AddDays(7 * (_activeCycle.WeekNumber - 1));
        var doneThisWeek = await sessionRepo.GetCompletedInRangeAsync(weekStart, DateTime.Now);
        return doneThisWeek.Count + 1;
    }

    private string ComputeDayName() => DateTime.Now.DayOfWeek switch
    {
        DayOfWeek.Monday    => "Måndag",
        DayOfWeek.Tuesday   => "Tisdag",
        DayOfWeek.Wednesday => "Onsdag",
        DayOfWeek.Thursday  => "Torsdag",
        DayOfWeek.Friday    => "Fredag",
        DayOfWeek.Saturday  => "Lördag",
        DayOfWeek.Sunday    => "Söndag",
        _                   => DateTime.Now.DayOfWeek.ToString(),
    };

    [RelayCommand]
    private async Task OpenMesocycleStatsAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.MesocycleStats); }
        catch (Exception ex) { await ShowErrorAsync(ex, "OpenMesocycleStats"); }
    }

    [RelayCommand]
    private async Task ShowStartWeightGuideAsync()
    {
        try
        {
            await Shell.Current.DisplayAlert(
                AppResources.StartWeight_Guide_Title,
                AppResources.StartWeight_Guide_Body,
                AppResources.Common_OK);
            ShowStartWeightHint = false;
            var settings = await settingsRepo.GetAsync();
            settings.HasSeenStartWeightHint = true;
            await settingsRepo.SaveAsync(settings);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "ShowStartWeightGuide"); }
    }

    [RelayCommand]
    private async Task ShowExerciseMenuAsync(WorkoutExerciseSection section)
    {
        if (section is null) return;

        try
        {
            var options = new[]
            {
                AppResources.ExerciseMenu_AddNote,
                AppResources.ExerciseMenu_MoveUp,
                AppResources.ExerciseMenu_MoveDown,
                AppResources.ExerciseMenu_Replace,
                AppResources.ExerciseMenu_JointPain,
                AppResources.ExerciseMenu_AddSet,
                AppResources.ExerciseMenu_SkipSets,
                AppResources.ExerciseMenu_Remove,
            };

            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            var choice = await page.DisplayActionSheet(
                AppResources.ExerciseMenu_Title,
                AppResources.Common_Cancel,
                null,
                options);
            if (string.IsNullOrEmpty(choice) || choice == AppResources.Common_Cancel) return;

            if      (choice == AppResources.ExerciseMenu_AddNote)   await AddNoteAsync(section);
            else if (choice == AppResources.ExerciseMenu_MoveUp)    MoveSection(section, -1);
            else if (choice == AppResources.ExerciseMenu_MoveDown)  MoveSection(section, +1);
            else if (choice == AppResources.ExerciseMenu_Replace)   await ReplaceExerciseAsync(section);
            else if (choice == AppResources.ExerciseMenu_JointPain) await OpenJointPainSheetAsync(section);
            else if (choice == AppResources.ExerciseMenu_AddSet)    AddSet(section);
            else if (choice == AppResources.ExerciseMenu_SkipSets)  await SkipRemainingSetsAsync(section);
            else if (choice == AppResources.ExerciseMenu_Remove)    await RemoveExerciseAsync(section);
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "ShowExerciseMenu"); }
    }

    private async Task AddNoteAsync(WorkoutExerciseSection section)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null) return;
        var note = await page.DisplayPromptAsync(
            AppResources.ExerciseMenu_AddNote,
            string.Format(AppResources.ExerciseMenu_AddNote_Body_Format, section.ExerciseName),
            placeholder: AppResources.ExerciseMenu_AddNote_Placeholder,
            initialValue: section.Notes);
        if (note is null) return;
        section.Notes = note;
        await sessionRepo.SetSessionExerciseNotesAsync(section.SessionExerciseId, note);
    }

    private void MoveSection(WorkoutExerciseSection section, int delta)
    {
        var idx = Exercises.IndexOf(section);
        var newIdx = idx + delta;
        if (idx < 0 || newIdx < 0 || newIdx >= Exercises.Count) return;
        Exercises.Move(idx, newIdx);
    }

    private async Task ReplaceExerciseAsync(WorkoutExerciseSection section)
    {
        await Shell.Current.GoToAsync(AppRoutes.ExercisePickerForMuscle(section.MuscleGroup), new Dictionary<string, object>
        {
            { "CallbackAction", new Action<Exercise>(async ex =>
            {
                try
                {
                    await sessionRepo.UpdateSessionExerciseExerciseIdAsync(section.SessionExerciseId, ex.Id);
                    section.ExerciseId = ex.Id;
                    section.ExerciseName = ex.DisplayName;
                    section.MuscleGroup = ex.MuscleGroup;
                    // PR-kontrollen per set går på ExerciseId — följ med bytet
                    foreach (var row in section.Sets)
                        row.ExerciseId = ex.Id;
                    section.NotifyExerciseChanged();
                }
                catch (Exception e) { await ShowErrorAsync(e, "ReplaceExercise"); }
            })}
        });
    }

    private async Task OpenJointPainSheetAsync(WorkoutExerciseSection section)
    {
        try
        {
            var services = Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext?.Services;
            if (services is null) return;
            var vm = services.GetRequiredService<JointPainSheetViewModel>();
            var sheet = new JointPainSheet(vm, section.SessionExerciseId, section.ExerciseName);
            // Om användaren stänger sheet:en (spara eller hoppa över) — markera övningen som besökt
            // så att MuscleFeedbackSheet inte visar JointPain-fältet igen för samma exercise.
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                _jointPainReportedFor.Add(section.SessionExerciseId);
                vm.CloseRequested -= handler;
            };
            vm.CloseRequested += handler;

            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            await page.ShowPopupAsync(sheet);
        }
        catch (Exception ex)
        {
            // Sheet-fel ska inte krascha pass
            System.Diagnostics.Debug.WriteLine($"[ActiveWorkout] JointPainSheet: {ex}");
        }
    }

    private async Task SkipRemainingSetsAsync(WorkoutExerciseSection section)
    {
        // Enklaste implementation: ta bort osparade set-rader från UI:t
        var unloggedSets = section.Sets.Where(s => !s.IsCompleted).ToList();
        foreach (var s in unloggedSets)
            section.Sets.Remove(s);
        await Toast.Make(AppResources.ExerciseMenu_SkipSets_Toast).Show();
    }

    [RelayCommand]
    private async Task OpenPlateCalculatorAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.PlateCalculator); }
        catch (Exception ex) { await ShowErrorAsync(ex, "OpenPlateCalculator"); }
    }

    [RelayCommand]
    private async Task ShowExerciseInfoAsync(WorkoutExerciseSection section)
    {
        if (string.IsNullOrWhiteSpace(section.ExerciseDescription)) return;
        try { await Shell.Current.DisplayAlert(section.ExerciseName, section.ExerciseDescription, AppResources.Common_OK); }
        catch (Exception ex) { await ShowErrorAsync(ex, "ShowExerciseInfo"); }
    }

    private void StartClock()
    {
        _clockCts = new CancellationTokenSource();
        var token = _clockCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token).ContinueWith(_ => { });
                    if (token.IsCancellationRequested) break;
                    var elapsed = DateTime.Now - _startTime;
                    MainThread.BeginInvokeOnMainThread(() =>
                        ElapsedTime = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}");
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync()
    {
        if (_session is null) return;
        try
        {
            // Ett pass utan ett enda loggat set ska inte hamna i historiken
            if (!Exercises.Any(s => s.Sets.Any(r => r.IsCompleted)))
            {
                var discard = await Shell.Current.DisplayAlert(
                    AppResources.ActiveWorkout_FinishEmpty_Title,
                    AppResources.ActiveWorkout_FinishEmpty_Body,
                    AppResources.ActiveWorkout_FinishEmpty_Discard,
                    AppResources.Common_Continue);
                if (!discard) return;

                var emptyId = _session.Id;
                _session = null;
                notifications.CancelTimer();
                ForceDeactivateCore();
                await sessionRepo.DeleteSessionAsync(emptyId);
                await Shell.Current.GoToAsync(AppRoutes.Back);
                return;
            }

            var confirmed = await Shell.Current.DisplayAlert(
                AppResources.ActiveWorkout_FinishConfirm_Title,
                AppResources.ActiveWorkout_FinishConfirm_Body,
                AppResources.ActiveWorkout_FinishConfirm_Yes,
                AppResources.Common_Continue);
            if (!confirmed || _session is null) return;

            // Vänta in pågående set-sparningar (t.ex. ett fält som just lämnades)
            await _setLock.WaitAsync();
            _setLock.Release();

            // Passet räknas som avslutat direkt, så att meriter och historik ser det på
            // efter-passet-sidan. Ångra där nollställer CompletedAt; KLAR sparar anteckningarna.
            _session.CompletedAt = DateTime.Now;
            await sessionRepo.SaveSessionAsync(_session);

            _clockCts?.Cancel();
            StopRestTimer();
            notifications.CancelTimer();

            var sessionId = _session.Id;
            state.Deactivate();   // döljer bannern men lämnar _session och Exercises intakta

            await Shell.Current.GoToAsync(AppRoutes.PostWorkout, new Dictionary<string, object>
            {
                { "SessionId", sessionId }
            });
        }
        catch (Exception ex) { await ShowErrorAsync(ex, "FinishWorkout"); }
    }

    /// <summary>
    /// Sparar anteckningar och släpper passet. False om passet inte längre är det pågående
    /// (redan sparat, eller ett nytt pass har startats) — anroparen sparar då anteckningarna själv.
    /// </summary>
    public async Task<bool> CommitFinishAsync(int sessionId, string notes)
    {
        // Rör aldrig ett annat (nyare) pass än det efter-passet-sidan visar
        if (_session is null || _session.Id != sessionId) return false;
        WorkoutSession committed = _session;
        try
        {
            committed.CompletedAt ??= DateTime.Now;
            committed.Notes = notes;
            await sessionRepo.SaveSessionAsync(committed);
            await ApplyProgressionAsync();
        }
        finally
        {
            _session = null;
            notifications.CancelTimer();
            ForceDeactivateCore();
        }
        return true;
    }

    /// <summary>"Ångra" på efter-passet-sidan: passet blir pågående igen. False om det redan är sparat.</summary>
    public async Task<bool> ResumeFromPostWorkoutAsync(int sessionId)
    {
        if (_session is null || _session.Id != sessionId) return false;
        _session.CompletedAt = null;
        await sessionRepo.SaveSessionAsync(_session);
        state.Activate();
        // Återstarta klockan från befintlig _startTime
        StartClock();
        return true;
    }

    private Task ApplyProgressionAsync()
    {
        // TODO Task 18/19: implementera progression via ITemplateService.GetDetailAsync
        // Nuvarande TemplateExercise-schema saknar Reps/TargetWeight/AutoProgressMode
        return Task.CompletedTask;
    }
}
