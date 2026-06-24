using System.Globalization;
using System.Resources;

namespace LockIn.Resources.Strings;

/// <summary>
/// Hand-written wrapper kring AppResources.resx satellitassemblier.
/// Lägg till nya properties här när nycklar läggs till i .resx-filerna.
/// </summary>
public static class AppResources
{
    public static ResourceManager ResourceManager { get; } = new(
        "LockIn.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    /// <summary>
    /// Override för tester / framtida språkval. När null används CurrentUICulture.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static string Get(string key) =>
        ResourceManager.GetString(key, Culture) ?? key;

    // ── Common ─────────────────────────────────────────────────────────
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Skip   => Get(nameof(Common_Skip));
}
