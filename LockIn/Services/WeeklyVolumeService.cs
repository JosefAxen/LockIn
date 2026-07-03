using LockIn.Data.Repositories;
using LockIn.Models;

namespace LockIn.Services;

public enum VolumeZone { UnderMEV, InMAV, NearMRV, OverMRV }

public sealed record WeeklyVolumeReport(
    MuscleGroup Muscle,
    int SetsThisWeek,
    int PlannedSets,
    int MEV,
    int MRV,
    VolumeZone Zone);

public interface IWeeklyVolumeService
{
    Task<IReadOnlyList<WeeklyVolumeReport>> GetCurrentWeekVolumeAsync();
}

// Räknar sets per muskelgrupp för aktuell ISO-vecka (mån–sön) och jämför mot MEV/MRV.
// Implementerar plan sektion H med korrekt filtrering (ej warmup, ej skippade, ej tomma).
// PlannedSets hämtas från VolumeProgressionService om aktiv mesocykel finns.
public class WeeklyVolumeService : IWeeklyVolumeService
{
    private readonly DatabaseService _db;
    private readonly IMesocycleRepository _mesocyclesRepo;
    private readonly IVolumeProgressionService _volumeProgression;

    public WeeklyVolumeService(DatabaseService db, IMesocycleRepository mesocyclesRepo, IVolumeProgressionService volumeProgression)
    {
        _db = db;
        _mesocyclesRepo = mesocyclesRepo;
        _volumeProgression = volumeProgression;
    }

    public async Task<IReadOnlyList<WeeklyVolumeReport>> GetCurrentWeekVolumeAsync()
    {
        var weekStart = GetMondayThisWeek();
        var weekEnd = weekStart.AddDays(7);
        var landmarks = await _mesocyclesRepo.GetLandmarksAsync();
        var landmarkByMuscle = landmarks.ToDictionary(l => l.MuscleGroup);

        // Aggregera sets per muskelgrupp via direkta SQL-joins för korrekt filtrering
        // (IsSkipped, SetType, tomma sets) enligt plan sektion H
        var sessions = await _db.GetCompletedSessionsInRangeAsync(weekStart, weekEnd);
        var setsByMuscle = new Dictionary<MuscleGroup, int>();

        foreach (var session in sessions)
        {
            var exercises = await _db.GetSessionExercisesAsync(session.Id);
            foreach (var se in exercises)
            {
                if (se.IsSkipped) continue;
                var muscle = se.MuscleGroup;

                var sets = await _db.GetLoggedSetsForSessionExerciseAsync(se.Id);
                foreach (var ls in sets)
                {
                    if (ls.SetType == SetType.Warmup) continue;
                    if (ls.WeightKg == 0 && ls.Reps == 0) continue; // inte loggat

                    setsByMuscle.TryGetValue(muscle, out var current);
                    setsByMuscle[muscle] = current + 1;
                }
            }
        }

        var order = new[]
        {
            MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
            MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Legs,
            MuscleGroup.Core, MuscleGroup.Forearms,
        };

        var reports = new List<WeeklyVolumeReport>(order.Length);
        foreach (var mg in order)
        {
            setsByMuscle.TryGetValue(mg, out var sets);
            var mev = landmarkByMuscle.TryGetValue(mg, out var lm) ? lm.MEV : 8;
            var mrv = landmarkByMuscle.TryGetValue(mg, out var lm2) ? lm2.MRV : 18;
            var planned = await _volumeProgression.GetTargetSetsForCurrentWeekAsync(mg);
            var zone = ClassifyZone(sets, mev, mrv);
            reports.Add(new WeeklyVolumeReport(mg, sets, planned, mev, mrv, zone));
        }
        return reports;
    }

    private static VolumeZone ClassifyZone(int sets, int mev, int mrv)
    {
        if (sets < mev) return VolumeZone.UnderMEV;
        if (sets > mrv) return VolumeZone.OverMRV;
        // NearMRV = de sista 20 % av intervallet MEV..MRV
        var nearThreshold = mev + (int)Math.Round((mrv - mev) * 0.8);
        if (sets >= nearThreshold) return VolumeZone.NearMRV;
        return VolumeZone.InMAV;
    }

    private static DateTime GetMondayThisWeek()
    {
        var today = DateTime.Today;
        int diff = ((int)today.DayOfWeek + 6) % 7; // 0=Mon..6=Sun
        return today.AddDays(-diff);
    }
}
