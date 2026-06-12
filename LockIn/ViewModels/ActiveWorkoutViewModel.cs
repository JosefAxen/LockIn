using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
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

    [ObservableProperty] private string _templateName = "FRITT PASS";
    [ObservableProperty] private string _elapsedTime = "0:00";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasPR;
    [ObservableProperty] private string _prMessage = "";
    [ObservableProperty] private bool _hasAutoProgress;
    [ObservableProperty] private string _autoProgressMessage = "";

    public ObservableCollection<WorkoutExerciseSection> Exercises { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int templateId = 0;
        if (query.TryGetValue("TemplateId", out var val) && val is int id)
            templateId = id;
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

            if (templateId != 0)
            {
                var templates = await db.GetTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Id == templateId);
                TemplateName = template?.Name ?? "PASS";
            }

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
                        te.TargetWeight, te.DefaultRestSeconds);
                }
            }

            StartClock();
            state.Activate();
        }
        catch
        {
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
        decimal targetWeight = 0, int restSeconds = 90)
    {
        var se = new SessionExercise
        {
            SessionId = _session!.Id,
            ExerciseId = exercise.Id,
            OrderIndex = orderIndex
        };
        await db.SaveSessionExerciseAsync(se);

        var section = new WorkoutExerciseSection
        {
            SessionExerciseId = se.Id,
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            ExerciseDescription = exercise.Description ?? "",
            DefaultRestSeconds = restSeconds,
            RestSeconds = restSeconds,
            TargetReps = reps
        };

        var prevSets = await db.GetLastSessionSetsAsync(exercise.Id, _session!.Id);

        for (int s = 1; s <= sets; s++)
        {
            var prev = prevSets.ElementAtOrDefault(s - 1);
            section.Sets.Add(new LoggedSetRow
            {
                SessionExerciseId = se.Id,
                ExerciseId = exercise.Id,
                SetNumber = s,
                WeightText = targetWeight > 0 ? targetWeight.ToString() :
                             (prev is { WeightKg: > 0 } ? prev.WeightKg.ToString("G") : ""),
                RepsText = reps.ToString(),
                PrevWeightHint = prev is { WeightKg: > 0 } ? prev.WeightKg.ToString("G") : "",
                PrevRepsHint = prev is { Reps: > 0 } ? prev.Reps.ToString() : "",
                TargetReps = reps
            });
        }

        Exercises.Add(section);
        return section;
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
    private async Task ChangeRestTimeAsync(WorkoutExerciseSection section)
    {
        var result = await Shell.Current.DisplayPromptAsync(
            "Vilotid",
            $"Ange vilotid i sekunder för {section.ExerciseName}:",
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
            TargetReps = section.TargetReps
        });
    }

    [RelayCommand]
    private void RemoveSet(WorkoutExerciseSection section)
    {
        if (section.Sets.Count > 1)
            section.Sets.RemoveAt(section.Sets.Count - 1);
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
        decimal weight = 0;
        int reps = 0;
        int durationSeconds = 0;

        if (set.SetType == SetType.Time)
        {
            decimal.TryParse(set.WeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out weight);

            if (!int.TryParse(set.RepsText, out durationSeconds) || durationSeconds <= 0)
            {
                await Shell.Current.DisplayAlert("Fel", "Ange duration i sekunder.", "OK");
                return;
            }
        }
        else
        {
            if (!decimal.TryParse(set.WeightText.Replace(',', '.'),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out weight) ||
                !int.TryParse(set.RepsText, out reps) || reps <= 0)
            {
                await Shell.Current.DisplayAlert("Fel", "Ange giltigt vikt och reps.", "OK");
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
            PrMessage = $"Nytt PR — {name}\n{weight} kg × {reps} reps · Est. 1RM {est:F0} kg";
        }

        // Warmup and Dropset skip rest timer
        if (set.SetType is SetType.Normal or SetType.Time)
            StartTimerForSection(set.SessionExerciseId);

        CheckAutoProgression(set.SessionExerciseId);
    }

    private void CheckAutoProgression(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section is null) return;

        var normalSets = section.Sets.Where(s => s.SetType == SetType.Normal).ToList();
        if (normalSets.Count == 0 || !normalSets.All(s => s.IsCompleted)) return;

        if (section.TargetReps <= 0) return;
        bool allHitTarget = normalSets.All(s =>
            int.TryParse(s.RepsText, out var r) && r >= section.TargetReps);
        if (!allHitTarget) return;

        var maxWeight = normalSets
            .Select(s => decimal.TryParse(s.WeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var w) ? w : 0m)
            .DefaultIfEmpty(0m)
            .Max();

        if (maxWeight <= 0) return;

        AutoProgressMessage = $"Öka till {maxWeight + 2.5m:G} kg nästa {section.ExerciseName}-pass";
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

    public void ForceDeactivate()
    {
        _session = null;
        _clockCts?.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();
        notifications.CancelTimer();
        state.Deactivate();
        Exercises.Clear();
        TemplateName = "FRITT PASS";
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
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1000, token).ContinueWith(_ => { });
                if (token.IsCancellationRequested) break;
                var elapsed = DateTime.Now - _startTime;
                MainThread.BeginInvokeOnMainThread(() =>
                    ElapsedTime = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}");
            }
        }, token);
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Avsluta pass", "Är du klar med passet?", "Ja, avsluta", "Fortsätt");
        if (!confirmed) return;

        _clockCts?.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;
        timer.Cancel();

        if (_session is null) return;
        _session.CompletedAt = DateTime.Now;
        await db.SaveSessionAsync(_session);

        var sessionId = _session.Id;
        _session = null;
        state.Deactivate();
        notifications.CancelTimer();

        await Shell.Current.GoToAsync(nameof(PostWorkoutPage), new Dictionary<string, object>
        {
            { "SessionId", sessionId }
        });
    }
}
