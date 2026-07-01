using LockIn.Models;

namespace LockIn.Services;

public enum VolumeZone { UnderMEV, InMAV, NearMRV, OverMRV }

public sealed record WeeklyVolumeReport(
    MuscleGroup Muscle,
    int SetsThisWeek,
    int MEV,
    int MRV,
    VolumeZone Zone);

public interface IWeeklyVolumeService
{
    Task<IReadOnlyList<WeeklyVolumeReport>> GetCurrentWeekVolumeAsync();
}

// Räknar sets per muskelgrupp för aktuell ISO-vecka (mån–sön) och jämför mot MEV/MRV.
// Sekundärmuskelviktning (chinup som rygg + biceps) skippas medvetet — se plan-fil.
public class WeeklyVolumeService : IWeeklyVolumeService
{
    private readonly DatabaseService _db;

    public WeeklyVolumeService(DatabaseService db) => _db = db;

    public async Task<IReadOnlyList<WeeklyVolumeReport>> GetCurrentWeekVolumeAsync()
    {
        var weekStart = GetMondayThisWeek();
        var weekEnd = weekStart.AddDays(7);
        var landmarks = await _db.GetMuscleVolumeLandmarksAsync();
        var landmarkByMuscle = landmarks.ToDictionary(l => l.MuscleGroup);

        // Aggregera sets per muskelgrupp för denna vecka
        var sessions = await _db.GetCompletedSessionsInRangeAsync(weekStart, weekEnd);
        var setsByMuscle = new Dictionary<MuscleGroup, int>();
        foreach (var s in sessions)
        {
            var volumes = await _db.GetSessionVolumeByMuscleGroupAsync(s.Id);
            foreach (var kv in volumes)
            {
                setsByMuscle.TryGetValue(kv.Key, out var existing);
                setsByMuscle[kv.Key] = existing + kv.Value.Sets;
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
            var zone = ClassifyZone(sets, mev, mrv);
            reports.Add(new WeeklyVolumeReport(mg, sets, mev, mrv, zone));
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
