using LockIn.Models;

namespace LockIn.Data.Repositories;

public interface IMesocycleRepository
{
    // Mesocycles
    Task<List<Mesocycle>> GetAllAsync();
    Task<Mesocycle?> GetAsync(int id);
    Task<Mesocycle?> GetActiveAsync();
    Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks);
    Task ActivateAsync(int mesocycleId);
    Task DeleteAsync(int mesocycleId);
    Task<List<WorkoutSession>> GetSessionsForMesocycleAsync(int mesocycleId);

    // Priorities + Weeks (för aktiv cykel eller specifik)
    Task<List<MesocyclePriority>> GetPrioritiesAsync(int mesocycleId);
    Task<List<MesocycleWeek>> GetWeeksAsync(int mesocycleId);

    /// <summary>
    /// Batch-hämtar mesocycle + weeks + priorities parallellt.
    /// Del av ADR-001 PR 5 — ersätter tre sekventiella queries.
    /// </summary>
    Task<MesocycleBundle?> GetBundleAsync(int mesocycleId);

    // Muscle volume landmarks (individuella MEV/MRV per muskelgrupp)
    Task<List<MuscleVolumeLandmarks>> GetLandmarksAsync();
    Task SaveLandmarksAsync(MuscleVolumeLandmarks landmarks);
}

public sealed record MesocycleBundle(
    Mesocycle Mesocycle,
    List<MesocycleWeek> Weeks,
    List<MesocyclePriority> Priorities);
