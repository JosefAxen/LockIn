using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class MuscleFeedbackSheetViewModel : ObservableObject
{
    private readonly ISessionRepository _sessionRepo;

    public int SessionId { get; set; }
    public MuscleGroup Muscle { get; set; }
    public int LastSessionExerciseId { get; set; }
    public string MuscleName { get; set; } = "";
    public bool ShowJointPain { get; set; } = true;

    public string Question => string.Format(AppResources.Feedback_Muscle_Question_Format, MuscleName);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsPumpLow))]
    [NotifyPropertyChangedFor(nameof(IsPumpModerate))]
    [NotifyPropertyChangedFor(nameof(IsPumpAmazing))]
    private PumpLevel? _pump;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsVolNotEnough))]
    [NotifyPropertyChangedFor(nameof(IsVolJustRight))]
    [NotifyPropertyChangedFor(nameof(IsVolPushed))]
    [NotifyPropertyChangedFor(nameof(IsVolTooMuch))]
    private VolumeLevel? _volume;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJpNone))]
    [NotifyPropertyChangedFor(nameof(IsJpLow))]
    [NotifyPropertyChangedFor(nameof(IsJpModerate))]
    [NotifyPropertyChangedFor(nameof(IsJpALotOf))]
    private JointPainLevel? _jointPain;

    public bool CanSave => Pump is not null && Volume is not null;

    public bool IsPumpLow      => Pump == PumpLevel.Low;
    public bool IsPumpModerate => Pump == PumpLevel.Moderate;
    public bool IsPumpAmazing  => Pump == PumpLevel.Amazing;

    public bool IsVolNotEnough => Volume == VolumeLevel.NotEnough;
    public bool IsVolJustRight => Volume == VolumeLevel.JustRight;
    public bool IsVolPushed    => Volume == VolumeLevel.PushedLimits;
    public bool IsVolTooMuch   => Volume == VolumeLevel.TooMuch;

    public bool IsJpNone     => JointPain == JointPainLevel.None;
    public bool IsJpLow      => JointPain == JointPainLevel.Low;
    public bool IsJpModerate => JointPain == JointPainLevel.Moderate;
    public bool IsJpALotOf   => JointPain == JointPainLevel.ALotOf;

    public event EventHandler? CloseRequested;

    public MuscleFeedbackSheetViewModel(ISessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public void Reset()
    {
        Pump = null;
        Volume = null;
        JointPain = null;
    }

    [RelayCommand]
    private void SelectPump(string level)
    {
        if (int.TryParse(level, out var v))
            Pump = (PumpLevel)v;
    }

    [RelayCommand]
    private void SelectVolume(string level)
    {
        if (int.TryParse(level, out var v))
            Volume = (VolumeLevel)v;
    }

    [RelayCommand]
    private void SelectJointPain(string level)
    {
        if (int.TryParse(level, out var v))
            JointPain = (JointPainLevel)v;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Pump is null || Volume is null) return;
        await _sessionRepo.SaveMuscleFeedbackAsync(new SessionMuscleFeedback
        {
            SessionId = SessionId,
            MuscleGroupValue = (int)Muscle,
            PumpLevelValue = (int)Pump.Value,
            VolumeLevelValue = (int)Volume.Value,
        });
        if (JointPain is not null && LastSessionExerciseId > 0)
        {
            await _sessionRepo.SaveExerciseFeedbackAsync(new SessionExerciseFeedback
            {
                SessionExerciseId = LastSessionExerciseId,
                JointPainLevelValue = (int)JointPain.Value,
            });
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Skip() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
