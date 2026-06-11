using SQLite;

namespace LockIn.Models;

[Table("WorkoutPhotos")]
public class WorkoutPhoto
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SessionId { get; set; }

    public string FilePath { get; set; } = "";
    public DateTime TakenAt { get; set; }
}
