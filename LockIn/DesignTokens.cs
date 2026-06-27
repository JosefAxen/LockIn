using SkiaSharp;

namespace LockIn;

/// <summary>
/// Single source of truth for all design tokens used in C# code.
/// XAML equivalents live in Resources/Styles/Colors.xaml.
/// </summary>
public static class DesignTokens
{
    private static bool IsDark =>
        Application.Current?.RequestedTheme != AppTheme.Light;

    // ─── Surfaces ────────────────────────────────────────────────────────────
    public static Color Background  => Color.FromArgb(IsDark ? "#141418" : "#FCFCFC");
    public static Color Surface     => Color.FromArgb(IsDark ? "#161618" : "#FFFFFF");
    public static Color Surface2    => Color.FromArgb(IsDark ? "#222228" : "#EDEDED");

    // ─── Accent / Primary ────────────────────────────────────────────────────
    public static readonly Color Accent  = Color.FromArgb("#B8B8BC");
    public static readonly Color Primary = Color.FromArgb("#3E3E44");

    // ─── Text ────────────────────────────────────────────────────────────────
    public static Color Text          => Color.FromArgb(IsDark ? "#E2E8F0" : "#171717");
    public static Color TextSecondary => Color.FromArgb(IsDark ? "#A2A2A2" : "#606060");
    public static Color TextMuted     => Color.FromArgb(IsDark ? "#52525E" : "#9090A0");

    // ─── Graph (IDrawable) ───────────────────────────────────────────────────
    public static Color GraphGrid      => Color.FromArgb(IsDark ? "#272732" : "#DCDCE8");
    public static Color GraphAxisText  => Color.FromArgb(IsDark ? "#52525E" : "#9090A0");
    public static Color GraphDotStroke => Color.FromArgb(IsDark ? "#0A0A0C" : "#FFFFFF");

    // ─── Chips / Segment buttons ─────────────────────────────────────────────
    public static readonly Color ChipActiveBg  = Color.FromArgb("#3E3E44");
    public static readonly Color ChipActiveFg  = Colors.White;
    public static Color ChipInactiveBg => Color.FromArgb(IsDark ? "#1C1C24" : "#E8E8F0");
    public static Color ChipInactiveFg => Color.FromArgb(IsDark ? "#52525E" : "#70707A");

    // ─── Streak calendar days ────────────────────────────────────────────────
    public static readonly Color CalTrainedStroke = Color.FromArgb("#B8B8BC");
    public static readonly Color CalTrainedFill   = Color.FromArgb("#1AB8B8BC");
    public static readonly Color CalTrainedText   = Color.FromArgb("#FAFAFA");
    public static readonly Color CalTodayStroke   = Color.FromArgb("#FBBF24");
    public static Color          CalTodayFill     => Color.FromArgb(IsDark ? "#222228" : "#FFF8E6");
    public static readonly Color CalTodayText     = Color.FromArgb("#FBBF24");
    public static Color          CalNormalText    => Color.FromArgb(IsDark ? "#424250" : "#A0A0AE");

    // ─── Set types ───────────────────────────────────────────────────────────
    public static readonly Color SetWarmup  = Color.FromArgb("#FBBF24");
    public static readonly Color SetDropset = Color.FromArgb("#FB7185");
    public static readonly Color SetTime    = Color.FromArgb("#38BDF8");
    public static Color          SetNormal  => Color.FromArgb(IsDark ? "#52525E" : "#70707A");

    // ─── FAB foreground (dark text on silver metallic button) ────────────────
    public static readonly Color FabForeground = Color.FromArgb("#0E0E10");

    // ─── Low-emphasis text (barely visible, for empty/inactive states) ───────
    public static readonly Color TextDim = Color.FromArgb("#303038");

    // ─── Filter pills ────────────────────────────────────────────────────────
    public static readonly Color PillTrack      = Color.FromArgb("#33787882");
    public static readonly Color PillIndicator  = Color.FromArgb("#525260");

    // ─── Workout banner ──────────────────────────────────────────────────────
    public static readonly Color BannerStroke   = Color.FromArgb("#55B8B8BC");

    // ─── Glass segment pill states (HistoryPage) ─────────────────────────────
    public static readonly Color GlassActiveBg   = Color.FromArgb("#2BFFFFFF");
    public static readonly Color GlassActiveFg   = Color.FromArgb("#E2E8F0");
    public static readonly Color GlassInactiveBg = Colors.Transparent;
    public static readonly Color GlassInactiveFg = Color.FromArgb("#80FFFFFF");

    // ─── Multi-accent colors ─────────────────────────────────────────────────
    public static readonly Color AccentBlue   = Color.FromArgb("#38BDF8");
    public static readonly Color AccentCoral  = Color.FromArgb("#FB7185");
    public static readonly Color AccentPurple = Color.FromArgb("#A78BFA");
    public static readonly Color AccentAmber  = Color.FromArgb("#FBBF24");
    public static readonly Color AccentOrange = Color.FromArgb("#FB923C");
    public static readonly Color AccentGreen  = Color.FromArgb("#22C55E");
    public static readonly Color AccentTeal   = Color.FromArgb("#2DD4BF");

    // ─── Primary foreground (text on primary buttons) ────────────────────────
    public static readonly Color PrimaryForeground = Color.FromArgb("#FAFAFA");

    // ─── SkiaSharp surface / spark tokens ────────────────────────────────────
    public static readonly SKColor SK_Background  = SKColor.Parse("#141418");
    public static readonly SKColor SK_AccentGlow1 = SKColor.Parse("#28B8B8BC");
    public static readonly SKColor SK_AccentGlow2 = SKColor.Parse("#1CB8B8BC");
    public static readonly SKColor SK_Vignette    = SKColor.Parse("#22000000");
    public static readonly SKColor SK_SparkCore   = SKColor.Parse("#FDE68A");
    public static readonly SKColor SK_SparkMid    = SKColor.Parse("#FBBF24");
    public static readonly SKColor SK_SparkOuter  = SKColor.Parse("#FB923C");

    // ─── Icon tinting ────────────────────────────────────────────────────────
    public static Color IconTint => Color.FromArgb(IsDark ? "#C8C8D2" : "#3A3A44");

    // ─── Heatmap ─────────────────────────────────────────────────────────────
    public static readonly Color HeatmapInactive = Color.FromArgb("#1A1A22");

    public static Color HeatmapTile(double normalizedScore)
    {
        if (normalizedScore < 0.01) return HeatmapInactive;
        double t = Math.Min(normalizedScore, 1.0);

        if (t < 0.9)
        {
            double f = t / 0.9;
            return Color.FromRgb(
                (int)(26 * (1 - f)),
                (int)(26 + 72 * f),
                (int)(34 + 23 * f));
        }
        else
        {
            double f = (t - 0.9) / 0.1;
            return Color.FromRgb(
                (int)(74 * f),
                (int)(98 + 124 * f),
                (int)(57 + 71 * f));
        }
    }

    public static Color HeatmapText(double normalizedScore) => Colors.White;

    // ─── Per-muscle-group sparkline colors ───────────────────────────────────
    public static Color MuscleColor(Models.MuscleGroup mg) => mg switch
    {
        Models.MuscleGroup.Chest     => AccentCoral,
        Models.MuscleGroup.Back      => AccentBlue,
        Models.MuscleGroup.Shoulders => AccentAmber,
        Models.MuscleGroup.Biceps    => AccentPurple,
        Models.MuscleGroup.Triceps   => AccentOrange,
        Models.MuscleGroup.Legs      => AccentGreen,
        Models.MuscleGroup.Core      => AccentTeal,
        _                            => Accent,
    };
}
