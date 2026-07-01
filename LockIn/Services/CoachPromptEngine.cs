// LockIn/Services/CoachPromptEngine.cs
using LockIn.Models;
using LockIn.Resources.Strings;
using Microsoft.Maui.Storage;

namespace LockIn.Services;

public static class CoachPromptEngine
{
    private const int MaxChips = 2;

    // Prioritetsordning för muskelgap-chip: tyngre grupper visas hellre
    private static readonly MuscleGroup[] s_muscleGapPriority =
    [
        MuscleGroup.Legs, MuscleGroup.Back, MuscleGroup.Chest,
        MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps,
        MuscleGroup.Core, MuscleGroup.Forearms
    ];

    public static IReadOnlyList<CoachChip> Evaluate(CoachContext ctx)
    {
        var candidates = new List<(int Priority, CoachChip Chip, TimeSpan Cooldown)>();

        // ── Chip 0a: Kalender-baserad deload (aktiv cykel-vecka är deload) ─
        if (ctx.IsCycleDeloadWeek)
        {
            var chip = new CoachChip(
                PromptId: "deload-cycle-week",
                ChipText: AppResources.CoachChip_DeloadCycle_Text,
                DetailHeader: AppResources.CoachChip_DeloadCycle_Header,
                DetailBody: AppResources.CoachChip_DeloadCycle_Body);
            candidates.Add((0, chip, TimeSpan.FromDays(3)));
        }

        // ── Chip 0b: Data-driven deload-rekommendation ───────────────────
        if (ctx.DeloadAdvice is not null)
        {
            var chip = new CoachChip(
                PromptId: "deload-recommendation",
                ChipText: AppResources.CoachChip_Deload_Text,
                DetailHeader: AppResources.CoachChip_Deload_Header,
                DetailBody: AppResources.CoachChip_Deload_Body);
            candidates.Add((0, chip, TimeSpan.FromDays(7)));
        }

        // ── Chip 1: PR-proximity ─────────────────────────────────────────
        if (ctx.NearestPRGapKg is > 0 and <= 10 && ctx.NearestPRExerciseName is not null)
        {
            var chip = new CoachChip(
                PromptId: "pr-proximity",
                ChipText: string.Format(AppResources.Hem_Chip_PRProximity_Text,
                    ctx.NearestPRExerciseName, ctx.NearestPRGapKg.Value),
                DetailHeader: string.Format(AppResources.Hem_Chip_PRProximity_Header,
                    ctx.NearestPRExerciseName),
                DetailBody: string.Format(AppResources.Hem_Chip_PRProximity_Body,
                    ctx.NearestPRExerciseName,
                    ctx.NearestPRGapKg.Value,
                    ctx.NearestPRRecentMaxKg,
                    ctx.NearestPRAllTimeMaxKg));
            candidates.Add((1, chip, TimeSpan.FromHours(24)));
        }

        // ── Chip 2: Muskelgap ────────────────────────────────────────────
        foreach (var mg in s_muscleGapPriority)
        {
            if (!ctx.MuscleScores.TryGetValue(mg, out var score) || score > 0.0)
                continue;
            var muscleName = MuscleDisplayName(mg);
            if (string.IsNullOrEmpty(muscleName)) continue;

            var chip = new CoachChip(
                PromptId: $"muscle-gap-{mg.ToString().ToLowerInvariant()}",
                ChipText: string.Format(AppResources.Hem_Chip_MuscleGap_Text, muscleName),
                DetailHeader: string.Format(AppResources.Hem_Chip_MuscleGap_Header, muscleName),
                DetailBody: string.Format(AppResources.Hem_Chip_MuscleGap_Body, muscleName));
            candidates.Add((2, chip, TimeSpan.FromHours(48)));
            break; // bara en muskelgap-chip
        }

        // ── Chip 1.5: Volume-advice per muskelgrupp ──────────────────────
        foreach (var advice in ctx.VolumeAdvices)
        {
            var muscleName = MuscleDisplayName(advice.Muscle);
            if (string.IsNullOrEmpty(muscleName)) continue;

            string chipText, header, body;
            if (advice.SetDelta > 0)
            {
                chipText = string.Format(AppResources.CoachChip_VolumeUp_Text_Format, muscleName);
                header   = string.Format(AppResources.CoachChip_VolumeUp_Header_Format, muscleName);
                body     = string.Format(AppResources.CoachChip_VolumeUp_Body_Format, muscleName);
            }
            else
            {
                chipText = string.Format(AppResources.CoachChip_VolumeDown_Text_Format, muscleName);
                header   = string.Format(AppResources.CoachChip_VolumeDown_Header_Format, muscleName);
                body     = string.Format(AppResources.CoachChip_VolumeDown_Body_Format, muscleName);
            }

            var chip = new CoachChip(
                PromptId: $"volume-advice-{advice.Muscle.ToString().ToLowerInvariant()}-{(advice.SetDelta > 0 ? "up" : "down")}",
                ChipText: chipText,
                DetailHeader: header,
                DetailBody: body);
            candidates.Add((2, chip, TimeSpan.FromDays(5)));
        }

        // ── Chip 3: Volymtrend ───────────────────────────────────────────
        if (ctx.PrevWeekVolumeKg > 0)
        {
            var delta = ctx.ThisWeekVolumeKg - ctx.PrevWeekVolumeKg;
            var pct = delta / ctx.PrevWeekVolumeKg;
            if (Math.Abs(pct) > 0.15)
            {
                var absPct = (int)Math.Round(Math.Abs(pct) * 100);
                var chipText = pct > 0
                    ? string.Format(AppResources.Hem_Chip_VolumeTrendUp_Text, absPct)
                    : string.Format(AppResources.Hem_Chip_VolumeTrendDown_Text, absPct);
                var chip = new CoachChip(
                    PromptId: "volume-trend",
                    ChipText: chipText,
                    DetailHeader: AppResources.Hem_Chip_VolumeTrend_Header,
                    DetailBody: string.Format(AppResources.Hem_Chip_VolumeTrend_Body,
                        ctx.ThisWeekVolumeKg, ctx.PrevWeekVolumeKg));
                candidates.Add((3, chip, TimeSpan.FromHours(48)));
            }
        }

        // ── Chip 4: Veckosammanfattning ──────────────────────────────────
        var today = DateTime.Today;
        var dayOfWeek = ((int)today.DayOfWeek + 6) % 7; // 0=Mån, 6=Sön
        if (dayOfWeek >= 3 && ctx.WeekSessions.Count >= 2) // tors–sön
        {
            var chip = new CoachChip(
                PromptId: "week-summary",
                ChipText: string.Format(AppResources.Hem_Chip_WeekSummary_Text,
                    ctx.WeekSessions.Count, ctx.ThisWeekVolumeKg),
                DetailHeader: AppResources.Hem_Chip_WeekSummary_Header,
                DetailBody: string.Format(AppResources.Hem_Chip_WeekSummary_Body,
                    ctx.WeekSessions.Count, ctx.ThisWeekVolumeKg));
            candidates.Add((4, chip, TimeSpan.FromHours(24)));
        }

        // ── Chip 5: Veckostreak ──────────────────────────────────────────
        if (ctx.WeekStreak >= 2)
        {
            var chip = new CoachChip(
                PromptId: "streak-weeks",
                ChipText: string.Format(AppResources.Hem_Chip_StreakWeeks_Text, ctx.WeekStreak),
                DetailHeader: AppResources.Hem_Chip_StreakWeeks_Header,
                DetailBody: string.Format(AppResources.Hem_Chip_StreakWeeks_Body, ctx.WeekStreak));
            candidates.Add((5, chip, TimeSpan.FromHours(72)));
        }

        // ── Filtrera på cooldown, sortera på prioritet, returnera max 2 ──
        var now = DateTime.UtcNow;
        return candidates
            .Where(c => !IsOnCooldown(c.Chip.PromptId, c.Cooldown, now))
            .OrderBy(c => c.Priority)
            .Take(MaxChips)
            .Select(c => c.Chip)
            .ToList();
    }

    public static void MarkShown(string promptId)
    {
        Preferences.Set($"coach_chip_{promptId}_shown_at", DateTime.UtcNow.ToString("O"));
    }

    private static bool IsOnCooldown(string promptId, TimeSpan cooldown, DateTime now)
    {
        var key = $"coach_chip_{promptId}_shown_at";
        if (!Preferences.ContainsKey(key)) return false;
        var raw = Preferences.Get(key, "");
        if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lastShown))
            return false;
        return now - lastShown < cooldown;
    }

    private static string MuscleDisplayName(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => AppResources.Train_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Train_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Train_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Train_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Train_Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Train_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Train_Muscle_Core,
        MuscleGroup.Forearms  => AppResources.Train_Muscle_Forearms,
        _                     => ""
    };
}
