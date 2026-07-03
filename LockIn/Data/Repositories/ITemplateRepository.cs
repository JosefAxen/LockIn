using LockIn.Models;

namespace LockIn.Data.Repositories;

public interface ITemplateRepository
{
    Task<List<WorkoutTemplate>> GetAllAsync();
    Task<WorkoutTemplate?> GetAsync(int id);
    Task<List<TemplateDay>> GetDaysAsync(int templateId);
    Task<List<TemplateMuscleSlot>> GetMuscleSlotsAsync(int templateDayId);
    Task<List<TemplateExercise>> GetExercisesForSlotAsync(int templateMuscleSlotId);

    /// <summary>
    /// Batch-hämtar hel struktur för en template i 3 queries (utan N+1).
    /// Löser efficiency-review-finding #8 (RP-clone).
    /// </summary>
    Task<TemplateStructure?> GetStructureAsync(int templateId);

    Task<int> SaveWithChildrenAsync(
        WorkoutTemplate template,
        List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDayIndex,
        Dictionary<(int dayIndex, int slotSortOrder), List<TemplateExercise>> exercisesBySlot);

    Task DeleteAsync(int templateId);
}

public sealed record TemplateStructure(
    WorkoutTemplate Template,
    List<TemplateDay> Days,
    Dictionary<int, List<TemplateMuscleSlot>> SlotsByDayId,
    Dictionary<int, List<TemplateExercise>> ExercisesBySlotId);
