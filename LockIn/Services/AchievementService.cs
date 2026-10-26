using LockIn.Models;
using LockIn.Resources.Strings;

namespace LockIn.Services;

public static class AchievementService
{
    /// <summary>
    /// Emoji = kort textsymbol (siffra/förkortning), ingen emoji. Namnet behålls eftersom
    /// PostWorkoutViewModel läser def.Emoji. Titel och beskrivning anger sakligt vad som uppnåtts.
    /// </summary>
    public record AchievementDef(AchievementId Id, string Emoji, string Title, string Description);

    public static readonly IReadOnlyList<AchievementDef> All = new[]
    {
        new AchievementDef(AchievementId.FirstWorkout,        "1",      AppResources.Achievement_FirstWorkout_Title,        AppResources.Achievement_FirstWorkout_Description),
        new AchievementDef(AchievementId.Sessions5,           "5",      AppResources.Achievement_Sessions5_Title,           AppResources.Achievement_Sessions5_Description),
        new AchievementDef(AchievementId.Sessions10,          "10",     AppResources.Achievement_Sessions10_Title,          AppResources.Achievement_Sessions10_Description),
        new AchievementDef(AchievementId.Sessions25,          "25",     AppResources.Achievement_Sessions25_Title,          AppResources.Achievement_Sessions25_Description),
        new AchievementDef(AchievementId.Sessions50,          "50",     AppResources.Achievement_Sessions50_Title,          AppResources.Achievement_Sessions50_Description),
        new AchievementDef(AchievementId.Sessions100,         "100",    AppResources.Achievement_Sessions100_Title,         AppResources.Achievement_Sessions100_Description),
        new AchievementDef(AchievementId.WeekStreak1,         "2 V",    AppResources.Achievement_WeekStreak1_Title,         AppResources.Achievement_WeekStreak1_Description),
        new AchievementDef(AchievementId.WeekStreak4,         "4 V",    AppResources.Achievement_WeekStreak4_Title,         AppResources.Achievement_WeekStreak4_Description),
        new AchievementDef(AchievementId.WeekStreak12,        "12 V",   AppResources.Achievement_WeekStreak12_Title,        AppResources.Achievement_WeekStreak12_Description),
        new AchievementDef(AchievementId.FirstPR,             "PR",     AppResources.Achievement_FirstPR_Title,             AppResources.Achievement_FirstPR_Description),
        new AchievementDef(AchievementId.PR10,                "10 PR",  AppResources.Achievement_PR10_Title,                AppResources.Achievement_PR10_Description),
        new AchievementDef(AchievementId.PR50,                "50 PR",  AppResources.Achievement_PR50_Title,                AppResources.Achievement_PR50_Description),
        new AchievementDef(AchievementId.TotalVolume100k,     "100 T",  AppResources.Achievement_TotalVolume100k_Title,     AppResources.Achievement_TotalVolume100k_Description),
        new AchievementDef(AchievementId.TotalVolume500k,     "500 T",  AppResources.Achievement_TotalVolume500k_Title,     AppResources.Achievement_TotalVolume500k_Description),
        new AchievementDef(AchievementId.TotalVolume1M,       "1000 T", AppResources.Achievement_TotalVolume1M_Title,       AppResources.Achievement_TotalVolume1M_Description),
        new AchievementDef(AchievementId.AllMuscleGroups,     "ALLA",   AppResources.Achievement_AllMuscleGroups_Title,     AppResources.Achievement_AllMuscleGroups_Description),
        new AchievementDef(AchievementId.LongSession,         "90+",    AppResources.Achievement_LongSession_Title,         AppResources.Achievement_LongSession_Description),
        new AchievementDef(AchievementId.EarlyBird,           "<07",    AppResources.Achievement_EarlyBird_Title,           AppResources.Achievement_EarlyBird_Description),
        new AchievementDef(AchievementId.NightOwl,            "21+",    AppResources.Achievement_NightOwl_Title,            AppResources.Achievement_NightOwl_Description),
        new AchievementDef(AchievementId.FirstCustomExercise, "+",      AppResources.Achievement_FirstCustomExercise_Title, AppResources.Achievement_FirstCustomExercise_Description),
    };

    public static AchievementDef? Get(AchievementId id) =>
        All.FirstOrDefault(a => a.Id == id);
}
