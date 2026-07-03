using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public sealed record PlannedVolume(MuscleGroup Muscle, int WeekNumber, int TargetSets);

public interface IVolumeProgressionService
{
    Task<IReadOnlyList<PlannedVolume>> GetPlannedVolumeForMesocycleAsync(int mesocycleId);
    Task<int> GetTargetSetsForCurrentWeekAsync(MuscleGroup muscle);
}

public class VolumeProgressionService : IVolumeProgressionService
{
    private readonly IMesocycleRepository _mesocyclesRepo;
    private readonly IMesocycleService _mesocycles;

    // MV per muskelgrupp (sektion A + D)
    private static readonly Dictionary<MuscleGroup, int> MV = new()
    {
        { MuscleGroup.Chest, 6 }, { MuscleGroup.Back, 6 }, { MuscleGroup.Shoulders, 6 },
        { MuscleGroup.Biceps, 6 }, { MuscleGroup.Triceps, 6 }, { MuscleGroup.Quads, 6 },
        { MuscleGroup.Hamstrings, 6 }, { MuscleGroup.Glutes, 6 }, { MuscleGroup.Calves, 6 },
        { MuscleGroup.Traps, 6 }, { MuscleGroup.Forearms, 4 }, { MuscleGroup.Abs, 4 },
    };

    public VolumeProgressionService(IMesocycleRepository mesocyclesRepo, IMesocycleService mesocycles)
    {
        _mesocyclesRepo = mesocyclesRepo;
        _mesocycles = mesocycles;
    }

    public async Task<IReadOnlyList<PlannedVolume>> GetPlannedVolumeForMesocycleAsync(int mesocycleId)
    {
        var detail = await _mesocycles.GetDetailAsync(mesocycleId);
        if (detail is null) return new List<PlannedVolume>();

        var landmarks = await _mesocyclesRepo.GetLandmarksAsync();
        var mevByMuscle = landmarks.ToDictionary(l => l.MuscleGroup, l => l.MEV);
        var mrvByMuscle = landmarks.ToDictionary(l => l.MuscleGroup, l => l.MRV);

        var result = new List<PlannedVolume>();
        var totalWeeks = detail.Meso.WeekCount;

        foreach (var priority in detail.Priorities)
        {
            var muscle = priority.MuscleGroup;
            var prio = priority.PriorityLevel;

            if (!mevByMuscle.TryGetValue(muscle, out int mev)) mev = 8;
            if (!mrvByMuscle.TryGetValue(muscle, out int mrv)) mrv = 18;

            int peakSets = mev;

            for (int wk = 1; wk <= totalWeeks; wk++)
            {
                int weekIndex = wk - 1;
                int planned;

                if (wk == totalWeeks) // deload
                {
                    planned = Math.Max(2, (int)Math.Floor(peakSets * 0.5));
                }
                else if (prio == MusclePriorityLevel.Maintain)
                {
                    planned = MV.TryGetValue(muscle, out var mv) ? mv : 6;
                }
                else
                {
                    int increment = prio == MusclePriorityLevel.Emphasize ? 2 : 1;
                    int target = mev + weekIndex * increment;
                    planned = Math.Min(target, mrv);
                    peakSets = Math.Max(peakSets, planned);
                }

                result.Add(new PlannedVolume(muscle, wk, planned));
            }
        }

        return result;
    }

    public async Task<int> GetTargetSetsForCurrentWeekAsync(MuscleGroup muscle)
    {
        var active = await _mesocycles.GetActiveAsync();
        if (active is null) return 0;

        var currentWeek = await _mesocycles.GetCurrentWeekAsync();
        if (currentWeek is null) return 0;

        var planned = await GetPlannedVolumeForMesocycleAsync(active.Id);
        var match = planned.FirstOrDefault(p => p.Muscle == muscle && p.WeekNumber == currentWeek.WeekNumber);
        return match?.TargetSets ?? 0;
    }
}
