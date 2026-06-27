using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using LockIn;

namespace LockIn.ViewModels;

public partial class TrainViewModel(DatabaseService db, ActiveWorkoutStateService state) : ObservableObject
{
    public ObservableCollection<ProgramGroup> ProgramGroups { get; } = new();
    public ObservableCollection<WorkoutTemplate> FreeTemplates { get; } = new();
    public ObservableCollection<MuscleScoreRow> MuscleScores { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNoTemplates;
    [ObservableProperty] private bool _hasProgramGroups;
    [ObservableProperty] private int _weekCount;
    [ObservableProperty] private int _streak;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _showDeloadBanner;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var templates = await db.GetTemplatesAsync();
        var lastUsedMap = await db.GetLastSessionDatePerTemplateAsync();
        foreach (var t in templates)
            if (lastUsedMap.TryGetValue(t.Id, out var dt))
                t.LastUsedAt = dt;

        ProgramGroups.Clear();
        var grouped = templates
            .Where(t => t.ProgramId != null)
            .GroupBy(t => t.ProgramId!);

        foreach (var group in grouped)
        {
            var program = WorkoutPrograms.All.FirstOrDefault(p => p.Id == group.Key);
            var pg = new ProgramGroup
            {
                ProgramId = group.Key,
                ProgramName = program?.Name ?? group.Key
            };
            foreach (var t in group) pg.Templates.Add(t);
            ProgramGroups.Add(pg);
        }

        FreeTemplates.Clear();
        foreach (var t in templates.Where(t => t.ProgramId == null))
            FreeTemplates.Add(t);

        HasProgramGroups = ProgramGroups.Count > 0;
        HasNoTemplates = ProgramGroups.Count == 0 && FreeTemplates.Count == 0;

        WeekCount = await db.GetSessionCountThisWeekAsync();
        Streak = await db.GetCurrentStreakAsync();
        TotalCount = await db.GetTotalCompletedSessionCountAsync();

        // Feature 5: Restday popup after 7+ consecutive training days
        if (Streak >= 7)
        {
            var lastTicks = Preferences.Default.Get("last_restday_popup", 0L);
            if (lastTicks == 0L || DateTime.Now.AddDays(-7) > new DateTime(lastTicks))
            {
                Preferences.Default.Set("last_restday_popup", DateTime.Now.Ticks);
                await Shell.Current.DisplayAlert(
                    AppResources.Train_RestReminder_Title,
                    string.Format(AppResources.Train_RestReminder_Body_Format, Streak),
                    AppResources.Train_RestReminder_OK);
            }
        }

        // Deload banner — check weekly volume trend
        await CheckDeloadAsync();

        // Feature 1: Muscle score bars (senaste 7 dagarna)
        var scoreData = await db.GetMuscleScoresAsync();
        MuscleScores.Clear();
        var muscles = new (MuscleGroup mg, string name)[]
        {
            (MuscleGroup.Chest,    AppResources.Train_Muscle_Chest),
            (MuscleGroup.Back,     AppResources.Train_Muscle_Back),
            (MuscleGroup.Shoulders,AppResources.Train_Muscle_Shoulders),
            (MuscleGroup.Biceps,   AppResources.Train_Muscle_Biceps),
            (MuscleGroup.Triceps,  AppResources.Train_Muscle_Triceps),
            (MuscleGroup.Legs,     AppResources.Train_Muscle_Legs),
            (MuscleGroup.Core,     AppResources.Train_Muscle_Core),
        };
        foreach (var (mg, name) in muscles)
        {
            var score = scoreData.TryGetValue(mg, out var s) ? s : 0.0;
            MuscleScores.Add(new MuscleScoreRow { Name = name, Score = score });
        }

        IsLoading = false;
    }

    private async Task CheckDeloadAsync()
    {
        var volumes = await db.GetWeeklyVolumeTrendAsync(4);
        if (volumes.Count < 3) { ShowDeloadBanner = false; return; }

        bool decreasing = volumes[0] < volumes[1] && volumes[1] < volumes[2];
        bool sustainedHigh = volumes.Count >= 4 && volumes.All(v => v > 15000);

        if (!decreasing && !sustainedHigh) { ShowDeloadBanner = false; return; }

        var lastTicks = Preferences.Default.Get("last_deload_dismiss", 0L);
        if (lastTicks != 0L && new DateTime(lastTicks) > DateTime.Now.AddDays(-14))
        {
            ShowDeloadBanner = false;
            return;
        }
        ShowDeloadBanner = true;
    }

    [RelayCommand]
    private void DismissDeload()
    {
        Preferences.Default.Set("last_deload_dismiss", DateTime.Now.Ticks);
        ShowDeloadBanner = false;
    }

    [RelayCommand]
    private void ToggleProgramGroup(ProgramGroup group) => group.IsExpanded = !group.IsExpanded;

    [RelayCommand]
    private async Task StartWorkoutAsync(WorkoutTemplate template)
    {
        if (state.IsActive)
        {
            var go = await Shell.Current.DisplayAlert(
                AppResources.Train_ActiveSession_Title,
                AppResources.Train_ActiveSession_Body,
                AppResources.Train_ActiveSession_GoTo, AppResources.Common_Cancel);
            if (go) await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
            return;
        }
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
        });
    }

    [RelayCommand]
    private async Task StartFreeWorkoutAsync()
    {
        if (state.IsActive)
        {
            var go = await Shell.Current.DisplayAlert(
                AppResources.Train_ActiveSession_Title,
                AppResources.Train_ActiveSession_Body,
                AppResources.Train_ActiveSession_GoTo, AppResources.Common_Cancel);
            if (go) await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
            return;
        }
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", 0 }
        });
    }

    [RelayCommand]
    private async Task OpenCardioAsync()
        => await Shell.Current.GoToAsync(nameof(CardioPage));

    [RelayCommand]
    private async Task DeleteTemplateAsync(WorkoutTemplate template)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.Train_DeleteTemplate_Title,
            string.Format(AppResources.Train_DeleteTemplate_Body_Format, template.Name),
            AppResources.Common_Delete, AppResources.Common_Cancel);
        if (!confirmed) return;
        await db.DeleteTemplateAsync(template);
        FreeTemplates.Remove(template);
        foreach (var pg in ProgramGroups)
            pg.Templates.Remove(template);
    }
}

public class MuscleScoreRow
{
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public double ScaleFraction => Score / 10.0;
    public string ScoreText => Score >= 0.05 ? Score.ToString("F1") : "—";
    public Color ScoreColor => Score >= 0.05
        ? DesignTokens.Accent
        : DesignTokens.TextDim;
}
