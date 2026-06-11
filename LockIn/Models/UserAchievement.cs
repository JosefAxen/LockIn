using SQLite;

namespace LockIn.Models;

public enum AchievementId
{
    FirstWorkout = 0,
    Sessions5 = 1,
    Sessions10 = 2,
    Sessions25 = 3,
    Sessions50 = 4,
    Sessions100 = 5,
    WeekStreak1 = 6,
    WeekStreak4 = 7,
    WeekStreak12 = 8,
    FirstPR = 9,
    PR10 = 10,
    PR50 = 11,
    TotalVolume100k = 12,
    TotalVolume500k = 13,
    TotalVolume1M = 14,
    AllMuscleGroups = 15,
    LongSession = 16,
    EarlyBird = 17,
    NightOwl = 18,
    FirstCustomExercise = 19,
}

[Table("UserAchievements")]
public class UserAchievement
{
    [PrimaryKey]
    public int Id { get; set; }

    public DateTime UnlockedAt { get; set; }
}
