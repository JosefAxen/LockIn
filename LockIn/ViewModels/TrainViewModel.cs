using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class TrainViewModel(DatabaseService db) : ObservableObject
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
                    "Kom ihåg att vila",
                    $"Du har tränat {Streak} dagar i rad — det är viktigt att lyssna på kroppen. Vila är en del av träningen.",
                    "Förstår");
            }
        }

        // Deload banner — check weekly volume trend
        await CheckDeloadAsync();

        // Feature 1: Muscle score bars (senaste 7 dagarna)
        var scoreData = await db.GetMuscleScoresAsync();
        MuscleScores.Clear();
        var muscles = new (MuscleGroup mg, string name)[]
        {
            (MuscleGroup.Chest,    "BRÖST"),
            (MuscleGroup.Back,     "RYGG"),
            (MuscleGroup.Shoulders,"AXLAR"),
            (MuscleGroup.Biceps,   "BICEPS"),
            (MuscleGroup.Triceps,  "TRICEPS"),
            (MuscleGroup.Legs,     "BEN"),
            (MuscleGroup.Core,     "CORE"),
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
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", template.Id }
        });
    }

    [RelayCommand]
    private async Task StartFreeWorkoutAsync()
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), new Dictionary<string, object>
        {
            { "TemplateId", 0 }
        });
    }

    [RelayCommand]
    private async Task DeleteTemplateAsync(WorkoutTemplate template)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort mall", $"Ta bort \"{template.Name}\"?", "Ta bort", "Avbryt");
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
        ? Color.FromArgb("#B8B8BC")
        : Color.FromArgb("#303038");
}
