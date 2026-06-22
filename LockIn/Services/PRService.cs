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
        var newEstimate = CalculateEpley1RM(weightKg, reps);
        var maxPrevious = await db.GetMaxEpley1RMAsync(exerciseId, excludeLoggedSetId);
        return newEstimate > maxPrevious;
    }
}
