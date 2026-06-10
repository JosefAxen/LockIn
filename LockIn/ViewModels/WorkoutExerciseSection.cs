using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class WorkoutExerciseSection : ObservableObject
{
    public int SessionExerciseId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public int DefaultRestSeconds { get; set; }
    public ObservableCollection<LoggedSetRow> Sets { get; } = new();

    [ObservableProperty] private bool _isTimerActive;
    [ObservableProperty] private int _timerSecondsRemaining;
    [ObservableProperty] private double _timerProgress;

    public string TimerDisplay => Services.RestTimerService.Format(TimerSecondsRemaining);

    public void RefreshTimerDisplay() => OnPropertyChanged(nameof(TimerDisplay));
}
