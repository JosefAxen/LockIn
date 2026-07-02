using LockIn.Data;
using LockIn.Models;

namespace LockIn.Services;

public sealed record MesoBuilderSpec(
    string MesocycleName,
    int WeekCount,             // inkl. deload
    UnitSystem UnitSystem,
    int DaysPerWeek,
    IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> Priorities,
    IReadOnlyList<int> PreferredEquipment,
    DateTime StartDate);

public interface IMesoBuilderService
{
    Task<Mesocycle> BuildAsync(MesoBuilderSpec spec);
    Task<Mesocycle> StartFromTemplateAsync(int templateId, string mesocycleName,
        DateTime startDate, int weekCount, UnitSystem unit,
        IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> priorities);
}

public class MesoBuilderServiceV2 : IMesoBuilderService
{
    private readonly DatabaseService _db;
    private readonly ITemplateService _templates;
    private readonly IMesocycleService _mesocycles;
    private readonly IExerciseRecommendationService _recommend;

    public MesoBuilderServiceV2(
        DatabaseService db,
        ITemplateService templates,
        IMesocycleService mesocycles,
        IExerciseRecommendationService recommend)
    {
        _db = db;
        _templates = templates;
        _mesocycles = mesocycles;
        _recommend = recommend;
    }

    public async Task<Mesocycle> BuildAsync(MesoBuilderSpec spec)
    {
        // 1. Skapa ny template
        var template = new WorkoutTemplate
        {
            Name = $"{spec.MesocycleName} template",
            IsCustom = true,
            DefaultDaysPerWeek = spec.DaysPerWeek,
            CreatedAt = DateTime.Now,
        };

        // 2. Hämta split
        var splitDays = SplitTemplates.GetSplit(spec.DaysPerWeek);

        // 3. Prio-justering (Emphasize +1 extra dag om möjligt, Maintain max 1x/vecka)
        var adjustedDays = ApplyPriorityAdjustment(splitDays, spec.Priorities);

        // 4. Bygg TemplateDay + TemplateMuscleSlot + TemplateExercise
        var days = new List<TemplateDay>();
        var slotsByDayIndex = new Dictionary<int, List<TemplateMuscleSlot>>();
        var exercisesBySlot = new Dictionary<(int, int), List<TemplateExercise>>();

        for (int i = 0; i < adjustedDays.Count; i++)
        {
            var d = adjustedDays[i];
            days.Add(new TemplateDay
            {
                DayIndex = i,
                Label = d.Label,
                DefaultDayOfWeek = d.DefaultDayOfWeek,
            });

            var slots = new List<TemplateMuscleSlot>();
            for (int j = 0; j < d.Muscles.Count; j++)
            {
                var muscle = d.Muscles[j];
                var slot = new TemplateMuscleSlot
                {
                    MuscleGroupValue = (int)muscle,
                    SortOrder = j,
                };
                slots.Add(slot);

                // Auto-picka övningar via ExerciseRecommendationService
                var prio = spec.Priorities.TryGetValue(muscle, out var p) ? p : MusclePriorityLevel.Grow;
                var picks = await _recommend.RecommendForMuscleAsync(muscle, prio, new List<int>(), 0, j);

                var exs = picks.Select((e, idx) => new TemplateExercise
                {
                    ExerciseId = e.Id,
                    SortOrder = idx,
                    TargetRepsMin = e.Mechanic == ExerciseMechanic.Compound ? 8 : 12,
                    TargetRepsMax = e.Mechanic == ExerciseMechanic.Compound ? 12 : 15,
                    DefaultRestSeconds = e.Mechanic == ExerciseMechanic.Compound ? 180 : 90,
                }).ToList();
                exercisesBySlot[(i, j)] = exs;
            }
            slotsByDayIndex[i] = slots;
        }

        int templateId = await _templates.SaveAsync(template, days, slotsByDayIndex, exercisesBySlot);

        // 5. Skapa Mesocycle + Priorities + Weeks
        return await StartFromTemplateAsync(
            templateId, spec.MesocycleName, spec.StartDate, spec.WeekCount, spec.UnitSystem, spec.Priorities);
    }

    public async Task<Mesocycle> StartFromTemplateAsync(
        int templateId,
        string mesocycleName,
        DateTime startDate,
        int weekCount,
        UnitSystem unit,
        IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> priorities)
    {
        var meso = new Mesocycle
        {
            Name = mesocycleName,
            TemplateId = templateId,
            StartDate = startDate.Date,
            WeekCount = weekCount,
            IsActive = true,
            UnitSystemValue = (int)unit,
            CreatedAt = DateTime.Now,
        };

        var priorityRecords = priorities.Select(kv => new MesocyclePriority
        {
            MuscleGroupValue = (int)kv.Key,
            PriorityLevelValue = (int)kv.Value,
        }).ToList();

        // Sektion I: sista veckan är deload (50% intensitet)
        var weeks = new List<MesocycleWeek>();
        for (int wk = 1; wk <= weekCount; wk++)
        {
            bool isDeload = wk == weekCount;
            int intensity = isDeload ? 50 : Math.Min(90, 70 + (wk - 1) * 5);
            weeks.Add(new MesocycleWeek
            {
                WeekNumber = wk,
                IsDeload = isDeload,
                IntensityPercent = intensity,
            });
        }

        int mesoId = await _mesocycles.SaveAsync(meso, priorityRecords, weeks);
        await _mesocycles.ActivateAsync(mesoId);
        meso.Id = mesoId;
        return meso;
    }

    private static IReadOnlyList<SplitDayDef> ApplyPriorityAdjustment(
        IReadOnlyList<SplitDayDef> baseSplit,
        IReadOnlyDictionary<MuscleGroup, MusclePriorityLevel> priorities)
    {
        var result = baseSplit.Select(d => new SplitDayDef(d.Label, d.DefaultDayOfWeek, d.Muscles.ToList())).ToList();

        // Maintain: reducera frekvens till max 1x/vecka
        var maintainMuscles = priorities.Where(p => p.Value == MusclePriorityLevel.Maintain).Select(p => p.Key).ToHashSet();
        foreach (var muscle in maintainMuscles)
        {
            bool foundFirst = false;
            for (int i = 0; i < result.Count; i++)
            {
                var newList = ((List<MuscleGroup>)result[i].Muscles).ToList();
                if (newList.Contains(muscle))
                {
                    if (foundFirst)
                    {
                        newList.Remove(muscle);
                        result[i] = new SplitDayDef(result[i].Label, result[i].DefaultDayOfWeek, newList);
                    }
                    else
                    {
                        foundFirst = true;
                    }
                }
            }
        }

        // Emphasize: försök addera 1 extra dag om muskeln bara finns på 1 dag i basen
        var emphasizeMuscles = priorities.Where(p => p.Value == MusclePriorityLevel.Emphasize).Select(p => p.Key).ToList();
        foreach (var muscle in emphasizeMuscles)
        {
            int count = result.Count(d => d.Muscles.Contains(muscle));
            if (count == 1 && result.Count >= 2)
            {
                // Lägg till på en dag som inte redan har den — välj den dag med minst antal muskler
                var candidateIdx = -1;
                int minMuscles = int.MaxValue;
                for (int i = 0; i < result.Count; i++)
                {
                    if (!result[i].Muscles.Contains(muscle) && result[i].Muscles.Count < minMuscles)
                    {
                        candidateIdx = i;
                        minMuscles = result[i].Muscles.Count;
                    }
                }
                if (candidateIdx >= 0)
                {
                    var newList = ((List<MuscleGroup>)result[candidateIdx].Muscles).ToList();
                    newList.Add(muscle);
                    result[candidateIdx] = new SplitDayDef(result[candidateIdx].Label, result[candidateIdx].DefaultDayOfWeek, newList);
                }
            }
        }

        return result;
    }
}
