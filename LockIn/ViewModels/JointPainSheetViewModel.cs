using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class JointPainSheetViewModel : ObservableObject
{
    private readonly ISessionRepository _sessionRepo;

    public int SessionExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string Question => string.Format(AppResources.Feedback_JointPain_Question_Format, ExerciseName);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsNoneSelected))]
    [NotifyPropertyChangedFor(nameof(IsLowSelected))]
    [NotifyPropertyChangedFor(nameof(IsModerateSelected))]
    [NotifyPropertyChangedFor(nameof(IsALotOfSelected))]
    private JointPainLevel? _selected;

    public bool CanSave => Selected is not null;

    public bool IsNoneSelected     => Selected == JointPainLevel.None;
    public bool IsLowSelected      => Selected == JointPainLevel.Low;
    public bool IsModerateSelected => Selected == JointPainLevel.Moderate;
    public bool IsALotOfSelected   => Selected == JointPainLevel.ALotOf;

    public event EventHandler? CloseRequested;

    public JointPainSheetViewModel(ISessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public void Reset()
    {
        Selected = null;
    }

    [RelayCommand]
    private void Select(string level)
    {
        if (int.TryParse(level, out var v))
            Selected = (JointPainLevel)v;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Selected is null) return;
        try
        {
            await _sessionRepo.SaveExerciseFeedbackAsync(new SessionExerciseFeedback
            {
                SessionExerciseId = SessionExerciseId,
                JointPainLevelValue = (int)Selected.Value,
            });
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[JointPain] Save: {ex}");
            try { await CommunityToolkit.Maui.Alerts.Toast.Make(AppResources.Common_ErrorRetry).Show(); }
            catch { /* felvisningen får inte själv fälla passet */ }
        }
    }

    [RelayCommand]
    private void Skip() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
