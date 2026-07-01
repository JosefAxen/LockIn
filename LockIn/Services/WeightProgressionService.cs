using LockIn.Models;
using LockIn.Resources.Strings;

namespace LockIn.Services;

public sealed record WeightSuggestion(decimal WeightKg, string Reason);

public interface IWeightProgressionService
{
    Task<WeightSuggestion?> SuggestNextWeightAsync(int exerciseId, int currentSessionId, int targetReps);
}

// RP-baserad vikt-progression (see docs/research/2026-06-30-rp-hypertrophy-research.md avsnitt 4):
//  - Alla reps nådda + sista sets RIR ≥ 1 → +2.5 kg compound / +1.25 kg isolation
//  - Alla reps nådda men RIR = 0 → behåll vikt (extra rep först)
//  - Ej alla reps nådda → behåll vikt
public class WeightProgressionService : IWeightProgressionService
{
    private readonly DatabaseService _db;

    public WeightProgressionService(DatabaseService db) => _db = db;

    public async Task<WeightSuggestion?> SuggestNextWeightAsync(int exerciseId, int currentSessionId, int targetReps)
    {
        var prevSets = await _db.GetLastSessionSetsAsync(exerciseId, currentSessionId);
        var working = prevSets
            .Where(s => s.SetType != SetType.Warmup && s.WeightKg > 0 && s.Reps > 0)
            .OrderBy(s => s.SetNumber)
            .ToList();
        if (working.Count == 0) return null;

        var exercise = await _db.GetExerciseAsync(exerciseId);
        var isIsolation = exercise?.Mechanic == ExerciseMechanic.Isolation;
        var increment = isIsolation ? 1.25m : 2.5m;

        var lastSet = working[^1];
        var hitAllReps = targetReps <= 0 || working.All(s => s.Reps >= targetReps);

        decimal RoundToStep(decimal w) => Math.Round(w / increment) * increment;

        if (hitAllReps && lastSet.RIR >= 1)
        {
            var next = RoundToStep(lastSet.WeightKg + increment);
            return new WeightSuggestion(next, AppResources.WeightSuggestion_Reason_HitAllReps);
        }
        if (hitAllReps)
        {
            return new WeightSuggestion(lastSet.WeightKg, AppResources.WeightSuggestion_Reason_MaintainWeight);
        }
        return new WeightSuggestion(lastSet.WeightKg, AppResources.WeightSuggestion_Reason_MissedReps);
    }
}
