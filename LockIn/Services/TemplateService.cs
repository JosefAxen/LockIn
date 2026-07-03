using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public sealed record TemplateDetail(
    WorkoutTemplate Template,
    IReadOnlyList<TemplateDay> Days,
    IReadOnlyDictionary<int, IReadOnlyList<TemplateMuscleSlot>> SlotsByDayId,
    IReadOnlyDictionary<int, IReadOnlyList<TemplateExercise>> ExercisesBySlotId);

public interface ITemplateService
{
    Task<List<WorkoutTemplate>> ListAsync();
    Task<WorkoutTemplate?> GetAsync(int id);
    Task<TemplateDetail?> GetDetailAsync(int templateId);
    Task<int> SaveAsync(WorkoutTemplate template, List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDayIndex,
        Dictionary<(int dayIndex, int slotSortOrder), List<TemplateExercise>> exercisesBySlot);
    Task DeleteAsync(int templateId);
    Task<int> CloneAsync(int sourceTemplateId, string newName);
}

public class TemplateService : ITemplateService
{
    private readonly ITemplateRepository _templates;
    public TemplateService(ITemplateRepository templates) => _templates = templates;

    public Task<List<WorkoutTemplate>> ListAsync() => _templates.GetAllAsync();
    public Task<WorkoutTemplate?> GetAsync(int id) => _templates.GetAsync(id);

    // ADR-001 PR 4: nu batch (3 queries) istället för N+1 foreach — löser efficiency-finding #8.
    public async Task<TemplateDetail?> GetDetailAsync(int templateId)
    {
        var s = await _templates.GetStructureAsync(templateId);
        if (s is null) return null;

        var slotsByDayId = s.SlotsByDayId
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<TemplateMuscleSlot>)kv.Value);
        var exercisesBySlotId = s.ExercisesBySlotId
            .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<TemplateExercise>)kv.Value);

        return new TemplateDetail(s.Template, s.Days, slotsByDayId, exercisesBySlotId);
    }

    public Task<int> SaveAsync(WorkoutTemplate template, List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDayIndex,
        Dictionary<(int dayIndex, int slotSortOrder), List<TemplateExercise>> exercisesBySlot)
        => _templates.SaveWithChildrenAsync(template, days, slotsByDayIndex, exercisesBySlot);

    public Task DeleteAsync(int templateId) => _templates.DeleteAsync(templateId);

    public async Task<int> CloneAsync(int sourceTemplateId, string newName)
    {
        var detail = await GetDetailAsync(sourceTemplateId);
        if (detail is null) throw new InvalidOperationException("Källtemplate hittades inte");

        var newTemplate = new WorkoutTemplate
        {
            Name = newName,
            IsCustom = true,
            DefaultDaysPerWeek = detail.Template.DefaultDaysPerWeek,
            CreatedAt = DateTime.Now,
        };

        var newDays = detail.Days.Select(d => new TemplateDay
        {
            DayIndex = d.DayIndex,
            Label = d.Label,
            DefaultDayOfWeek = d.DefaultDayOfWeek,
        }).ToList();

        var slotsByDayIndex = new Dictionary<int, List<TemplateMuscleSlot>>();
        var exercisesBySlot = new Dictionary<(int, int), List<TemplateExercise>>();
        foreach (var day in detail.Days)
        {
            var slots = detail.SlotsByDayId[day.Id];
            slotsByDayIndex[day.DayIndex] = slots.Select(s => new TemplateMuscleSlot
            {
                MuscleGroupValue = s.MuscleGroupValue,
                SortOrder = s.SortOrder,
            }).ToList();

            foreach (var slot in slots)
            {
                var exs = detail.ExercisesBySlotId[slot.Id];
                exercisesBySlot[(day.DayIndex, slot.SortOrder)] = exs.Select(e => new TemplateExercise
                {
                    ExerciseId = e.ExerciseId,
                    SortOrder = e.SortOrder,
                    TargetRepsMin = e.TargetRepsMin,
                    TargetRepsMax = e.TargetRepsMax,
                    DefaultRestSeconds = e.DefaultRestSeconds,
                }).ToList();
            }
        }

        return await SaveAsync(newTemplate, newDays, slotsByDayIndex, exercisesBySlot);
    }
}
