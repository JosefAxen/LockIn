namespace LockIn.ViewModels;

internal static class TabColorHelper
{
    public static Color ActiveBg  => Color.FromArgb("#FF5A1F");
    public static Color ActiveFg  => Colors.White;
    public static Color InactiveBg =>
        Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#1A1A1A") : Color.FromArgb("#EBEBF0");
    public static Color InactiveFg =>
        Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#505058") : Color.FromArgb("#8E8E93");
}
