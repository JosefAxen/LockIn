using LockIn.Models;

namespace LockIn.Services;

public sealed record MesoBuilderSpec(string Name, int WeekCount, DateTime StartDate);

public interface IMesoBuilderService
{
    Task<TrainingCycle> BuildAsync(MesoBuilderSpec spec);
}

// Genererar en RP-inspirerad mesocykel: N-1 Accumulation-veckor + 1 Deload.
// Volym/vikt-progression sker naturligt via WeightProgressionService och SetProgressionEngine
// baserat på faktisk RIR/feedback — cykeln lagrar bara IntensityPercent-band per vecka.
public class MesoBuilderService : IMesoBuilderService
{
    private readonly DatabaseService _db;

    public MesoBuilderService(DatabaseService db) => _db = db;

    public async Task<TrainingCycle> BuildAsync(MesoBuilderSpec spec)
    {
        // Inaktivera tidigare aktiva cykler
        var existing = await _db.GetCyclesAsync();
        foreach (var c in existing.Where(c => c.IsActive))
        {
            c.IsActive = false;
            await _db.SaveCycleAsync(c, await _db.GetCycleWeeksAsync(c.Id),
                Enumerable.Range(0, (await _db.GetCycleWeeksAsync(c.Id)).Count)
                    .Select(_ => new List<CycleSession>()).ToList());
        }

        var cycle = new TrainingCycle
        {
            Name      = spec.Name,
            StartDate = spec.StartDate.Date,
            WeekCount = spec.WeekCount,
            IsActive  = true,
        };

        var weeks = new List<CycleWeek>();
        var sessionsByWeek = new List<List<CycleSession>>();
        var accumulationWeeks = spec.WeekCount - 1;

        for (int i = 0; i < spec.WeekCount; i++)
        {
            var isDeload = i == spec.WeekCount - 1;
            int intensityPct;
            string label;
            if (isDeload)
            {
                intensityPct = 50;
                label = Resources.Strings.AppResources.MesoBuilder_Week_Deload;
            }
            else if (accumulationWeeks <= 1)
            {
                intensityPct = 80;
                label = string.Format(Resources.Strings.AppResources.MesoBuilder_Week_Accum, i + 1);
            }
            else
            {
                intensityPct = 70 + (int)Math.Round(20.0 * i / (accumulationWeeks - 1));
                label = string.Format(Resources.Strings.AppResources.MesoBuilder_Week_Accum, i + 1);
            }
            weeks.Add(new CycleWeek
            {
                WeekNumber       = i + 1,
                IntensityPercent = intensityPct,
                Label            = label,
            });
            sessionsByWeek.Add(new List<CycleSession>());
        }

        await _db.SaveCycleAsync(cycle, weeks, sessionsByWeek);
        return cycle;
    }
}
