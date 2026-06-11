using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class WorkoutExerciseSection : ObservableObject
{
    public int SessionExerciseId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string ExerciseDescription { get; set; } = "";
    public bool HasDescription => !string.IsNullOrWhiteSpace(ExerciseDescription);
    public int DefaultRestSeconds { get; set; }
    public int TargetReps { get; set; } = 0;

    [ObservableProperty] private int _restSeconds;
    [ObservableProperty] private bool _isTimerActive;
    [ObservableProperty] private int _timerSecondsRemaining;
    [ObservableProperty] private double _timerProgress;

    public ObservableCollection<LoggedSetRow> Sets { get; } = new();

    public string TimerDisplay => Services.RestTimerService.Format(TimerSecondsRemaining);
    public string RestDisplay => Services.RestTimerService.Format(RestSeconds);

    public void RefreshTimerDisplay()
    {
        OnPropertyChanged(nameof(TimerDisplay));
        OnPropertyChanged(nameof(RestDisplay));
    }

    partial void OnRestSecondsChanged(int value) =>
        OnPropertyChanged(nameof(RestDisplay));
}
