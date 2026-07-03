using System.Text.Json;
using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public interface IExerciseRecommendationService
{
    Task<List<Exercise>> RecommendForMuscleAsync(
        MuscleGroup muscle,
        MusclePriorityLevel priority,
        IReadOnlyList<int> excludeExerciseIds,
        int mesocycleIdForTieBreak = 0,
        int slotIdForTieBreak = 0);
}

public class ExerciseRecommendationService : IExerciseRecommendationService
{
    private readonly IExerciseRepository _exerciseRepo;
    private readonly ISettingsRepository _settingsRepo;

    public ExerciseRecommendationService(IExerciseRepository exerciseRepo, ISettingsRepository settingsRepo)
    {
        _exerciseRepo = exerciseRepo;
        _settingsRepo = settingsRepo;
    }

    public async Task<List<Exercise>> RecommendForMuscleAsync(
        MuscleGroup muscle,
        MusclePriorityLevel priority,
        IReadOnlyList<int> excludeExerciseIds,
        int mesocycleIdForTieBreak = 0,
        int slotIdForTieBreak = 0)
    {
        var all = await _exerciseRepo.GetAllAsync();
        var settings = await _settingsRepo.GetAsync();
        var preferredEquipment = settings.ParsePreferredEquipment();

        // Filter: rätt muskel + inte exkluderad + equipment tillåtet (om preferred finns)
        var candidates = all
            .Where(e => e.MuscleGroup == muscle)
            .Where(e => !excludeExerciseIds.Contains(e.Id))
            .Where(e => preferredEquipment.Count == 0 || preferredEquipment.Contains((int)e.Equipment))
            .ToList();

        if (candidates.Count == 0) return new List<Exercise>();

        var primaryEquipment = preferredEquipment.Count > 0 ? preferredEquipment[0] : -1;

        // Rank per sektion G
        var ranked = candidates.Select(e =>
        {
            int score = 0;
            if (e.Mechanic == ExerciseMechanic.Compound) score += 100;
            if (e.Level == ExerciseLevel.Beginner || e.Level == ExerciseLevel.Intermediate) score += 50;
            if ((int)e.Equipment == primaryEquipment) score += 30;
            if (!e.IsCustom) score += 10;

            // Deterministic tie-break
            unchecked
            {
                int hash = mesocycleIdForTieBreak * 31 + slotIdForTieBreak * 17 + e.Id * 7;
                score += Math.Abs(hash) % 20;
            }

            return (Exercise: e, Score: score);
        }).OrderByDescending(x => x.Score).ToList();

        // Retur per prio (sektion G)
        int count = priority switch
        {
            MusclePriorityLevel.Emphasize => 3,
            MusclePriorityLevel.Grow => 2,
            MusclePriorityLevel.Maintain => 1,
            _ => 1,
        };

        // Compound-first: ta compound först, sen fyll på med resten
        var compounds = ranked.Where(r => r.Exercise.Mechanic == ExerciseMechanic.Compound).ToList();
        var isolations = ranked.Where(r => r.Exercise.Mechanic != ExerciseMechanic.Compound).ToList();

        var result = new List<Exercise>();
        foreach (var r in compounds) { if (result.Count >= count) break; result.Add(r.Exercise); }
        foreach (var r in isolations) { if (result.Count >= count) break; result.Add(r.Exercise); }

        return result;
    }

}
