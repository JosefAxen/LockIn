using SQLite;

namespace LockIn.Models;

[Table("BodyCompositionEntries")]
public class BodyCompositionEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.Now;
    public decimal? WaistCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? HipCm { get; set; }
    public decimal? ArmCm { get; set; }
    public decimal? ThighCm { get; set; }

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (WaistCm.HasValue) parts.Add($"M:{WaistCm:F0}");
            if (ChestCm.HasValue) parts.Add($"B:{ChestCm:F0}");
            if (HipCm.HasValue)   parts.Add($"H:{HipCm:F0}");
            if (ArmCm.HasValue)   parts.Add($"A:{ArmCm:F0}");
            if (ThighCm.HasValue) parts.Add($"L:{ThighCm:F0}");
            return parts.Count > 0 ? string.Join(" · ", parts) + " cm" : "";
        }
    }
}
