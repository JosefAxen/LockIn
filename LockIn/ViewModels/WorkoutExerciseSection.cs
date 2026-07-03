using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class WorkoutExerciseSection : ObservableObject
{
    public int SessionExerciseId { get; set; }
    public int ExerciseId { get; set; }
    public int TemplateExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string ExerciseDescription { get; set; } = "";
    public bool HasDescription => !string.IsNullOrWhiteSpace(ExerciseDescription);
    public string PrevSessionSummary { get; set; } = "";
    public bool HasPrevSession => !string.IsNullOrEmpty(PrevSessionSummary);
    public string WeightSuggestionReason { get; set; } = "";
    public bool HasWeightSuggestionReason => !string.IsNullOrEmpty(WeightSuggestionReason);
    public int TargetRir { get; set; } = -1;
    public bool HasTargetRir => TargetRir >= 0;
    public string TargetRirDisplay => TargetRir >= 0
        ? string.Format(Resources.Strings.AppResources.LoggedSet_TargetRir_Format, TargetRir)
        : "";
    public int DefaultRestSeconds { get; set; }
    public int TargetReps { get; set; } = 0;
    public int TargetRepsMin { get; set; } = 0;
    public int TargetRepsMax { get; set; } = 0;
    public decimal WeightIncrementKg { get; set; } = 2.5m;
    public int AutoProgressMode { get; set; } = 0;
    public int? SupersetGroupId { get; set; }
    public bool IsInSuperset => SupersetGroupId.HasValue;
    public MuscleGroup MuscleGroup { get; set; }

    public Color AccentColor => DesignTokens.MuscleColor(MuscleGroup);
    public SolidColorBrush AccentBrush => new(AccentColor);
    public string ExerciseMuscleUpper => MuscleGroup.ToLocalizedLabel().ToUpperInvariant();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriorityTag))]
    [NotifyPropertyChangedFor(nameof(PriorityTagColor))]
    private MusclePriorityLevel _priority = MusclePriorityLevel.Grow;

    public string PriorityTag => Priority.ToRomanNumeral();
    public Color PriorityTagColor => Priority.ToTagColor();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
    private string _notes = "";
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    [ObservableProperty] private bool _showRecommendedRirHint;
    [ObservableProperty] private string _recommendedRirHintText = "";

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

    /// <summary>Trigger notifications för read-only-properties (t.ex. efter Replace).</summary>
    public void NotifyExerciseChanged()
    {
        OnPropertyChanged(nameof(ExerciseName));
        OnPropertyChanged(nameof(ExerciseMuscleUpper));
        OnPropertyChanged(nameof(AccentColor));
        OnPropertyChanged(nameof(AccentBrush));
    }

    partial void OnRestSecondsChanged(int value) =>
        OnPropertyChanged(nameof(RestDisplay));
}
