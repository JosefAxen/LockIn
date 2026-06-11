using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class TrainingScoreDrawable : IDrawable
{
    public double Score { get; set; } = 72;
    public bool IsDark { get; set; } = true;

    public void Draw(ICanvas canvas, RectF r)
    {
        float cx = r.Width / 2f;
        float cy = r.Height * 0.90f;
        float outerR = MathF.Min(r.Width * 0.43f, r.Height * 0.84f);
        float trackThick = outerR * 0.115f;

        float bLeft = cx - outerR;
        float bTop = cy - outerR;
        float bW = outerR * 2f;
        float bH = outerR * 2f;

        // Track (full top-half arc)
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = trackThick;
        canvas.StrokeColor = IsDark ? Color.FromArgb("#252525") : Color.FromArgb("#E0E0E0");
        canvas.DrawArc(bLeft, bTop, bW, bH, 180f, 360f, true, false);

        if (Score > 0)
        {
            float progress = MathF.Min((float)(Score / 100.0), 1f);
            float endAngle = 180f + progress * 180f;

            // Outer glow
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Color.FromRgba(0x4A, 0xDE, 0x80, 45);
            canvas.StrokeSize = trackThick + 12f;
            canvas.DrawArc(bLeft, bTop, bW, bH, 180f, endAngle, true, false);

            // Mid glow
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Color.FromRgba(0x4A, 0xDE, 0x80, 80);
            canvas.StrokeSize = trackThick + 4f;
            canvas.DrawArc(bLeft, bTop, bW, bH, 180f, endAngle, true, false);

            // Main arc
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Color.FromArgb("#4ADE80");
            canvas.StrokeSize = trackThick;
            canvas.DrawArc(bLeft, bTop, bW, bH, 180f, endAngle, true, false);

            // End cap dot
            double endRad = endAngle * Math.PI / 180.0;
            float dotX = cx + outerR * (float)Math.Cos(endRad);
            float dotY = cy + outerR * (float)Math.Sin(endRad);
            canvas.FillColor = Color.FromArgb("#FFFFFF");
            canvas.FillCircle(dotX, dotY, trackThick * 0.45f);
        }

        // Scale ticks (small marks at 0, 25, 50, 75, 100 percent positions)
        var tickAngles = new float[] { 180f, 225f, 270f, 315f, 360f };
        foreach (var tickAngle in tickAngles)
        {
            double rad = tickAngle * Math.PI / 180.0;
            float innerTickR = outerR + trackThick * 0.8f;
            float outerTickR = outerR + trackThick * 1.8f;
            float x1 = cx + innerTickR * (float)Math.Cos(rad);
            float y1 = cy + innerTickR * (float)Math.Sin(rad);
            float x2 = cx + outerTickR * (float)Math.Cos(rad);
            float y2 = cy + outerTickR * (float)Math.Sin(rad);
            canvas.StrokeColor = IsDark ? Color.FromArgb("#383838") : Color.FromArgb("#CCCCCC");
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(x1, y1, x2, y2);
        }
    }
}
