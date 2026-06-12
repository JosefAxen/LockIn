using LockIn.Views;
using Microsoft.Maui.Graphics;

namespace LockIn.ViewModels;

public class HemViewModel
{
    public double TrainingScore { get; } = 72;
    public string TrainingScoreText => ((int)TrainingScore).ToString();

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

    public string UserName => "Josef";
    public string UserInitial => UserName.Length > 0 ? UserName[0].ToString().ToUpper() : "?";
    public string GreetingText => $"{Greeting}, {UserName}!";
    public string MotivationText => "Stark vecka — du är 72% av veckans träningmål.";
    public string StreakLabel => "3 DAGARS STREAK";

    // Day streak
    public IReadOnlyList<DayStreakItem> Days { get; }

    // Stats
    public string StepsText => "8 420";
    public string CaloriesText => "612";
    public string ActiveText => "87";
    public string HeartRateText => "158";

    // Sparklines
    public TrainingScoreDrawable GaugeDrawable { get; }
    public SparklineDrawable StepsSparkline { get; }
    public SparklineDrawable CaloriesSparkline { get; }
    public SparklineDrawable ActiveSparkline { get; }
    public SparklineDrawable HeartRateSparkline { get; }

    // AI coach prompts
    public IReadOnlyList<string> CoachPrompts { get; } = new[]
    {
        "Visa min veckosammanfattning",
        "Tips för återhämtning",
        "Föreslå nästa pass"
    };

    public HemViewModel()
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        GaugeDrawable = new TrainingScoreDrawable { Score = TrainingScore, IsDark = isDark };

        StepsSparkline = new SparklineDrawable
        {
            Values = new double[] { 5200, 7100, 4800, 9200, 6300, 8100, 8420 },
            LineColor = Color.FromArgb("#4ADE80")
        };
        CaloriesSparkline = new SparklineDrawable
        {
            Values = new double[] { 480, 590, 320, 710, 540, 620, 612 },
            LineColor = Color.FromArgb("#FB7185")
        };
        ActiveSparkline = new SparklineDrawable
        {
            Values = new double[] { 60, 90, 45, 110, 70, 95, 87 },
            LineColor = Color.FromArgb("#38BDF8")
        };
        HeartRateSparkline = new SparklineDrawable
        {
            Values = new double[] { 145, 162, 138, 170, 155, 148, 158 },
            LineColor = Color.FromArgb("#A78BFA")
        };

        Days = BuildStreakDays();
    }

    private static IReadOnlyList<DayStreakItem> BuildStreakDays()
    {
        var today = DateTime.Today;
        var items = new List<DayStreakItem>();
        var abbrs = new[] { "MÅN", "TIS", "ONS", "TOR", "FRE", "LÖR", "SÖN" };

        // Show current 7-day window ending today
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            int idx = ((int)day.DayOfWeek + 6) % 7; // Monday = 0
            bool isToday = i == 0;
            // Mock: completed last 3 days + today ongoing
            bool isCompleted = i >= 1 && i <= 3;
            bool isActive = isToday;

            items.Add(new DayStreakItem
            {
                DayAbbr = abbrs[idx],
                DayNum = day.Day.ToString(),
                IsCompleted = isCompleted,
                IsToday = isActive
            });
        }
        return items;
    }
}

public class DayStreakItem
{
    public string DayAbbr { get; init; } = "";
    public string DayNum { get; init; } = "";
    public bool IsCompleted { get; init; }
    public bool IsToday { get; init; }

    public Color CircleFill => IsCompleted
        ? Color.FromArgb("#1F4ADE80")
        : IsToday
            ? Color.FromArgb("#2A2A2A")
            : Color.FromArgb("#1A1A1A");

    public Color CircleStroke => IsCompleted
        ? Color.FromArgb("#4ADE80")
        : IsToday
            ? Color.FromArgb("#FBBF24")
            : Color.FromArgb("#2A2A2A");

    public Color DayNumColor => IsCompleted
        ? Color.FromArgb("#4ADE80")
        : IsToday
            ? Color.FromArgb("#FBBF24")
            : Color.FromArgb("#484848");

    public Color DayAbbrColor => IsToday
        ? Color.FromArgb("#A2A2A2")
        : Color.FromArgb("#383838");
}
