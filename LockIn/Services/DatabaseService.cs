using LockIn.Models;
using SQLite;

namespace LockIn.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db = null!;
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public async Task InitAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lockin.db");
            _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<Exercise>();
            await _db.CreateTableAsync<WorkoutTemplate>();
            await _db.CreateTableAsync<TemplateExercise>();
            await _db.CreateTableAsync<WorkoutSession>();
            await _db.CreateTableAsync<SessionExercise>();
            await _db.CreateTableAsync<LoggedSet>();
            await _db.CreateTableAsync<AppSettings>();
            await _db.CreateTableAsync<BodyWeightEntry>();

            // Plan 3: SetType migrations
            try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN SetType INTEGER NOT NULL DEFAULT 0"); } catch { }
            try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN DurationSeconds INTEGER NOT NULL DEFAULT 0"); } catch { }

            await SeedAsync();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task SeedAsync()
    {
        var count = await _db.Table<Exercise>().CountAsync();
        if (count > 0) return;

        var exercises = new List<Exercise>
        {
            // Bröst
            new() { Name = "Bänkpress", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 180 },
            new() { Name = "Lutande bänkpress", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 180 },
            new() { Name = "Kabelkorsning", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 90 },
            new() { Name = "Dips", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 120 },

            // Rygg
            new() { Name = "Marklyft", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 240 },
            new() { Name = "Skivstångsrodd", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 180 },
            new() { Name = "Latsdrag", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 120 },
            new() { Name = "Chins", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 150 },
            new() { Name = "Sittande rodd", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 120 },

            // Axlar
            new() { Name = "Militärpress", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 180 },
            new() { Name = "Sidolyft", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },
            new() { Name = "Frontlyft", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },
            new() { Name = "Facepull", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },

            // Biceps
            new() { Name = "Skivstångscurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 90 },
            new() { Name = "Hammarcurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 90 },
            new() { Name = "Koncentrationscurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 60 },

            // Triceps
            new() { Name = "Tricepspressdown", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },
            new() { Name = "Skullcrusher", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },
            new() { Name = "Triceps dips", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },

            // Ben
            new() { Name = "Knäböj", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 240 },
            new() { Name = "Benpress", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 180 },
            new() { Name = "Rumänska marklyft", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 180 },
            new() { Name = "Utfall", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 120 },
            new() { Name = "Benböjning", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 90 },
            new() { Name = "Bensträckning", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 90 },
            new() { Name = "Stående vadpress", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 60 },

            // Core
            new() { Name = "Plankan", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
            new() { Name = "Crunches", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
            new() { Name = "Rysk twist", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
        };

        await _db.InsertAllAsync(exercises);

        var settings = await _db.Table<AppSettings>().FirstOrDefaultAsync();
        if (settings is null)
            await _db.InsertAsync(new AppSettings());
    }

    // ── Exercises ──────────────────────────────────────────────────────────

    public Task<List<Exercise>> GetExercisesAsync() =>
        _db.Table<Exercise>().OrderBy(e => e.Name).ToListAsync();

    public async Task<Exercise?> GetExerciseAsync(int id) =>
        await _db.Table<Exercise>().Where(e => e.Id == id).FirstOrDefaultAsync();

    public Task<int> SaveExerciseAsync(Exercise exercise) =>
        exercise.Id == 0 ? _db.InsertAsync(exercise) : _db.UpdateAsync(exercise);

    public Task<int> DeleteExerciseAsync(Exercise exercise) =>
        _db.DeleteAsync(exercise);

    // ── Templates ──────────────────────────────────────────────────────────

    public Task<List<WorkoutTemplate>> GetTemplatesAsync() =>
        _db.Table<WorkoutTemplate>().ToListAsync();

    public Task<int> SaveTemplateAsync(WorkoutTemplate template) =>
        template.Id == 0 ? _db.InsertAsync(template) : _db.UpdateAsync(template);

    public Task<int> DeleteTemplateAsync(WorkoutTemplate template) =>
        _db.DeleteAsync(template);

    public Task<List<TemplateExercise>> GetTemplateExercisesAsync(int templateId) =>
        _db.Table<TemplateExercise>().Where(te => te.TemplateId == templateId).OrderBy(te => te.OrderIndex).ToListAsync();

    public Task<int> SaveTemplateExerciseAsync(TemplateExercise te) =>
        te.Id == 0 ? _db.InsertAsync(te) : _db.UpdateAsync(te);

    public Task<int> DeleteTemplateExerciseAsync(TemplateExercise te) =>
        _db.DeleteAsync(te);

    // ── Sessions ───────────────────────────────────────────────────────────

    public Task<int> SaveSessionAsync(WorkoutSession session) =>
        session.Id == 0 ? _db.InsertAsync(session) : _db.UpdateAsync(session);

    public Task<List<WorkoutSession>> GetSessionsAsync() =>
        _db.Table<WorkoutSession>().OrderByDescending(s => s.StartedAt).ToListAsync();

    public Task<int> SaveSessionExerciseAsync(SessionExercise se) =>
        se.Id == 0 ? _db.InsertAsync(se) : _db.UpdateAsync(se);

    public Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId) =>
        _db.Table<SessionExercise>().Where(se => se.SessionId == sessionId).OrderBy(se => se.OrderIndex).ToListAsync();

    // ── Logged Sets ────────────────────────────────────────────────────────

    public Task<int> SaveLoggedSetAsync(LoggedSet set) =>
        set.Id == 0 ? _db.InsertAsync(set) : _db.UpdateAsync(set);

    public Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId) =>
        _db.Table<LoggedSet>().Where(s => s.SessionExerciseId == sessionExerciseId).OrderBy(s => s.SetNumber).ToListAsync();

    public Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId) =>
        _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ls.LoggedAt DESC", exerciseId);

    public async Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId)
    {
        var sets = await _db.QueryAsync<LoggedSet>(
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

    // ── History ────────────────────────────────────────────────────────────

    public async Task<List<(DateTime Date, decimal WeightKg, int Reps, double Epley1RM, bool IsPR)>> GetBestSetPerSessionForExerciseAsync(int exerciseId)
    {
        var rows = await _db.QueryAsync<BestSetRow>(
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
            .ToList();
    }

    public async Task<Dictionary<MuscleGroup, (decimal Volume, int Sets)>> GetSessionVolumeByMuscleGroupAsync(int sessionId)
    {
        var rows = await _db.QueryAsync<MuscleVolumeRow>(
            @"SELECT e.MuscleGroup, ls.WeightKg, ls.Reps
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              WHERE se.SessionId = ? AND ls.SetType = 0", sessionId);

        return rows
            .GroupBy(r => (MuscleGroup)r.MuscleGroup)
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(r => r.WeightKg * r.Reps), g.Count()));
    }

    public Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId) =>
        _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ? AND ls.IsPR = 1", sessionId);

    public Task<List<SessionSummaryRow>> GetCompletedSessionsAsync() =>
        _db.QueryAsync<SessionSummaryRow>(
            @"SELECT ws.Id, ws.StartedAt, ws.CompletedAt, ws.Notes,
                     COALESCE(wt.Name, 'Fritt pass') as TemplateName,
                     COALESCE(SUM(ls.WeightKg * ls.Reps), 0) as TotalVolume,
                     COUNT(ls.Id) as TotalSets,
                     SUM(CASE WHEN ls.IsPR = 1 THEN 1 ELSE 0 END) as PRCount
              FROM WorkoutSessions ws
              LEFT JOIN WorkoutTemplates wt ON wt.Id = ws.TemplateId
              LEFT JOIN SessionExercises se ON se.SessionId = ws.Id
              LEFT JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE ws.CompletedAt IS NOT NULL
              GROUP BY ws.Id
              ORDER BY ws.StartedAt DESC");

    public Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId) =>
        _db.QueryAsync<SessionExerciseDetailRow>(
            @"SELECT e.Name as ExerciseName, ls.SetNumber, ls.WeightKg, ls.Reps, ls.RIR, ls.IsPR,
                     ls.SetType, ls.DurationSeconds
              FROM SessionExercises se
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE se.SessionId = ?
              ORDER BY se.OrderIndex, ls.SetNumber", sessionId);

    // ── Train stats ────────────────────────────────────────────────────────

    public Task<int> GetTotalCompletedSessionCountAsync() =>
        _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL");

    public Task<int> GetSessionCountThisWeekAsync()
    {
        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var monday = today.AddDays(-daysFromMonday);
        return _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND StartedAt >= ?",
            monday);
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        var rows = await _db.QueryAsync<SessionDateRow>(
            "SELECT StartedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL ORDER BY StartedAt DESC");

        var distinctDates = rows
            .Select(r => r.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (distinctDates.Count == 0) return 0;
        var today = DateTime.Today;
        if (distinctDates[0] < today.AddDays(-1)) return 0;

        var expected = distinctDates[0];
        int streak = 0;
        foreach (var d in distinctDates)
        {
            if (d == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }
        return streak;
    }

    // ── Body weight ────────────────────────────────────────────────────────

    public Task<List<BodyWeightEntry>> GetBodyWeightEntriesAsync() =>
        _db.Table<BodyWeightEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();

    public Task<int> SaveBodyWeightEntryAsync(BodyWeightEntry entry) =>
        entry.Id == 0 ? _db.InsertAsync(entry) : _db.UpdateAsync(entry);

    public Task<int> DeleteBodyWeightEntryAsync(BodyWeightEntry entry) =>
        _db.DeleteAsync(entry);

    public Task<int> DeleteAllDataAsync() => _db.DeleteAllAsync<LoggedSet>()
        .ContinueWith(_ => _db.DeleteAllAsync<SessionExercise>()).Unwrap()
        .ContinueWith(_ => _db.DeleteAllAsync<WorkoutSession>()).Unwrap()
        .ContinueWith(_ => _db.DeleteAllAsync<TemplateExercise>()).Unwrap()
        .ContinueWith(_ => _db.DeleteAllAsync<WorkoutTemplate>()).Unwrap()
        .ContinueWith(_ => _db.DeleteAllAsync<Exercise>()).Unwrap()
        .ContinueWith(async _ => { await SeedAsync(); return 0; }).Unwrap();

    public class SessionSummaryRow
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TemplateName { get; set; } = "";
        public decimal TotalVolume { get; set; }
        public int TotalSets { get; set; }
        public int PRCount { get; set; }
        public string Notes { get; set; } = "";
    }

    public class SessionExerciseDetailRow
    {
        public string ExerciseName { get; set; } = "";
        public int SetNumber { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
        public int RIR { get; set; }
        public bool IsPR { get; set; }
        public Models.SetType SetType { get; set; } = Models.SetType.Normal;
        public int DurationSeconds { get; set; } = 0;

        public string SetDisplay => SetType == Models.SetType.Time
            ? $"⏱ {DurationSeconds}s"
            : $"{WeightKg:G} kg × {Reps}";
    }

    private class BestSetRow
    {
        public DateTime SessionDate { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
        public bool IsPR { get; set; }
    }

    private class MuscleVolumeRow
    {
        public int MuscleGroup { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
    }

    private class SessionDateRow
    {
        public DateTime StartedAt { get; set; }
    }

    // ── Settings ───────────────────────────────────────────────────────────

    public Task<AppSettings> GetSettingsAsync() =>
        _db.Table<AppSettings>().FirstAsync();

    public Task<int> SaveSettingsAsync(AppSettings settings) =>
        _db.UpdateAsync(settings);
}
