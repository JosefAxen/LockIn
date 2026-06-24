using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class MetricRingView : SKCanvasView
{
    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(float), typeof(MetricRingView), 0f,
            propertyChanged: (b, o, n) => ((MetricRingView)b).OnProgressChanged((float)o, (float)n));

    public static readonly BindableProperty RingColorProperty =
        BindableProperty.Create(nameof(RingColor), typeof(Color), typeof(MetricRingView), Colors.White,
            propertyChanged: (b, _, _) => ((MetricRingView)b).InvalidateSurface());

    public static readonly BindableProperty CenterTextProperty =
        BindableProperty.Create(nameof(CenterText), typeof(string), typeof(MetricRingView), "–",
            propertyChanged: (b, _, _) => ((MetricRingView)b).InvalidateSurface());

    public static readonly BindableProperty TargetProgressProperty =
        BindableProperty.Create(nameof(TargetProgress), typeof(float), typeof(MetricRingView), 0f,
            propertyChanged: (b, _, _) => ((MetricRingView)b).InvalidateSurface());

    public float Progress
    {
        get => (float)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color RingColor
    {
        get => (Color)GetValue(RingColorProperty);
        set => SetValue(RingColorProperty, value);
    }

    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public float TargetProgress
    {
        get => (float)GetValue(TargetProgressProperty);
        set => SetValue(TargetProgressProperty, value);
    }

    private const float StartAngleDeg = 135f;
    private const float TotalSweepDeg = 270f;

    // Entrance animation: animera Progress 0 → target första gången värdet sätts (per instans)
    private bool _isAnimating;
    private bool _hasAnimatedIn;

    private void OnProgressChanged(float oldValue, float newValue)
    {
        if (_isAnimating)
        {
            InvalidateSurface();
            return;
        }
        if (!_hasAnimatedIn && newValue > 0.001f)
        {
            _hasAnimatedIn = true;
            _isAnimating = true;
            var anim = new Animation(v => Progress = (float)v, 0, newValue, Easing.CubicOut);
            anim.Commit(this, "RingEntry", length: 800, rate: 16,
                finished: (_, __) => { _isAnimating = false; InvalidateSurface(); });
        }
        else
        {
            InvalidateSurface();
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float scale  = e.Info.Width > 0 && Width > 0 ? (float)(e.Info.Width / Width) : 1f;
        float sw     = 14f * scale;
        float cx     = e.Info.Width  / 2f;
        float cy     = e.Info.Height / 2f;
        float radius = Math.Min(cx, cy) - sw * 0.5f - 2f * scale;
        var   rect   = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        // Background track
        using var trackPath = new SKPath();
        trackPath.AddArc(rect, StartAngleDeg, TotalSweepDeg);
        using var trackPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = sw,
            StrokeCap   = SKStrokeCap.Round,
            Color       = SkiaTokens.RingTrack
        };
        canvas.DrawPath(trackPath, trackPaint);

        // Target halo — halv-transparent stråk till TargetProgress
        float targetSweep = TotalSweepDeg * Math.Clamp(TargetProgress, 0f, 1f);
        if (targetSweep > 0.5f)
        {
            var c = RingColor;
            var skTarget = new SKColor(
                (byte)(c.Red   * 255),
                (byte)(c.Green * 255),
                (byte)(c.Blue  * 255),
                (byte)90);
            using var targetPath = new SKPath();
            targetPath.AddArc(rect, StartAngleDeg, targetSweep);
            using var targetPaint = new SKPaint
            {
                IsAntialias = true,
                Style       = SKPaintStyle.Stroke,
                StrokeWidth = sw,
                StrokeCap   = SKStrokeCap.Round,
                Color       = skTarget
            };
            canvas.DrawPath(targetPath, targetPaint);
        }

        // Active arc
        float activeSweep = TotalSweepDeg * Math.Clamp(Progress, 0f, 1f);
        if (activeSweep > 0.5f)
        {
            var c = RingColor;
            var skColor = new SKColor(
                (byte)(c.Red   * 255),
                (byte)(c.Green * 255),
                (byte)(c.Blue  * 255));

            using var arcPath = new SKPath();
            arcPath.AddArc(rect, StartAngleDeg, activeSweep);
            using var arcPaint = new SKPaint
            {
                IsAntialias = true,
                Style       = SKPaintStyle.Stroke,
                StrokeWidth = sw,
                StrokeCap   = SKStrokeCap.Round,
                Color       = skColor
            };
            canvas.DrawPath(arcPath, arcPaint);

            // Subtle glow at endpoint
            float endRad = (StartAngleDeg + activeSweep) * MathF.PI / 180f;
            float ex = cx + radius * MathF.Cos(endRad);
            float ey = cy + radius * MathF.Sin(endRad);
            using var glowPaint = new SKPaint
            {
                IsAntialias = true,
                Style       = SKPaintStyle.Fill,
                Color       = skColor.WithAlpha(60),
                MaskFilter  = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, sw * 0.6f)
            };
            canvas.DrawCircle(ex, ey, sw * 0.7f, glowPaint);
        }

        // Center text — system font (SF Pro on iOS)
        float textSize = 21f * scale;
        using var textPaint = new SKPaint
        {
            IsAntialias  = true,
            Color        = SKColors.White,
            TextSize     = textSize,
            FakeBoldText = true,
            TextAlign    = SKTextAlign.Center,
            Typeface     = SKTypeface.FromFamilyName(null)
        };
        textPaint.GetFontMetrics(out var metrics);
        float textY = cy - (metrics.Ascent + metrics.Descent) / 2f;
        canvas.DrawText(CenterText ?? "–", cx, textY, textPaint);
    }
}
