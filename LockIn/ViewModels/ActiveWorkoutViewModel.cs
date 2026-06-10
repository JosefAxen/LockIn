using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(TemplateId), "TemplateId")]
public partial class ActiveWorkoutViewModel(DatabaseService db, PRService pr, RestTimerService timer) : ObservableObject
{
    private WorkoutSession? _session;
    private WorkoutExerciseSection? _activeTimerSection;
    private DateTime _startTime;

    [ObservableProperty] private int _templateId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _elapsedTime = "0:00";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasPR;
    [ObservableProperty] private string _prMessage = "";

    public ObservableCollection<WorkoutExerciseSection> Exercises { get; } = new();

    private CancellationTokenSource? _clockCts;

    partial void OnTemplateIdChanged(int value) => _ = LoadAsync(value);

    private async Task LoadAsync(int templateId)
    {
        IsLoading = true;

        var templates = await db.GetTemplatesAsync();
        var template = templates.FirstOrDefault(t => t.Id == templateId);
        TemplateName = template?.Name ?? "";

        _session = new WorkoutSession
        {
            TemplateId = templateId,
            StartedAt = DateTime.Now
        };
        await db.SaveSessionAsync(_session);
        _startTime = _session.StartedAt;

        var templateExercises = await db.GetTemplateExercisesAsync(templateId);
        for (int i = 0; i < templateExercises.Count; i++)
        {
            var te = templateExercises[i];
            var exercise = await db.GetExerciseAsync(te.ExerciseId);

            var se = new SessionExercise
            {
                SessionId = _session.Id,
                ExerciseId = te.ExerciseId,
                OrderIndex = i
            };
            await db.SaveSessionExerciseAsync(se);

            var section = new WorkoutExerciseSection
            {
                SessionExerciseId = se.Id,
                ExerciseId = te.ExerciseId,
                ExerciseName = exercise?.Name ?? "",
                DefaultRestSeconds = te.DefaultRestSeconds
            };

            for (int s = 1; s <= te.Sets; s++)
            {
                section.Sets.Add(new LoggedSetRow
                {
                    SessionExerciseId = se.Id,
                    ExerciseId = te.ExerciseId,
                    SetNumber = s,
                    WeightText = te.TargetWeight > 0 ? te.TargetWeight.ToString() : "",
                    RepsText = te.Reps.ToString()
                });
            }

            Exercises.Add(section);
        }

        StartClock();
        IsLoading = false;
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
            PrMessage = $"Nytt PR — {GetExerciseName(set.SessionExerciseId)}";
            var estimate = PRService.CalculateEpley1RM(weight, reps);
            PrMessage += $"\n{weight} kg × {reps} reps · Est. 1RM {estimate:F0} kg";
        }

        StartTimerForSection(set.SessionExerciseId);
    }

    private string GetExerciseName(int sessionExerciseId) =>
        Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId)?.ExerciseName ?? "";

    private void StartTimerForSection(int sessionExerciseId)
    {
        var section = Exercises.FirstOrDefault(e => e.SessionExerciseId == sessionExerciseId);
        if (section is null) return;

        _activeTimerSection?.Let(s => { s.IsTimerActive = false; });
        _activeTimerSection = section;

        timer.Cancel();
        timer.Start(section.DefaultRestSeconds);
        section.IsTimerActive = true;
        section.TimerSecondsRemaining = section.DefaultRestSeconds;
        section.TimerProgress = 1.0;

        timer.Tick += secs =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                section.TimerSecondsRemaining = secs;
                section.TimerProgress = (double)secs / section.DefaultRestSeconds;
                section.RefreshTimerDisplay();
            });
        };

        timer.Completed += () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                section.IsTimerActive = false;
                _activeTimerSection = null;
            });
        };
    }

    [RelayCommand]
    private void SetRir(object param)
    {
        if (param is not string[] parts || parts.Length != 2) return;
        var set = Exercises.SelectMany(e => e.Sets)
            .FirstOrDefault(s => s.SetNumber.ToString() == parts[0] &&
                                 s.SessionExerciseId.ToString() == parts[1]);
        if (set is null) return;
        if (int.TryParse(parts[2] ?? "", out var rir))
            set.Rir = rir;
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Avsluta pass", "Är du klar med passet?", "Ja, avsluta", "Fortsätt");
        if (!confirmed) return;

        _clockCts?.Cancel();
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
