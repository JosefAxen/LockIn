using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

public class AtmosphericBackgroundView : SKCanvasView
{
    // ── Spark data ──────────────────────────────────────────────────────────

    private struct Spark
    {
        public float Xrel;
        public float StartYrel;
        public float TravelYrel;
        public float DriftRel;
        public float SizePx;
        public float Duration;
        public float Phase;
    }

    private static readonly Spark[] _sparks = CreateSparks();

    // ── Cached static colors (parsed once) ──────────────────────────────────

    private static readonly SKColor s_base       = SKColor.Parse("#141418");
    private static readonly SKColor s_glow1Inner = SKColor.Parse("#28B8B8BC");
    private static readonly SKColor s_glow2Inner = SKColor.Parse("#1CB8B8BC");
    private static readonly SKColor s_vigOuter   = SKColor.Parse("#22000000");
    private static readonly SKColor s_coreColor  = SKColor.Parse("#FDE68A");
    private static readonly SKColor s_midColor   = SKColor.Parse("#FBBF24");
    private static readonly SKColor s_outerColor = SKColor.Parse("#FB923C");

    // ── Pre-baked static background (gradients + vignette) ──────────────────
    // Skapas en gång när storleken är känd. Partiklarna ritas ovanpå varje frame.

    private SKBitmap? _bgBitmap;
    private float _bgW;
    private float _bgH;

    // ── Cached paint + blur filter (create once, reuse every frame) ────────────
    // SKMaskFilter.CreateBlur ger äkta gaussian-glow, inte hårda cirklar.

    private static readonly SKMaskFilter s_glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 10f);
    private readonly SKPaint _sparkPaint = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus, MaskFilter = s_glowBlur };

    // ── Animation state ──────────────────────────────────────────────────────

    private IDispatcherTimer? _timer;
    private readonly DateTime _start = DateTime.Now;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public AtmosphericBackgroundView()
    {
        EnableTouchEvents = false;
        Loaded   += (_, _) => StartTimer();
        Unloaded += (_, _) => StopTimer();
    }

    private void StartTimer()
    {
        if (_timer is null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50); // 20fps
            _timer.Tick += (_, _) => InvalidateSurface();
        }
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _bgBitmap?.Dispose();
        _bgBitmap = null;
    }

    // ── Paint ────────────────────────────────────────────────────────────────

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;

        EnsureBackground(w, h);
        canvas.DrawBitmap(_bgBitmap, 0, 0);
        DrawSparks(canvas, w, h);
    }

    // Skapar (eller återskapar vid storleksändring) den statiska bakgrunden.
    private void EnsureBackground(float w, float h)
    {
        if (_bgBitmap != null && _bgW == w && _bgH == h)
            return;

        _bgBitmap?.Dispose();
        _bgBitmap = new SKBitmap((int)w, (int)h);
        _bgW = w;
        _bgH = h;

        using var c = new SKCanvas(_bgBitmap);
        var bounds = new SKRect(0, 0, w, h);
        c.Clear(s_base);

        using (var shader = SKShader.CreateRadialGradient(
                   new SKPoint(w * 0.5f, h * 0.17f), w * 0.65f,
                   [s_glow1Inner, SKColors.Transparent], [0f, 1f],
                   SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
            c.DrawRect(bounds, paint);

        using (var shader = SKShader.CreateRadialGradient(
                   new SKPoint(w * 0.5f, h * 0.12f), w * 0.3f,
                   [s_glow2Inner, SKColors.Transparent], [0f, 1f],
                   SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
            c.DrawRect(bounds, paint);

        using (var shader = SKShader.CreateRadialGradient(
                   new SKPoint(w * 0.5f, h * 0.45f), MathF.Max(w, h) * 0.75f,
                   [SKColors.Transparent, s_vigOuter], [0.45f, 1f],
                   SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
            c.DrawRect(bounds, paint);
    }

    // Ritar bara partiklarna (den enda delen som ändras varje frame).
    private void DrawSparks(SKCanvas canvas, float w, float h)
    {
        var elapsed = (float)(DateTime.Now - _start).TotalSeconds;

        foreach (var s in _sparks)
        {
            float t = ((elapsed / s.Duration) + s.Phase) % 1f;

            float x = s.Xrel * w + s.DriftRel * w * t;
            float y = s.StartYrel * h - s.TravelYrel * h * t;

            float opacity;
            if (t < 0.12f)      opacity = t / 0.12f;
            else if (t > 0.80f) opacity = (1f - t) / 0.20f;
            else                opacity = 1f;
            opacity = MathF.Min(opacity, 1f) * 0.95f;

            float sizeMul = t < 0.15f
                ? 0.5f + (t / 0.15f) * 0.5f
                : 1.0f - (t - 0.15f) * 0.65f;
            sizeMul = MathF.Max(0.3f, sizeMul);
            float size = s.SizePx * sizeMul;

            // Yttre halo: stor, orange, gaussian-glow via MaskFilter
            _sparkPaint.Color = s_outerColor.WithAlpha((byte)(opacity * 70));
            canvas.DrawCircle(x, y, size * 4.0f, _sparkPaint);

            // Ljus kärna: liten, varm gul
            _sparkPaint.Color = s_coreColor.WithAlpha((byte)(opacity * 230));
            canvas.DrawCircle(x, y, size, _sparkPaint);
        }
    }

    // ── Spark setup ──────────────────────────────────────────────────────────

    private static Spark[] CreateSparks()
    {
        var rng = new Random(7);
        var arr = new Spark[14];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new Spark
            {
                Xrel       = (float)((i + 0.5) / arr.Length + (rng.NextDouble() - 0.5) * 0.05),
                StartYrel  = 0.38f + (float)rng.NextDouble() * 0.06f,
                TravelYrel = 0.34f + (float)rng.NextDouble() * 0.08f,
                DriftRel   = (float)(rng.NextDouble() - 0.5) * 0.05f,
                SizePx     = (float)(rng.NextDouble() * 1.5 + 1.5),
                Duration   = (float)(rng.NextDouble() * 1.6 + 4.2),
                Phase      = (float)rng.NextDouble(),
            };
        }
        return arr;
    }
}
