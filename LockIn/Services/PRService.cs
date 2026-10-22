using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public class PRService(ISessionRepository sessionRepo)
{
    public static double CalculateEpley1RM(decimal weightKg, int reps)
    {
        if (reps == 1) return (double)weightKg;
        return (double)weightKg * (1 + reps / 30.0);
    }

    public async Task<bool> IsPRAsync(int exerciseId, decimal weightKg, int reps, int excludeLoggedSetId = 0, int currentSessionId = 0)
    {
        // Utan historik från ett tidigare avslutat pass finns inget rekord att slå —
        // första passet med en övning sätter bara en baslinje.
        if (!await sessionRepo.HasCompletedHistoryAsync(exerciseId, currentSessionId))
            return false;

        var newEstimate = CalculateEpley1RM(weightKg, reps);
        var maxPrevious = await sessionRepo.GetMaxEpley1RMAsync(exerciseId, excludeLoggedSetId);
        return newEstimate > maxPrevious;
    }
}
