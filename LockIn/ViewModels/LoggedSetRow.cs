using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;

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
    [ObservableProperty] private SetType _setType = SetType.Normal;

    public string PrevWeightHint { get; init; } = "";
    public string PrevRepsHint { get; init; } = "";
    public int TargetReps { get; init; } = 0;

    public string RirDisplay => _rir >= 0 ? _rir.ToString() : "RIR";

    public string SetLabel => SetType switch
    {
        SetType.Warmup  => "W",
        SetType.Dropset => "↓",
        SetType.Time    => "⏱",
        _               => SetNumber.ToString()
    };

    public Color SetLabelColor => SetType switch
    {
        SetType.Warmup  => Color.FromArgb("#F59E0B"),
        SetType.Dropset => Color.FromArgb("#FF5A1F"),
        SetType.Time    => Color.FromArgb("#6EA8DC"),
        _               => Color.FromArgb("#505055")
    };

    public string SeparatorLabel => SetType == SetType.Time ? "⏱" : "×";

    public string RepsPlaceholder => SetType == SetType.Time
        ? (PrevRepsHint.Length > 0 ? PrevRepsHint : "SEK")
        : PrevRepsHint;

    partial void OnSetTypeChanged(SetType value)
    {
        OnPropertyChanged(nameof(SetLabel));
        OnPropertyChanged(nameof(SetLabelColor));
        OnPropertyChanged(nameof(SeparatorLabel));
        OnPropertyChanged(nameof(RepsPlaceholder));
    }

    partial void OnSetNumberChanged(int value) =>
        OnPropertyChanged(nameof(SetLabel));
}
