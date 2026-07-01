using LockIn.Models;

namespace LockIn.Services;

public sealed record VolumeAdvice(MuscleGroup Muscle, int SetDelta);
public sealed record DeloadAdvice(int SeveritySignal);

public sealed record SetProgressionAnalysis(
    IReadOnlyList<VolumeAdvice> VolumeAdvices,
    DeloadAdvice? DeloadAdvice);

public interface ISetProgressionEngine
{
    Task<SetProgressionAnalysis> AnalyzeAsync();
}

// Analyserar senaste passens recovery-markörer (pump/soreness/performance) och returnerar
// per-muskelgrupp volym-advice + eventuell deload-flagga. Källa: docs/research avsnitt 7.
public class SetProgressionEngine : ISetProgressionEngine
{
    private readonly DatabaseService _db;

    public SetProgressionEngine(DatabaseService db) => _db = db;

    public async Task<SetProgressionAnalysis> AnalyzeAsync()
    {
        // Kolla senaste 3 avslutade sessioner för att ha 2 pass per muskelgrupp
        var recent = await _db.GetCompletedSessionsInRangeAsync(
            DateTime.Now.AddDays(-21), DateTime.Now.AddDays(1));

        var withFeedback = recent
            .Where(s => s.PerformanceRating.HasValue || s.SorenessRating.HasValue || s.PumpRating.HasValue)
            .OrderByDescending(s => s.CompletedAt ?? s.StartedAt)
            .Take(3)
            .ToList();

        if (withFeedback.Count == 0)
            return new SetProgressionAnalysis(Array.Empty<VolumeAdvice>(), null);

        // ── Deload-signal ────────────────────────────────────────────────
        DeloadAdvice? deload = null;
        var last = withFeedback[0];
        if (last.PerformanceRating == 4)
        {
            deload = new DeloadAdvice(2); // stark signal
        }
        else if (withFeedback.Count >= 2 &&
                 withFeedback.Take(2).All(s => (s.SorenessRating ?? 0) >= 3 && (s.PerformanceRating ?? 0) >= 3))
        {
            deload = new DeloadAdvice(1); // 2 pass i rad med hög trötthet
        }

        // ── Volume-advice per muskelgrupp (kräver 2 pass med samma muskel) ─
        var advices = new List<VolumeAdvice>();
        if (withFeedback.Count >= 2)
        {
            var lastTwo = withFeedback.Take(2).ToList();
            var muscleGroupsBySession = new Dictionary<int, HashSet<MuscleGroup>>();
            foreach (var s in lastTwo)
            {
                var volumes = await _db.GetSessionVolumeByMuscleGroupAsync(s.Id);
                muscleGroupsBySession[s.Id] = new HashSet<MuscleGroup>(volumes.Keys);
            }
            var commonMuscles = muscleGroupsBySession[lastTwo[0].Id]
                .Intersect(muscleGroupsBySession[lastTwo[1].Id])
                .ToList();

            foreach (var mg in commonMuscles)
            {
                bool highSoreness = lastTwo.All(s => (s.SorenessRating ?? 0) >= 3);
                bool highPerf     = lastTwo.All(s => (s.PerformanceRating ?? 0) >= 3);
                bool lowPump      = lastTwo.All(s => (s.PumpRating ?? 3) <= 2);
                bool lowSoreness  = lastTwo.All(s => (s.SorenessRating ?? 3) <= 2);

                if (highSoreness && highPerf)
                    advices.Add(new VolumeAdvice(mg, -1));
                else if (lowPump && lowSoreness)
                    advices.Add(new VolumeAdvice(mg, +1));
            }
        }

        return new SetProgressionAnalysis(advices, deload);
    }
}
