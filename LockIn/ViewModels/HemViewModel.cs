using CommunityToolkit.Mvvm.ComponentModel;
using LockIn;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class HemViewModel(DatabaseService db, IHealthService health) : ObservableObject
{
    private static bool _permissionsRequested;

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
    [ObservableProperty] private IReadOnlyList<MuscleTrendItem> _muscleTrends = Array.Empty<MuscleTrendItem>();
    [ObservableProperty] private double[] _stepsValues     = [];
    [ObservableProperty] private double[] _caloriesValues  = [];
    [ObservableProperty] private double[] _activeValues    = [];
    [ObservableProperty] private double[] _heartRateValues = [];

    // Strain / Recovery / Sleep-ringar
    [ObservableProperty] private float  _strainProgress;
    [ObservableProperty] private string _strainText   = "–";
    [ObservableProperty] private float  _recoveryProgress;
    [ObservableProperty] private string _recoveryText = "–";
    [ObservableProperty] private string _recoveryComponentsText = "";
    [ObservableProperty] private SleepStages _sleepStages = new(0, 0, 0, 0, 0, 0);
    [ObservableProperty] private string _coreSleepText  = "–";
    [ObservableProperty] private string _deepSleepText  = "–";
    [ObservableProperty] private string _remSleepText   = "–";
    [ObservableProperty] private string _awakeSleepText = "–";
    [ObservableProperty] private string _todayRecommendation       = AppResources.Hem_Loading_Data;
    [ObservableProperty] private string _todayRecommendationDetail = "";
    [ObservableProperty] private float  _strainTarget;
    [ObservableProperty] private string _strainTargetText = "–";
    [ObservableProperty] private float  _sleepProgress;
    [ObservableProperty] private string _sleepText    = "–";

    // Statusindikatorer — kontext för mätvärden (OPTIMAL / BRA / MEDEL / LÅGT etc.)
    [ObservableProperty] private string _recoveryStatusText = "";
    [ObservableProperty] private string _sleepStatusText    = "";

    // Accessibility-labels för SkiaSharp-kontroller (VoiceOver)
    [ObservableProperty] private string _gaugeAccessibilityLabel    = AppResources.Hem_Accessibility_GaugeLoading;
    [ObservableProperty] private string _strainAccessibilityLabel   = AppResources.Hem_Accessibility_StrainLoading;
    [ObservableProperty] private string _recoveryAccessibilityLabel = AppResources.Hem_Accessibility_RecoveryLoading;
    [ObservableProperty] private string _sleepAccessibilityLabel    = AppResources.Hem_Accessibility_SleepLoading;

    public float GaugeProgress => (float)(TrainingScore / 100.0);

    [ObservableProperty] private string _userName = "";
    public string UserInitial => UserName.Length > 0 ? UserName[0].ToString().ToUpper() : "?";

    public string Greeting
    {
        get
        {
            int h = DateTime.Now.Hour;
            if (h < 10) return AppResources.Hem_Greeting_Morning;
            if (h < 13) return AppResources.Hem_Greeting_Forenoon;
            if (h < 18) return AppResources.Hem_Greeting_Afternoon;
            return AppResources.Hem_Greeting_Evening;
        }
    }

    public string GreetingText => UserName.Length > 0 ? $"{Greeting}, {UserName.Split(' ')[0].ToUpper()}!" : $"{Greeting}!";

    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<CoachChip> _coachChips = new();
    [ObservableProperty] private bool _hasCoachChips;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            if (!_permissionsRequested)
            {
                _permissionsRequested = true;
                await health.RequestPermissionsAsync();
            }

            var weekStart    = GetMondayThisWeek();
            var sevenAgo     = DateTime.Today.AddDays(-6);
            var fortyFiveAgo = DateTime.Today.AddDays(-45);

            var weekSessionsTask   = db.GetCompletedSessionsInRangeAsync(weekStart, DateTime.Now);
            var recentSessionsTask = db.GetCompletedSessionsInRangeAsync(sevenAgo, DateTime.Now);
            var streakSessionsTask = db.GetCompletedSessionsInRangeAsync(fortyFiveAgo, DateTime.Now);
            var settingsTask       = db.GetAppSettingsAsync();
            var stepsTask          = health.GetTodayStepsAsync();
            var caloriesTask       = health.GetTodayActiveCaloriesAsync();
            var heartRateTask      = health.GetTodayMaxHeartRateAsync();
            var weeklyStepsTask    = health.GetWeeklyStepsAsync();
            var weeklyCaloriesTask = health.GetWeeklyCaloriesAsync();
            var weeklyMaxHRTask    = health.GetWeeklyMaxHeartRateAsync();
            var sleepHoursTask     = health.GetSleepHoursLastNightAsync();
            var sleepStagesTask    = health.GetSleepStagesLastNightAsync();
            var hrvTask            = health.GetHrvSampleAsync();
            var rhrTask            = health.GetRestingHrSampleAsync();
            var hrSamplesTask      = health.GetTodayHeartRateSamplesAsync();
            var maxHrTask          = health.GetEstimatedMaxHeartRateAsync();

            // Volym × intensitet idag + ACWR-fönster (acute = senaste 7d inkl idag, chronic = 28d).
            // ACWR (Acute:Chronic Workload Ratio) är en etablerad metod inom sport science
            // för att bedöma träningsbelastning vs kapacitet — sweet spot ~0.8–1.3, risk >1.5.
            var today        = DateTime.Today;
            var tomorrow     = today.AddDays(1);
            var volTodayTask = db.GetVolumeIntensityForRangeAsync(today, tomorrow);
            var volAcuteTask = db.GetVolumeIntensityForRangeAsync(today.AddDays(-6), tomorrow);   // 7d
            var volChrTask   = db.GetVolumeIntensityForRangeAsync(today.AddDays(-27), tomorrow); // 28d

            await Task.WhenAll(weekSessionsTask, recentSessionsTask, streakSessionsTask, settingsTask,
                stepsTask, caloriesTask, heartRateTask,
                weeklyStepsTask, weeklyCaloriesTask, weeklyMaxHRTask,
                sleepHoursTask, sleepStagesTask, hrvTask, rhrTask,
                hrSamplesTask, maxHrTask,
                volTodayTask, volAcuteTask, volChrTask);

            var weekSessions   = weekSessionsTask.Result;
            var recentSessions = recentSessionsTask.Result;
            var streakSessions = streakSessionsTask.Result;
            var settings       = settingsTask.Result;

            // Training score
            UserName = settings.UserName;
            OnPropertyChanged(nameof(UserInitial));
            OnPropertyChanged(nameof(GreetingText));

            int goal   = settings.WeeklyWorkoutGoal > 0 ? settings.WeeklyWorkoutGoal : 4;
            double score = Math.Min(100.0, weekSessions.Count / (double)goal * 100.0);
            TrainingScore     = score;
            TrainingScoreText = ((int)score).ToString();
            MotivationText    = BuildMotivationText((int)score);
            OnPropertyChanged(nameof(GaugeProgress));

            // Streak and calendar
            Days       = BuildStreakDays(streakSessions);
            int streak = CalculateStreak(streakSessions);
            StreakLabel = streak > 0
                ? string.Format(streak == 1 ? AppResources.Hem_StreakDays_One : AppResources.Hem_StreakDays_Many, streak)
                : AppResources.Hem_NoStreak;

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

            // Strain (Ansträngning) = HR-TRIMP + träningsvolym, log-mappad till 0–100.
            // Båda källorna summeras okappat och komprimeras via tanh — toppen är
            // asymptotisk (à la Whoop) så "ordinär hård dag" hamnar 60–75 och
            // exceptionella dagar 90+ men 100 är reserverat för verkligen extrema dagar.
            var hrSamples   = hrSamplesTask.Result;
            var estMaxHr    = maxHrTask.Result;
            var restingBpm  = rhrTask.Result.TodayBpm > 0
                ? (int)rhrTask.Result.TodayBpm
                : (rhrTask.Result.BaselineBpm > 0 ? (int)rhrTask.Result.BaselineBpm : 60);
            double hrStrainRaw  = CalculateStrainRaw(hrSamples, estMaxHr, restingBpm);
            double volStrainRaw = volTodayTask.Result / 100.0;
            double rawCombined  = hrStrainRaw + volStrainRaw;
            double strainPct    = 100.0 * Math.Tanh(rawCombined / 80.0);

            StrainProgress = (float)(strainPct / 100.0);
            bool hasStrainData = hrSamples.Count > 5 || volStrainRaw > 0;
            StrainText     = hasStrainData ? ((int)strainPct).ToString() : "–";

            var hrv   = hrvTask.Result;
            var rhr   = rhrTask.Result;
            var stages = sleepStagesTask.Result;
            double sleepH = sleepHoursTask.Result;

            // HRV-komponent: lnSDNN z-score mot 14-dagars baseline.
            // HRV är log-normalfördelad; CV ~20% är typisk inom-individ-variabilitet (Plews et al.).
            // z = (ln(today) − ln(baseline)) / 0.2, mappad linjärt till 0–100 runt z=0 → 50.
            double hrvComp;
            if (hrv.BaselineMs > 0 && hrv.TodayMs > 0)
            {
                double z = (Math.Log(hrv.TodayMs) - Math.Log(hrv.BaselineMs)) / 0.20;
                hrvComp = Math.Clamp(50 + z * 25, 0, 100); // z=+2 → 100, z=−2 → 0
            }
            else hrvComp = 50;

            // Vilopuls: lägre än baseline ⇒ bättre. Skala kring ±15% kring baseline.
            double rhrComp = rhr.BaselineBpm > 0 && rhr.TodayBpm > 0
                ? Math.Clamp((rhr.BaselineBpm / rhr.TodayBpm - 0.85) / 0.3, 0, 1) * 100
                : 50;

            // Sömn: viktad mot stadier — deep + REM är de regenerativa faserna.
            // 50% × stadier-score (4h deep+REM = max) + 50% × total-tid-score (8h = max).
            double sleepComp;
            if (stages.TotalHours > 0)
            {
                double deepRemH = (stages.DeepMinutes + stages.RemMinutes) / 60.0;
                double stageScore = Math.Clamp(deepRemH / 4.0, 0, 1) * 100;
                double totalScore = Math.Clamp(stages.TotalHours / 8.0, 0, 1) * 100;
                sleepComp = stageScore * 0.5 + totalScore * 0.5;
            }
            else if (sleepH > 0)
            {
                sleepComp = Math.Clamp(sleepH / 8.0, 0, 1) * 100;
            }
            else sleepComp = 50;

            // Fatigue: ACWR (acute 7d / chronic 28d-snitt). Sweet spot 0.8–1.3, risk >1.5.
            // Skalar till 0–100 där hög ACWR = lågt score = mer trötthet.
            double acute = volAcuteTask.Result / 7.0;
            double chronicAvg = volChrTask.Result / 28.0;
            double acwr = chronicAvg > 0 ? acute / chronicAvg : 0;
            // ACWR 1.0 = perfekt (100 fatigueComp), 0.8 = lite under-tränad, 1.5+ = ackumulerad trötthet
            double fatigueComp = chronicAvg < 50  // för lite historik för meningsfull ACWR
                ? 100 - Math.Clamp(volAcuteTask.Result / 700.0, 0, 1) * 100
                : Math.Clamp(100 - Math.Max(0, acwr - 1.0) * 80, 20, 100);

            double recoveryPct = hrvComp * 0.55 + rhrComp * 0.20 + sleepComp * 0.15 + fatigueComp * 0.10;
            RecoveryProgress = (float)(recoveryPct / 100.0);
            RecoveryText     = ((int)recoveryPct).ToString();
            RecoveryComponentsText = hrv.TodayMs > 0
                ? string.Format(AppResources.Hem_Recovery_Components, $"{hrv.TodayMs:F0}", $"{rhr.TodayBpm:F0}", $"{sleepH:F1}")
                : AppResources.Hem_Recovery_NoWatch;

            SleepStages    = stages;
            CoreSleepText  = FormatSleepDuration(stages.CoreMinutes);
            DeepSleepText  = FormatSleepDuration(stages.DeepMinutes);
            RemSleepText   = FormatSleepDuration(stages.RemMinutes);
            AwakeSleepText = FormatSleepDuration(stages.AwakeMinutes);
            SleepProgress  = sleepH > 0 ? (float)Math.Clamp(sleepH / 8.0, 0.0, 1.0) : 0f;
            SleepText      = sleepH > 0 ? $"{sleepH:F1}h" : "–";

            // Strain-target = fryst morgon-recovery. Sätts en gång per dag vid första laddning
            // så att sjunkande vilopuls under dagen inte sänker målet.
            string todayKey = today.ToString("yyyy-MM-dd");
            double target   = (settings.MorningRecoveryDate == todayKey && settings.MorningRecoveryPct > 0)
                ? settings.MorningRecoveryPct
                : recoveryPct;
            if (settings.MorningRecoveryDate != todayKey || settings.MorningRecoveryPct <= 0)
            {
                settings.MorningRecoveryDate = todayKey;
                settings.MorningRecoveryPct  = recoveryPct;
                await db.SaveAppSettingsAsync(settings);
            }
            StrainTarget     = (float)(target / 100.0);
            StrainTargetText = string.Format(AppResources.Hem_StrainTarget_Format, (int)target);

            // Statusindikatorer
            RecoveryStatusText = recoveryPct switch
            {
                >= 80 => AppResources.Hem_Recovery_Optimal,
                >= 60 => AppResources.Hem_Recovery_Good,
                >= 40 => AppResources.Hem_Recovery_Medium,
                _     => AppResources.Hem_Recovery_Low
            };
            SleepStatusText = sleepH switch
            {
                >= 7.5 => AppResources.Hem_Sleep_Sufficient,
                >= 6.0 => AppResources.Hem_Sleep_OK,
                >= 1.0 => AppResources.Hem_Sleep_TooLittle,
                _      => "–"
            };

            // Accessibility-labels
            GaugeAccessibilityLabel    = string.Format(AppResources.Hem_Accessibility_Gauge, (int)score);
            StrainAccessibilityLabel   = hasStrainData
                ? string.Format(AppResources.Hem_Accessibility_Strain, (int)strainPct)
                : AppResources.Hem_Accessibility_StrainNoData;
            RecoveryAccessibilityLabel = string.Format(AppResources.Hem_Accessibility_Recovery, (int)recoveryPct);
            SleepAccessibilityLabel    = sleepH > 0
                ? string.Format(AppResources.Hem_Accessibility_Sleep, $"{sleepH:F1}")
                : AppResources.Hem_Accessibility_SleepNoData;

            var (recHead, recDetail) = BuildRecommendation(recoveryPct, recentSessions);
            TodayRecommendation       = recHead;
            TodayRecommendationDetail = recDetail;

            await LoadCoachChipsAsync(weekSessions, recentSessions, recoveryPct);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCoachChipsAsync(
        IReadOnlyList<WorkoutSession> weekSessions,
        IReadOnlyList<WorkoutSession> recentSessions,
        double recoveryPct)
    {
        try
        {
            var weekStart     = GetMondayThisWeek();
            var prevWeekStart = weekStart.AddDays(-7);
            var prevWeekEnd   = weekStart.AddTicks(-1);
            var tomorrow      = DateTime.Today.AddDays(1);
            var recentCutoff  = DateTime.Now.AddDays(-30);

            var prevWeekSessionsTask = db.GetCompletedSessionsInRangeAsync(prevWeekStart, prevWeekEnd);
            var thisWeekVolumeTask   = db.GetRawVolumeForRangeAsync(weekStart, tomorrow);
            var prevWeekVolumeTask   = db.GetRawVolumeForRangeAsync(prevWeekStart, weekStart);
            var nearestPRTask        = db.GetNearestPRGapAsync(recentCutoff);
            var weekStreakTask        = db.GetCurrentWeekStreakAsync();
            var muscleScoresTask     = db.GetMuscleScoresAsync();

            await Task.WhenAll(prevWeekSessionsTask, thisWeekVolumeTask,
                               prevWeekVolumeTask, nearestPRTask, weekStreakTask, muscleScoresTask);

            var prResult = nearestPRTask.Result;
            var lastSession = recentSessions
                .Where(s => s.CompletedAt.HasValue)
                .OrderByDescending(s => s.CompletedAt!.Value)
                .FirstOrDefault();
            int daysSinceLast = lastSession is not null
                ? (int)(DateTime.Now - lastSession.CompletedAt!.Value).TotalDays
                : 999;

            var ctx = new CoachContext(
                RecentSessions:        recentSessions,
                WeekSessions:          weekSessions,
                PrevWeekSessions:      prevWeekSessionsTask.Result,
                MuscleScores:          muscleScoresTask.Result,
                RecoveryPct:           recoveryPct,
                NearestPRGapKg:        prResult.HasValue ? prResult.Value.GapKg : null,
                NearestPRExerciseName: prResult.HasValue ? prResult.Value.ExerciseName : null,
                NearestPRRecentMaxKg:  prResult.HasValue ? prResult.Value.RecentMaxKg : 0,
                NearestPRAllTimeMaxKg: prResult.HasValue ? prResult.Value.AllTimeMaxKg : 0,
                DaysSinceLastWorkout:  daysSinceLast,
                ThisWeekVolumeKg:      thisWeekVolumeTask.Result,
                PrevWeekVolumeKg:      prevWeekVolumeTask.Result,
                WeekStreak:            weekStreakTask.Result
            );

            var chips = CoachPromptEngine.Evaluate(ctx);
            CoachChips.Clear();
            foreach (var chip in chips)
                CoachChips.Add(chip);
            HasCoachChips = CoachChips.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CoachChips] LoadCoachChipsAsync failed: {ex.Message}");
            HasCoachChips = false;
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
        var scores    = await db.GetMuscleScoresAsync();
        var frequency = await db.GetMuscleFrequencyAsync(4);
        var weeklyVol = await db.GetWeeklyVolumeByMuscleGroupAsync(4);
        var muscles = new (MuscleGroup mg, string name)[]
        {
            (MuscleGroup.Chest,     AppResources.Train_Muscle_Chest),
            (MuscleGroup.Back,      AppResources.Train_Muscle_Back),
            (MuscleGroup.Shoulders, AppResources.Train_Muscle_Shoulders),
            (MuscleGroup.Biceps,    AppResources.Train_Muscle_Biceps),
            (MuscleGroup.Triceps,   AppResources.Train_Muscle_Triceps),
            (MuscleGroup.Legs,      AppResources.Train_Muscle_Legs),
            (MuscleGroup.Core,      AppResources.Train_Muscle_Core),
        };
        HeatmapItems = muscles.Select(m =>
        {
            var score = scores.TryGetValue(m.mg, out var s) ? s : 0.0;
            var t = score / 10.0;
            var count = frequency.TryGetValue(m.mg, out var c) ? c : 0;
            var freqPerWeek = count / 4.0;
            var freqText = count == 0 ? ""
                : freqPerWeek % 1 < 0.05 ? $"{(int)Math.Round(freqPerWeek)}/v"
                : $"{freqPerWeek:F1}/v";
            return new HeatmapTile
            {
                Name          = m.name,
                Score         = score,
                TileColor     = DesignTokens.HeatmapTile(t),
                TextColor     = DesignTokens.HeatmapText(t),
                FrequencyText = freqText,
            };
        }).ToList();

        MuscleTrends = muscles
            .Where(m => weeklyVol.ContainsKey(m.mg))
            .OrderByDescending(m => weeklyVol[m.mg].Sum())
            .Select(m => new MuscleTrendItem
            {
                Name   = m.name,
                Color  = DesignTokens.MuscleColor(m.mg),
                Values = weeklyVol[m.mg],
            })
            .ToList();
    }

    public int TodayIndex { get; private set; } = 45;

    private static string BuildMotivationText(int score) => score switch
    {
        >= 100 => AppResources.Hem_Motivation_Complete,
        >= 75  => string.Format(AppResources.Hem_Motivation_Strong,  score),
        >= 50  => string.Format(AppResources.Hem_Motivation_Halfway, score),
        >= 25  => string.Format(AppResources.Hem_Motivation_Going,   score),
        > 0    => string.Format(AppResources.Hem_Motivation_Started, score),
        _      => AppResources.Hem_Motivation_New,
    };

    private IReadOnlyList<DayStreakItem> BuildStreakDays(List<WorkoutSession> sessions)
    {
        const int daysBack    = 45;
        const int daysForward = 6;

        var today = DateTime.Today;
        var completedDates = sessions
            .Where(s => s.CompletedAt.HasValue)
            .Select(s => s.CompletedAt!.Value.Date)
            .ToHashSet();

        var abbrs = new[]
        {
            AppResources.Hem_Weekday_Mon,
            AppResources.Hem_Weekday_Tue,
            AppResources.Hem_Weekday_Wed,
            AppResources.Hem_Weekday_Thu,
            AppResources.Hem_Weekday_Fri,
            AppResources.Hem_Weekday_Sat,
            AppResources.Hem_Weekday_Sun
        };
        var items = new List<DayStreakItem>(daysBack + 1 + daysForward);

        for (int i = -daysBack; i <= daysForward; i++)
        {
            var day          = today.AddDays(i);
            int idx          = ((int)day.DayOfWeek + 6) % 7;
            bool isToday     = i == 0;
            bool isFuture    = i > 0;
            bool isCompleted = !isFuture && completedDates.Contains(day.Date);

            items.Add(new DayStreakItem
            {
                DayAbbr     = abbrs[idx],
                DayNum      = day.Day.ToString(),
                IsCompleted = isCompleted,
                IsToday     = isToday
            });
        }

        TodayIndex = daysBack;
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

    /// <summary>Formaterar sömnstadier. Över 60 min ⇒ "1h 23m"; annars "47m".</summary>
    private static string FormatSleepDuration(double minutes)
    {
        if (minutes < 1) return "0m";
        if (minutes < 60) return $"{minutes:F0}m";
        return $"{minutes / 60.0:F1}h";
    }

    private static int CalculateActiveMinutesToday(List<WorkoutSession> sessions)
    {
        var today = DateTime.Today;
        return (int)sessions
            .Where(s => s.CompletedAt.HasValue && s.StartedAt.Date == today)
            .Sum(s => (s.CompletedAt!.Value - s.StartedAt).TotalMinutes);
    }

    /// <summary>
    /// Edwards TRIMP-inspirerad raw Strain — viktar tid i 5 HR-zoner.
    /// Karvonen-formel för %-of-Reserve: (HR - HRrest) / (HRmax - HRrest).
    /// Zon 1 (50-60% reserve) × 1, Zon 2 × 2, ... Zon 5 × 5.
    /// Returnerar okappade poäng dividerade med 3 — komprimering till 0–100
    /// sker via tanh-mappning i LoadAsync så toppen blir asymptotisk.
    /// </summary>
    private static double CalculateStrainRaw(IReadOnlyList<HeartRateSample> samples, int maxHr, int restingHr)
    {
        if (samples.Count < 2 || maxHr <= restingHr) return 0;

        var ordered  = samples.OrderBy(s => s.Time).ToList();
        var reserve  = (double)(maxHr - restingHr);
        double points = 0;

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var minutes = (ordered[i + 1].Time - ordered[i].Time).TotalMinutes;
            // Hoppa över luckor större än 5 min (HR-data sannolikt inte representativ)
            if (minutes <= 0 || minutes > 5) continue;

            var pctReserve = (ordered[i].Bpm - restingHr) / reserve;
            int multiplier = pctReserve switch
            {
                < 0.5 => 0,    // Under Zon 1 = återhämtning, ingen strain
                < 0.6 => 1,    // Zon 1
                < 0.7 => 2,    // Zon 2
                < 0.8 => 3,    // Zon 3
                < 0.9 => 4,    // Zon 4
                _     => 5     // Zon 5
            };
            points += minutes * multiplier;
        }

        // Returnera okappade poäng / 3 (typisk hård cardio-timme ≈ 100, monster-pass kan gå >150).
        return points / 3.0;
    }

    private static (string Headline, string Detail) BuildRecommendation(
        double recoveryPct,
        IReadOnlyList<WorkoutSession> recentSessions)
    {
        var completed = recentSessions
            .Where(s => s.CompletedAt.HasValue)
            .OrderByDescending(s => s.CompletedAt!.Value)
            .ToList();

        var lastSession = completed.FirstOrDefault();
        double hoursSinceLast = lastSession is not null
            ? (DateTime.Now - lastSession.CompletedAt!.Value).TotalHours
            : double.MaxValue;

        // Nyss tränat (< 3 timmar)
        if (hoursSinceLast < 3)
            return (AppResources.Hem_Rec_JustTrained_Head, AppResources.Hem_Rec_JustTrained_Body);

        // Långt uppehåll (5+ dagar sedan senaste pass)
        if (hoursSinceLast > 120)
            return (AppResources.Hem_Rec_LongBreak_Head, AppResources.Hem_Rec_LongBreak_Body);

        // Konsekutiva dagar med låg återhämtning
        int streak = CountConsecutiveTrainingDays(completed);
        if (streak >= 3 && recoveryPct < 50)
            return (AppResources.Hem_Rec_PlanRest_Head,
                string.Format(AppResources.Hem_Rec_PlanRest_Body, streak));

        // Basscenario på återhämtning
        string headline = recoveryPct switch
        {
            < 33 => AppResources.Hem_Rec_Head_RestPriority,
            < 50 => AppResources.Hem_Rec_Head_LightMove,
            < 66 => AppResources.Hem_Rec_Head_NormalSession,
            < 85 => AppResources.Hem_Rec_Head_GoHard,
            _    => AppResources.Hem_Rec_Head_PeakForm,
        };

        string body = recoveryPct switch
        {
            < 33 => AppResources.Hem_Rec_Body_RestPriority,
            < 50 => AppResources.Hem_Rec_Body_LightMove,
            < 66 => AppResources.Hem_Rec_Body_NormalSession,
            < 85 => AppResources.Hem_Rec_Body_GoHard,
            _    => AppResources.Hem_Rec_Body_PeakForm,
        };

        if (lastSession is not null && hoursSinceLast < 48 && recoveryPct >= 50)
            body += AppResources.Hem_Rec_DifferentMuscle;

        return (headline, body);
    }

    private static int CountConsecutiveTrainingDays(IReadOnlyList<WorkoutSession> orderedSessions)
    {
        if (orderedSessions.Count == 0) return 0;
        var uniqueDays = orderedSessions
            .Select(s => s.CompletedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();
        int count = 0;
        for (int i = 0; i < uniqueDays.Count; i++)
        {
            if (uniqueDays[i] == DateTime.Today.AddDays(-i))
                count++;
            else
                break;
        }
        return count;
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

public class MuscleTrendItem
{
    public string Name { get; set; } = "";
    public Color Color { get; set; } = Colors.White;
    public double[] Values { get; set; } = [];
}
