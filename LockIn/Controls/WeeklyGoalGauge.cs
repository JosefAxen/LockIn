using Microsoft.Maui.Graphics;

namespace LockIn.Controls;

public class WeeklyGoalGauge : GraphicsView
{
    private readonly WeeklyGoalDrawable _drawable;

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(
            nameof(Progress), typeof(float), typeof(WeeklyGoalGauge), 0f,
            propertyChanged: (b, _, n) =>
            {
                var g = (WeeklyGoalGauge)b;
                g._drawable.Progress = (float)n;
                g.Invalidate();
            });

    public float Progress
    {
        get => (float)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public WeeklyGoalGauge()
    {
        _drawable = new WeeklyGoalDrawable();
        Drawable = _drawable;
    }
}

internal sealed class WeeklyGoalDrawable : IDrawable
{
    public float Progress { get; set; }

    public void Draw(ICanvas canvas, RectF r)
    {
        float cx = r.Width / 2f;
        float cy = r.Height / 2f;
        float radius = MathF.Min(r.Width, r.Height) / 2f * 0.78f;
        const float strokeWidth = 18f;
        float bL = cx - radius, bT = cy - radius, bD = radius * 2f;

        // Background ring
        canvas.StrokeLineCap = LineCap.Butt;
        canvas.StrokeColor = Color.FromArgb("#252525");
        canvas.StrokeSize = strokeWidth;
        canvas.DrawCircle(cx, cy, radius);

        float clamped = MathF.Min(MathF.Max(Progress, 0f), 1f);
        if (clamped <= 0f) return;

        // Progress arc: clockwise from top (270°), 60 gradient segments
        float totalSweep = clamped * 360f;
        const int segs = 60;
        float perSeg = totalSweep / segs;

        canvas.StrokeLineCap = LineCap.Butt;
        canvas.StrokeSize = strokeWidth;
        for (int i = 0; i < segs; i++)
        {
            float t = segs > 1 ? (float)i / (segs - 1) : 0f;
            float segStart = 270f + i * perSeg;
            float segEnd = segStart + perSeg + 0.5f;
            canvas.StrokeColor = GradientColor(t);
            canvas.DrawArc(bL, bT, bD, bD, segStart, segEnd, true, false);
        }

        // Round start cap
        float sRad = 270f * MathF.PI / 180f;
        canvas.FillColor = GradientColor(0f);
        canvas.FillCircle(cx + radius * MathF.Cos(sRad), cy + radius * MathF.Sin(sRad), strokeWidth / 2f);

        // White tip dot
        float sweepEnd = 270f + totalSweep;
        float eRad = sweepEnd * MathF.PI / 180f;
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx + radius * MathF.Cos(eRad), cy + radius * MathF.Sin(eRad), strokeWidth / 2f + 2f);
    }

    // Deep green (#22C55E) → ForgeAccent (#4ADE80)
    private static Color GradientColor(float t) => Color.FromRgba(
        Lerp(34f / 255, 74f / 255, t),
        Lerp(197f / 255, 222f / 255, t),
        Lerp(94f / 255, 128f / 255, t),
        1f);

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
