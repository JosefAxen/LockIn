using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace LockIn.ViewModels;

public partial class ActiveWorkoutViewModel(DatabaseService db, PRService pr, RestTimerService timer, ISoundService sound, ActiveWorkoutStateService state, NotificationService notifications)
    : ObservableObject, IQueryAttributable
{
    private WorkoutSession? _session;
    private WorkoutExerciseSection? _activeTimerSection;
    private WorkoutExerciseSection? _currentTimerSection;
    private DateTime _startTime;
    private CancellationTokenSource? _clockCts;
    private readonly Dictionary<int, HashSet<int>> _supersetRound = new();
    private readonly object _supersetLock = new();

    [ObservableProperty] private string _templateName = AppResources.ActiveWorkout_FreeWorkout;
    [ObservableProperty] private string _elapsedTime = "0:00";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasPR;
    [ObservableProperty] private string _prMessage = "";
    [ObservableProperty] private bool _hasAutoProgress;
    [ObservableProperty] private string _autoProgressMessage = "";

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
        if (_session is not null && _session.TemplateId == templateId)
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

            if (templateId != 0)
            {
                var templates = await db.GetTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Id == templateId);
                TemplateName = template?.Name ?? AppResources.ActiveWorkout_FreeWorkout;
            }

            // Skapa sessionen EFTER att övningar laddats, för att undvika orphan sessions
            // vid avbrutet pass (t.ex. Back-gesture innan SessionExercises skapas)
            _session = new WorkoutSession { TemplateId = templateId, StartedAt = DateTime.Now };
            await db.SaveSessionAsync(_session);
            _startTime = _session.StartedAt;

            if (templateId != 0)
            {
                var templateExercises = await db.GetTemplateExercisesAsync(templateId);
                for (int i = 0; i < templateExercises.Count; i++)
                {
                    var te = templateExercises[i];
                    var exercise = await db.GetExerciseAsync(te.ExerciseId);
                    if (exercise is null) continue;
                    await AddExerciseSectionAsync(exercise, i, te.Sets, te.Reps,
                        te.TargetWeight, te.DefaultRestSeconds,
                        te.TargetRepsMin, te.TargetRepsMax, te.WeightIncrementKg,
                        te.AutoProgressMode, te.SupersetGroupId, te.Id);
                }
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
                _ = db.SaveSessionAsync(_session);
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
        await db.SaveSessionExerciseAsync(se);

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
        };

        var prevSets = await db.GetLastSessionSetsAsync(exercise.Id, _session!.Id);

        for (int s = 1; s <= sets; s++)
        {
            var prev = prevSets.ElementAtOrDefault(s - 1);

            // Progression äger hints när aktiv, annars senaste session
            var weightHint = autoProgressMode > 0 && targetWeight > 0
                ? targetWeight.ToString("G")
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
                TargetReps = reps
            });
        }

        Exercises.Add(section);
        ScrollToSectionRequested?.Invoke(this, section.SessionExerciseId);
        return section;
    }

    public Task AddExerciseFromPickerAsync(Exercise exercise)
        => AddExerciseSectionAsync(exercise, Exercises.Count);

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
        await db.DeleteSessionExerciseWithSetsAsync(section.SessionExerciseId);
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
        await db.SaveLoggedSetAsync(loggedSet);

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

    public void ForceDeactivate()
    {
        // Stäng eventuell orphan-session (inga sets loggades)
        if (_session is not null && !Exercises.Any(s => s.Sets.Any(r => r.IsCompleted)))
        {
            var s = _session;
            s.CompletedAt = DateTime.Now;
            _ = db.SaveSessionAsync(s);
        }
        _session = null;
        _clockCts?.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();
        notifications.CancelTimer();
        state.Deactivate();
        Exercises.Clear();
        _supersetRound.Clear();
        TemplateName = AppResources.ActiveWorkout_FreeWorkout;
        ElapsedTime = "0:00";
        HasPR = false;
        PrMessage = "";
        HasAutoProgress = false;
        AutoProgressMessage = "";
        _activeTimerSection = null;
        _currentTimerSection = null;
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
        _session.CompletedAt = DateTime.Now;
        await db.SaveSessionAsync(_session);

        await ApplyProgressionAsync();

        var sessionId = _session.Id;
        _session = null;
        state.Deactivate();
        notifications.CancelTimer();

        await Shell.Current.GoToAsync(nameof(PostWorkoutPage), new Dictionary<string, object>
        {
            { "SessionId", sessionId }
        });
    }

    private async Task ApplyProgressionAsync()
    {
        if (_session?.TemplateId is not int templateId || templateId == 0) return;

        var templateExercises = await db.GetTemplateExercisesAsync(templateId);

        foreach (var section in Exercises)
        {
            if (section.AutoProgressMode == 0 || section.TemplateExerciseId == 0) continue;

            var te = templateExercises.FirstOrDefault(t => t.Id == section.TemplateExerciseId);
            if (te is null) continue;

            var normalSets = section.Sets
                .Where(s => s.SetType == SetType.Normal && s.IsCompleted)
                .ToList();
            if (normalSets.Count == 0) continue;

            var currentTarget = section.TargetReps;
            if (currentTarget <= 0) continue;

            bool allHitTarget = normalSets.All(s =>
                int.TryParse(s.RepsText, out var r) && r >= currentTarget);
            if (!allHitTarget) continue;

            bool hitsWeightThreshold = section.TargetRepsMax > 0 && currentTarget >= section.TargetRepsMax;
            if (hitsWeightThreshold)
            {
                var maxWeight = normalSets
                    .Select(s => decimal.TryParse(s.WeightText.Replace(',', '.'),
                        NumberStyles.Number, CultureInfo.InvariantCulture, out var w) ? w : 0m)
                    .DefaultIfEmpty(0m)
                    .Max();
                if (maxWeight > 0)
                {
                    var increment = section.WeightIncrementKg > 0 ? section.WeightIncrementKg : 2.5m;
                    te.TargetWeight = maxWeight + increment;
                    te.Reps = section.TargetRepsMin > 0 ? section.TargetRepsMin : currentTarget;
                }
            }
            else
            {
                te.Reps = currentTarget + 1;
            }

            await db.SaveTemplateExerciseAsync(te);
        }
    }
}
