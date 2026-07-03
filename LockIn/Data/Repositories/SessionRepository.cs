using LockIn.Models;
using LockIn.Services;
using static LockIn.Services.DatabaseService;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för WorkoutSession + SessionExercise + LoggedSet + Feedback + CardioSession.
/// Del av ADR-001 PR 6 av 7.
/// </summary>
public sealed class SessionRepository : ISessionRepository
{
    private readonly DatabaseService _db;

    public SessionRepository(DatabaseService db) => _db = db;

    // ── WorkoutSession ─────────────────────────────────────────────────────
    public async Task<int> SaveSessionAsync(WorkoutSession session)
    {
        var db = await _db.EnsureConnectionAsync();
        return session.Id == 0 ? await db.InsertAsync(session) : await db.UpdateAsync(session);
    }

    public async Task<WorkoutSession?> GetSessionAsync(int id)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutSession>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<WorkoutSession>> GetSessionsAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutSession>().OrderByDescending(s => s.StartedAt).ToListAsync();
    }

    public async Task<List<WorkoutSession>> GetCompletedInRangeAsync(DateTime from, DateTime to)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<WorkoutSession>(
            "SELECT * FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND CompletedAt >= ? AND CompletedAt <= ? ORDER BY StartedAt DESC",
            from, to);
    }

    public async Task<List<WorkoutSession>> GetRecentCompletedAsync(int limit)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<WorkoutSession>()
            .Where(s => s.CompletedAt != null)
            .OrderByDescending(s => s.CompletedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> HasAnyCompletedAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL") > 0;
    }

    public async Task<List<SessionSummaryRow>> GetCompletedSummariesAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<SessionSummaryRow>(
            @"SELECT ws.Id, ws.StartedAt, ws.CompletedAt, ws.Notes,
                     '' as TemplateName,
                     COALESCE(SUM(ls.WeightKg * ls.Reps), 0) as TotalVolume,
                     COUNT(ls.Id) as TotalSets,
                     SUM(CASE WHEN ls.IsPR = 1 THEN 1 ELSE 0 END) as PRCount
              FROM WorkoutSessions ws
              LEFT JOIN SessionExercises se ON se.SessionId = ws.Id
              LEFT JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE ws.CompletedAt IS NOT NULL
              GROUP BY ws.Id
              ORDER BY ws.StartedAt DESC");
    }

    public async Task<int> GetTotalCompletedCountAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL");
    }

    public async Task<int> GetCountThisWeekAsync()
    {
        var db = await _db.EnsureConnectionAsync();
        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var monday = today.AddDays(-daysFromMonday);
        return await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND StartedAt >= ?",
            monday);
    }

    // ── SessionExercise ────────────────────────────────────────────────────
    public async Task<int> SaveSessionExerciseAsync(SessionExercise se)
    {
        var db = await _db.EnsureConnectionAsync();
        return se.Id == 0 ? await db.InsertAsync(se) : await db.UpdateAsync(se);
    }

    public async Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<SessionExercise>()
            .Where(se => se.SessionId == sessionId)
            .OrderBy(se => se.OrderIndex)
            .ToListAsync();
    }

    public async Task DeleteSessionExerciseWithSetsAsync(int sessionExerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.ExecuteAsync("DELETE FROM LoggedSets WHERE SessionExerciseId = ?", sessionExerciseId);
        await db.ExecuteAsync("DELETE FROM SessionExercises WHERE Id = ?", sessionExerciseId);
    }

    public async Task SetSessionExerciseNotesAsync(int sessionExerciseId, string notes)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.ExecuteAsync(
            "UPDATE SessionExercises SET Notes = ? WHERE Id = ?",
            notes ?? "", sessionExerciseId);
    }

    public async Task UpdateSessionExerciseExerciseIdAsync(int sessionExerciseId, int newExerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.ExecuteAsync(
            "UPDATE SessionExercises SET ExerciseId = ? WHERE Id = ?",
            newExerciseId, sessionExerciseId);
    }

    public async Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<SessionExerciseDetailRow>(
            @"SELECT e.Name as ExerciseName, ls.SetNumber, ls.WeightKg, ls.Reps, ls.RIR, ls.IsPR,
                     ls.SetType, ls.DurationSeconds
              FROM SessionExercises se
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE se.SessionId = ?
              ORDER BY se.OrderIndex, ls.SetNumber", sessionId);
    }

    // ── LoggedSet ──────────────────────────────────────────────────────────
    public async Task<int> SaveLoggedSetAsync(LoggedSet set)
    {
        var db = await _db.EnsureConnectionAsync();
        return set.Id == 0 ? await db.InsertAsync(set) : await db.UpdateAsync(set);
    }

    public async Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<LoggedSet>()
            .Where(s => s.SessionExerciseId == sessionExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();
    }

    public async Task<List<LoggedSet>> GetLoggedSetsForSessionExerciseAsync(int sessionExerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<LoggedSet>()
            .Where(ls => ls.SessionExerciseId == sessionExerciseId)
            .OrderBy(ls => ls.SetNumber)
            .ToListAsync();
    }

    public async Task<List<LoggedSet>> GetAllSetsForSessionAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ?
              ORDER BY se.Id, ls.SetNumber", sessionId);
    }

    public async Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ls.LoggedAt DESC", exerciseId);
    }

    public async Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        var sets = await db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ? AND ws.Id != ? AND ws.CompletedAt IS NOT NULL
              ORDER BY ws.StartedAt DESC, ls.SetNumber ASC",
            exerciseId, excludeSessionId);

        if (sets.Count == 0) return sets;
        var latestSeId = sets[0].SessionExerciseId;
        return sets.Where(s => s.SessionExerciseId == latestSeId).ToList();
    }

    public async Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ? AND ls.IsPR = 1", sessionId);
    }

    public async Task<double> GetMaxEpley1RMAsync(int exerciseId, int excludeLoggedSetId = 0)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.ExecuteScalarAsync<double>(
            @"SELECT COALESCE(MAX(WeightKg * (1.0 + CAST(Reps AS REAL) / 30.0)), 0)
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.ExerciseId = ? AND ls.Id != ?",
            exerciseId, excludeLoggedSetId);
    }

    public async Task<List<(DateTime Date, decimal WeightKg, int Reps, double Epley1RM, bool IsPR)>>
        GetBestSetPerSessionForExerciseAsync(int exerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        var rows = await db.QueryAsync<BestSetRow>(
            @"SELECT ws.StartedAt as SessionDate, ls.WeightKg, ls.Reps, ls.IsPR
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ws.StartedAt DESC", exerciseId);

        return rows
            .GroupBy(r => r.SessionDate.Date)
            .Select(g =>
            {
                var best = g.OrderByDescending(r => (double)r.WeightKg * (1 + r.Reps / 30.0)).First();
                return (best.SessionDate, best.WeightKg, best.Reps,
                    (double)best.WeightKg * (1 + best.Reps / 30.0), best.IsPR);
            })
            .OrderBy(r => r.SessionDate)
            .ToList();
    }

    public async Task<Dictionary<MuscleGroup, (decimal Volume, int Sets)>> GetSessionVolumeByMuscleGroupAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        var rows = await db.QueryAsync<MuscleVolumeRow>(
            @"SELECT e.MuscleGroup, ls.WeightKg, ls.Reps
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              WHERE se.SessionId = ? AND (ls.SetType = 0 OR ls.SetType IS NULL)", sessionId);

        return rows
            .GroupBy(r => (MuscleGroup)r.MuscleGroup)
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(r => r.WeightKg * r.Reps), g.Count()));
    }

    // ── Feedback ───────────────────────────────────────────────────────────
    public async Task<int> SaveMuscleFeedbackAsync(SessionMuscleFeedback fb)
    {
        var db = await _db.EnsureConnectionAsync();
        return fb.Id == 0 ? await db.InsertAsync(fb) : await db.UpdateAsync(fb);
    }

    public async Task<List<SessionMuscleFeedback>> GetMuscleFeedbackAsync(int sessionId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<SessionMuscleFeedback>()
            .Where(f => f.SessionId == sessionId).ToListAsync();
    }

    public async Task<int> SaveExerciseFeedbackAsync(SessionExerciseFeedback fb)
    {
        var db = await _db.EnsureConnectionAsync();
        return fb.Id == 0 ? await db.InsertAsync(fb) : await db.UpdateAsync(fb);
    }

    public async Task<SessionExerciseFeedback?> GetExerciseFeedbackAsync(int sessionExerciseId)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<SessionExerciseFeedback>()
            .Where(f => f.SessionExerciseId == sessionExerciseId).FirstOrDefaultAsync();
    }

    // ── Cardio ─────────────────────────────────────────────────────────────
    public async Task SaveCardioSessionAsync(CardioSession session)
    {
        var db = await _db.EnsureConnectionAsync();
        if (session.Id == 0)
            await db.InsertAsync(session);
        else
            await db.UpdateAsync(session);
    }

    public async Task<List<CardioSession>> GetCardioSessionsAsync(int limit = 50)
    {
        var db = await _db.EnsureConnectionAsync();
        return await db.Table<CardioSession>()
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteCardioSessionAsync(int id)
    {
        var db = await _db.EnsureConnectionAsync();
        await db.DeleteAsync<CardioSession>(id);
    }
}
