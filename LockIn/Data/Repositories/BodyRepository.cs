using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för BodyWeight + BodyComposition + WorkoutPhoto.
/// Del av ADR-001 (DatabaseService-splittring), PR 2 av 7.
/// </summary>
public sealed class BodyRepository : IBodyRepository
{
    private readonly DatabaseService _db;

    public BodyRepository(DatabaseService db) => _db = db;

    public async Task<List<BodyWeightEntry>> GetWeightEntriesAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<BodyWeightEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveWeightEntryAsync(BodyWeightEntry entry)
    {
        var db = await _db.EnsureConnectionAsync();
        return entry.Id == 0 ? await db.InsertAsync(entry) : await db.UpdateAsync(entry);
    }

    public async Task<int> DeleteWeightEntryAsync(BodyWeightEntry entry)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.DeleteAsync(entry);
    }

    public async Task<List<BodyCompositionEntry>> GetCompositionEntriesAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<BodyCompositionEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveCompositionEntryAsync(BodyCompositionEntry entry)
    {
        var db = await _db.EnsureConnectionAsync();
        return entry.Id == 0 ? await db.InsertAsync(entry) : await db.UpdateAsync(entry);
    }

    public async Task<int> DeleteCompositionEntryAsync(BodyCompositionEntry entry)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.DeleteAsync(entry);
    }

    public async Task<List<WorkoutPhoto>> GetPhotosForSessionAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutPhoto>()
            .Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.TakenAt)
            .ToListAsync();
    }

    public async Task<List<WorkoutPhoto>> GetAllPhotosAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutPhoto>().OrderByDescending(p => p.TakenAt).ToListAsync();
    }

    public async Task<int> SavePhotoAsync(WorkoutPhoto photo)
    {
        var db = await _db.EnsureConnectionAsync();
        return photo.Id == 0 ? await db.InsertAsync(photo) : await db.UpdateAsync(photo);
    }

    public async Task<int> DeletePhotoAsync(WorkoutPhoto photo)
    {
        var db = await _db.EnsureConnectionAsync();
        if (File.Exists(photo.FilePath))
            File.Delete(photo.FilePath);
        return await db.DeleteAsync(photo);
    }
}
