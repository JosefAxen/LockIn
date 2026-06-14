using CommunityToolkit.Mvvm.ComponentModel;
using LockIn;
using LockIn.Models;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class HemViewModel(DatabaseService db, IHealthService health) : ObservableObject
{
    [ObservableProperty] private double _trainingScore;
    [ObservableProperty] private string _trainingScoreText = "–";
    [ObservableProperty] private string _streakLabel = "–";
    [ObservableProperty] private string _motivationText = "";
    [ObservableProperty] private string _stepsText = "–";
    [ObservableProperty] private string _caloriesText = "–";
    [ObservableProperty] private string _activeText = "–";
    [ObservableProperty] private string _heartRateText = "–";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private IReadOnlyList<DayStreakItem> _days = Array.Empty<DayStreakItem>();
    [ObservableProperty] private IReadOnlyList<HeatmapTile> _heatmapItems = Array.Empty<HeatmapTile>();
    [ObservableProperty] private double[] _stepsValues     = [];
    [ObservableProperty] private double[] _caloriesValues  = [];
    [ObservableProperty] private double[] _activeValues    = [];
    [ObservableProperty] private double[] _heartRateValues = [];

    public float GaugeProgress => (float)(TrainingScore / 100.0);

    public string UserName    => "Josef";
    public string UserInitial => "J";

    public string Greeting
    {
        get
        {
            int h = DateTime.Now.Hour;
            if (h < 10) return "GOD MORGON";
            if (h < 13) return "GOD FÖRMIDDAG";
            if (h < 18) return "GOD EFTERMIDDAG";
            return "GOD KVÄLL";
        }
    }

    public string GreetingText => $"{Greeting}, {UserName}!";

    public IReadOnlyList<string> CoachPrompts { get; } = new[]
    {
        "Visa min veckosammanfattning",
        "Tips för återhämtning",
        "Föreslå nästa pass"
    };

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await health.RequestPermissionsAsync();

            var weekStart    = GetMondayThisWeek();
            var sevenAgo     = DateTime.Today.AddDays(-6);
            var thirtyAgo    = DateTime.Today.AddDays(-30);

            var weekSessionsTask   = db.GetCompletedSessionsInRangeAsync(weekStart, DateTime.Now);
            var recentSessionsTask = db.GetCompletedSessionsInRangeAsync(sevenAgo, DateTime.Now);
            var streakSessionsTask = db.GetCompletedSessionsInRangeAsync(thirtyAgo, DateTime.Now);
            var settingsTask       = db.GetAppSettingsAsync();
            var stepsTask          = health.GetTodayStepsAsync();
            var caloriesTask       = health.GetTodayActiveCaloriesAsync();
            var heartRateTask      = health.GetTodayMaxHeartRateAsync();
            var weeklyStepsTask    = health.GetWeeklyStepsAsync();
            var weeklyCaloriesTask = health.GetWeeklyCaloriesAsync();
            var weeklyMaxHRTask    = health.GetWeeklyMaxHeartRateAsync();

            await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
                stepsTask, caloriesTask, heartRateTask,
                weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask);

            var weekSessions   = weekSessionsTask.Result;
            var recentSessions = recentSessionsTask.Result;
            var streakSessions = streakSessionsTask.Result;
            var settings       = settingsTask.Result;

            // Training score
            int goal   = settings.WeeklyWorkoutGoal > 0 ? settings.WeeklyWorkoutGoal : 4;
            double score = Math.Min(100.0, weekSessions.Count / (double)goal * 100.0);
            TrainingScore     = score;
            TrainingScoreText = ((int)score).ToString();
            MotivationText    = $"Stark vecka — du är {(int)score}% av veckans träningmål.";
            OnPropertyChanged(nameof(GaugeProgress));

            // Streak and calendar
            Days       = BuildStreakDays(recentSessions);
            int streak = CalculateStreak(streakSessions);
            StreakLabel = streak > 0 ? $"{streak} DAGARS STREAK" : "INGEN STREAK";

            // Heatmap
            await LoadHeatmapAsync();

            // Health stats
            var steps = stepsTask.Result;
            StepsText     = steps > 0 ? $"{steps:N0}" : "0";
            CaloriesText  = ((int)caloriesTask.Result).ToString();
            ActiveText    = CalculateActiveMinutesToday(recentSessions).ToString();
            var maxHR     = heartRateTask.Result;
            HeartRateText = maxHR > 0 ? maxHR.ToString() : "–";

            StepsValues     = weeklyStepsTask.Result.ToArray();
            CaloriesValues  = weeklyCaloriesTask.Result.ToArray();
            ActiveValues    = BuildWeeklyActiveMinutes(recentSessions);
            HeartRateValues = weeklyMaxHRTask.Result.ToArray();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static DateTime GetMondayThisWeek()
    {
        var today = DateTime.Today;
        int daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysFromMonday);
    }

    private async Task LoadHeatmapAsync()
    {
        var scores = await db.GetMuscleScoresAsync();
        var muscles = new (MuscleGroup mg, string name)[]
        {
            (MuscleGroup.Chest,     "BRÖST"),
            (MuscleGroup.Back,      "RYGG"),
            (MuscleGroup.Shoulders, "AXLAR"),
            (MuscleGroup.Biceps,    "BICEPS"),
            (MuscleGroup.Triceps,   "TRICEPS"),
            (MuscleGroup.Legs,      "BEN"),
            (MuscleGroup.Core,      "CORE"),
        };
        HeatmapItems = muscles.Select(m =>
        {
            var score = scores.TryGetValue(m.mg, out var s) ? s : 0.0;
            var t = score / 10.0;
            return new HeatmapTile
            {
                Name      = m.name,
                Score     = score,
                TileColor = DesignTokens.HeatmapTile(t),
                TextColor = DesignTokens.HeatmapText(t),
            };
        }).ToList();
    }

    private static IReadOnlyList<DayStreakItem> BuildStreakDays(List<WorkoutSession> sessions)
    {
        var today = DateTime.Today;
        var completedDates = sessions
            .Where(s => s.CompletedAt.HasValue)
            .Select(s => s.CompletedAt!.Value.Date)
            .ToHashSet();

        var abbrs = new[] { "MÅN", "TIS", "ONS", "TOR", "FRE", "LÖR", "SÖN" };
        var items = new List<DayStreakItem>(7);

        for (int i = 6; i >= 0; i--)
        {
            var day   = today.AddDays(-i);
            int idx   = ((int)day.DayOfWeek + 6) % 7;
            bool isToday     = i == 0;
            bool isCompleted = completedDates.Contains(day.Date) && !isToday;

            items.Add(new DayStreakItem
            {
                DayAbbr     = abbrs[idx],
                DayNum      = day.Day.ToString(),
                IsCompleted = isCompleted,
                IsToday     = isToday
            });
        }
        return items;
    }

    private static int CalculateStreak(List<WorkoutSession> sessions)
    {
        var completedDates = sessions
            .Where(s => s.CompletedAt.HasValue)
            .Select(s => s.CompletedAt!.Value.Date)
            .ToHashSet();

        int streak = 0;
        var check  = DateTime.Today;
        while (completedDates.Contains(check))
        {
            streak++;
            check = check.AddDays(-1);
        }
        return streak;
    }

    private static int CalculateActiveMinutesToday(List<WorkoutSession> sessions)
    {
        var today = DateTime.Today;
        return (int)sessions
            .Where(s => s.CompletedAt.HasValue && s.StartedAt.Date == today)
            .Sum(s => (s.CompletedAt!.Value - s.StartedAt).TotalMinutes);
    }

    private static double[] BuildWeeklyActiveMinutes(List<WorkoutSession> sessions)
    {
        var result = new double[7];
        for (int i = 0; i < 7; i++)
        {
            var date  = DateTime.Today.AddDays(i - 6).Date;
            result[i] = sessions
                .Where(s => s.CompletedAt.HasValue && s.StartedAt.Date == date)
                .Sum(s => (s.CompletedAt!.Value - s.StartedAt).TotalMinutes);
        }
        return result;
    }
}

public class DayStreakItem
{
    public string DayAbbr { get; init; } = "";
    public string DayNum  { get; init; } = "";
    public bool IsCompleted { get; init; }
    public bool IsToday     { get; init; }

    public Color CircleFill => IsCompleted
        ? DesignTokens.CalTrainedFill
        : IsToday
            ? DesignTokens.CalTodayFill
            : Colors.Transparent;

    public Color CircleStroke => IsCompleted
        ? DesignTokens.CalTrainedStroke
        : IsToday
            ? DesignTokens.CalTodayStroke
            : Colors.Transparent;

    public Color DayNumColor => IsCompleted
        ? DesignTokens.CalTrainedText
        : IsToday
            ? DesignTokens.CalTodayText
            : DesignTokens.CalNormalText;

    public Color DayAbbrColor => IsToday
        ? DesignTokens.TextSecondary
        : DesignTokens.TextMuted;
}
