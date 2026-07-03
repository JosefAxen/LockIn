using LockIn.Models;
using static LockIn.Services.DatabaseService;

namespace LockIn.Data.Repositories;

/// <summary>
/// Repository för WorkoutSession + SessionExercise + LoggedSet + Feedback + CardioSession.
/// Del av ADR-001 PR 6 av 7. Största aggregatet.
/// </summary>
public interface ISessionRepository
{
    // WorkoutSession CRUD + queries
    Task<int> SaveSessionAsync(WorkoutSession session);
    Task<WorkoutSession?> GetSessionAsync(int id);
    Task<List<WorkoutSession>> GetSessionsAsync();
    Task<List<WorkoutSession>> GetCompletedInRangeAsync(DateTime from, DateTime to);
    Task<List<WorkoutSession>> GetRecentCompletedAsync(int limit);
    Task<bool> HasAnyCompletedAsync();
    Task<List<SessionSummaryRow>> GetCompletedSummariesAsync();
    Task<int> GetTotalCompletedCountAsync();
    Task<int> GetCountThisWeekAsync();

    // SessionExercise
    Task<int> SaveSessionExerciseAsync(SessionExercise se);
    Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId);
    Task DeleteSessionExerciseWithSetsAsync(int sessionExerciseId);
    Task SetSessionExerciseNotesAsync(int sessionExerciseId, string notes);
    Task UpdateSessionExerciseExerciseIdAsync(int sessionExerciseId, int newExerciseId);
    Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId);

    // LoggedSet
    Task<int> SaveLoggedSetAsync(LoggedSet set);
    Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId);
    Task<List<LoggedSet>> GetLoggedSetsForSessionExerciseAsync(int sessionExerciseId);
    Task<List<LoggedSet>> GetAllSetsForSessionAsync(int sessionId);
    Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId);
    Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId);
    Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId);
    Task<double> GetMaxEpley1RMAsync(int exerciseId, int excludeLoggedSetId = 0);
    Task<List<(DateTime Date, decimal WeightKg, int Reps, double Epley1RM, bool IsPR)>>
        GetBestSetPerSessionForExerciseAsync(int exerciseId);
    Task<Dictionary<MuscleGroup, (decimal Volume, int Sets)>> GetSessionVolumeByMuscleGroupAsync(int sessionId);

    // Feedback
    Task<int> SaveMuscleFeedbackAsync(SessionMuscleFeedback fb);
    Task<List<SessionMuscleFeedback>> GetMuscleFeedbackAsync(int sessionId);
    Task<int> SaveExerciseFeedbackAsync(SessionExerciseFeedback fb);
    Task<SessionExerciseFeedback?> GetExerciseFeedbackAsync(int sessionExerciseId);

    // Cardio (löskopplat men samma aggregat-nivå)
    Task SaveCardioSessionAsync(CardioSession session);
    Task<List<CardioSession>> GetCardioSessionsAsync(int limit = 50);
    Task DeleteCardioSessionAsync(int id);
}
