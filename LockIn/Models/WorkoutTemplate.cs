using SQLite;

namespace LockIn.Models;

[Table("WorkoutTemplates")]
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;
}
