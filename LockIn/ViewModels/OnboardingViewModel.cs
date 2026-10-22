using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using LockIn;

namespace LockIn.ViewModels;

public partial class OnboardingViewModel(DatabaseService db, IHealthService health, ISettingsRepository settingsRepo) : ObservableObject
{
    [ObservableProperty] private int    _currentStep        = 0;
    [ObservableProperty] private string _userNameInput      = "";
    [ObservableProperty] private int    _selectedWeeklyGoal = 4;
    [ObservableProperty] private int    _selectedExperience = -1;
    [ObservableProperty] private int    _selectedGoal       = -1;

    // ── Equipment (Step 5) ─────────────────────────────────────────────────
    [ObservableProperty] private bool _hasEquipmentBarbell    = true;
    [ObservableProperty] private bool _hasEquipmentDumbbell   = true;
    [ObservableProperty] private bool _hasEquipmentCable      = true;
    [ObservableProperty] private bool _hasEquipmentMachine    = true;
    [ObservableProperty] private bool _hasEquipmentBodyOnly   = true;
    [ObservableProperty] private bool _hasEquipmentEZBar      = true;
    [ObservableProperty] private bool _hasEquipmentKettlebell = true;
    [ObservableProperty] private bool _hasEquipmentBands      = true;
    [ObservableProperty] private bool _hasEquipmentOther      = true;

    // ── Step visibility ────────────────────────────────────────────────────

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsStep5));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(ShowBackButton));
        OnPropertyChanged(nameof(ShowBottomCta));
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

    public bool ShowBackButton  => CurrentStep > 0;

    // Bottom CTA visas bara steg 0–3 (inte steg 4 = rekommendation, inte steg 5 = equipment)
    public bool ShowBottomCta   => CurrentStep < 4;

    public bool CanGoNext => CurrentStep switch
    {
        0 => UserNameInput.Trim().Length > 0,
        1 => SelectedWeeklyGoal >= 1,
        2 => SelectedExperience >= 0,
        3 => SelectedGoal >= 0,
        4 => true,   // Step 4 = Rekommendation — alltid möjligt att gå vidare
        5 => true,   // Step 5 = Equipment — alltid möjligt (minst 0 val = "allt")
        _ => false
    };

    [RelayCommand]
    private void Next()
    {
        if (CanGoNext && CurrentStep < 5)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    // ── Weekly goal selection (1–7, samma intervall som Inställningar) ─────

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
        if (!CanGoNext || CurrentStep >= 5) return;
        // Liten paus så användaren ser sitt val markeras innan vi byter steg
        await Task.Delay(220);
        if (CanGoNext && CurrentStep < 5)
            CurrentStep++;
    }

    // ── Universal skip (alla steg) ─────────────────────────────────────────

    // Onboardingen är Window.Page utan Shell — Shell.Current är null här.
    private static Page? CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    [RelayCommand]
    private async Task SkipOnboardingAsync()
    {
        var page = CurrentPage;
        if (page is null) return;
        var ok = await page.DisplayAlert(
            AppResources.Onboarding_SkipDialogTitle,
            AppResources.Onboarding_SkipDialogBody,
            AppResources.Onboarding_SkipLink,
            AppResources.Common_Cancel);
        if (!ok) return;
        await FinishAsync();
    }

    // ── Equipment commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleEquipment(int equipmentTypeInt)
    {
        switch ((EquipmentType)equipmentTypeInt)
        {
            case EquipmentType.Barbell:    HasEquipmentBarbell    = !HasEquipmentBarbell;    break;
            case EquipmentType.Dumbbell:   HasEquipmentDumbbell   = !HasEquipmentDumbbell;   break;
            case EquipmentType.Cable:      HasEquipmentCable      = !HasEquipmentCable;      break;
            case EquipmentType.Machine:    HasEquipmentMachine    = !HasEquipmentMachine;    break;
            case EquipmentType.BodyOnly:   HasEquipmentBodyOnly   = !HasEquipmentBodyOnly;   break;
            case EquipmentType.EZBar:      HasEquipmentEZBar      = !HasEquipmentEZBar;      break;
            case EquipmentType.Kettlebell: HasEquipmentKettlebell = !HasEquipmentKettlebell; break;
            case EquipmentType.Bands:      HasEquipmentBands      = !HasEquipmentBands;      break;
            case EquipmentType.Other:      HasEquipmentOther      = !HasEquipmentOther;      break;
        }
    }

    private async Task SaveEquipmentAsync()
    {
        var selected = new List<int>();
        if (HasEquipmentBarbell)    selected.Add((int)EquipmentType.Barbell);
        if (HasEquipmentDumbbell)   selected.Add((int)EquipmentType.Dumbbell);
        if (HasEquipmentCable)      selected.Add((int)EquipmentType.Cable);
        if (HasEquipmentMachine)    selected.Add((int)EquipmentType.Machine);
        if (HasEquipmentBodyOnly)   selected.Add((int)EquipmentType.BodyOnly);
        if (HasEquipmentEZBar)      selected.Add((int)EquipmentType.EZBar);
        if (HasEquipmentKettlebell) selected.Add((int)EquipmentType.Kettlebell);
        if (HasEquipmentBands)      selected.Add((int)EquipmentType.Bands);
        if (HasEquipmentOther)      selected.Add((int)EquipmentType.Other);

        var settings = await settingsRepo.GetAsync();
        settings.PreferredEquipmentJson = JsonSerializer.Serialize(selected);
        await settingsRepo.SaveAsync(settings);
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
        RecommendedProgram is { } p
            ? string.Format(AppResources.Onboarding_RecommendedFormat, p.DaysPerWeek, p.Days.Count)
            : "";

    // ── Completion ─────────────────────────────────────────────────────────

    // Steg 4 → steg 5 (equipment). Programmet aktiveras inte här (program-koncept ersatt
    // av template-flöde, Task 21) — texten på steg 4 säger var programmet finns.
    [RelayCommand]
    private void ContinueFromRecommendation() => CurrentStep = 5;

    // Steg 5: Klar-knapp
    [RelayCommand]
    private async Task FinishAsync()
    {
        try
        {
            await SaveEquipmentAsync();

            var settings = await settingsRepo.GetAsync();
            settings.UserName               = UserNameInput.Trim();
            settings.WeeklyWorkoutGoal      = SelectedWeeklyGoal;
            settings.HasCompletedOnboarding = true;
            await settingsRepo.SaveAsync(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onboarding] Kunde inte spara inställningar: {ex}");
            if (CurrentPage is { } errorPage)
                await errorPage.DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
            return;
        }

        await RequestHealthAccessAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var shell = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
            Application.Current!.Windows[0].Page = shell;
        });
    }

    // Förklara först varför appen vill läsa Hälsa-data, fråga sedan systemet.
    // Får aldrig stoppa onboardingen: saknas Hälsa returnerar tjänsten false.
    private async Task RequestHealthAccessAsync()
    {
        try
        {
            var page = CurrentPage;
            if (page is null) return;
            var proceed = await page.DisplayAlert(
                AppResources.Onboarding_Health_Title,
                AppResources.Onboarding_Health_Body,
                AppResources.Onboarding_Health_Allow,
                AppResources.Onboarding_Health_NotNow);
            if (!proceed) return;

            var granted = await health.RequestPermissionsAsync();
            if (granted)
                Preferences.Default.Set("healthkit_sync_enabled", true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onboarding] Hälsa-behörighet misslyckades: {ex}");
        }
    }

    // ── Card colors ────────────────────────────────────────────────────────

    private static Color SelBg  => DesignTokens.Accent;
    private static Color SelFg  => DesignTokens.FabForeground;
    private static Color IdleBg => DesignTokens.Surface2;
    private static Color IdleFg => DesignTokens.Text;

    // Weekly goal (1–7)
    public Color Wk1Bg => SelectedWeeklyGoal == 1 ? SelBg : IdleBg;
    public Color Wk2Bg => SelectedWeeklyGoal == 2 ? SelBg : IdleBg;
    public Color Wk3Bg => SelectedWeeklyGoal == 3 ? SelBg : IdleBg;
    public Color Wk4Bg => SelectedWeeklyGoal == 4 ? SelBg : IdleBg;
    public Color Wk5Bg => SelectedWeeklyGoal == 5 ? SelBg : IdleBg;
    public Color Wk6Bg => SelectedWeeklyGoal == 6 ? SelBg : IdleBg;
    public Color Wk7Bg => SelectedWeeklyGoal == 7 ? SelBg : IdleBg;
    public Color Wk1Fg => SelectedWeeklyGoal == 1 ? SelFg : IdleFg;
    public Color Wk2Fg => SelectedWeeklyGoal == 2 ? SelFg : IdleFg;
    public Color Wk3Fg => SelectedWeeklyGoal == 3 ? SelFg : IdleFg;
    public Color Wk4Fg => SelectedWeeklyGoal == 4 ? SelFg : IdleFg;
    public Color Wk5Fg => SelectedWeeklyGoal == 5 ? SelFg : IdleFg;
    public Color Wk6Fg => SelectedWeeklyGoal == 6 ? SelFg : IdleFg;
    public Color Wk7Fg => SelectedWeeklyGoal == 7 ? SelFg : IdleFg;

    private void RefreshWeeklyGoalColors()
    {
        OnPropertyChanged(nameof(Wk1Bg)); OnPropertyChanged(nameof(Wk1Fg));
        OnPropertyChanged(nameof(Wk2Bg)); OnPropertyChanged(nameof(Wk2Fg));
        OnPropertyChanged(nameof(Wk3Bg)); OnPropertyChanged(nameof(Wk3Fg));
        OnPropertyChanged(nameof(Wk4Bg)); OnPropertyChanged(nameof(Wk4Fg));
        OnPropertyChanged(nameof(Wk5Bg)); OnPropertyChanged(nameof(Wk5Fg));
        OnPropertyChanged(nameof(Wk6Bg)); OnPropertyChanged(nameof(Wk6Fg));
        OnPropertyChanged(nameof(Wk7Bg)); OnPropertyChanged(nameof(Wk7Fg));
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
