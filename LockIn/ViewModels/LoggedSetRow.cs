using CommunityToolkit.Mvvm.ComponentModel;
using LockIn;
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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetLabel))]
    [NotifyPropertyChangedFor(nameof(SetLabelColor))]
    [NotifyPropertyChangedFor(nameof(SeparatorLabel))]
    [NotifyPropertyChangedFor(nameof(IsDropset))]
    [NotifyPropertyChangedFor(nameof(RowAccentColor))]
    private SetType _setType = SetType.Normal;

    public string PrevWeightHint { get; init; } = "";
    public string PrevRepsHint { get; init; } = "";
    public int TargetReps { get; init; } = 0;

    /// <summary>Markerad när raden precis lades till via +SET — Loaded-handlern
    /// kör en "veckla ut"-animation och nollställer flaggan.</summary>
    public bool IsFreshlyAdded { get; set; }

    public string RirDisplay => Rir >= 0 ? Rir.ToString() : "RIR";
    public Color  RirColor   => Rir >= 0 ? Color.FromArgb("#4ADE80") : DesignTokens.SetNormal;

    partial void OnRirChanged(int value)
    {
        OnPropertyChanged(nameof(RirDisplay));
        OnPropertyChanged(nameof(RirColor));
    }

    public string SetLabel => SetType switch
    {
        SetType.Warmup  => "W",
        SetType.Dropset => "↓",
        SetType.Time    => "⏱",
        _               => SetNumber.ToString()
    };

    public Color SetLabelColor => SetType switch
    {
        SetType.Warmup  => Color.FromArgb("#FBBF24"),
        SetType.Dropset => Color.FromArgb("#FB7185"),
        SetType.Time    => Color.FromArgb("#38BDF8"),
        _               => DesignTokens.SetNormal
    };

    public bool IsDropset => SetType == SetType.Dropset;

    public Color RowAccentColor => SetType == SetType.Dropset
        ? DesignTokens.SetDropset.WithAlpha(0.15f)
        : Colors.Transparent;

    public string SeparatorLabel => SetType == SetType.Time ? "⏱" : "×";

    public string RepsPlaceholder => SetType == SetType.Time
        ? (PrevRepsHint.Length > 0 ? PrevRepsHint : "SEK")
        : PrevRepsHint;

    partial void OnSetTypeChanged(SetType value)
    {
        OnPropertyChanged(nameof(RepsPlaceholder));
    }

    partial void OnSetNumberChanged(int value) =>
        OnPropertyChanged(nameof(SetLabel));
}
