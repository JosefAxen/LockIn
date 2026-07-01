using LockIn.Models;

namespace LockIn.Services;

public sealed record CycleContext(
    TrainingCycle Cycle,
    CycleWeek CurrentWeek,
    int TotalWeeks,
    int TargetRir,
    bool IsDeloadWeek);

public interface ICycleContextService
{
    Task<CycleContext?> GetCurrentAsync();
}

// Beräknar aktuell cykel-vecka + RIR-mål utifrån aktiv TrainingCycle och dagens datum.
// RIR-progression (docs/research avsnitt 3): trappas ned från ~4 första veckan till 1 sista pre-deload,
// deload-vecka fixar 4. Sista veckan i cykeln antas vara deload.
public class CycleContextService : ICycleContextService
{
    private readonly DatabaseService _db;

    public CycleContextService(DatabaseService db) => _db = db;

    public async Task<CycleContext?> GetCurrentAsync()
    {
        var cycles = await _db.GetCyclesAsync();
        var active = cycles.FirstOrDefault(c => c.IsActive);
        if (active is null) return null;

        var weeks = await _db.GetCycleWeeksAsync(active.Id);
        if (weeks.Count == 0) return null;

        var daysSinceStart = (DateTime.Today - active.StartDate.Date).Days;
        if (daysSinceStart < 0) daysSinceStart = 0;
        var weekIndex = daysSinceStart / 7; // 0-based
        if (weekIndex >= weeks.Count) weekIndex = weeks.Count - 1;
        var currentWeek = weeks[weekIndex];

        var totalWeeks = weeks.Count;
        var isDeload = weekIndex == totalWeeks - 1;
        var targetRir = ComputeTargetRir(weekIndex, totalWeeks);

        return new CycleContext(active, currentWeek, totalWeeks, targetRir, isDeload);
    }

    // RIR-target per vecka. Deload-vecka = 4. Övriga: linjär nedtrappning 4 → 1
    // över accumulation-veckorna (vecka 1 till (totalWeeks - 1)).
    private static int ComputeTargetRir(int weekIndex, int totalWeeks)
    {
        if (totalWeeks <= 1) return 3;
        if (weekIndex == totalWeeks - 1) return 4; // deload
        var accumulationWeeks = totalWeeks - 1;
        if (accumulationWeeks == 1) return 3;
        var progress = (double)weekIndex / (accumulationWeeks - 1); // 0.0 → 1.0
        var raw = 4.0 - progress * 3.0; // 4.0 → 1.0
        return Math.Max(1, (int)Math.Round(raw));
    }
}
