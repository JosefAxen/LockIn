using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class SparklineView : SKCanvasView
{
    public static readonly BindableProperty ValuesProperty =
        BindableProperty.Create(nameof(Values), typeof(double[]), typeof(SparklineView),
            defaultValue: null,
            propertyChanged: (b, _, _) => ((SparklineView)b).InvalidateSurface());

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(SparklineView),
            defaultValue: Colors.White,
            propertyChanged: (b, _, _) => ((SparklineView)b).InvalidateSurface());

    public double[] Values
    {
        get => (double[])GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
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

        var pts = Values;
        if (pts is null || pts.Length < 2)
            return;

        float w = e.Info.Width;
        float h = e.Info.Height;

        double min = pts[0], max = pts[0];
        foreach (var v in pts) { if (v < min) min = v; if (v > max) max = v; }
        double range = max - min;
        if (range < 1e-9) range = 1;

        var c = StrokeColor;
        var sk = new SKColor(
            (byte)(c.Red * 255),
            (byte)(c.Green * 255),
            (byte)(c.Blue * 255),
            (byte)(c.Alpha * 255));

        // Build coordinate arrays
        var xCoords = new float[pts.Length];
        var yCoords = new float[pts.Length];
        float step = pts.Length > 1 ? w / (pts.Length - 1) : w;
        for (int i = 0; i < pts.Length; i++)
        {
            xCoords[i] = i * step;
            yCoords[i] = h - (float)((pts[i] - min) / range) * h;
        }

        // Fill path
        using var fillPath = new SKPath();
        fillPath.MoveTo(xCoords[0], h);
        fillPath.LineTo(xCoords[0], yCoords[0]);
        for (int i = 1; i < pts.Length; i++)
            fillPath.LineTo(xCoords[i], yCoords[i]);
        fillPath.LineTo(xCoords[pts.Length - 1], h);
        fillPath.Close();

        using var fillPaint = new SKPaint
        {
            Color       = sk.WithAlpha(35),
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
        };
        canvas.DrawPath(fillPath, fillPaint);

        // Stroke path
        using var linePath = new SKPath();
        linePath.MoveTo(xCoords[0], yCoords[0]);
        for (int i = 1; i < pts.Length; i++)
            linePath.LineTo(xCoords[i], yCoords[i]);

        using var strokePaint = new SKPaint
        {
            Color       = sk,
            StrokeWidth = 1.8f,
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeCap   = SKStrokeCap.Round,
            StrokeJoin  = SKStrokeJoin.Round,
        };
        canvas.DrawPath(linePath, strokePaint);
    }
}
