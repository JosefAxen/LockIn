using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class AtmosphericBackgroundView : SKCanvasView
{
    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        var bounds = new SKRect(0, 0, w, h);

        // Base fill
        canvas.Clear(SKColor.Parse("#0E0E10"));

        // Green radial glow — topp-center, bakom gauge-zonen
        using var glowShader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.17f),
            w * 0.65f,
            new[] { SKColor.Parse("#284ADE80"), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var glowPaint = new SKPaint { Shader = glowShader, IsAntialias = true };
        canvas.DrawRect(bounds, glowPaint);

        // Sekundär grön glow — mindre, mer koncentrerad
        using var glow2Shader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.12f),
            w * 0.3f,
            new[] { SKColor.Parse("#1C4ADE80"), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var glow2Paint = new SKPaint { Shader = glow2Shader, IsAntialias = true };
        canvas.DrawRect(bounds, glow2Paint);

        // Blå ambient dimma — undre tredjedelen
        using var blueShader = SKShader.CreateLinearGradient(
            new SKPoint(0, h),
            new SKPoint(0, h * 0.55f),
            new[] { SKColor.Parse("#1A3B82E6"), SKColors.Transparent },
            SKShaderTileMode.Clamp);
        using var bluePaint = new SKPaint { Shader = blueShader, IsAntialias = true };
        canvas.DrawRect(bounds, bluePaint);

        // Edge vignette — ger djup åt kanter
        using var vigShader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.45f),
            Math.Max(w, h) * 0.75f,
            new[] { SKColors.Transparent, SKColor.Parse("#22000000") },
            new[] { 0.45f, 1f },
            SKShaderTileMode.Clamp);
        using var vigPaint = new SKPaint { Shader = vigShader, IsAntialias = true };
        canvas.DrawRect(bounds, vigPaint);
    }
}
