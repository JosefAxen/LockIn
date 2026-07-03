using LockIn.Data.Repositories;
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
    private readonly IMesocycleRepository _mesocycles;
    private readonly ITemplateRepository _templates;

    public MesocycleService(IMesocycleRepository mesocycles, ITemplateRepository templates)
    {
        _mesocycles = mesocycles;
        _templates = templates;
    }

    public Task<List<Mesocycle>> ListAsync() => _mesocycles.GetAllAsync();
    public Task<Mesocycle?> GetActiveAsync() => _mesocycles.GetActiveAsync();

    // ADR-001 PR 5: bundle-metod hämtar weeks + priorities parallellt.
    public async Task<MesocycleDetail?> GetDetailAsync(int mesocycleId)
    {
        var bundle = await _mesocycles.GetBundleAsync(mesocycleId);
        if (bundle is null) return null;
        var template = await _templates.GetAsync(bundle.Mesocycle.TemplateId);
        return new MesocycleDetail(bundle.Mesocycle, template, bundle.Weeks, bundle.Priorities);
    }

    public Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks)
        => _mesocycles.SaveAsync(meso, priorities, weeks);

    public Task ActivateAsync(int mesocycleId) => _mesocycles.ActivateAsync(mesocycleId);
    public Task DeleteAsync(int mesocycleId) => _mesocycles.DeleteAsync(mesocycleId);

    public async Task<MesocycleWeek?> GetCurrentWeekAsync()
    {
        var active = await GetActiveAsync();
        if (active is null) return null;

        var daysSinceStart = (DateTime.Today - active.StartDate.Date).Days;
        var weekIndex = Math.Max(0, daysSinceStart / 7);
        var weekNumber = weekIndex + 1;
        if (weekNumber > active.WeekCount) return null;

        var weeks = await _mesocycles.GetWeeksAsync(active.Id);
        return weeks.FirstOrDefault(w => w.WeekNumber == weekNumber);
    }

    public async Task<MesocyclePriority?> GetPriorityAsync(int mesocycleId, MuscleGroup muscle)
    {
        var priorities = await _mesocycles.GetPrioritiesAsync(mesocycleId);
        return priorities.FirstOrDefault(p => p.MuscleGroupValue == (int)muscle);
    }
}
