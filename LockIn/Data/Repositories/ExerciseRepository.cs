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

    public async Task<List<Exercise>> GetAllAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<Exercise>().OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Exercise?> GetAsync(int id)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<Exercise>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(Exercise exercise)
    {
        var db = await _db.EnsureConnectionAsync();
        return exercise.Id == 0 ? await db.InsertAsync(exercise) : await db.UpdateAsync(exercise);
    }

    public async Task<int> DeleteAsync(Exercise exercise)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.DeleteAsync(exercise);
    }
}
