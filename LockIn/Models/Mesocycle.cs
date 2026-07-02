using SQLite;

namespace LockIn.Models;

public enum UnitSystem { Kg = 0, Lb = 1 }

[Table("Mesocycles")]
public class Mesocycle
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] public string Name { get; set; } = "";

    public int TemplateId { get; set; }

    public DateTime StartDate { get; set; }

    public int WeekCount { get; set; }

    public bool IsActive { get; set; }

    public int UnitSystemValue { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public UnitSystem UnitSystem
    {
        get => (UnitSystem)UnitSystemValue;
        set => UnitSystemValue = (int)value;
    }
}
