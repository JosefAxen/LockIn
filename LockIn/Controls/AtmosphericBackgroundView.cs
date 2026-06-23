using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class AtmosphericBackgroundView : SKCanvasView
{
    private struct Spark
    {
        public float Xrel;        // horisontell start (0..1 av canvas-bredd)
        public float StartYrel;   // vertikal start (0..1 av canvas-höjd)
        public float TravelYrel;  // distans uppåt (andel av canvas-höjd)
        public float DriftRel;    // horisontell drift (andel av bredd)
        public float SizePx;      // kärnstorlek i pixlar (skalas av sizeMul)
        public float Duration;    // life-cycle i sekunder
        public float Phase;       // 0..1 stagger för att inte alla pulsera i sync
    }

    private static readonly Spark[] _sparks = CreateSparks();
    private IDispatcherTimer? _timer;
    private readonly DateTime _start = DateTime.Now;

    public AtmosphericBackgroundView()
    {
        EnableTouchEvents = false;
        Loaded += OnLoadedHandler;
        Unloaded += OnUnloadedHandler;
    }

    private void OnLoadedHandler(object? sender, EventArgs e)
    {
        if (_timer == null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33); // ~30fps
            _timer.Tick += OnTick;
        }
        _timer.Start();
    }

    private void OnUnloadedHandler(object? sender, EventArgs e) => _timer?.Stop();

    private void OnTick(object? sender, EventArgs e) => InvalidateSurface();

    private static Spark[] CreateSparks()
    {
        var rng = new Random(7); // deterministisk fördelning
        var arr = new Spark[14];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new Spark
            {
                Xrel = (float)((i + 0.5) / arr.Length + (rng.NextDouble() - 0.5) * 0.05),
                StartYrel = 0.38f + (float)rng.NextDouble() * 0.06f,
                TravelYrel = 0.34f + (float)rng.NextDouble() * 0.08f,
                DriftRel = (float)(rng.NextDouble() - 0.5) * 0.05f,
                SizePx = (float)(rng.NextDouble() * 3 + 4),  // 4-7 px kärna
                Duration = (float)(rng.NextDouble() * 1.6 + 4.2), // 4.2-5.8s
                Phase = (float)rng.NextDouble(),
            };
        }
        return arr;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;
        var bounds = new SKRect(0, 0, w, h);

        // Base fill
        canvas.Clear(SKColor.Parse("#141418"));

        // Primär radial glow — topp-center, bakom gauge-zonen
        using var glowShader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.17f),
            w * 0.65f,
            new[] { SKColor.Parse("#28B8B8BC"), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var glowPaint = new SKPaint { Shader = glowShader, IsAntialias = true };
        canvas.DrawRect(bounds, glowPaint);

        // Sekundär glow — mindre, mer koncentrerad
        using var glow2Shader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.12f),
            w * 0.3f,
            new[] { SKColor.Parse("#1CB8B8BC"), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var glow2Paint = new SKPaint { Shader = glow2Shader, IsAntialias = true };
        canvas.DrawRect(bounds, glow2Paint);

        // Edge vignette — ger djup åt kanter
        using var vigShader = SKShader.CreateRadialGradient(
            new SKPoint(w * 0.5f, h * 0.45f),
            Math.Max(w, h) * 0.75f,
            new[] { SKColors.Transparent, SKColor.Parse("#22000000") },
            new[] { 0.45f, 1f },
            SKShaderTileMode.Clamp);
        using var vigPaint = new SKPaint { Shader = vigShader, IsAntialias = true };
        canvas.DrawRect(bounds, vigPaint);

        // Amber-gnistor — svävar uppåt i toppen
        DrawSparks(canvas, w, h);
    }

    private void DrawSparks(SKCanvas canvas, float w, float h)
    {
        var elapsed = (float)(DateTime.Now - _start).TotalSeconds;

        using var paint = new SKPaint { IsAntialias = true, BlendMode = SKBlendMode.Plus };
        var coreColor = SKColor.Parse("#FDE68A"); // ljus kärna
        var midColor  = SKColor.Parse("#FBBF24"); // amber mellanglow
        var outerColor = SKColor.Parse("#FB923C"); // orange ytterglow

        foreach (var s in _sparks)
        {
            // Life-progress 0..1, loopas
            float t = ((elapsed / s.Duration) + s.Phase) % 1f;

            // Position
            float x = s.Xrel * w + s.DriftRel * w * t;
            float y = s.StartYrel * h - s.TravelYrel * h * t;

            // Opacity envelope: fade in 0-12%, plateau, fade out 80-100%
            float opacity;
            if (t < 0.12f) opacity = t / 0.12f;
            else if (t > 0.80f) opacity = (1f - t) / 0.20f;
            else opacity = 1f;
            opacity = Math.Clamp(opacity, 0f, 1f) * 0.95f;

            // Storlek: börjar liten, växer snabbt, krymper mot slutet
            float sizeMul = t < 0.15f
                ? 0.5f + (t / 0.15f) * 0.5f
                : 1.0f - (t - 0.15f) * 0.65f;
            sizeMul = Math.Max(0.3f, sizeMul);
            float size = s.SizePx * sizeMul;

            // Yttre glow (orange, stor, mjuk)
            paint.Color = outerColor.WithAlpha((byte)(opacity * 55));
            canvas.DrawCircle(x, y, size * 4.5f, paint);

            // Mellan-glow (amber)
            paint.Color = midColor.WithAlpha((byte)(opacity * 130));
            canvas.DrawCircle(x, y, size * 2.2f, paint);

            // Kärna (varm vit-gul)
            paint.Color = coreColor.WithAlpha((byte)(opacity * 255));
            canvas.DrawCircle(x, y, size * 0.9f, paint);
        }
    }
}
