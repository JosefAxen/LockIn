using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class SparklineDrawable : IDrawable
{
    public IReadOnlyList<double> Values { get; set; } = Array.Empty<double>();
    public Color LineColor { get; set; } = Color.FromArgb("#4ADE80");

    public void Draw(ICanvas canvas, RectF r)
    {
        if (Values.Count < 2) return;

        const float padH = 2f;
        const float padV = 3f;
        float w = r.Width - padH * 2;
        float h = r.Height - padV * 2;

        double min = Values.Min();
        double max = Values.Max();
        double range = max - min;
        if (range < 0.001) range = 1.0;

        var pts = Values
            .Select((v, i) => new PointF(
                padH + w * i / (Values.Count - 1),
                padV + h * (float)(1.0 - (v - min) / range)
            ))
            .ToList();

        // Area fill
        var fill = new PathF();
        fill.MoveTo(pts[0].X, r.Height);
        foreach (var p in pts) fill.LineTo(p.X, p.Y);
        fill.LineTo(pts[^1].X, r.Height);
        fill.Close();
        canvas.FillColor = Color.FromRgba(LineColor.Red, LineColor.Green, LineColor.Blue, 0.12f);
        canvas.FillPath(fill);

        // Line
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 1.8f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        for (int i = 1; i < pts.Count; i++)
            canvas.DrawLine(pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);

        // End dot with glow
        float ex = pts[^1].X, ey = pts[^1].Y;
        canvas.FillColor = Color.FromRgba(LineColor.Red, LineColor.Green, LineColor.Blue, 0.25f);
        canvas.FillCircle(ex, ey, 5f);
        canvas.FillColor = LineColor;
        canvas.FillCircle(ex, ey, 2.5f);
    }
}
