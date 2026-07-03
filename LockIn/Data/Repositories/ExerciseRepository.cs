using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för Exercises (inkl. custom exercises).
/// Del av ADR-001 (DatabaseService-splittring), PR 3 av 7.
/// </summary>
public sealed class ExerciseRepository : IExerciseRepository
{
    private readonly DatabaseService _db;

    public ExerciseRepository(DatabaseService db) => _db = db;

    public Task<List<Exercise>> GetAllAsync() => _db.GetExercisesAsync();
    public Task<Exercise?> GetAsync(int id) => _db.GetExerciseAsync(id);
    public Task<int> SaveAsync(Exercise exercise) => _db.SaveExerciseAsync(exercise);
    public Task<int> DeleteAsync(Exercise exercise) => _db.DeleteExerciseAsync(exercise);
}
