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

    public string ExerciseMuscleUpper => MuscleGroupLabel(MuscleGroup).ToUpperInvariant();

    private static string MuscleGroupLabel(MuscleGroup g) => g switch
    {
        MuscleGroup.Chest     => "Bröst",
        MuscleGroup.Back      => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps    => "Biceps",
        MuscleGroup.Triceps   => "Triceps",
        MuscleGroup.Legs      => "Ben",
        MuscleGroup.Core      => "Mage",
        MuscleGroup.FullBody  => "Helkropp",
        MuscleGroup.Forearms  => "Underarmar",
        _                     => "Övrigt",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriorityTagColor))]
    private string _priorityTag = "II";

    public Color PriorityTagColor => PriorityTag switch
    {
        "III" => Color.FromArgb("#FF5A1F"),
        "II"  => Color.FromArgb("#71717A"),
        "I"   => Color.FromArgb("#52525B"),
        _     => Color.FromArgb("#52525B"),
    };

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
