using LockIn;

namespace LockIn.ViewModels;

internal static class TabColorHelper
{
    public static Color ActiveBg   => DesignTokens.ChipActiveBg;
    public static Color ActiveFg   => DesignTokens.ChipActiveFg;
    public static Color InactiveBg => DesignTokens.ChipInactiveBg;
    public static Color InactiveFg => DesignTokens.ChipInactiveFg;
}
