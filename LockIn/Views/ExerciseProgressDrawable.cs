using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class ExerciseProgressDrawable : IDrawable
{
    public List<(DateTime Date, double Epley1RM, bool IsPR)> Points { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirty)
    {
        const float padL = 50f, padR = 16f, padT = 20f, padB = 36f;

        if (Points.Count == 0)
        {
            canvas.FontColor = Color.FromArgb("#484850");
            canvas.FontSize = 13;
            canvas.DrawString("Logga ett pass för att se progress",
                dirty.Left + dirty.Width / 2, dirty.Top + dirty.Height / 2 - 6,
                HorizontalAlignment.Center);
            return;
        }

        var pts = Points.OrderBy(p => p.Date).TakeLast(12).ToList();
        float plotW = dirty.Width - padL - padR;
        float plotH = dirty.Height - padT - padB;

        double rawMin = pts.Min(p => p.Epley1RM);
        double rawMax = pts.Max(p => p.Epley1RM);
        double range = rawMax - rawMin;
        double minY = rawMin - (range < 5 ? 5 : range * 0.12);
        double maxY = rawMax + (range < 5 ? 5 : range * 0.12);

        float MapY(double val) => padT + plotH - (float)((val - minY) / (maxY - minY) * plotH);
        float MapX(int i) => padL + (pts.Count == 1 ? plotW / 2 : i * plotW / (pts.Count - 1));

        // Grid lines
        canvas.StrokeColor = Color.FromArgb("#1E1E22");
        canvas.StrokeSize = 1;
        for (int g = 0; g <= 4; g++)
        {
            double val = minY + (maxY - minY) * g / 4;
            float y = MapY(val);
            canvas.DrawLine(padL, y, padL + plotW, y);

            canvas.FontColor = Color.FromArgb("#484850");
            canvas.FontSize = 9;
            canvas.DrawString($"{(int)val}", padL - 6, y, HorizontalAlignment.Right);
        }

        // Area fill under line
        if (pts.Count > 1)
        {
            var area = new PathF();
            area.MoveTo(MapX(0), padT + plotH);
            for (int i = 0; i < pts.Count; i++)
                area.LineTo(MapX(i), MapY(pts[i].Epley1RM));
            area.LineTo(MapX(pts.Count - 1), padT + plotH);
            area.Close();
            canvas.FillColor = Color.FromArgb("#1AFF5A1F");
            canvas.FillPath(area);
        }

        // Line
        if (pts.Count > 1)
        {
            var line = new PathF();
            line.MoveTo(MapX(0), MapY(pts[0].Epley1RM));
            for (int i = 1; i < pts.Count; i++)
                line.LineTo(MapX(i), MapY(pts[i].Epley1RM));
            canvas.StrokeColor = Color.FromArgb("#FF5A1F");
            canvas.StrokeSize = 2;
            canvas.DrawPath(line);
        }

        // Dots
        for (int i = 0; i < pts.Count; i++)
        {
            float x = MapX(i), y = MapY(pts[i].Epley1RM);
            canvas.FillColor = pts[i].IsPR
                ? Color.FromArgb("#4ADE80")
                : Color.FromArgb("#FF5A1F");
            canvas.FillCircle(x, y, 4.5f);
            canvas.StrokeColor = Color.FromArgb("#111111");
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(x, y, 4.5f);
        }

        // X-axis date labels
        canvas.FontColor = Color.FromArgb("#484850");
        canvas.FontSize = 9;
        var show = new HashSet<int> { 0, pts.Count - 1 };
        if (pts.Count >= 5) show.Add(pts.Count / 2);
        foreach (int i in show)
            canvas.DrawString(pts[i].Date.ToString("d/M"), MapX(i), padT + plotH + 16,
                HorizontalAlignment.Center);
    }
}
