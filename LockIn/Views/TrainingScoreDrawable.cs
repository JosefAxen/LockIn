using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class TrainingScoreDrawable : IDrawable
{
    public double Score { get; set; } = 0;

    public void Draw(ICanvas canvas, RectF r)
    {
        float cx = r.Width / 2f;
        float cy = r.Height * 0.75f;
        float radius = MathF.Min(r.Width, r.Height) * 0.42f;
        const float strokeWidth = 12f;
        float bL = cx - radius, bT = cy - radius, bD = radius * 2f;

        // Background track
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Color.FromArgb("#2a2a2a");
        canvas.StrokeSize = strokeWidth;
        canvas.DrawArc(bL, bT, bD, bD, 180f, 360f, true, false);

        if (Score <= 0) return;

        float progress = MathF.Min((float)(Score / 100.0), 1f);
        float totalDeg = progress * 180f;
        const int segments = 60;
        float sweepPerSeg = totalDeg / segments;

        // Gradient arc — 60 segments, LineCap.Butt to avoid gaps at seams
        canvas.StrokeLineCap = LineCap.Butt;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float start = 180f + i * sweepPerSeg;
            float end = start + sweepPerSeg + 0.5f;
            canvas.StrokeColor = InterpolateColor(t);
            canvas.StrokeSize = strokeWidth;
            canvas.DrawArc(bL, bT, bD, bD, start, end, true, false);
        }

        // Endpoint dot
        float endRad = (180f + totalDeg) * MathF.PI / 180f;
        float dotX = cx + radius * MathF.Cos(endRad);
        float dotY = cy + radius * MathF.Sin(endRad);
        canvas.FillColor = Colors.White;
        canvas.FillCircle(dotX, dotY, 8f);
    }

    // Red → Purple → Blue
    private static Color InterpolateColor(float t)
    {
        if (t < 0.5f)
        {
            float l = t / 0.5f;
            return Color.FromRgba(
                Lerp(0.75f, 0.56f, l),
                Lerp(0.22f, 0.27f, l),
                Lerp(0.17f, 0.68f, l), 1f);
        }
        else
        {
            float l = (t - 0.5f) / 0.5f;
            return Color.FromRgba(
                Lerp(0.56f, 0.20f, l),
                Lerp(0.27f, 0.60f, l),
                Lerp(0.68f, 0.85f, l), 1f);
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
