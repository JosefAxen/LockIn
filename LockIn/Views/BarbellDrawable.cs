using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class BarbellDrawable : IDrawable
{
    public List<(decimal Plate, int Count)> Plates { get; set; } = new();

    // Plate visual thickness (px) per kg
    private static readonly Dictionary<decimal, float> PlateWidth = new()
    {
        [25m]   = 28f,
        [20m]   = 24f,
        [15m]   = 20f,
        [10m]   = 16f,
        [5m]    = 13f,
        [2.5m]  = 10f,
        [1.25m] = 8f,
    };

    // Plate height (px) per kg — proportional to real life
    private static readonly Dictionary<decimal, float> PlateHeight = new()
    {
        [25m]   = 120f,
        [20m]   = 110f,
        [15m]   = 96f,
        [10m]   = 82f,
        [5m]    = 68f,
        [2.5m]  = 56f,
        [1.25m] = 46f,
    };

    private static readonly Color BarColor    = Color.FromArgb("#484850");
    private static readonly Color PlateColor  = Color.FromArgb("#FF5A1F");
    private static readonly Color PlateLabel  = Color.FromArgb("#FFFFFF");
    private static readonly Color CollarColor = Color.FromArgb("#303035");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f;
        float barHeight = 12f;
        float barHalfLen = dirtyRect.Width * 0.45f;
        float collarW = 14f;

        // Bar
        canvas.FillColor = BarColor;
        canvas.FillRoundedRectangle(cx - barHalfLen, cy - barHeight / 2f,
            barHalfLen * 2f, barHeight, 4f);

        // Draw plates outward from collar on both sides
        if (Plates.Count == 0) return;

        float rightX = cx + collarW;
        float leftX  = cx - collarW;

        // Collar
        canvas.FillColor = CollarColor;
        canvas.FillRoundedRectangle(cx - collarW, cy - 20f, collarW * 2f, 40f, 3f);

        foreach (var (plate, count) in Plates)
        {
            if (!PlateWidth.TryGetValue(plate, out var pw)) pw = 12f;
            if (!PlateHeight.TryGetValue(plate, out var ph)) ph = 50f;
            float halfPh = ph / 2f;

            for (int i = 0; i < count; i++)
            {
                // Right side
                float rx = rightX;
                canvas.FillColor = PlateColor;
                canvas.FillRoundedRectangle(rx, cy - halfPh, pw, ph, 3f);
                DrawPlateLabel(canvas, plate, rx + pw / 2f, cy, PlateLabel);
                rightX += pw + 2f;

                // Left side
                float lx = leftX - pw;
                canvas.FillColor = PlateColor;
                canvas.FillRoundedRectangle(lx, cy - halfPh, pw, ph, 3f);
                DrawPlateLabel(canvas, plate, lx + pw / 2f, cy, PlateLabel);
                leftX -= pw + 2f;
            }
        }
    }

    private static void DrawPlateLabel(ICanvas canvas, decimal plate, float x, float y, Color color)
    {
        canvas.FontColor = color;
        canvas.FontSize = 9f;
        var label = plate >= 10 ? $"{(int)plate}" : $"{plate:G}";
        canvas.DrawString(label, x - 12f, y - 8f, 24f, 16f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
