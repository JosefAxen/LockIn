using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
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
        // Navigating back to an ongoing workout (e.g. via "PASS PÅGÅR" banner) — no TemplateId in query
        if (state.IsActive && !query.ContainsKey("TemplateId"))
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
                    _activeCycle.WeekNumber, ComputeDayNumber());
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
        catch
        {
            // Om sessionen hann sparas i DB men laddningen misslyckades — stäng den direkt
            // så att den inte hamnar som en "orphan" session utan sets och utan CompletedAt.
            if (_session is not null)
            {
                _session.CompletedAt = DateTime.Now;
                _ = sessionRepo.SaveSessionAsync(_session);
            }
            _session = null;
            Exercises.Clear();
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
        int autoProgressMode = 0, int? supersetGroupId = null, int templateExerciseId = 0)
    {
        var se = new SessionExercise
        {
            SessionId = _session!.Id,
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
            ExerciseName = exercise.Name,
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

        var prevSets = await sessionRepo.GetLastSessionSetsAsync(exercise.Id, _session!.Id);

        string prevSummary = "";
        if (prevSets.Count > 0)
        {
            var parts = prevSets
                .Where(s => s.SetType != SetType.Warmup)
                .Select(s => s.SetType == SetType.Time && s.DurationSeconds > 0
                    ? $"{s.DurationSeconds}s"
                    : s.WeightKg > 0
                        ? $"{s.WeightKg:G}×{s.Reps}"
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
            rpSuggestion = await weightProgression.SuggestNextWeightAsync(exercise.Id, _session!.Id, reps);
            if (rpSuggestion is not null)
                section.WeightSuggestionReason = rpSuggestion.Reason;
        }

        for (int s = 1; s <= sets; s++)
        {
            var prev = prevSets.ElementAtOrDefault(s - 1);

            // Progression äger hints när aktiv, annars RP-suggestion från förra passet, sist senaste sessions vikt
            var weightHint = autoProgressMode > 0 && targetWeight > 0
                ? targetWeight.ToString("G")
                : rpSuggestion is not null
                    ? rpSuggestion.WeightKg.ToString("G", CultureInfo.InvariantCulture)
                    : (prev is { WeightKg: > 0 } ? prev.WeightKg.ToString("G") : "");
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
        ScrollToSectionRequested?.Invoke(this, section.SessionExerciseId);
        return section;
    }

    public Task AddExerciseFromPickerAsync(Exercise exercise)
        => AddExerciseSectionAsync(exercise, Exercises.Count);

    [RelayCommand]
    private async Task DismissRirHintAsync()
    {
        ShowRirHint = false;
        var settings = await settingsRepo.GetAsync();
        settings.HasSeenRirHint = true;
        await settingsRepo.SaveAsync(settings);
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        await Shell.Current.GoToAsync(nameof(ExercisePickerPage), new Dictionary<string, object>
        {
            { "CallbackAction", new Action<Exercise>(async ex =>
            {
                await AddExerciseSectionAsync(ex, Exercises.Count);
            })}
        });
    }

    [RelayCommand]
    private async Task RemoveExerciseAsync(WorkoutExerciseSection section)
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
    }

    [RelayCommand]
    private async Task ChangeRestTimeAsync(WorkoutExerciseSection section)
    {
        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.ActiveWorkout_RestTime_Title,
            string.Format(AppResources.ActiveWorkout_RestTime_Body_Format, section.ExerciseName),
            initialValue: section.RestSeconds.ToString(),
            keyboard: Keyboard.Numeric);

        if (int.TryParse(result, out var secs) && secs > 0)
            section.RestSeconds = secs;
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
            IsFreshlyAdded = true
        });
    }

    [RelayCommand]
    private void RemoveSet(WorkoutExerciseSection section)
    {
        if (section.Sets.Count > 1)
            section.Sets.RemoveAt(section.Sets.Count - 1);
    }

    [RelayCommand]
    private void DeleteSet(LoggedSetRow set)
    {
        var section = Exercises.FirstOrDefault(e => e.Sets.Contains(set));
        if (section is null || section.Sets.Count <= 1) return;
        section.Sets.Remove(set);
        for (int i = 0; i < section.Sets.Count; i++)
            section.Sets[i].SetNumber = i + 1;
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
        if (!decimal.TryParse(baseStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var baseW) || baseW <= 0) return;
        var suggested = Math.Round(baseW * 0.8m / 2.5m) * 2.5m;
        set.WeightText = suggested.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task CompleteSetAsync(LoggedSetRow set)
    {
        // Auto-fill från hint om fälten är tomma
        if (string.IsNullOrWhiteSpace(set.WeightText) && !string.IsNullOrWhiteSpace(set.PrevWeightHint))
            set.WeightText = set.PrevWeightHint;
        if (string.IsNullOrWhiteSpace(set.RepsText) && !string.IsNullOrWhiteSpace(set.PrevRepsHint))
            set.RepsText = set.PrevRepsHint;

        decimal weight = 0;
        int reps = 0;
        int durationSeconds = 0;

        if (set.SetType == SetType.Time)
        {
            decimal.TryParse(set.WeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out weight);

            if (!int.TryParse(set.RepsText, out durationSeconds) || durationSeconds <= 0)
            {
                await Toast.Make(AppResources.ActiveWorkout_Toast_EnterDuration).Show();
                return;
            }
        }
        else
        {
            decimal.TryParse(set.WeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out weight);
            if (!int.TryParse(set.RepsText, out reps) || reps <= 0)
            {
                await Toast.Make(AppResources.ActiveWorkout_Toast_EnterReps).Show();
                return;
            }
        }

        var rir = set.Rir >= 0 ? set.Rir : 0;
        var isPR = set.SetType == SetType.Normal && weight > 0 && reps > 0
            ? await pr.IsPRAsync(set.ExerciseId, weight, reps)
            : false;

        var loggedSet = new LoggedSet
        {
            SessionExerciseId = set.SessionExerciseId,
            SetNumber = set.SetNumber,
            WeightKg = weight,
            Reps = reps,
            RIR = rir,
            LoggedAt = DateTime.Now,
            IsPR = isPR,
            SetType = set.SetType,
            DurationSeconds = durationSeconds
        };
        await sessionRepo.SaveLoggedSetAsync(loggedSet);

        set.IsCompleted = true;
        set.IsPR = isPR;

        if (isPR)
        {
            HasPR = true;
            var name = GetExerciseName(set.SessionExerciseId);
            var est = PRService.CalculateEpley1RM(weight, reps);
            PrMessage = string.Format(AppResources.ActiveWorkout_PR_Message_Format, name, weight, reps, est.ToString("F0"));
            PRScored?.Invoke(this, EventArgs.Empty);
        }

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
                ScrollToSectionRequested?.Invoke(this, Exercises[nextIdx].SessionExerciseId);

            // RP-clone Task 26: trigga MuscleFeedbackSheet när sista övningen i muskelgruppen är klar
            _ = MaybeShowMuscleFeedbackAsync(completedSection);
        }
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
            vm.MuscleName = MuscleGroupDisplayName(completedSection.MuscleGroup);
            vm.LastSessionExerciseId = completedSection.SessionExerciseId;
            vm.ShowJointPain = !_jointPainReportedFor.Contains(completedSection.SessionExerciseId);

            var sheet = new MuscleFeedbackSheet(vm);
            var page = Shell.Current?.CurrentPage;
            if (page is null) return;
            await page.ShowPopupAsync(sheet);
        }
        catch
        {
            // Sheet-fel ska inte krascha pass — silently ignorera
        }
    }

    private static string MuscleGroupDisplayName(MuscleGroup g) => g switch
    {
        MuscleGroup.Chest     => AppResources.Muscle_Chest,
        MuscleGroup.Back      => AppResources.Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Library_Muscle_Core,
        MuscleGroup.FullBody  => AppResources.Library_Muscle_FullBody,
        MuscleGroup.Forearms  => AppResources.Library_Muscle_Forearms,
        _                     => AppResources.Library_Muscle_Other,
    };

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
            ScrollToSectionRequested?.Invoke(this, next.SessionExerciseId);
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
            .Select(s => decimal.TryParse(s.WeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var w) ? w : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        if (maxWeight <= 0) return;

        bool hitsWeightThreshold = section.TargetRepsMax > 0 && currentTarget >= section.TargetRepsMax;
        if (hitsWeightThreshold)
        {
            var increment = section.WeightIncrementKg > 0 ? section.WeightIncrementKg : 2.5m;
            var resetReps = section.TargetRepsMin > 0 ? section.TargetRepsMin : currentTarget;
            AutoProgressMessage = string.Format(AppResources.ActiveWorkout_AutoProgress_WeightUp_Format, section.ExerciseName, (maxWeight + increment).ToString("G"), resetReps);
        }
        else
        {
            AutoProgressMessage = string.Format(AppResources.ActiveWorkout_AutoProgress_RepsUp_Format, currentTarget + 1, maxWeight.ToString("G"));
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
            if (Preferences.Default.Get("haptic_enabled", true))
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
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

    private void ForceDeactivateCore()
    {
        _clockCts?.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();
        state.Deactivate();
        Exercises.Clear();
        _supersetRound.Clear();
        _feedbackShownFor.Clear();
        _jointPainReportedFor.Clear();
        TemplateName = AppResources.ActiveWorkout_FreeWorkout;
        ElapsedTime = "0:00";
        HasPR = false;
        PrMessage = "";
        HasAutoProgress = false;
        AutoProgressMessage = "";
        _activeTimerSection = null;
        _currentTimerSection = null;
    }

    public void ForceDeactivate()
    {
        // Stäng eventuell orphan-session (inga sets loggades)
        if (_session is not null && !Exercises.Any(s => s.Sets.Any(r => r.IsCompleted)))
        {
            var s = _session;
            s.CompletedAt = DateTime.Now;
            _ = sessionRepo.SaveSessionAsync(s);
        }
        _session = null;
        notifications.CancelTimer();
        ForceDeactivateCore();
    }

    // TODO Task 30: räkna antal loggade sessioner denna vecka (måndag-söndag) + 1
    private int ComputeDayNumber() => 1;
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
    private async Task OpenMesocycleStatsAsync() =>
        await Shell.Current.GoToAsync("mesocyclestats");

    [RelayCommand]
    private async Task ShowStartWeightGuideAsync()
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

    [RelayCommand]
    private async Task ShowExerciseMenuAsync(WorkoutExerciseSection section)
    {
        if (section is null) return;

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
        var muscleQuery = section.MuscleGroup.ToString();
        await Shell.Current.GoToAsync($"{nameof(ExercisePickerPage)}?muscle={muscleQuery}", new Dictionary<string, object>
        {
            { "CallbackAction", new Action<Exercise>(async ex =>
            {
                await sessionRepo.UpdateSessionExerciseExerciseIdAsync(section.SessionExerciseId, ex.Id);
                section.ExerciseId = ex.Id;
                section.ExerciseName = ex.Name;
                section.MuscleGroup = ex.MuscleGroup;
                section.NotifyExerciseChanged();
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
        catch
        {
            // Sheet-fel ska inte krascha pass
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
    private async Task OpenPlateCalculatorAsync() =>
        await Shell.Current.GoToAsync(nameof(PlateCalculatorPage));

    [RelayCommand]
    private async Task ShowExerciseInfoAsync(WorkoutExerciseSection section)
    {
        if (string.IsNullOrWhiteSpace(section.ExerciseDescription)) return;
        await Shell.Current.DisplayAlert(section.ExerciseName, section.ExerciseDescription, "OK");
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
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.ActiveWorkout_FinishConfirm_Title,
            AppResources.ActiveWorkout_FinishConfirm_Body,
            AppResources.ActiveWorkout_FinishConfirm_Yes,
            AppResources.Common_Continue);
        if (!confirmed) return;

        _clockCts?.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();

        if (_session is null) return;

        // CompletedAt sätts INTE här — defer till CommitFinishAsync
        var sessionId = _session.Id;
        state.Deactivate();   // döljer bannern men lämnar _session och Exercises intakta

        await Shell.Current.GoToAsync(nameof(PostWorkoutPage), new Dictionary<string, object>
        {
            { "SessionId", sessionId }
        });
    }

    public async Task CommitFinishAsync(string notes)
    {
        if (_session is null) return;
        WorkoutSession committed = _session;
        try
        {
            committed.CompletedAt = DateTime.Now;
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
    }

    public void ResumeFromPostWorkout()
    {
        if (_session is null) return;
        state.Activate();
        // Återstarta klockan från befintlig _startTime
        StartClock();
    }

    private Task ApplyProgressionAsync()
    {
        // TODO Task 18/19: implementera progression via ITemplateService.GetDetailAsync
        // Nuvarande TemplateExercise-schema saknar Reps/TargetWeight/AutoProgressMode
        return Task.CompletedTask;
    }
}
