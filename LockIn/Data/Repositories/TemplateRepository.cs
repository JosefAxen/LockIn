using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för WorkoutTemplate + hierarki (Days → MuscleSlots → Exercises).
/// Del av ADR-001 (DatabaseService-splittring), PR 4 av 7.
/// GetStructureAsync använder batch-queries (löser efficiency-finding #8).
/// </summary>
public sealed class TemplateRepository : ITemplateRepository
{
    private readonly DatabaseService _db;

    public TemplateRepository(DatabaseService db) => _db = db;

#pragma warning disable CS0618 // Delegate till Obsolete-metoder — flyttas fysiskt i PR 7
    public Task<List<WorkoutTemplate>> GetAllAsync() => _db.GetTemplatesAsync();
    public Task<WorkoutTemplate?> GetAsync(int id) => _db.GetTemplateAsync(id);
    public Task<List<TemplateDay>> GetDaysAsync(int templateId) => _db.GetTemplateDaysAsync(templateId);
    public Task<List<TemplateMuscleSlot>> GetMuscleSlotsAsync(int templateDayId) => _db.GetTemplateMuscleSlotsAsync(templateDayId);
    public Task<List<TemplateExercise>> GetExercisesForSlotAsync(int templateMuscleSlotId) => _db.GetTemplateExercisesForSlotAsync(templateMuscleSlotId);

    public Task<int> SaveWithChildrenAsync(
        WorkoutTemplate template,
        List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDayIndex,
        Dictionary<(int dayIndex, int slotSortOrder), List<TemplateExercise>> exercisesBySlot)
        => _db.SaveTemplateWithChildrenAsync(template, days, slotsByDayIndex, exercisesBySlot);

    public Task DeleteAsync(int templateId) => _db.DeleteTemplateAsync(templateId);
#pragma warning restore CS0618

    public async Task<TemplateStructure?> GetStructureAsync(int templateId)
    {
        var (template, days, slots, exercises) = await _db.GetTemplateStructureAsync(templateId);
        if (template is null) return null;

        var slotsByDayId = slots
            .GroupBy(s => s.TemplateDayId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SortOrder).ToList());

        var exercisesBySlotId = exercises
            .GroupBy(e => e.TemplateMuscleSlotId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.SortOrder).ToList());

        return new TemplateStructure(template, days, slotsByDayId, exercisesBySlotId);
    }
}
