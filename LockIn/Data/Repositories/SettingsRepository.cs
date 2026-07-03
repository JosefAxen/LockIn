using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för AppSettings + UserAchievement.
/// Del av ADR-001 (DatabaseService-splittring), PR 1 av 7.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly DatabaseService _db;

    public SettingsRepository(DatabaseService db) => _db = db;

    public Task<AppSettings> GetAsync() => _db.GetAppSettingsAsync();
    public Task SaveAsync(AppSettings settings) => _db.SaveAppSettingsAsync(settings);

    public Task<HashSet<int>> GetUnlockedAchievementIdsAsync()
        => _db.GetUnlockedAchievementIdsAsync();

    public Task<List<UserAchievement>> GetUnlockedAchievementsAsync()
        => _db.GetUnlockedAchievementsAsync();

    public Task<bool> UnlockAchievementAsync(AchievementId id)
        => _db.UnlockAchievementAsync(id);
}
