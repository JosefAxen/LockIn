using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Resources.Strings;

namespace LockIn.Services;

public sealed record WeightSuggestion(decimal WeightKg, string Reason);

public interface IWeightProgressionService
{
    Task<WeightSuggestion?> SuggestNextWeightAsync(int exerciseId, int currentSessionId, int targetReps);
}

// RP-baserad vikt-progression (plan sektion C):
//  - Alla reps nådda + sista sets RIR ≥ 1 → +2.5 kg compound / +1.25 kg isolation
//  - Alla reps nådda men RIR = 0 → behåll vikt (extra rep först)
//  - Alla reps nådda men ingen RIR loggad → behåll vikt (set utan RIR ignoreras)
//  - Ej alla reps nådda → behåll vikt
//  - Deload-vecka override: peak × 0.80, RIR-target 4
public class WeightProgressionService : IWeightProgressionService
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IExerciseRepository _exerciseRepo;
    private readonly IMesocycleService _mesocycles;

    public WeightProgressionService(ISessionRepository sessionRepo, IExerciseRepository exerciseRepo, IMesocycleService mesocycles)
    {
        _sessionRepo = sessionRepo;
        _exerciseRepo = exerciseRepo;
        _mesocycles = mesocycles;
    }

    public async Task<WeightSuggestion?> SuggestNextWeightAsync(int exerciseId, int currentSessionId, int targetReps)
    {
        var prevSets = await _sessionRepo.GetLastSessionSetsAsync(exerciseId, currentSessionId);
        var working = prevSets
            .Where(s => s.SetType != SetType.Warmup && s.WeightKg > 0 && s.Reps > 0)
            .OrderBy(s => s.SetNumber)
            .ToList();
        if (working.Count == 0) return null;

        var exercise = await _exerciseRepo.GetAsync(exerciseId);
        var isIsolation = exercise?.Mechanic == ExerciseMechanic.Isolation;
        var increment = isIsolation ? 1.25m : 2.5m;

        decimal RoundToStep(decimal w) => Math.Round(w / increment) * increment;

        // Deload-override: om aktiv mesocykel-vecka är deload → 80 % av peak, avrundat till lastbar vikt
        var currentWeek = await _mesocycles.GetCurrentWeekAsync();
        if (currentWeek?.IsDeload == true)
        {
            var peakWeight = working.Max(s => s.WeightKg);
            var deloadWeight = RoundToStep(peakWeight * 0.80m);
            return new WeightSuggestion(deloadWeight, AppResources.WeightSuggestion_Reason_Deload);
        }

        var lastSet = working[^1];
        var hitAllReps = targetReps <= 0 || working.All(s => s.Reps >= targetReps);

        // Set utan loggad RIR (null) ignoreras — de säger inget om marginalen till failure.
        var lastRated = working.LastOrDefault(s => s.RIR is not null);

        if (hitAllReps && lastRated is null)
        {
            return new WeightSuggestion(lastSet.WeightKg, AppResources.WeightSuggestion_Reason_NoRir);
        }
        if (hitAllReps && lastRated!.RIR >= 1)
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
