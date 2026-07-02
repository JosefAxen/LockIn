using LockIn.Models;

namespace LockIn.Services;

public sealed record CycleContext(int WeekNumber, int TotalWeeks, bool IsDeload, int TargetRir, string CycleName);

public interface ICycleContextService
{
    Task<CycleContext?> GetCurrentAsync();
}

// Beräknar aktuell mesocykel-vecka + RIR-mål utifrån aktiv Mesocycle och dagens datum.
// RIR-progression (plan sektion B): trappas ned från ~4 första veckan till 1 sista pre-deload,
// deload-vecka fixar 4. Sista veckan i cykeln antas vara deload om MesocycleWeek.IsDeload är satt.
public class CycleContextService : ICycleContextService
{
    private readonly IMesocycleService _mesocycles;

    public CycleContextService(IMesocycleService mesocycles) => _mesocycles = mesocycles;

    public async Task<CycleContext?> GetCurrentAsync()
    {
        var active = await _mesocycles.GetActiveAsync();
        if (active is null) return null;

        var currentWeek = await _mesocycles.GetCurrentWeekAsync();
        if (currentWeek is null) return null;

        var totalWeeks = active.WeekCount;
        var weekIndex = currentWeek.WeekNumber - 1; // 0-indexerad
        var isDeload = currentWeek.IsDeload;
        var targetRir = isDeload ? 4 : ComputeTargetRir(weekIndex, totalWeeks);

        return new CycleContext(currentWeek.WeekNumber, totalWeeks, isDeload, targetRir, active.Name);
    }

    // RIR-target per vecka. Deload-vecka = 4. Övriga: linjär nedtrappning 4 → 1
    // över accumulation-veckorna (vecka 1 till (totalWeeks - 1)).
    // Formel: plan sektion B.
    private static int ComputeTargetRir(int weekIndex, int totalWeeks)
    {
        if (totalWeeks <= 1) return 3;
        if (weekIndex == totalWeeks - 1) return 4; // deload (fallback om IsDeload inte satts)
        var accumulationWeeks = totalWeeks - 1;
        if (accumulationWeeks == 1) return 3;
        var progress = (double)weekIndex / (accumulationWeeks - 1); // 0.0 → 1.0
        var raw = 4.0 - progress * 3.0; // 4.0 → 1.0
        return Math.Max(1, (int)Math.Round(raw));
    }
}
