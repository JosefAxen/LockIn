using LockIn.Models;

namespace LockIn.Services;

public class PRService(DatabaseService db)
{
    public static double CalculateEpley1RM(decimal weightKg, int reps)
    {
        if (reps == 1) return (double)weightKg;
        return (double)weightKg * (1 + reps / 30.0);
    }

    public async Task<bool> IsPRAsync(int exerciseId, decimal weightKg, int reps, int excludeLoggedSetId = 0)
    {
        var allSets = await db.GetAllSetsForExerciseAsync(exerciseId);
        var newEstimate = CalculateEpley1RM(weightKg, reps);

        double maxPrevious = allSets
            .Where(s => s.Id != excludeLoggedSetId)
            .Select(s => CalculateEpley1RM(s.WeightKg, s.Reps))
            .DefaultIfEmpty(0)
            .Max();

        return newEstimate > maxPrevious;
    }
}
