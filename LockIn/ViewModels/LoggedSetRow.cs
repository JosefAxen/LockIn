using CommunityToolkit.Mvvm.ComponentModel;
using LockIn;
using LockIn.Models;
using LockIn.Resources.Strings;

namespace LockIn.ViewModels;

public partial class LoggedSetRow : ObservableObject
{
    /// <summary>Högsta RIR-steget i växlaren. Visas som "4+".</summary>
    public const int MaxRir = 4;

    public int SessionExerciseId { get; set; }
    public int ExerciseId { get; set; }

    /// <summary>Id för sparad LoggedSet (0 = inte sparad). Ett sparat set uppdateras i stället för att infogas igen.</summary>
    public int LoggedSetId { get; set; }
    public DateTime LoggedAt { get; set; }

    // Texterna som senast sparades — används för att upptäcka ändringar på ett klart set.
    internal string SavedWeightText { get; set; } = "";
    internal string SavedRepsText { get; set; } = "";

    /// <summary>Spärr mot dubbeltryck medan setet sparas eller tas bort.</summary>
    internal bool IsSaving { get; set; }

    /// <summary>När en ändring senast sparades när fältet lämnades. Ett ✓-tryck strax
    /// efteråt tolkas som "bekräfta", inte som "ångra klart".</summary>
    internal DateTime LastEditSavedAt { get; set; }

    public bool HasUnsavedEdits => IsCompleted && LoggedSetId > 0
        && (WeightText != SavedWeightText || RepsText != SavedRepsText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetTypeA11y))]
    private int _setNumber;
    [ObservableProperty] private string _weightText = "";
    [ObservableProperty] private string _repsText = "";
    // -1 = inte angiven (sparas som null). MaxRir betyder "4 eller fler".
    [ObservableProperty] private int _rir = -1;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DoneA11y))]
    [NotifyPropertyChangedFor(nameof(RowOpacity))]
    private bool _isCompleted;
    [ObservableProperty] private bool _isPR;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetLabel))]
    [NotifyPropertyChangedFor(nameof(SetLabelColor))]
    [NotifyPropertyChangedFor(nameof(SeparatorLabel))]
    [NotifyPropertyChangedFor(nameof(IsDropset))]
    [NotifyPropertyChangedFor(nameof(RowAccentColor))]
    [NotifyPropertyChangedFor(nameof(SetTypeA11y))]
    private SetType _setType = SetType.Normal;

    public string PrevWeightHint { get; init; } = "";
    public string PrevRepsHint { get; init; } = "";
    public int TargetReps { get; init; } = 0;
    public int TargetRir { get; init; } = -1;

    public bool HasTargetRir => TargetRir >= 0;
    public string TargetRirDisplay => TargetRir >= 0
        ? string.Format(AppResources.LoggedSet_TargetRir_Format, TargetRir)
        : "";

    /// <summary>Markerad när raden precis lades till via +SET — Loaded-handlern
    /// kör en "veckla ut"-animation och nollställer flaggan.</summary>
    public bool IsFreshlyAdded { get; set; }

    /// <summary>Klara set tonas ned, men inte så mycket att texten tappar kontrast (≥ 4,5:1).</summary>
    public double RowOpacity => IsCompleted ? 0.8 : 1.0;

    public string DoneA11y => IsCompleted ? AppResources.A11y_SetCompleted : AppResources.A11y_CompleteSet;

    public string RirDisplay => Rir < 0 ? "–" : Rir >= MaxRir ? $"{MaxRir}+" : Rir.ToString();
    public Color  RirColor   => Rir >= 0 ? DesignTokens.Text : DesignTokens.TextSecondary;
    public string RirA11y    => Rir < 0
        ? AppResources.ActiveWorkout_A11y_RirNone
        : string.Format(AppResources.ActiveWorkout_A11y_Rir_Format, RirDisplay);

    /// <summary>Växlar – → 0 → 1 → 2 → 3 → 4+ → –.</summary>
    public void CycleRir() => Rir = Rir < 0 ? 0 : Rir >= MaxRir ? -1 : Rir + 1;

    partial void OnRirChanged(int value)
    {
        OnPropertyChanged(nameof(RirDisplay));
        OnPropertyChanged(nameof(RirColor));
        OnPropertyChanged(nameof(RirA11y));
    }

    public string SetLabel => SetType switch
    {
        SetType.Warmup  => "W",
        SetType.Dropset => "↓",
        SetType.Time    => "⏱",
        _               => SetNumber.ToString()
    };

    public string SetTypeA11y => string.Format(AppResources.ActiveWorkout_A11y_SetType_Format, SetType switch
    {
        SetType.Warmup  => AppResources.ActiveWorkout_A11y_SetWarmup,
        SetType.Dropset => AppResources.ActiveWorkout_A11y_SetDropset,
        SetType.Time    => AppResources.ActiveWorkout_A11y_SetTime,
        _               => string.Format(AppResources.ActiveWorkout_A11y_SetNormal_Format, SetNumber)
    });

    public Color SetLabelColor => SetType switch
    {
        SetType.Warmup  => DesignTokens.SetWarmup,
        SetType.Dropset => DesignTokens.SetDropset,
        SetType.Time    => DesignTokens.SetTime,
        _               => DesignTokens.SetNormal
    };

    public bool IsDropset => SetType == SetType.Dropset;

    public Color RowAccentColor => SetType == SetType.Dropset
        ? DesignTokens.SetDropset.WithAlpha(0.15f)
        : Colors.Transparent;

    public string SeparatorLabel => SetType == SetType.Time ? "⏱" : "×";

    // RIR loggas i en egen kolumn — reps-fältet visar bara reps (eller sekunder för tidsset).
    public string RepsPlaceholder => SetType == SetType.Time
        ? (PrevRepsHint.Length > 0 ? PrevRepsHint : "SEK")
        : PrevRepsHint.Length > 0
            ? PrevRepsHint
            : TargetReps > 0 ? TargetReps.ToString() : "";

    partial void OnSetTypeChanged(SetType value) =>
        OnPropertyChanged(nameof(RepsPlaceholder));

    partial void OnSetNumberChanged(int value) =>
        OnPropertyChanged(nameof(SetLabel));
}
