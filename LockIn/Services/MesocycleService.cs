using LockIn.Models;

namespace LockIn.Services;

public sealed record MesocycleDetail(
    Mesocycle Meso,
    WorkoutTemplate? Template,
    IReadOnlyList<MesocycleWeek> Weeks,
    IReadOnlyList<MesocyclePriority> Priorities);

public interface IMesocycleService
{
    Task<List<Mesocycle>> ListAsync();
    Task<Mesocycle?> GetActiveAsync();
    Task<MesocycleDetail?> GetDetailAsync(int mesocycleId);
    Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks);
    Task ActivateAsync(int mesocycleId);
    Task DeleteAsync(int mesocycleId);
    Task<MesocycleWeek?> GetCurrentWeekAsync();
    Task<MesocyclePriority?> GetPriorityAsync(int mesocycleId, MuscleGroup muscle);
}

public class MesocycleService : IMesocycleService
{
    private readonly DatabaseService _db;
    public MesocycleService(DatabaseService db) => _db = db;

    public Task<List<Mesocycle>> ListAsync() => _db.GetMesocyclesAsync();
    public Task<Mesocycle?> GetActiveAsync() => _db.GetActiveMesocycleAsync();

    public async Task<MesocycleDetail?> GetDetailAsync(int mesocycleId)
    {
        var meso = await _db.GetMesocycleAsync(mesocycleId);
        if (meso is null) return null;
        var template = await _db.GetTemplateAsync(meso.TemplateId);
        var weeks = await _db.GetMesocycleWeeksAsync(mesocycleId);
        var priorities = await _db.GetMesocyclePrioritiesAsync(mesocycleId);
        return new MesocycleDetail(meso, template, weeks, priorities);
    }

    public Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks)
        => _db.SaveMesocycleAsync(meso, priorities, weeks);

    public Task ActivateAsync(int mesocycleId) => _db.ActivateMesocycleAsync(mesocycleId);
    public Task DeleteAsync(int mesocycleId) => _db.DeleteMesocycleAsync(mesocycleId);

    public async Task<MesocycleWeek?> GetCurrentWeekAsync()
    {
        var active = await GetActiveAsync();
        if (active is null) return null;

        var daysSinceStart = (DateTime.Today - active.StartDate.Date).Days;
        var weekIndex = Math.Max(0, daysSinceStart / 7);
        var weekNumber = weekIndex + 1;
        if (weekNumber > active.WeekCount) return null;

        var weeks = await _db.GetMesocycleWeeksAsync(active.Id);
        return weeks.FirstOrDefault(w => w.WeekNumber == weekNumber);
    }

    public async Task<MesocyclePriority?> GetPriorityAsync(int mesocycleId, MuscleGroup muscle)
    {
        var priorities = await _db.GetMesocyclePrioritiesAsync(mesocycleId);
        return priorities.FirstOrDefault(p => p.MuscleGroupValue == (int)muscle);
    }
}
