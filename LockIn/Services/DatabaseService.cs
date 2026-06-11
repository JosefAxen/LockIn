using LockIn.Models;
using SQLite;

namespace LockIn.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db = null!;
    private readonly Lazy<Task> _lazyInit;

    public DatabaseService()
    {
        _lazyInit = new Lazy<Task>(() => InitCoreAsync(),
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task InitAsync() => _lazyInit.Value;

    private async Task InitCoreAsync()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lockin.db");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<Exercise>();
        await _db.CreateTableAsync<WorkoutTemplate>();
        await _db.CreateTableAsync<TemplateExercise>();
        await _db.CreateTableAsync<WorkoutSession>();
        await _db.CreateTableAsync<SessionExercise>();
        await _db.CreateTableAsync<LoggedSet>();
        await _db.CreateTableAsync<AppSettings>();
        await _db.CreateTableAsync<BodyWeightEntry>();
        await _db.CreateTableAsync<BodyCompositionEntry>();

        try { await _db.ExecuteAsync("ALTER TABLE WorkoutTemplates ADD COLUMN ProgramId TEXT NULL"); } catch { }

        // Plan 3: SetType migrations (no-op if already added by sqlite-net-pcl)
        try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN SetType INTEGER NOT NULL DEFAULT 0"); } catch { }
        try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN DurationSeconds INTEGER NOT NULL DEFAULT 0"); } catch { }
        // Backfill NULLs that sqlite-net-pcl may have added without DEFAULT
        try { await _db.ExecuteAsync("UPDATE LoggedSets SET SetType = 0 WHERE SetType IS NULL"); } catch { }
        try { await _db.ExecuteAsync("UPDATE LoggedSets SET DurationSeconds = 0 WHERE DurationSeconds IS NULL"); } catch { }

        await SeedAsync();
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

    public async Task<List<Exercise>> GetExercisesAsync()
    {
        await InitAsync();
        return await _db.Table<Exercise>().OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Exercise?> GetExerciseAsync(int id)
    {
        await InitAsync();
        return await _db.Table<Exercise>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return exercise.Id == 0 ? await _db.InsertAsync(exercise) : await _db.UpdateAsync(exercise);
    }

    public async Task<int> DeleteExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return await _db.DeleteAsync(exercise);
    }

    // ── Templates ──────────────────────────────────────────────────────────

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync()
    {
        await InitAsync();
        return await _db.Table<WorkoutTemplate>().ToListAsync();
    }

    public async Task<int> SaveTemplateAsync(WorkoutTemplate template)
    {
        await InitAsync();
        return template.Id == 0 ? await _db.InsertAsync(template) : await _db.UpdateAsync(template);
    }

    public async Task<int> DeleteTemplateAsync(WorkoutTemplate template)
    {
        await InitAsync();
        return await _db.DeleteAsync(template);
    }

    public async Task<List<TemplateExercise>> GetTemplateExercisesAsync(int templateId)
    {
        await InitAsync();
        return await _db.Table<TemplateExercise>()
            .Where(te => te.TemplateId == templateId)
            .OrderBy(te => te.OrderIndex)
            .ToListAsync();
    }

    public async Task<int> SaveTemplateExerciseAsync(TemplateExercise te)
    {
        await InitAsync();
        return te.Id == 0 ? await _db.InsertAsync(te) : await _db.UpdateAsync(te);
    }

    public async Task<int> DeleteTemplateExerciseAsync(TemplateExercise te)
    {
        await InitAsync();
        return await _db.DeleteAsync(te);
    }

    // ── Sessions ───────────────────────────────────────────────────────────

    public async Task<int> SaveSessionAsync(WorkoutSession session)
    {
        await InitAsync();
        return session.Id == 0 ? await _db.InsertAsync(session) : await _db.UpdateAsync(session);
    }

    public async Task<List<WorkoutSession>> GetSessionsAsync()
    {
        await InitAsync();
        return await _db.Table<WorkoutSession>().OrderByDescending(s => s.StartedAt).ToListAsync();
    }

    public async Task<int> SaveSessionExerciseAsync(SessionExercise se)
    {
        await InitAsync();
        return se.Id == 0 ? await _db.InsertAsync(se) : await _db.UpdateAsync(se);
    }

    public async Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId)
    {
        await InitAsync();
        return await _db.Table<SessionExercise>()
            .Where(se => se.SessionId == sessionId)
            .OrderBy(se => se.OrderIndex)
            .ToListAsync();
    }

    // ── Logged Sets ────────────────────────────────────────────────────────

    public async Task<int> SaveLoggedSetAsync(LoggedSet set)
    {
        await InitAsync();
        return set.Id == 0 ? await _db.InsertAsync(set) : await _db.UpdateAsync(set);
    }

    public async Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId)
    {
        await InitAsync();
        return await _db.Table<LoggedSet>()
            .Where(s => s.SessionExerciseId == sessionExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();
    }

    public async Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId)
    {
        await InitAsync();
        return await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ls.LoggedAt DESC", exerciseId);
    }

    public async Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId)
    {
        await InitAsync();
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
        await InitAsync();
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
        await InitAsync();
        var rows = await _db.QueryAsync<MuscleVolumeRow>(
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

    public async Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId)
    {
        await InitAsync();
        return await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ? AND ls.IsPR = 1", sessionId);
    }

    public async Task<List<SessionSummaryRow>> GetCompletedSessionsAsync()
    {
        await InitAsync();
        return await _db.QueryAsync<SessionSummaryRow>(
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
    }

    public async Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId)
    {
        await InitAsync();
        return await _db.QueryAsync<SessionExerciseDetailRow>(
            @"SELECT e.Name as ExerciseName, ls.SetNumber, ls.WeightKg, ls.Reps, ls.RIR, ls.IsPR,
                     ls.SetType, ls.DurationSeconds
              FROM SessionExercises se
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE se.SessionId = ?
              ORDER BY se.OrderIndex, ls.SetNumber", sessionId);
    }

    // ── Train stats ────────────────────────────────────────────────────────

    public async Task<int> GetTotalCompletedSessionCountAsync()
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL");
    }

    public async Task<int> GetSessionCountThisWeekAsync()
    {
        await InitAsync();
        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var monday = today.AddDays(-daysFromMonday);
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND StartedAt >= ?",
            monday);
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        await InitAsync();
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

    public async Task<List<BodyWeightEntry>> GetBodyWeightEntriesAsync()
    {
        await InitAsync();
        return await _db.Table<BodyWeightEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveBodyWeightEntryAsync(BodyWeightEntry entry)
    {
        await InitAsync();
        return entry.Id == 0 ? await _db.InsertAsync(entry) : await _db.UpdateAsync(entry);
    }

    public async Task<int> DeleteBodyWeightEntryAsync(BodyWeightEntry entry)
    {
        await InitAsync();
        return await _db.DeleteAsync(entry);
    }

    public async Task<int> DeleteAllDataAsync()
    {
        await InitAsync();
        await _db.DeleteAllAsync<LoggedSet>();
        await _db.DeleteAllAsync<SessionExercise>();
        await _db.DeleteAllAsync<WorkoutSession>();
        await _db.DeleteAllAsync<TemplateExercise>();
        await _db.DeleteAllAsync<WorkoutTemplate>();
        await _db.DeleteAllAsync<Exercise>();
        await _db.DeleteAllAsync<BodyWeightEntry>();
        await _db.DeleteAllAsync<BodyCompositionEntry>();
        await SeedAsync();
        return 0;
    }

    // ── Muscle scores ──────────────────────────────────────────────────────

    public async Task<Dictionary<MuscleGroup, double>> GetMuscleScoresAsync()
    {
        await InitAsync();
        var cutoff = DateTime.Now.AddDays(-7);
        var rows = await _db.QueryAsync<MuscleSetRow>(
            @"SELECT e.MuscleGroup, ls.RIR
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL
                AND ws.StartedAt >= ?
                AND (ls.SetType = 0 OR ls.SetType IS NULL)", cutoff);

        var result = new Dictionary<MuscleGroup, double>();
        foreach (var g in rows.GroupBy(r => (MuscleGroup)r.MuscleGroup))
        {
            var sets = g.ToList();
            var avgIntensity = sets.Average(s => 1.0 - Math.Max(s.RIR, 0) / 6.0);
            var score = Math.Min(sets.Count * avgIntensity, 10.0);
            result[g.Key] = Math.Round(score, 1);
        }
        return result;
    }

    public async Task<HashSet<int>> GetTrainedDaysInMonthAsync(int year, int month)
    {
        await InitAsync();
        var rows = await _db.QueryAsync<SessionDateRow>(
            "SELECT StartedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL");
        return rows
            .Where(r => r.StartedAt.Year == year && r.StartedAt.Month == month)
            .Select(r => r.StartedAt.Day)
            .ToHashSet();
    }

    // ── Body composition ───────────────────────────────────────────────────

    public async Task<List<BodyCompositionEntry>> GetBodyCompositionEntriesAsync()
    {
        await InitAsync();
        return await _db.Table<BodyCompositionEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveBodyCompositionEntryAsync(BodyCompositionEntry entry)
    {
        await InitAsync();
        return entry.Id == 0 ? await _db.InsertAsync(entry) : await _db.UpdateAsync(entry);
    }

    public async Task<int> DeleteBodyCompositionEntryAsync(BodyCompositionEntry entry)
    {
        await InitAsync();
        return await _db.DeleteAsync(entry);
    }

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

    private class MuscleSetRow
    {
        public int MuscleGroup { get; set; }
        public int RIR { get; set; }
    }

    // ── Settings ───────────────────────────────────────────────────────────

    public async Task<AppSettings> GetSettingsAsync()
    {
        await InitAsync();
        return await _db.Table<AppSettings>().FirstAsync();
    }

    public async Task<int> SaveSettingsAsync(AppSettings settings)
    {
        await InitAsync();
        return await _db.UpdateAsync(settings);
    }
}
