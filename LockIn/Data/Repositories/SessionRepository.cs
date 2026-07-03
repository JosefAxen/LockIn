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

#pragma warning disable CS0618 // Delegate till Obsolete-metoder — flyttas fysiskt i PR 7
    // WorkoutSession
    public Task<int> SaveSessionAsync(WorkoutSession session) => _db.SaveWorkoutSessionAsync(session);
    public Task<WorkoutSession?> GetSessionAsync(int id) => _db.GetWorkoutSessionAsync(id);
    public Task<List<WorkoutSession>> GetSessionsAsync() => _db.GetSessionsAsync();
    public Task<List<WorkoutSession>> GetCompletedInRangeAsync(DateTime from, DateTime to) => _db.GetCompletedSessionsInRangeAsync(from, to);
    public Task<List<WorkoutSession>> GetRecentCompletedAsync(int limit) => _db.GetRecentCompletedSessionsAsync(limit);
    public Task<bool> HasAnyCompletedAsync() => _db.HasAnyCompletedSessionsAsync();
    public Task<List<SessionSummaryRow>> GetCompletedSummariesAsync() => _db.GetCompletedSessionsAsync();
    public Task<int> GetTotalCompletedCountAsync() => _db.GetTotalCompletedSessionCountAsync();
    public Task<int> GetCountThisWeekAsync() => _db.GetSessionCountThisWeekAsync();

    // SessionExercise
    public Task<int> SaveSessionExerciseAsync(SessionExercise se) => _db.SaveSessionExerciseAsync(se);
    public Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId) => _db.GetSessionExercisesAsync(sessionId);
    public Task DeleteSessionExerciseWithSetsAsync(int sessionExerciseId) => _db.DeleteSessionExerciseWithSetsAsync(sessionExerciseId);
    public Task SetSessionExerciseNotesAsync(int sessionExerciseId, string notes) => _db.SetSessionExerciseNotesAsync(sessionExerciseId, notes);
    public Task UpdateSessionExerciseExerciseIdAsync(int sessionExerciseId, int newExerciseId) => _db.UpdateSessionExerciseExerciseIdAsync(sessionExerciseId, newExerciseId);
    public Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId) => _db.GetSessionExerciseDetailsAsync(sessionId);

    // LoggedSet
    public Task<int> SaveLoggedSetAsync(LoggedSet set) => _db.SaveLoggedSetAsync(set);
    public Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId) => _db.GetSetsForSessionExerciseAsync(sessionExerciseId);
    public Task<List<LoggedSet>> GetLoggedSetsForSessionExerciseAsync(int sessionExerciseId) => _db.GetLoggedSetsForSessionExerciseAsync(sessionExerciseId);
    public Task<List<LoggedSet>> GetAllSetsForSessionAsync(int sessionId) => _db.GetAllSetsForSessionAsync(sessionId);
    public Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId) => _db.GetAllSetsForExerciseAsync(exerciseId);
    public Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId) => _db.GetLastSessionSetsAsync(exerciseId, excludeSessionId);
    public Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId) => _db.GetPRsForSessionAsync(sessionId);
    public Task<double> GetMaxEpley1RMAsync(int exerciseId, int excludeLoggedSetId = 0) => _db.GetMaxEpley1RMAsync(exerciseId, excludeLoggedSetId);
    public Task<List<(DateTime Date, decimal WeightKg, int Reps, double Epley1RM, bool IsPR)>>
        GetBestSetPerSessionForExerciseAsync(int exerciseId) => _db.GetBestSetPerSessionForExerciseAsync(exerciseId);
    public Task<Dictionary<MuscleGroup, (decimal Volume, int Sets)>> GetSessionVolumeByMuscleGroupAsync(int sessionId) => _db.GetSessionVolumeByMuscleGroupAsync(sessionId);

    // Feedback
    public Task<int> SaveMuscleFeedbackAsync(SessionMuscleFeedback fb) => _db.SaveSessionMuscleFeedbackAsync(fb);
    public Task<List<SessionMuscleFeedback>> GetMuscleFeedbackAsync(int sessionId) => _db.GetSessionMuscleFeedbackAsync(sessionId);
    public Task<int> SaveExerciseFeedbackAsync(SessionExerciseFeedback fb) => _db.SaveSessionExerciseFeedbackAsync(fb);
    public Task<SessionExerciseFeedback?> GetExerciseFeedbackAsync(int sessionExerciseId) => _db.GetSessionExerciseFeedbackAsync(sessionExerciseId);

    // Cardio
    public Task SaveCardioSessionAsync(CardioSession session) => _db.SaveCardioSessionAsync(session);
    public Task<List<CardioSession>> GetCardioSessionsAsync(int limit = 50) => _db.GetCardioSessionsAsync(limit);
    public Task DeleteCardioSessionAsync(int id) => _db.DeleteCardioSessionAsync(id);
#pragma warning restore CS0618
}
