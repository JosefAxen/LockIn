using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class TrainingScoreDrawable : IDrawable
{
    public double Score { get; set; } = 72;

    private static readonly Color s_trackDark  = Color.FromArgb("#282828");
    private static readonly Color s_trackLight = Color.FromArgb("#D0D0D0");
    private static readonly Color s_white      = Color.FromArgb("#FFFFFF");

    public void Draw(ICanvas canvas, RectF r)
    {
        bool isDark = Application.Current?.RequestedTheme != AppTheme.Light;

        float cx     = r.Width / 2f;
        float cy     = r.Height * 0.90f;
        float radius = MathF.Min(r.Width * 0.43f, r.Height * 0.84f);
        float thick  = radius * 0.115f;

        float bL = cx - radius;
        float bT = cy - radius;
        float bW = radius * 2f;
        float bH = radius * 2f;

        // 1. Dotted background track (prickad linje som på referensbilden)
        canvas.StrokeLineCap     = LineCap.Round;
        canvas.StrokeSize        = thick * 0.50f;
        canvas.StrokeColor       = isDark ? s_trackDark : s_trackLight;
        canvas.StrokeDashPattern = new float[] { 0.01f, thick * 0.85f };
        canvas.DrawArc(bL, bT, bW, bH, 180f, 360f, true, false);
        canvas.StrokeDashPattern = null;

        // 2. Gradient progress arc: röd → grön, 60 DrawArc-segment med LineCap.Butt
        if (Score > 0)
        {
            float progress = MathF.Min((float)(Score / 100.0), 1f);
            float totalDeg = progress * 180f;
            const int segs = 60;
            float segSize  = totalDeg / segs;

            canvas.StrokeLineCap = LineCap.Butt;
            canvas.StrokeSize    = thick + 0.5f;

            for (int i = 0; i < segs; i++)
            {
                float t     = segs > 1 ? (float)i / (segs - 1) : 0f;
                float start = 180f + i * segSize;
                float end   = start + segSize + 0.4f; // litet överlapp mot AA-sömmar

                int rr = (int)(239 - 165 * t);
                int gg = (int)(68  + 154 * t);
                int bb = (int)(68  +  60 * t);

                canvas.StrokeColor = Color.FromRgb(rr, gg, bb);
                canvas.DrawArc(bL, bT, bW, bH, start, end, true, false);
            }

            // 3. Vit indikator-dot vid progressens spets
            float endRad = (180f + totalDeg) * MathF.PI / 180f;
            float dotX   = cx + radius * MathF.Cos(endRad);
            float dotY   = cy + radius * MathF.Sin(endRad);
            canvas.FillColor = s_white;
            canvas.FillCircle(dotX, dotY, thick * 0.52f);
        }

        // 4. Tick-markeringar — 21 st, var 5% (0 % → 100 %)
        //    Större tick var 25 % (i = 0, 5, 10, 15, 20)
        canvas.StrokeLineCap = LineCap.Round;
        for (int i = 0; i <= 20; i++)
        {
            float tickAngle = 180f + i * 9f; // 180° / 20 intervall = 9°/tick
            double rad = tickAngle * Math.PI / 180.0;

            bool isMajor = (i % 5 == 0);
            float innerR = radius + thick * 0.55f;
            float outerT = innerR + thick * (isMajor ? 1.05f : 0.60f);

            float x1 = cx + innerR * (float)Math.Cos(rad);
            float y1 = cy + innerR * (float)Math.Sin(rad);
            float x2 = cx + outerT  * (float)Math.Cos(rad);
            float y2 = cy + outerT  * (float)Math.Sin(rad);

            canvas.StrokeSize  = isMajor ? 1.8f : 1f;
            canvas.StrokeColor = isDark
                ? Color.FromArgb(isMajor ? "#4A4A4A" : "#343434")
                : Color.FromArgb(isMajor ? "#AAAAAA" : "#C8C8C8");
            canvas.DrawLine(x1, y1, x2, y2);
        }
    }
}
