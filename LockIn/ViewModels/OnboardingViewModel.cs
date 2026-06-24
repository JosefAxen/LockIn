using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Services;
using Microsoft.Extensions.DependencyInjection;
using LockIn;

namespace LockIn.ViewModels;

public partial class OnboardingViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private int    _currentStep        = 0;
    [ObservableProperty] private string _userNameInput      = "";
    [ObservableProperty] private int    _selectedWeeklyGoal = 4;
    [ObservableProperty] private int    _selectedExperience = -1;
    [ObservableProperty] private int    _selectedGoal       = -1;

    // ── Step visibility ────────────────────────────────────────────────────

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(ShowBackButton));
    }

    partial void OnUserNameInputChanged(string value) => OnPropertyChanged(nameof(CanGoNext));

    partial void OnSelectedExperienceChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        RefreshExperienceColors();
    }

    partial void OnSelectedGoalChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        RefreshGoalColors();
        OnPropertyChanged(nameof(RecommendedProgram));
        OnPropertyChanged(nameof(RecommendedDescription));
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    public bool ShowBackButton => CurrentStep > 0;

    public bool CanGoNext => CurrentStep switch
    {
        0 => UserNameInput.Trim().Length > 0,
        1 => SelectedWeeklyGoal >= 2,
        2 => SelectedExperience >= 0,
        3 => SelectedGoal >= 0,
        _ => false
    };

    [RelayCommand]
    private void Next()
    {
        if (CanGoNext && CurrentStep < 4)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    // ── Weekly goal selection (2–6) ────────────────────────────────────────

    [RelayCommand]
    private async Task SelectWeeklyGoalAsync(int days)
    {
        SelectedWeeklyGoal = days;
        RefreshWeeklyGoalColors();
        OnPropertyChanged(nameof(CanGoNext));
        await AutoAdvanceAsync();
    }

    // ── Experience & goal selection ────────────────────────────────────────

    [RelayCommand]
    private async Task SelectExperienceAsync(int level)
    {
        SelectedExperience = level;
        await AutoAdvanceAsync();
    }

    [RelayCommand]
    private async Task SelectGoalAsync(int goal)
    {
        SelectedGoal = goal;
        await AutoAdvanceAsync();
    }

    private async Task AutoAdvanceAsync()
    {
        if (!CanGoNext || CurrentStep >= 4) return;
        // Liten paus så användaren ser sitt val markeras innan vi byter steg
        await Task.Delay(220);
        if (CanGoNext && CurrentStep < 4)
            CurrentStep++;
    }

    // ── Universal skip (alla steg) ─────────────────────────────────────────

    [RelayCommand]
    private async Task SkipOnboardingAsync()
    {
        var ok = await Shell.Current.DisplayAlert(
            "Hoppa över?",
            "Du kan välja program senare i Bibliotek.",
            "Hoppa över", "Avbryt");
        if (!ok) return;
        await FinishOnboardingAsync(activateProgram: false);
    }

    // ── Recommendation ─────────────────────────────────────────────────────

    public WorkoutProgram? RecommendedProgram
    {
        get
        {
            if (SelectedExperience < 0 || SelectedGoal < 0) return null;
            var id = (SelectedExperience, SelectedGoal) switch
            {
                (0, 0) => "startingstrength",
                (0, 1) => "fullbody",
                (1, 0) => "texasmethod",
                (1, 1) => "upperlower",
                (2, 0) => "531bbb",
                (2, 1) => "ppl",
                _      => "fullbody"
            };
            return WorkoutPrograms.All.FirstOrDefault(p => p.Id == id);
        }
    }

    public string RecommendedDescription =>
        RecommendedProgram is { } p ? $"{p.DaysPerWeek} dagar/vecka · {p.Days.Count} pass" : "";

    // ── Completion ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ActivateProgramAsync()
    {
        await FinishOnboardingAsync(activateProgram: true);
    }

    [RelayCommand]
    private async Task SkipProgramAsync()
    {
        await FinishOnboardingAsync(activateProgram: false);
    }

    private async Task FinishOnboardingAsync(bool activateProgram)
    {
        var settings = await db.GetAppSettingsAsync();
        settings.UserName               = UserNameInput.Trim();
        settings.WeeklyWorkoutGoal      = SelectedWeeklyGoal;
        settings.HasCompletedOnboarding = true;
        await db.SaveAppSettingsAsync(settings);

        if (activateProgram && RecommendedProgram is { } program)
            await db.ActivateProgramAsync(program);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
            Application.Current!.Windows[0].Page = shell;
        });
    }

    // ── Card colors ────────────────────────────────────────────────────────

    private static Color SelBg  => DesignTokens.Accent;
    private static Color SelFg  => DesignTokens.FabForeground;
    private static Color IdleBg => DesignTokens.Surface2;
    private static Color IdleFg => DesignTokens.Text;

    // Weekly goal (2–6)
    public Color Wk2Bg => SelectedWeeklyGoal == 2 ? SelBg : IdleBg;
    public Color Wk3Bg => SelectedWeeklyGoal == 3 ? SelBg : IdleBg;
    public Color Wk4Bg => SelectedWeeklyGoal == 4 ? SelBg : IdleBg;
    public Color Wk5Bg => SelectedWeeklyGoal == 5 ? SelBg : IdleBg;
    public Color Wk6Bg => SelectedWeeklyGoal == 6 ? SelBg : IdleBg;
    public Color Wk2Fg => SelectedWeeklyGoal == 2 ? SelFg : IdleFg;
    public Color Wk3Fg => SelectedWeeklyGoal == 3 ? SelFg : IdleFg;
    public Color Wk4Fg => SelectedWeeklyGoal == 4 ? SelFg : IdleFg;
    public Color Wk5Fg => SelectedWeeklyGoal == 5 ? SelFg : IdleFg;
    public Color Wk6Fg => SelectedWeeklyGoal == 6 ? SelFg : IdleFg;

    private void RefreshWeeklyGoalColors()
    {
        OnPropertyChanged(nameof(Wk2Bg)); OnPropertyChanged(nameof(Wk2Fg));
        OnPropertyChanged(nameof(Wk3Bg)); OnPropertyChanged(nameof(Wk3Fg));
        OnPropertyChanged(nameof(Wk4Bg)); OnPropertyChanged(nameof(Wk4Fg));
        OnPropertyChanged(nameof(Wk5Bg)); OnPropertyChanged(nameof(Wk5Fg));
        OnPropertyChanged(nameof(Wk6Bg)); OnPropertyChanged(nameof(Wk6Fg));
    }

    // Experience level
    public Color Exp0Bg => SelectedExperience == 0 ? SelBg : IdleBg;
    public Color Exp1Bg => SelectedExperience == 1 ? SelBg : IdleBg;
    public Color Exp2Bg => SelectedExperience == 2 ? SelBg : IdleBg;
    public Color Exp0Fg => SelectedExperience == 0 ? SelFg : IdleFg;
    public Color Exp1Fg => SelectedExperience == 1 ? SelFg : IdleFg;
    public Color Exp2Fg => SelectedExperience == 2 ? SelFg : IdleFg;
    public bool  Exp0Selected => SelectedExperience == 0;
    public bool  Exp1Selected => SelectedExperience == 1;
    public bool  Exp2Selected => SelectedExperience == 2;

    private void RefreshExperienceColors()
    {
        OnPropertyChanged(nameof(Exp0Bg)); OnPropertyChanged(nameof(Exp0Fg));
        OnPropertyChanged(nameof(Exp1Bg)); OnPropertyChanged(nameof(Exp1Fg));
        OnPropertyChanged(nameof(Exp2Bg)); OnPropertyChanged(nameof(Exp2Fg));
        OnPropertyChanged(nameof(Exp0Selected));
        OnPropertyChanged(nameof(Exp1Selected));
        OnPropertyChanged(nameof(Exp2Selected));
    }

    // Training goal
    public Color GoalStrengthBg => SelectedGoal == 0 ? SelBg : IdleBg;
    public Color GoalStrengthFg => SelectedGoal == 0 ? SelFg : IdleFg;
    public Color GoalHyperBg    => SelectedGoal == 1 ? SelBg : IdleBg;
    public Color GoalHyperFg    => SelectedGoal == 1 ? SelFg : IdleFg;
    public bool  GoalStrengthSelected => SelectedGoal == 0;
    public bool  GoalHyperSelected    => SelectedGoal == 1;

    private void RefreshGoalColors()
    {
        OnPropertyChanged(nameof(GoalStrengthBg));
        OnPropertyChanged(nameof(GoalStrengthFg));
        OnPropertyChanged(nameof(GoalHyperBg));
        OnPropertyChanged(nameof(GoalHyperFg));
        OnPropertyChanged(nameof(GoalStrengthSelected));
        OnPropertyChanged(nameof(GoalHyperSelected));
    }
}
