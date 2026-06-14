using LockIn;
using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class BodyWeightDrawable : IDrawable
{
    public List<(DateTime Date, double Weight)> Points { get; set; } = new();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Points.Count == 0)
        {
            canvas.FontColor = DesignTokens.GraphAxisText;
            canvas.FontSize = 13;
            canvas.DrawString("Ingen data ännu", dirtyRect.Width / 2, dirtyRect.Height / 2,
                HorizontalAlignment.Center);
            return;
        }

        float left = 8, right = dirtyRect.Width - 8;
        float top = 12, bottom = dirtyRect.Height - 24;
        float w = right - left, h = bottom - top;

        var ordered = Points.OrderBy(p => p.Date).ToList();
        double minW = ordered.Min(p => p.Weight) - 1;
        double maxW = ordered.Max(p => p.Weight) + 1;
        double range = maxW - minW;
        if (range < 1) range = 1;

        // Grid lines
        canvas.StrokeColor = DesignTokens.GraphGrid;
        canvas.StrokeSize = 1;
        for (int i = 0; i <= 4; i++)
        {
            float y = top + h * i / 4f;
            canvas.DrawLine(left, y, right, y);
        }

        // Area fill
        var path = new PathF();
        var pts = ordered.Select((p, i) =>
        {
            float x = left + w * i / Math.Max(1, ordered.Count - 1);
            float y = top + h * (float)(1 - (p.Weight - minW) / range);
            return (x, y);
        }).ToList();

        path.MoveTo(pts[0].x, bottom);
        for (int i = 0; i < pts.Count; i++)
            path.LineTo(pts[i].x, pts[i].y);
        path.LineTo(pts[^1].x, bottom);
        path.Close();

        canvas.FillColor = Color.FromArgb("#1A6EA8DC");
        canvas.FillPath(path);

        // Line
        canvas.StrokeColor = Color.FromArgb("#6EA8DC");
        canvas.StrokeSize = 2;
        for (int i = 1; i < pts.Count; i++)
            canvas.DrawLine(pts[i - 1].x, pts[i - 1].y, pts[i].x, pts[i].y);

        // Dots
        foreach (var (x, y) in pts)
        {
            canvas.FillColor = Color.FromArgb("#6EA8DC");
            canvas.FillCircle(x, y, 4);
            canvas.StrokeColor = DesignTokens.GraphDotStroke;
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(x, y, 4);
        }

        // X-axis dates (first, middle, last)
        canvas.FontColor = DesignTokens.GraphAxisText;
        canvas.FontSize = 10;
        if (ordered.Count >= 1)
            canvas.DrawString(ordered[0].Date.ToString("d/M"), pts[0].x, bottom + 4, HorizontalAlignment.Center);
        if (ordered.Count >= 3)
        {
            int mid = ordered.Count / 2;
            canvas.DrawString(ordered[mid].Date.ToString("d/M"), pts[mid].x, bottom + 4, HorizontalAlignment.Center);
        }
        if (ordered.Count >= 2)
            canvas.DrawString(ordered[^1].Date.ToString("d/M"), pts[^1].x, bottom + 4, HorizontalAlignment.Center);
    }
}
