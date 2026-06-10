using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class ActiveWorkoutViewModel(DatabaseService db, PRService pr, RestTimerService timer)
    : ObservableObject, IQueryAttributable
{
    private WorkoutSession? _session;
    private WorkoutExerciseSection? _activeTimerSection;
    private DateTime _startTime;
    private CancellationTokenSource? _clockCts;

    [ObservableProperty] private string _templateName = "FRITT PASS";
    [ObservableProperty] private string _elapsedTime = "0:00";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasPR;
    [ObservableProperty] private string _prMessage = "";

    public ObservableCollection<WorkoutExerciseSection> Exercises { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        int templateId = 0;
        if (query.TryGetValue("TemplateId", out var val) && val is int id)
            templateId = id;
        _ = LoadAsync(templateId);
    }

    private async Task LoadAsync(int templateId)
    {
        IsLoading = true;

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
                await AddExerciseSectionAsync(exercise!, i, te.Sets, te.Reps,
                    te.TargetWeight, te.DefaultRestSeconds);
            }
        }

        StartClock();
        IsLoading = false;
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
            DefaultRestSeconds = restSeconds,
            RestSeconds = restSeconds
        };

        for (int s = 1; s <= sets; s++)
        {
            section.Sets.Add(new LoggedSetRow
            {
                SessionExerciseId = se.Id,
                ExerciseId = exercise.Id,
                SetNumber = s,
                WeightText = targetWeight > 0 ? targetWeight.ToString() : "",
                RepsText = reps.ToString()
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
            RepsText = last?.RepsText ?? "8"
        });
    }

    [RelayCommand]
    private void RemoveSet(WorkoutExerciseSection section)
    {
        if (section.Sets.Count > 1)
            section.Sets.RemoveAt(section.Sets.Count - 1);
    }

    [RelayCommand]
    private async Task CompleteSetAsync(LoggedSetRow set)
    {
        if (!decimal.TryParse(set.WeightText.Replace(',', '.'), out var weight) ||
            !int.TryParse(set.RepsText, out var reps) || reps <= 0)
        {
            await Shell.Current.DisplayAlert("Fel", "Ange giltigt vikt och reps.", "OK");
            return;
        }

        var rir = set.Rir >= 0 ? set.Rir : 0;
        var isPR = await pr.IsPRAsync(set.ExerciseId, weight, reps);

        var loggedSet = new LoggedSet
        {
            SessionExerciseId = set.SessionExerciseId,
            SetNumber = set.SetNumber,
            WeightKg = weight,
            Reps = reps,
            RIR = rir,
            LoggedAt = DateTime.Now,
            IsPR = isPR
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

        StartTimerForSection(set.SessionExerciseId);
    }

    private string GetExerciseName(int sessionExerciseId) =>
        Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId)?.ExerciseName ?? "";

    private void StartTimerForSection(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section is null) return;

        _activeTimerSection?.Let(s => s.IsTimerActive = false);
        _activeTimerSection = section;

        timer.Cancel();
        timer.Tick -= OnTimerTick;
        timer.Completed -= OnTimerCompleted;

        _currentTimerSection = section;
        timer.Tick += OnTimerTick;
        timer.Completed += OnTimerCompleted;

        timer.Start(section.RestSeconds);
        section.IsTimerActive = true;
        section.TimerSecondsRemaining = section.RestSeconds;
        section.TimerProgress = 1.0;
    }

    private WorkoutExerciseSection? _currentTimerSection;

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
        var section = _currentTimerSection;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (section != null) section.IsTimerActive = false;
            _activeTimerSection = null;
            _currentTimerSection = null;
        });
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

        _session!.CompletedAt = DateTime.Now;
        await db.SaveSessionAsync(_session);

        await Shell.Current.GoToAsync(nameof(PostWorkoutPage), new Dictionary<string, object>
        {
            { "SessionId", _session.Id }
        });
    }
}

file static class Extensions
{
    public static void Let<T>(this T obj, Action<T> action) => action(obj);
}
