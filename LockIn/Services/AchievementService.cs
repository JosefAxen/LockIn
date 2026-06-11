using LockIn.Models;

namespace LockIn.Services;

public static class AchievementService
{
    public record AchievementDef(AchievementId Id, string Emoji, string Title, string Description);

    public static readonly IReadOnlyList<AchievementDef> All = new[]
    {
        new AchievementDef(AchievementId.FirstWorkout,       "🏋️", "Första steget",      "Du klarade ditt allra första pass!"),
        new AchievementDef(AchievementId.Sessions5,          "🔥", "Varm i kläderna",    "5 pass avklarade."),
        new AchievementDef(AchievementId.Sessions10,         "⚡", "I rullning",          "10 pass avklarade."),
        new AchievementDef(AchievementId.Sessions25,         "💪", "Vana",                "25 pass avklarade — nu sitter det!"),
        new AchievementDef(AchievementId.Sessions50,         "🎯", "Dedikerad",           "50 pass avklarade."),
        new AchievementDef(AchievementId.Sessions100,        "🏆", "LockIn Legend",       "100 pass avklarade. Legendarisk."),
        new AchievementDef(AchievementId.WeekStreak1,        "📅", "Aktiv vecka",         "Tränat minst en gång två veckor i rad."),
        new AchievementDef(AchievementId.WeekStreak4,        "🗓️", "En månad stark",     "4 veckor i rad med träning."),
        new AchievementDef(AchievementId.WeekStreak12,       "🔩", "Järnvilja",           "12 veckor i rad — tre månader!"),
        new AchievementDef(AchievementId.FirstPR,            "⭐", "Nytt rekord",         "Du slog ditt första personliga rekord!"),
        new AchievementDef(AchievementId.PR10,               "🎖️", "PR-maskin",          "10 personliga rekord totalt."),
        new AchievementDef(AchievementId.PR50,               "🥇", "Rekordjägare",        "50 personliga rekord totalt."),
        new AchievementDef(AchievementId.TotalVolume100k,    "💯", "100-tonslyftet",      "Du har totalt lyft 100 000 kg."),
        new AchievementDef(AchievementId.TotalVolume500k,    "💎", "Halvmiljonär",        "Du har totalt lyft 500 000 kg."),
        new AchievementDef(AchievementId.TotalVolume1M,      "👑", "Miljonlyftare",       "En miljon kg totalt lyft."),
        new AchievementDef(AchievementId.AllMuscleGroups,    "🌟", "Komplett",            "Alla muskelgrupper tränade under sju dagar."),
        new AchievementDef(AchievementId.LongSession,        "⏳", "Maratonpass",         "Du genomförde ett pass längre än 90 minuter."),
        new AchievementDef(AchievementId.EarlyBird,          "🌅", "Morgonfågeln",        "Pass startat före klockan 07:00."),
        new AchievementDef(AchievementId.NightOwl,           "🦉", "Nattuglan",           "Pass startat efter klockan 21:00."),
        new AchievementDef(AchievementId.FirstCustomExercise,"🔧", "Uppfinnaren",         "Du skapade en egen övning."),
    };

    public static AchievementDef? Get(AchievementId id) =>
        All.FirstOrDefault(a => a.Id == id);
}
