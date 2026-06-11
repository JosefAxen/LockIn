using CommunityToolkit.Mvvm.ComponentModel;

namespace LockIn.ViewModels;

public partial class LoggedSetRow : ObservableObject
{
    public int SessionExerciseId { get; set; }
    public int ExerciseId { get; set; }

    [ObservableProperty] private int _setNumber;
    [ObservableProperty] private string _weightText = "";
    [ObservableProperty] private string _repsText = "";
    [ObservableProperty] private int _rir = -1;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isPR;

    public string PrevWeightHint { get; init; } = "";
    public string PrevRepsHint { get; init; } = "";

    public string RirDisplay => _rir >= 0 ? _rir.ToString() : "RIR";
}
