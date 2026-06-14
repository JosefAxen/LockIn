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
        float outerR = MathF.Min(r.Width * 0.50f, cy - 2f);
        float trackThick = MathF.Max(3f, outerR * 0.040f);

        // Track arc
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize  = trackThick;
        canvas.StrokeColor = isDark ? s_trackDark : s_trackLight;
        canvas.DrawArc(cx - outerR, cy - outerR, outerR * 2f, outerR * 2f, 180f, 360f, true, false);

        if (Score > 0)
        {
            float innerR   = outerR - trackThick;
            float progress = MathF.Min((float)(Score / 100.0), 1f);
            float totalDeg = progress * 180f;
            const int segments = 72;
            float segSize = totalDeg / segments;

            for (int s = 0; s < segments; s++)
            {
                float t        = segments > 1 ? (float)s / (segments - 1) : 0f;
                float segStart = 180f + s * segSize;
                float segEnd   = segStart + segSize;

                int rr = (int)(239 - 165 * t);
                int gg = (int)(68  + 154 * t);
                int bb = (int)(68  +  60 * t);

                float sRad = segStart * MathF.PI / 180f;
                float eRad = segEnd   * MathF.PI / 180f;

                var path = new PathF();
                path.MoveTo(cx + outerR * MathF.Cos(sRad), cy + outerR * MathF.Sin(sRad));
                path.AddArc(cx - outerR, cy - outerR, outerR * 2f, outerR * 2f, segStart, segEnd, true);
                path.LineTo(cx + innerR * MathF.Cos(eRad), cy + innerR * MathF.Sin(eRad));
                path.AddArc(cx - innerR, cy - innerR, innerR * 2f, innerR * 2f, segEnd, segStart, false);
                path.Close();

                canvas.FillColor = Color.FromRgb(rr, gg, bb);
                canvas.FillPath(path);
            }

            // White indicator dot at tip
            float endAngle = 180f + totalDeg;
            double endRad  = endAngle * Math.PI / 180.0;
            float  midR    = (outerR + innerR) / 2f;
            float  dotX    = cx + midR * (float)Math.Cos(endRad);
            float  dotY    = cy + midR * (float)Math.Sin(endRad);
            canvas.FillColor = s_white;
            canvas.FillCircle(dotX, dotY, trackThick * 1.15f);
        }
    }
}
