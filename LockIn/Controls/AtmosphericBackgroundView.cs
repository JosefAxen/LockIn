using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

/// <summary>
/// Statisk sidbakgrund: grundfärg + mjuk silverbloom upptill + vinjett.
/// Ingen animation och inga partiklar — ritas om bara när storleken ändras.
/// </summary>
public class AtmosphericBackgroundView : SKCanvasView
{
    private static readonly SKColor s_base       = DesignTokens.SK_Background;
    private static readonly SKColor s_glow1Inner = DesignTokens.SK_AccentGlow1;
    private static readonly SKColor s_glow2Inner = DesignTokens.SK_AccentGlow2;
    private static readonly SKColor s_vigOuter   = DesignTokens.SK_Vignette;

    public AtmosphericBackgroundView()
    {
        EnableTouchEvents = false;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var c = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        var bounds = new SKRect(0, 0, w, h);

        c.Clear(s_base);

        // Mjuk diffus bloom — 5 överlappande gradienter med fyra stopp vardera
        // för att undvika synliga ringar i ljuset.
        DrawSoftGlow(c, bounds, w * 0.50f, h * 0.10f, w * 0.80f, s_glow1Inner, 0.11f);
        DrawSoftGlow(c, bounds, w * 0.42f, h * 0.06f, w * 0.60f, s_glow1Inner, 0.08f);
        DrawSoftGlow(c, bounds, w * 0.58f, h * 0.06f, w * 0.60f, s_glow1Inner, 0.08f);
        DrawSoftGlow(c, bounds, w * 0.50f, h * 0.02f, w * 0.45f, s_glow2Inner, 0.10f);
        DrawSoftGlow(c, bounds, w * 0.50f, 0f,        w * 0.30f, s_glow2Inner, 0.08f);

        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.45f), MathF.Max(w, h) * 0.75f,
            [SKColors.Transparent, SKColors.Transparent, s_vigOuter],
            [0f, 0.6f, 1f],
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        c.DrawRect(bounds, paint);
    }

    private static void DrawSoftGlow(SKCanvas c, SKRect bounds, float cx, float cy, float radius, SKColor color, float peakAlpha)
    {
        var colors = new[]
        {
            color.WithAlpha((byte)(peakAlpha * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.72f * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.38f * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.12f * 255)),
            SKColors.Transparent,
        };
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy), radius, colors,
            [0f, 0.25f, 0.5f, 0.75f, 1f],
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        c.DrawRect(bounds, paint);
    }
}
