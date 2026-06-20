using SQLite;

namespace LockIn.Models;

[Table("SessionExercises")]
public class SessionExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionId { get; set; }

    public int ExerciseId { get; set; }

    public int OrderIndex { get; set; }

    // Superset (Fas 3) — kopias från TemplateExercise vid sessionsstart
    public int? SupersetGroupId { get; set; }
}
