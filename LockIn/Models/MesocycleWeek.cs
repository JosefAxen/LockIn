using SQLite;

namespace LockIn.Models;

[Table("MesocycleWeeks")]
public class MesocycleWeek
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int MesocycleId { get; set; }

    public int WeekNumber { get; set; }

    public bool IsDeload { get; set; }

    public int IntensityPercent { get; set; }
}
