using System.Text.Json;

namespace LockIn.Models;

public static class AppSettingsExtensions
{
    public static List<int> ParsePreferredEquipment(this AppSettings? settings)
    {
        var json = settings?.PreferredEquipmentJson;
        if (string.IsNullOrWhiteSpace(json)) return new List<int>();
        try { return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>(); }
        catch { return new List<int>(); }
    }
}
