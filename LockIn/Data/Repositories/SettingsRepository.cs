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

    public async Task<AppSettings> GetAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<AppSettings>().FirstOrDefaultAsync() ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.UpdateAsync(settings);
    }

    public async Task<HashSet<int>> GetUnlockedAchievementIdsAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        var rows = await db.Table<UserAchievement>().ToListAsync();
        return rows.Select(r => r.Id).ToHashSet();
    }

    public async Task<List<UserAchievement>> GetUnlockedAchievementsAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<UserAchievement>().ToListAsync();
    }

    public async Task<bool> UnlockAchievementAsync(AchievementId id)
    {
        var db = await _db.EnsureConnectionAsync();
        var existing = await db.Table<UserAchievement>().Where(a => a.Id == (int)id).FirstOrDefaultAsync();
        if (existing is not null) return false;
        await db.InsertAsync(new UserAchievement { Id = (int)id, UnlockedAt = DateTime.Now });
        return true;
    }
}
