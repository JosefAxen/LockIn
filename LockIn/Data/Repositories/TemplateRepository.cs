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

    public async Task<List<WorkoutTemplate>> GetAllAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutTemplate>().OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<WorkoutTemplate?> GetAsync(int id)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutTemplate>().Where(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<TemplateDay>> GetDaysAsync(int templateId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<TemplateDay>()
            .Where(d => d.TemplateId == templateId)
            .OrderBy(d => d.DayIndex)
            .ToListAsync();
    }

    public async Task<List<TemplateMuscleSlot>> GetMuscleSlotsAsync(int templateDayId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<TemplateMuscleSlot>()
            .Where(s => s.TemplateDayId == templateDayId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
    }

    public async Task<List<TemplateExercise>> GetExercisesForSlotAsync(int templateMuscleSlotId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<TemplateExercise>()
            .Where(e => e.TemplateMuscleSlotId == templateMuscleSlotId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();
    }

    public async Task<int> SaveWithChildrenAsync(
        WorkoutTemplate template,
        List<TemplateDay> days,
        Dictionary<int, List<TemplateMuscleSlot>> slotsByDayIndex,
        Dictionary<(int dayIndex, int slotSortOrder), List<TemplateExercise>> exercisesBySlot)
    {
        var db = await _db.EnsureConnectionAsync();
        int templateId = template.Id;
        await db.RunInTransactionAsync(conn =>
        {
            if (template.Id == 0)
                conn.Insert(template);
            else
                conn.Update(template);
            templateId = template.Id;

            // Rensa gamla children
            conn.Execute("DELETE FROM TemplateExercises WHERE TemplateMuscleSlotId IN (SELECT Id FROM TemplateMuscleSlots WHERE TemplateDayId IN (SELECT Id FROM TemplateDays WHERE TemplateId = ?))", templateId);
            conn.Execute("DELETE FROM TemplateMuscleSlots WHERE TemplateDayId IN (SELECT Id FROM TemplateDays WHERE TemplateId = ?)", templateId);
            conn.Execute("DELETE FROM TemplateDays WHERE TemplateId = ?", templateId);

            // Skapa nya
            foreach (var day in days)
            {
                day.Id = 0;
                day.TemplateId = templateId;
                conn.Insert(day);

                if (slotsByDayIndex.TryGetValue(day.DayIndex, out var slots))
                {
                    foreach (var slot in slots)
                    {
                        slot.Id = 0;
                        slot.TemplateDayId = day.Id;
                        conn.Insert(slot);

                        if (exercisesBySlot.TryGetValue((day.DayIndex, slot.SortOrder), out var exs))
                        {
                            foreach (var ex in exs)
                            {
                                ex.Id = 0;
                                ex.TemplateMuscleSlotId = slot.Id;
                                conn.Insert(ex);
                            }
                        }
                    }
                }
            }
        });
        return templateId;
    }

    public async Task DeleteAsync(int templateId)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute("DELETE FROM TemplateExercises WHERE TemplateMuscleSlotId IN (SELECT Id FROM TemplateMuscleSlots WHERE TemplateDayId IN (SELECT Id FROM TemplateDays WHERE TemplateId = ?))", templateId);
            conn.Execute("DELETE FROM TemplateMuscleSlots WHERE TemplateDayId IN (SELECT Id FROM TemplateDays WHERE TemplateId = ?)", templateId);
            conn.Execute("DELETE FROM TemplateDays WHERE TemplateId = ?", templateId);
            conn.Execute("DELETE FROM WorkoutTemplates WHERE Id = ?", templateId);
        });
    }

    public async Task<TemplateStructure?> GetStructureAsync(int templateId)
    {
        var db = await _db.EnsureConnectionAsync();
        var template = await db.Table<WorkoutTemplate>().Where(t => t.Id == templateId).FirstOrDefaultAsync();
        if (template is null) return null;

        var days = await db.Table<TemplateDay>()
            .Where(d => d.TemplateId == templateId)
            .OrderBy(d => d.DayIndex)
            .ToListAsync();
        if (days.Count == 0)
            return new TemplateStructure(template, new(), new(), new());

        var dayIds = days.Select(d => d.Id).ToList();
        var dayIdsCsv = string.Join(",", dayIds);
        var slots = await db.QueryAsync<TemplateMuscleSlot>(
            $"SELECT * FROM TemplateMuscleSlots WHERE TemplateDayId IN ({dayIdsCsv}) ORDER BY SortOrder");

        List<TemplateExercise> exercises;
        if (slots.Count == 0)
        {
            exercises = new();
        }
        else
        {
            var slotIds = slots.Select(s => s.Id).ToList();
            var slotIdsCsv = string.Join(",", slotIds);
            exercises = await db.QueryAsync<TemplateExercise>(
                $"SELECT * FROM TemplateExercises WHERE TemplateMuscleSlotId IN ({slotIdsCsv}) ORDER BY SortOrder");
        }

        var slotsByDayId = slots
            .GroupBy(s => s.TemplateDayId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SortOrder).ToList());

        var exercisesBySlotId = exercises
            .GroupBy(e => e.TemplateMuscleSlotId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.SortOrder).ToList());

        return new TemplateStructure(template, days, slotsByDayId, exercisesBySlotId);
    }
}
