using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class WeeklyGoalGauge : SKCanvasView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(float), typeof(WeeklyGoalGauge), 0f,
            propertyChanged: (b, _, _) => ((WeeklyGoalGauge)b).InvalidateSurface());

    public float Progress
    {
        get => (float)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    private const float StartAngleDeg = 120f;
    private const float TotalSweepDeg = 300f;

    // Logical dp — multiplied by display density at render time
    private const float StrokeWidthDp  = 20f;
    private const float ScoreTextDp    = 58f;
    private const float SubTextDp      = 17f;

    private float _scale = 1f;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info   = e.Info;
        canvas.Clear();

        _scale = info.Width > 0 && Width > 0 ? (float)(info.Width / Width) : 1f;

        float sw     = StrokeWidthDp * _scale;
        float cx     = info.Width  / 2f;
        float cy     = info.Height / 2f;
        float radius = Math.Min(cx, cy) - sw - 4f * _scale;

        DrawTrack(canvas, cx, cy, radius, sw);
        DrawActiveArc(canvas, cx, cy, radius, sw);
        DrawEndpointDot(canvas, cx, cy, radius, sw);
        DrawCenterText(canvas, cx, cy);
    }

    private static void DrawTrack(SKCanvas canvas, float cx, float cy, float radius, float sw)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = sw,
            StrokeCap   = SKStrokeCap.Round,
            Color       = SkiaTokens.TrackBg
        };
        using var path = BuildArcPath(cx, cy, radius, StartAngleDeg, TotalSweepDeg);
        canvas.DrawPath(path, paint);
    }

    private static void DrawActiveArc(SKCanvas canvas, float cx, float cy, float radius, float sw, float progress)
    {
        if (progress <= 0f) return;

        float activeSweep = TotalSweepDeg * progress;

        // Sweep-gradient med fast 0°–360°-range. Färgstopp placerade vid arcens
        // nyckelpoitioner: start(120°)→t=0.333, topp(270°)→t=0.750, slut(60°)→t=0.167.
        // t=0 och t=1 har samma färg → sömlös övergång vid 360°-gränsen.
        // Ingen dynamisk endAngle → ingen 360°-wrapping-bugg.
        using var shader = SKShader.CreateSweepGradient(
            new SKPoint(cx, cy),
            new SKColor[] {
                new SKColor( 88, 118, 201),  // t=0.000   0°  – högersidesblend (sömlöst)
                new SKColor( 52, 152, 219),  // t=0.167  60°  – arcslut (blå)
                new SKColor(192,  57,  43),  // t=0.333 120°  – arcstart (röd)
                new SKColor(142,  68, 173),  // t=0.750 270°  – arctopp (lila)
                new SKColor( 88, 118, 201),  // t=1.000 360°  – samma som t=0
            },
            new float[] { 0f, 0.167f, 0.333f, 0.75f, 1.0f },
            SKShaderTileMode.Clamp,
            0f,
            360f);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = sw,
            StrokeCap   = SKStrokeCap.Round,
            Shader      = shader
        };

        using var path = BuildArcPath(cx, cy, radius, StartAngleDeg, activeSweep);
        canvas.DrawPath(path, paint);
    }

    private static void DrawEndpointDot(SKCanvas canvas, float cx, float cy, float radius, float sw, float progress)
    {
        if (progress <= 0f) return;

        float endAngleRad = (StartAngleDeg + TotalSweepDeg * progress) * MathF.PI / 180f;
        float dotX = cx + radius * MathF.Cos(endAngleRad);
        float dotY = cy + radius * MathF.Sin(endAngleRad);

        using var glowPaint = new SKPaint
        {
            IsAntialias  = true,
            Style        = SKPaintStyle.Fill,
            Color        = SkiaTokens.GlowWhite,
            MaskFilter   = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sw * 0.5f)
        };
        canvas.DrawCircle(dotX, dotY, sw * 0.75f, glowPaint);

        using var dotPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = SKColors.White
        };
        canvas.DrawCircle(dotX, dotY, sw * 0.48f, dotPaint);
    }

    private void DrawCenterText(SKCanvas canvas, float cx, float cy)
    {
        int   pct     = (int)Math.Round(Progress * 100);
        float scoreTs = ScoreTextDp * _scale;
        float subTs   = SubTextDp   * _scale;

        using var scorePaint = new SKPaint
        {
            IsAntialias  = true,
            Color        = SKColors.White,
            TextSize     = scoreTs,
            FakeBoldText = true,
            TextAlign    = SKTextAlign.Center,
            Typeface     = SKTypeface.FromFamilyName(null)  // System font (SF Pro på iOS)
        };
        using var subPaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SkiaTokens.AxisText,
            TextSize    = subTs,
            TextAlign   = SKTextAlign.Center
        };

        scorePaint.GetFontMetrics(out var sm);
        subPaint.GetFontMetrics(out var pm);

        float scoreH = sm.Descent - sm.Ascent;
        float subH   = pm.Descent - pm.Ascent;
        float gap    = subTs * 0.25f;

        float totalH    = scoreH + gap + subH;
        float blockTop  = cy - totalH / 2f;
        float scoreBase = blockTop - sm.Ascent;
        float subBase   = blockTop + scoreH + gap - pm.Ascent;

        canvas.DrawText(pct.ToString(), cx, scoreBase, scorePaint);
        canvas.DrawText("/ 100",        cx, subBase,   subPaint);
    }

    // Overloads to bridge instance Progress to static helpers
    private void DrawActiveArc(SKCanvas c, float cx, float cy, float r, float sw)
        => DrawActiveArc(c, cx, cy, r, sw, Progress);

    private void DrawEndpointDot(SKCanvas c, float cx, float cy, float r, float sw)
        => DrawEndpointDot(c, cx, cy, r, sw, Progress);

    private static SKPath BuildArcPath(float cx, float cy, float radius, float startDeg, float sweepDeg)
    {
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
        var path = new SKPath();
        path.AddArc(rect, startDeg, sweepDeg);
        return path;
    }

}
