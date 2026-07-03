using LockIn.Models;

namespace LockIn.Data.Repositories;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync();
    Task SaveAsync(AppSettings settings);

    Task<HashSet<int>> GetUnlockedAchievementIdsAsync();
    Task<List<UserAchievement>> GetUnlockedAchievementsAsync();
    Task<bool> UnlockAchievementAsync(AchievementId id);
}
