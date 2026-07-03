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

    public Task<List<BodyWeightEntry>> GetWeightEntriesAsync()
        => _db.GetBodyWeightEntriesAsync();

    public Task<int> SaveWeightEntryAsync(BodyWeightEntry entry)
        => _db.SaveBodyWeightEntryAsync(entry);

    public Task<int> DeleteWeightEntryAsync(BodyWeightEntry entry)
        => _db.DeleteBodyWeightEntryAsync(entry);

    public Task<List<BodyCompositionEntry>> GetCompositionEntriesAsync()
        => _db.GetBodyCompositionEntriesAsync();

    public Task<int> SaveCompositionEntryAsync(BodyCompositionEntry entry)
        => _db.SaveBodyCompositionEntryAsync(entry);

    public Task<int> DeleteCompositionEntryAsync(BodyCompositionEntry entry)
        => _db.DeleteBodyCompositionEntryAsync(entry);

    public Task<List<WorkoutPhoto>> GetPhotosForSessionAsync(int sessionId)
        => _db.GetPhotosForSessionAsync(sessionId);

    public Task<List<WorkoutPhoto>> GetAllPhotosAsync()
        => _db.GetAllPhotosAsync();

    public Task<int> SavePhotoAsync(WorkoutPhoto photo)
        => _db.SavePhotoAsync(photo);

    public Task<int> DeletePhotoAsync(WorkoutPhoto photo)
        => _db.DeletePhotoAsync(photo);
}
