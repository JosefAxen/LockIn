using LockIn.Models;

namespace LockIn.Data.Repositories;

public interface IBodyRepository
{
    // Body weight
    Task<List<BodyWeightEntry>> GetWeightEntriesAsync();
    Task<int> SaveWeightEntryAsync(BodyWeightEntry entry);
    Task<int> DeleteWeightEntryAsync(BodyWeightEntry entry);

    // Body composition
    Task<List<BodyCompositionEntry>> GetCompositionEntriesAsync();
    Task<int> SaveCompositionEntryAsync(BodyCompositionEntry entry);
    Task<int> DeleteCompositionEntryAsync(BodyCompositionEntry entry);

    // Workout photos
    Task<List<WorkoutPhoto>> GetPhotosForSessionAsync(int sessionId);
    Task<List<WorkoutPhoto>> GetAllPhotosAsync();
    Task<int> SavePhotoAsync(WorkoutPhoto photo);
    Task<int> DeletePhotoAsync(WorkoutPhoto photo);
}
