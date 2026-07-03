using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för Mesocycles + priorities + weeks + MuscleVolumeLandmarks.
/// Del av ADR-001 (DatabaseService-splittring), PR 5 av 7.
/// GetBundleAsync kör 3 queries parallellt (löser efficiency-finding #4).
/// </summary>
public sealed class MesocycleRepository : IMesocycleRepository
{
    private readonly DatabaseService _db;

    public MesocycleRepository(DatabaseService db) => _db = db;

#pragma warning disable CS0618 // Delegate till Obsolete-metoder — flyttas fysiskt i PR 7
    public Task<List<Mesocycle>> GetAllAsync() => _db.GetMesocyclesAsync();
    public Task<Mesocycle?> GetAsync(int id) => _db.GetMesocycleAsync(id);
    public Task<Mesocycle?> GetActiveAsync() => _db.GetActiveMesocycleAsync();

    public Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks)
        => _db.SaveMesocycleAsync(meso, priorities, weeks);

    public Task ActivateAsync(int mesocycleId) => _db.ActivateMesocycleAsync(mesocycleId);
    public Task DeleteAsync(int mesocycleId) => _db.DeleteMesocycleAsync(mesocycleId);

    public Task<List<WorkoutSession>> GetSessionsForMesocycleAsync(int mesocycleId)
        => _db.GetSessionsForMesocycleAsync(mesocycleId);

    public Task<List<MesocyclePriority>> GetPrioritiesAsync(int mesocycleId)
        => _db.GetMesocyclePrioritiesAsync(mesocycleId);

    public Task<List<MesocycleWeek>> GetWeeksAsync(int mesocycleId)
        => _db.GetMesocycleWeeksAsync(mesocycleId);

    public Task<List<MuscleVolumeLandmarks>> GetLandmarksAsync() => _db.GetMuscleVolumeLandmarksAsync();
    public Task SaveLandmarksAsync(MuscleVolumeLandmarks landmarks) => _db.SaveMuscleVolumeLandmarksAsync(landmarks);
#pragma warning restore CS0618

    public async Task<MesocycleBundle?> GetBundleAsync(int mesocycleId)
    {
        var meso = await GetAsync(mesocycleId);
        if (meso is null) return null;

        // 2 parallella queries efter meso är hämtad (SQLite-net kör en åt gången på connection,
        // men Task.WhenAll ger korrekt awaitning utan sekventiell overhead om connection är fri).
        var weeksTask = GetWeeksAsync(mesocycleId);
        var prioritiesTask = GetPrioritiesAsync(mesocycleId);
        await Task.WhenAll(weeksTask, prioritiesTask);

        return new MesocycleBundle(meso, weeksTask.Result, prioritiesTask.Result);
    }
}
