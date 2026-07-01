using LockIn.Services;

namespace LockIn.Models;

public record CoachContext(
    IReadOnlyList<WorkoutSession> RecentSessions,
    IReadOnlyList<WorkoutSession> WeekSessions,
    IReadOnlyList<WorkoutSession> PrevWeekSessions,
    IReadOnlyDictionary<MuscleGroup, double> MuscleScores,
    double RecoveryPct,
    double? NearestPRGapKg,
    string? NearestPRExerciseName,
    double NearestPRRecentMaxKg,
    double NearestPRAllTimeMaxKg,
    int DaysSinceLastWorkout,
    double ThisWeekVolumeKg,
    double PrevWeekVolumeKg,
    int WeekStreak,
    IReadOnlyList<VolumeAdvice> VolumeAdvices,
    DeloadAdvice? DeloadAdvice
);
