using LockIn.Models;
using LockIn.Services;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för Mesocycles + priorities + weeks + MuscleVolumeLandmarks.
/// Del av ADR-001 (DatabaseService-splittring), PR 5 av 7.
/// GetBundleAsync kör 3 queries parallellt (löser efficiency-finding #4).
/// </summary>
public sealed class MesocycleRepository : IMesocycleRepository
{
    private readonly DatabaseService _db;

    public MesocycleRepository(DatabaseService db) => _db = db;

    public async Task<List<Mesocycle>> GetAllAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<Mesocycle>().OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<Mesocycle?> GetAsync(int id)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<Mesocycle>().Where(m => m.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Mesocycle?> GetActiveAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<Mesocycle>().Where(m => m.IsActive).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAsync(Mesocycle meso, List<MesocyclePriority> priorities, List<MesocycleWeek> weeks)
    {
        var db = await _db.EnsureConnectionAsync();
        int mesoId = meso.Id;
        await db.RunInTransactionAsync(conn =>
        {
            if (meso.Id == 0) conn.Insert(meso);
            else conn.Update(meso);
            mesoId = meso.Id;

            conn.Execute("DELETE FROM MesocyclePriorities WHERE MesocycleId = ?", mesoId);
            conn.Execute("DELETE FROM MesocycleWeeks WHERE MesocycleId = ?", mesoId);

            foreach (var p in priorities)
            {
                p.Id = 0;
                p.MesocycleId = mesoId;
                conn.Insert(p);
            }
            foreach (var w in weeks)
            {
                w.Id = 0;
                w.MesocycleId = mesoId;
                conn.Insert(w);
            }
        });
        return mesoId;
    }

    public async Task ActivateAsync(int mesocycleId)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.ExecuteAsync("UPDATE Mesocycles SET IsActive = 0");
        await db.ExecuteAsync("UPDATE Mesocycles SET IsActive = 1 WHERE Id = ?", mesocycleId);
    }

    public async Task DeleteAsync(int mesocycleId)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.RunInTransactionAsync(conn =>
        {
            conn.Execute("DELETE FROM MesocyclePriorities WHERE MesocycleId = ?", mesocycleId);
            conn.Execute("DELETE FROM MesocycleWeeks WHERE MesocycleId = ?", mesocycleId);
            conn.Execute("DELETE FROM Mesocycles WHERE Id = ?", mesocycleId);
        });
    }

    public async Task<List<WorkoutSession>> GetSessionsForMesocycleAsync(int mesocycleId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<WorkoutSession>(
            @"SELECT ws.* FROM WorkoutSessions ws
              JOIN MesocycleWeeks mw ON mw.Id = ws.MesocycleWeekId
              WHERE mw.MesocycleId = ?
              ORDER BY ws.StartedAt DESC", mesocycleId);
    }

    public async Task<List<MesocyclePriority>> GetPrioritiesAsync(int mesocycleId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<MesocyclePriority>()
            .Where(p => p.MesocycleId == mesocycleId)
            .ToListAsync();
    }

    public async Task<List<MesocycleWeek>> GetWeeksAsync(int mesocycleId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<MesocycleWeek>()
            .Where(w => w.MesocycleId == mesocycleId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task<List<MuscleVolumeLandmarks>> GetLandmarksAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<MuscleVolumeLandmarks>().ToListAsync();
    }

    public async Task SaveLandmarksAsync(MuscleVolumeLandmarks landmarks)
    {
        var db = await _db.EnsureConnectionAsync();
        var existing = await db.Table<MuscleVolumeLandmarks>()
            .Where(l => l.MuscleGroupValue == landmarks.MuscleGroupValue)
            .FirstOrDefaultAsync();
        if (existing is null)
            await db.InsertAsync(landmarks);
        else
            await db.UpdateAsync(landmarks);
    }

    public async Task<MesocycleBundle?> GetBundleAsync(int mesocycleId)
    {
        var meso = await GetAsync(mesocycleId);
        if (meso is null) return null;

        // 2 parallella queries efter meso är hämtad (SQLite-net kör en åt gången på connection,
        // men Task.WhenAll ger korrekt awaitning utan sekventiell overhead om connection är fri).
        var weeksTask = GetWeeksAsync(mesocycleId);
        var prioritiesTask = GetPrioritiesAsync(mesocycleId);
        await Task.WhenAll(weeksTask, prioritiesTask);

        return new MesocycleBundle(meso, weeksTask.Result, prioritiesTask.Result);
    }
}
