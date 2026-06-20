using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;
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
    public int TargetRepsMax { get; set; } = 0;
    public decimal WeightIncrementKg { get; set; } = 2.5m;
    public int AutoProgressMode { get; set; } = 0;
    public int? SupersetGroupId { get; set; }
    public bool IsInSuperset => SupersetGroupId.HasValue;
    public MuscleGroup MuscleGroup { get; set; }

    public Color AccentColor => MuscleGroup switch
    {
        MuscleGroup.Chest     => Color.FromArgb("#FB7185"),
        MuscleGroup.Back      => Color.FromArgb("#38BDF8"),
        MuscleGroup.Shoulders => Color.FromArgb("#A78BFA"),
        MuscleGroup.Biceps    => Color.FromArgb("#4ADE80"),
        MuscleGroup.Triceps   => Color.FromArgb("#FBBF24"),
        MuscleGroup.Legs      => Color.FromArgb("#F97316"),
        MuscleGroup.Core      => Color.FromArgb("#EC4899"),
        MuscleGroup.FullBody  => Color.FromArgb("#EF4444"),
        _                     => Color.FromArgb("#52525E"),
    };

    public SolidColorBrush AccentBrush => new(AccentColor);

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
