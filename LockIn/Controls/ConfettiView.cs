using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public sealed class ConfettiView : SKCanvasView
{
    private const int ParticleCount = 80;
    private const float TotalDuration = 3.2f;
    private const float FadeTime = 0.7f;
    private const float Gravity = 0.26f;

    private static readonly SKColor[] Palette =
    [
        new(0xFF, 0x5A, 0x1F), // Forge orange
        new(0xF5, 0xA6, 0x23), // amber
        new(0x6E, 0xA8, 0xDC), // blue
        new(0xFF, 0xFF, 0xFF), // white
        new(0xA8, 0x55, 0xF7), // purple
        new(0x4A, 0xDE, 0x80), // green
    ];

    private readonly List<Particle> _particles = [];
    private IDispatcherTimer? _timer;
    private long _startMs;
    private bool _running;

    public ConfettiView()
    {
        InputTransparent = true;
        IsVisible = false;
        BackgroundColor = Colors.Transparent;
    }

    public void Start()
    {
        if (_running) Stop();
        SpawnParticles();
        _startMs = Environment.TickCount64;
        _running = true;
        IsVisible = true;

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _running = false;
        IsVisible = false;
        _particles.Clear();
    }

    private void SpawnParticles()
    {
        _particles.Clear();
        var rng = new Random();
        for (int i = 0; i < ParticleCount; i++)
        {
            _particles.Add(new Particle
            {
                NormX    = 0.05f + (float)rng.NextDouble() * 0.90f,
                NormY    = -0.04f - (float)rng.NextDouble() * 0.22f,
                VelX     = (float)(rng.NextDouble() - 0.5) * 0.52f,
                VelY     = (float)(rng.NextDouble() * 0.35 + 0.14),
                RotDeg   = (float)(rng.NextDouble() * 360),
                RotSpeed = (float)(rng.NextDouble() * 560 - 280),
                Color    = Palette[rng.Next(Palette.Length)],
                IsRect   = rng.NextDouble() > 0.40,
                SizeDp   = (float)(rng.NextDouble() * 9.0 + 5.0),
                Delay    = (float)(rng.NextDouble() * 0.45),
            });
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        float elapsed = (Environment.TickCount64 - _startMs) / 1000f;
        if (elapsed >= TotalDuration)
        {
            Stop();
            return;
        }
        InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (!_running || _particles.Count == 0 || e.Info.Width == 0) return;

        float elapsed = (Environment.TickCount64 - _startMs) / 1000f;
        float w = e.Info.Width;
        float h = e.Info.Height;
        float scale = Width > 0 ? w / (float)Width : 1f;

        float globalFade = elapsed >= TotalDuration - FadeTime
            ? Math.Max(0f, (TotalDuration - elapsed) / FadeTime)
            : 1f;

        using var paint = new SKPaint { IsAntialias = true };

        foreach (var p in _particles)
        {
            float t = elapsed - p.Delay;
            if (t <= 0) continue;

            float px = (p.NormX + p.VelX * t) * w;
            float py = (p.NormY + p.VelY * t + 0.5f * Gravity * t * t) * h;
            if (py > h + 40 * scale) continue;

            canvas.Save();
            canvas.Translate(px, py);
            canvas.RotateDegrees(p.RotDeg + p.RotSpeed * t);

            paint.Color = p.Color.WithAlpha((byte)(255 * globalFade));
            float sz = p.SizeDp * scale;

            if (p.IsRect)
                canvas.DrawRect(SKRect.Create(-sz / 2f, -sz * 0.28f, sz, sz * 0.56f), paint);
            else
                canvas.DrawCircle(0, 0, sz * 0.44f, paint);

            canvas.Restore();
        }
    }

    private sealed class Particle
    {
        public float NormX, NormY;
        public float VelX, VelY;
        public float RotDeg, RotSpeed;
        public float SizeDp, Delay;
        public SKColor Color;
        public bool IsRect;
    }
}
