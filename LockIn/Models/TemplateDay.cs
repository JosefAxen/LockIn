using SQLite;

namespace LockIn.Models;

[Table("TemplateDays")]
public class TemplateDay
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int TemplateId { get; set; }

    public int DayIndex { get; set; }

    public string Label { get; set; } = "";

    public int? DefaultDayOfWeek { get; set; }
}
