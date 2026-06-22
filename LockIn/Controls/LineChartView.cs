using LockIn.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class LineChartView : SKCanvasView
{
    public static readonly BindableProperty PointsProperty =
        BindableProperty.Create(nameof(Points), typeof(IReadOnlyList<ChartPoint>), typeof(LineChartView),
            defaultValue: null,
            propertyChanged: (b, _, _) => ((LineChartView)b).InvalidateSurface());

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(LineChartView),
            defaultValue: Colors.White,
            propertyChanged: (b, _, _) => ((LineChartView)b).InvalidateSurface());

    public IReadOnlyList<ChartPoint> Points
    {
        get => (IReadOnlyList<ChartPoint>)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var pts = Points;
        if (pts is null || pts.Count < 2)
            return;

        float w = e.Info.Width;
        float h = e.Info.Height;

        const float leftMargin   = 44f;
        const float bottomMargin = 22f;
        const float topMargin    = 8f;
        const float rightMargin  = 8f;

        float plotW = w - leftMargin - rightMargin;
        float plotH = h - topMargin - bottomMargin;

        double minVal = pts[0].Value, maxVal = pts[0].Value;
        foreach (var p in pts) { if (p.Value < minVal) minVal = p.Value; if (p.Value > maxVal) maxVal = p.Value; }
        double valRange = maxVal - minVal;
        if (valRange < 1e-9) valRange = 1;

        long minTick = pts[0].Date.Ticks, maxTick = pts[0].Date.Ticks;
        foreach (var p in pts) { if (p.Date.Ticks < minTick) minTick = p.Date.Ticks; if (p.Date.Ticks > maxTick) maxTick = p.Date.Ticks; }
        long tickRange = maxTick - minTick;
        if (tickRange == 0) tickRange = 1;

        var c = StrokeColor;
        var sk = new SKColor(
            (byte)(c.Red * 255),
            (byte)(c.Green * 255),
            (byte)(c.Blue * 255),
            (byte)(c.Alpha * 255));

        var separatorColor = SkiaTokens.ChartSep;
        var labelColor     = SkiaTokens.AxisText;
        var dotBorder      = SkiaTokens.DotBorder;

        // Horizontal separator lines + y-labels (4 steps)
        using var sepPaint = new SKPaint
        {
            Color       = separatorColor,
            StrokeWidth = 1f,
            IsAntialias = false,
            Style       = SKPaintStyle.Stroke,
        };
        using var yLabelPaint = new SKPaint
        {
            Color     = labelColor,
            TextSize  = 9f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right,
        };

        for (int i = 0; i <= 3; i++)
        {
            float t = i / 3f;
            float y = topMargin + plotH - t * plotH;
            canvas.DrawLine(leftMargin, y, leftMargin + plotW, y, sepPaint);

            double labelVal = minVal + t * valRange;
            canvas.DrawText($"{(int)labelVal}", leftMargin - 4f, y + 3.5f, yLabelPaint);
        }

        // Compute pixel coordinates for all points
        var xCoords = new float[pts.Count];
        var yCoords = new float[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            float tx = (float)((pts[i].Date.Ticks - minTick) / (double)tickRange);
            float ty = (float)((pts[i].Value - minVal) / valRange);
            xCoords[i] = leftMargin + tx * plotW;
            yCoords[i] = topMargin + plotH - ty * plotH;
        }

        // X-axis date labels (max 6, evenly spaced)
        using var xLabelPaint = new SKPaint
        {
            Color       = labelColor,
            TextSize    = 9f,
            IsAntialias = true,
            TextAlign   = SKTextAlign.Center,
        };
        int labelCount = Math.Min(6, pts.Count);
        for (int i = 0; i < labelCount; i++)
        {
            int idx = (int)Math.Round(i * (pts.Count - 1.0) / (labelCount - 1.0));
            float lx = xCoords[idx];
            float ly = topMargin + plotH + bottomMargin - 4f;
            canvas.DrawText(pts[idx].Date.ToString("d/M"), lx, ly, xLabelPaint);
        }

        // Fill area
        using var fillPath = new SKPath();
        fillPath.MoveTo(xCoords[0], topMargin + plotH);
        fillPath.LineTo(xCoords[0], yCoords[0]);
        for (int i = 1; i < pts.Count; i++)
            fillPath.LineTo(xCoords[i], yCoords[i]);
        fillPath.LineTo(xCoords[pts.Count - 1], topMargin + plotH);
        fillPath.Close();

        using var fillPaint = new SKPaint
        {
            Color       = sk.WithAlpha(30),
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
        };
        canvas.DrawPath(fillPath, fillPaint);

        // Line
        using var linePath = new SKPath();
        linePath.MoveTo(xCoords[0], yCoords[0]);
        for (int i = 1; i < pts.Count; i++)
            linePath.LineTo(xCoords[i], yCoords[i]);

        using var linePaint = new SKPaint
        {
            Color       = sk,
            StrokeWidth = 2f,
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeCap   = SKStrokeCap.Round,
            StrokeJoin  = SKStrokeJoin.Round,
        };
        canvas.DrawPath(linePath, linePaint);

        // Dots
        using var dotFill = new SKPaint
        {
            Color       = sk,
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
        };
        using var dotStroke = new SKPaint
        {
            Color       = dotBorder,
            StrokeWidth = 1.5f,
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
        };
        for (int i = 0; i < pts.Count; i++)
        {
            canvas.DrawCircle(xCoords[i], yCoords[i], 4f, dotFill);
            canvas.DrawCircle(xCoords[i], yCoords[i], 4f, dotStroke);
        }
    }
}
