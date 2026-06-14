using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class TrainingScoreDrawable : IDrawable
{
    public double Score { get; set; } = 72;

    private static readonly Color s_trackDark  = Color.FromArgb("#252525");
    private static readonly Color s_trackLight = Color.FromArgb("#E0E0E0");
    private static readonly Color s_white      = Color.FromArgb("#FFFFFF");

    public void Draw(ICanvas canvas, RectF r)
    {
        bool isDark = Application.Current?.RequestedTheme != AppTheme.Light;

        float cx     = r.Width / 2f;
        float cy     = r.Height * 0.97f;
        float radius = MathF.Min(r.Width * 0.50f, cy - 2f);
        float thick  = MathF.Max(6f, radius * 0.055f);

        // Track arc — full background semicircle
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize    = thick;
        canvas.StrokeColor   = isDark ? s_trackDark : s_trackLight;
        canvas.DrawArc(cx - radius, cy - radius, radius * 2f, radius * 2f,
                       180f, 360f, true, false);

        if (Score <= 0) return;

        float progress = MathF.Min((float)(Score / 100.0), 1f);
        float totalDeg = progress * 180f;
        const int segs = 60;
        float segSize  = totalDeg / segs;

        // Draw gradient arc using individual DrawArc calls (not PathF — different angle convention).
        // LineCap.Butt = flat ends, no bleed between adjacent segments.
        canvas.StrokeLineCap = LineCap.Butt;
        canvas.StrokeSize    = thick + 0.5f; // fractionally wider to seal AA seams

        for (int i = 0; i < segs; i++)
        {
            float t     = segs > 1 ? (float)i / (segs - 1) : 0f;
            float start = 180f + i * segSize;
            float end   = start + segSize + 0.4f; // tiny overlap to prevent AA gaps

            int rr = (int)(239 - 165 * t);
            int gg = (int)(68  + 154 * t);
            int bb = (int)(68  +  60 * t);

            canvas.StrokeColor = Color.FromRgb(rr, gg, bb);
            canvas.DrawArc(cx - radius, cy - radius, radius * 2f, radius * 2f,
                           start, end, true, false);
        }

        // White indicator dot at the progress tip
        float endRad = (180f + totalDeg) * MathF.PI / 180f;
        float dotX   = cx + radius * MathF.Cos(endRad);
        float dotY   = cy + radius * MathF.Sin(endRad);
        canvas.FillColor = s_white;
        canvas.FillCircle(dotX, dotY, thick * 0.85f);
    }
}
