using Microsoft.Maui.Graphics;

namespace LockIn.Views;

public class SparklineDrawable : IDrawable
{
    private IReadOnlyList<double> _values = Array.Empty<double>();
    private PointF[]? _pts;
    private PathF? _fillPath;
    private PathF? _linePath;
    private float _cachedW, _cachedH;

    public IReadOnlyList<double> Values
    {
        get => _values;
        set { _values = value; _pts = null; _fillPath = null; _linePath = null; }
    }

    public Color LineColor { get; set; } = Color.FromArgb("#4ADE80");

    public void Draw(ICanvas canvas, RectF r)
    {
        if (_values.Count < 2) return;

        if (_pts is null || _cachedW != r.Width || _cachedH != r.Height)
            Precompute(r);

        var pts = _pts!;

        canvas.FillColor = Color.FromRgba(LineColor.Red, LineColor.Green, LineColor.Blue, 0.12f);
        canvas.FillPath(_fillPath!);

        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 1.8f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(_linePath!);

        float ex = pts[^1].X, ey = pts[^1].Y;
        canvas.FillColor = Color.FromRgba(LineColor.Red, LineColor.Green, LineColor.Blue, 0.25f);
        canvas.FillCircle(ex, ey, 5f);
        canvas.FillColor = LineColor;
        canvas.FillCircle(ex, ey, 2.5f);
    }

    private void Precompute(RectF r)
    {
        const float padH = 2f, padV = 3f;
        float w = r.Width - padH * 2;
        float h = r.Height - padV * 2;

        double min = double.MaxValue, max = double.MinValue;
        foreach (var v in _values) { if (v < min) min = v; if (v > max) max = v; }
        double range = max - min;
        if (range < 0.001) range = 1.0;

        _pts = new PointF[_values.Count];
        for (int i = 0; i < _values.Count; i++)
            _pts[i] = new PointF(
                padH + w * i / (_values.Count - 1),
                padV + h * (float)(1.0 - (_values[i] - min) / range));

        var fill = new PathF();
        fill.MoveTo(_pts[0].X, r.Height);
        foreach (var p in _pts) fill.LineTo(p.X, p.Y);
        fill.LineTo(_pts[^1].X, r.Height);
        fill.Close();
        _fillPath = fill;

        var line = new PathF();
        line.MoveTo(_pts[0].X, _pts[0].Y);
        for (int i = 1; i < _pts.Length; i++) line.LineTo(_pts[i].X, _pts[i].Y);
        _linePath = line;

        _cachedW = r.Width;
        _cachedH = r.Height;
    }
}
