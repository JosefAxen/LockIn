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
    private const float StrokeWidth = 14f;
    private const int GradientSegments = 80;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        canvas.Clear();

        float cx = info.Width / 2f;
        float cy = info.Height / 2f;
        float radius = Math.Min(cx, cy) - StrokeWidth - 4f;

        DrawTrack(canvas, cx, cy, radius);
        DrawActiveArc(canvas, cx, cy, radius);
        DrawEndpointDot(canvas, cx, cy, radius);
        DrawCenterText(canvas, cx, cy);
    }

    private void DrawTrack(SKCanvas canvas, float cx, float cy, float radius)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            StrokeCap = SKStrokeCap.Round,
            Color = new SKColor(0x25, 0x25, 0x25)
        };
        using var path = BuildArcPath(cx, cy, radius, StartAngleDeg, TotalSweepDeg);
        canvas.DrawPath(path, paint);
    }

    private void DrawActiveArc(SKCanvas canvas, float cx, float cy, float radius)
    {
        if (Progress <= 0f) return;

        float activeSweep = TotalSweepDeg * Progress;
        float sweepPerSegment = activeSweep / GradientSegments;

        for (int i = 0; i < GradientSegments; i++)
        {
            float t = (float)i / GradientSegments;
            float segStart = StartAngleDeg + i * sweepPerSegment;
            float segEnd = segStart + sweepPerSegment + 0.5f;

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                StrokeCap = SKStrokeCap.Butt,
                Color = InterpolateColor(t)
            };
            using var path = BuildArcPath(cx, cy, radius, segStart, segEnd - segStart);
            canvas.DrawPath(path, paint);
        }

        // Round cap vid start
        using var startPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            StrokeCap = SKStrokeCap.Round,
            Color = InterpolateColor(0f)
        };
        using var startPath = BuildArcPath(cx, cy, radius, StartAngleDeg, sweepPerSegment);
        canvas.DrawPath(startPath, startPaint);

        // Round cap vid slut
        float lastStart = StartAngleDeg + activeSweep - sweepPerSegment;
        using var endPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            StrokeCap = SKStrokeCap.Round,
            Color = InterpolateColor(1f)
        };
        using var endPath = BuildArcPath(cx, cy, radius, lastStart, sweepPerSegment);
        canvas.DrawPath(endPath, endPaint);
    }

    private void DrawEndpointDot(SKCanvas canvas, float cx, float cy, float radius)
    {
        if (Progress <= 0f) return;

        float endAngleRad = (StartAngleDeg + TotalSweepDeg * Progress) * MathF.PI / 180f;
        float dotX = cx + radius * MathF.Cos(endAngleRad);
        float dotY = cy + radius * MathF.Sin(endAngleRad);

        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(0xFF, 0xFF, 0xFF, 0x55),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, StrokeWidth * 0.5f)
        };
        canvas.DrawCircle(dotX, dotY, StrokeWidth * 0.75f, glowPaint);

        using var dotPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.White
        };
        canvas.DrawCircle(dotX, dotY, StrokeWidth * 0.48f, dotPaint);
    }

    private void DrawCenterText(SKCanvas canvas, float cx, float cy)
    {
        int pct = (int)Math.Round(Progress * 100);

        // Stor siffra
        using var scorePaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White,
            TextSize = 64f,
            FakeBoldText = true,
            TextAlign = SKTextAlign.Center
        };
        canvas.DrawText(pct.ToString(), cx, cy - 4f, scorePaint);

        // / 100
        using var subPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0x55, 0x55, 0x55),
            TextSize = 20f,
            TextAlign = SKTextAlign.Center
        };
        canvas.DrawText("/ 100", cx, cy + 32f, subPaint);
    }

    private static SKPath BuildArcPath(float cx, float cy, float radius, float startDeg, float sweepDeg)
    {
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
        var path = new SKPath();
        path.AddArc(rect, startDeg, sweepDeg);
        return path;
    }

    private static SKColor InterpolateColor(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float r, g, b;
        if (t < 0.5f)
        {
            float l = t / 0.5f;
            r = Lerp(192, 142, l);
            g = Lerp(57, 68, l);
            b = Lerp(43, 173, l);
        }
        else
        {
            float l = (t - 0.5f) / 0.5f;
            r = Lerp(142, 52, l);
            g = Lerp(68, 152, l);
            b = Lerp(173, 219, l);
        }
        return new SKColor((byte)r, (byte)g, (byte)b);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
