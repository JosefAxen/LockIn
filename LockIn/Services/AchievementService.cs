using LockIn.Models;
using LockIn.Resources.Strings;

namespace LockIn.Services;

public static class AchievementService
{
    public record AchievementDef(AchievementId Id, string Emoji, string Title, string Description);

    public static readonly IReadOnlyList<AchievementDef> All = new[]
    {
        new AchievementDef(AchievementId.FirstWorkout,        "🏋️", AppResources.Achievement_FirstWorkout_Title,        AppResources.Achievement_FirstWorkout_Description),
        new AchievementDef(AchievementId.Sessions5,           "🔥", AppResources.Achievement_Sessions5_Title,           AppResources.Achievement_Sessions5_Description),
        new AchievementDef(AchievementId.Sessions10,          "⚡", AppResources.Achievement_Sessions10_Title,          AppResources.Achievement_Sessions10_Description),
        new AchievementDef(AchievementId.Sessions25,          "💪", AppResources.Achievement_Sessions25_Title,          AppResources.Achievement_Sessions25_Description),
        new AchievementDef(AchievementId.Sessions50,          "🎯", AppResources.Achievement_Sessions50_Title,          AppResources.Achievement_Sessions50_Description),
        new AchievementDef(AchievementId.Sessions100,         "🏆", AppResources.Achievement_Sessions100_Title,         AppResources.Achievement_Sessions100_Description),
        new AchievementDef(AchievementId.WeekStreak1,         "📅", AppResources.Achievement_WeekStreak1_Title,         AppResources.Achievement_WeekStreak1_Description),
        new AchievementDef(AchievementId.WeekStreak4,         "🗓️", AppResources.Achievement_WeekStreak4_Title,        AppResources.Achievement_WeekStreak4_Description),
        new AchievementDef(AchievementId.WeekStreak12,        "🔩", AppResources.Achievement_WeekStreak12_Title,        AppResources.Achievement_WeekStreak12_Description),
        new AchievementDef(AchievementId.FirstPR,             "⭐", AppResources.Achievement_FirstPR_Title,             AppResources.Achievement_FirstPR_Description),
        new AchievementDef(AchievementId.PR10,                "🎖️", AppResources.Achievement_PR10_Title,              AppResources.Achievement_PR10_Description),
        new AchievementDef(AchievementId.PR50,                "🥇", AppResources.Achievement_PR50_Title,               AppResources.Achievement_PR50_Description),
        new AchievementDef(AchievementId.TotalVolume100k,     "💯", AppResources.Achievement_TotalVolume100k_Title,    AppResources.Achievement_TotalVolume100k_Description),
        new AchievementDef(AchievementId.TotalVolume500k,     "💎", AppResources.Achievement_TotalVolume500k_Title,    AppResources.Achievement_TotalVolume500k_Description),
        new AchievementDef(AchievementId.TotalVolume1M,       "👑", AppResources.Achievement_TotalVolume1M_Title,      AppResources.Achievement_TotalVolume1M_Description),
        new AchievementDef(AchievementId.AllMuscleGroups,     "🌟", AppResources.Achievement_AllMuscleGroups_Title,    AppResources.Achievement_AllMuscleGroups_Description),
        new AchievementDef(AchievementId.LongSession,         "⏳", AppResources.Achievement_LongSession_Title,        AppResources.Achievement_LongSession_Description),
        new AchievementDef(AchievementId.EarlyBird,           "🌅", AppResources.Achievement_EarlyBird_Title,          AppResources.Achievement_EarlyBird_Description),
        new AchievementDef(AchievementId.NightOwl,            "🦉", AppResources.Achievement_NightOwl_Title,           AppResources.Achievement_NightOwl_Description),
        new AchievementDef(AchievementId.FirstCustomExercise, "🔧", AppResources.Achievement_FirstCustomExercise_Title, AppResources.Achievement_FirstCustomExercise_Description),
    };

    public static AchievementDef? Get(AchievementId id) =>
        All.FirstOrDefault(a => a.Id == id);
}
