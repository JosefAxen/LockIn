using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class TrainingScoreDrawable : IDrawable
{
    public double Score { get; set; } = 72;

    private static readonly Color s_trackDark  = Color.FromArgb("#252525");
    private static readonly Color s_trackLight = Color.FromArgb("#E0E0E0");
    private static readonly Color s_glow1      = Color.FromRgba(0x4A, 0xDE, 0x80, 45);
    private static readonly Color s_glow2      = Color.FromRgba(0x4A, 0xDE, 0x80, 80);
    private static readonly Color s_accent     = Color.FromArgb("#4ADE80");
    private static readonly Color s_white      = Color.FromArgb("#FFFFFF");
    private static readonly Color s_tickDark   = Color.FromArgb("#383838");
    private static readonly Color s_tickLight  = Color.FromArgb("#CCCCCC");
    private static readonly float[] s_tickAngles = { 180f, 225f, 270f, 315f, 360f };

    public void Draw(ICanvas canvas, RectF r)
    {
        bool isDark = Application.Current?.RequestedTheme != AppTheme.Light;

        float cx = r.Width / 2f;
        float cy = r.Height * 0.90f;
        float outerR = MathF.Min(r.Width * 0.43f, r.Height * 0.84f);
        float trackThick = outerR * 0.115f;

        float bLeft = cx - outerR;
        float bTop  = cy - outerR;
        float bW    = outerR * 2f;
        float bH    = outerR * 2f;

        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeSize = trackThick;
        canvas.StrokeColor = isDark ? s_trackDark : s_trackLight;
        canvas.DrawArc(bLeft, bTop, bW, bH, 180f, 360f, true, false);

        if (Score > 0)
        {
            float progress = MathF.Min((float)(Score / 100.0), 1f);
            float totalDeg = progress * 180f;
            const int segments = 24;
            float segSize = totalDeg / segments;

            for (int s = 0; s < segments; s++)
            {
                float t = segments > 1 ? (float)s / (segments - 1) : 0f;
                float segStart = 180f + s * segSize;
                float segEnd   = segStart + segSize;

                // Red #EF4444 → Green #4ADE80
                int rr = (int)(239 - 165 * t);
                int gg = (int)(68  + 154 * t);
                int bb = (int)(68  +  60 * t);

                canvas.StrokeLineCap = LineCap.Butt;
                canvas.StrokeColor = Color.FromRgba(rr, gg, bb, 45);
                canvas.StrokeSize = trackThick + 12f;
                canvas.DrawArc(bLeft, bTop, bW, bH, segStart, segEnd, true, false);

                canvas.StrokeColor = Color.FromRgba(rr, gg, bb, 80);
                canvas.StrokeSize = trackThick + 4f;
                canvas.DrawArc(bLeft, bTop, bW, bH, segStart, segEnd, true, false);

                canvas.StrokeColor = Color.FromRgb(rr, gg, bb);
                canvas.StrokeSize = trackThick;
                canvas.DrawArc(bLeft, bTop, bW, bH, segStart, segEnd, true, false);
            }

            float endAngle = 180f + totalDeg;
            double endRad = endAngle * Math.PI / 180.0;
            float dotX = cx + outerR * (float)Math.Cos(endRad);
            float dotY = cy + outerR * (float)Math.Sin(endRad);
            canvas.FillColor = s_white;
            canvas.FillCircle(dotX, dotY, trackThick * 0.45f);
        }

        var tickColor = isDark ? s_tickDark : s_tickLight;
        foreach (var tickAngle in s_tickAngles)
        {
            double rad = tickAngle * Math.PI / 180.0;
            float innerR = outerR + trackThick * 0.8f;
            float outerT  = outerR + trackThick * 1.8f;
            float x1 = cx + innerR * (float)Math.Cos(rad);
            float y1 = cy + innerR * (float)Math.Sin(rad);
            float x2 = cx + outerT  * (float)Math.Cos(rad);
            float y2 = cy + outerT  * (float)Math.Sin(rad);
            canvas.StrokeColor = tickColor;
            canvas.StrokeSize = 1.5f;
            canvas.DrawLine(x1, y1, x2, y2);
        }
    }
}
