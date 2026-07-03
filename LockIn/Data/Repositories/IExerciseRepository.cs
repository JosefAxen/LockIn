using LockIn.Models;

namespace LockIn.Data.Repositories;

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync();
    Task<Exercise?> GetAsync(int id);
    Task<int> SaveAsync(Exercise exercise);
    Task<int> DeleteAsync(Exercise exercise);
}
