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

    // ── Cached static colors (via DesignTokens) ─────────────────────────────

    private static readonly SKColor s_base       = DesignTokens.SK_Background;
    private static readonly SKColor s_glow1Inner = DesignTokens.SK_AccentGlow1;
    private static readonly SKColor s_glow2Inner = DesignTokens.SK_AccentGlow2;
    private static readonly SKColor s_vigOuter   = DesignTokens.SK_Vignette;
    private static readonly SKColor s_coreColor  = DesignTokens.SK_SparkCore;
    private static readonly SKColor s_midColor   = DesignTokens.SK_SparkMid;
    private static readonly SKColor s_outerColor = DesignTokens.SK_SparkOuter;

    // ── Pre-baked static background (gradients + vignette) ──────────────────
    // Skapas en gång när storleken är känd och återanvänds under pause/resume.

    private SKBitmap? _bgBitmap;
    private float _bgW;
    private float _bgH;

    // ── Cached paint objects (create once, reuse every frame) ────────────────
    // Tre separata paint-objekt med fasta MaskFilter — undviker native
    // state-ogiltigförklaring som annars sker 28 gånger per frame.

    private static readonly SKMaskFilter s_glowBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 5f);
    private static readonly SKMaskFilter s_haloBlur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14f);
    private readonly SKPaint _haloP = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus, MaskFilter = s_haloBlur };
    private readonly SKPaint _midP  = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus, MaskFilter = s_glowBlur };
    private readonly SKPaint _coreP = new() { IsAntialias = true, BlendMode = SKBlendMode.Plus };

    // ── Animation state ──────────────────────────────────────────────────────

    private IDispatcherTimer? _timer;
    private readonly DateTime _start = DateTime.Now;
    private bool _reduceMotion;

    // ── Page lifecycle tracking ───────────────────────────────────────────────
    // Prenumererar på föräldrasidans Appearing/Disappearing för att pausa timern
    // vid tabväxling — annars körs alla 5 tab-bakgrunder simultant.

    private Page? _parentPage;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public AtmosphericBackgroundView()
    {
        EnableTouchEvents = false;
        Loaded   += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        _reduceMotion = UIKit.UIAccessibility.IsReduceMotionEnabled;
#endif
        if (_reduceMotion)
        {
            InvalidateSurface();
            return;
        }

        _parentPage = FindParentPage();
        if (_parentPage is not null)
        {
            _parentPage.Appearing    += OnPageAppearing;
            _parentPage.Disappearing += OnPageDisappearing;
        }
        // Timer startas via OnPageAppearing (Shell anropar Appearing
        // strax efter Loaded för den aktiva sidan, och vid varje tabväxling).
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        if (_parentPage is not null)
        {
            _parentPage.Appearing    -= OnPageAppearing;
            _parentPage.Disappearing -= OnPageDisappearing;
            _parentPage = null;
        }
        DisposeResources();
    }

    private void OnPageAppearing(object sender, EventArgs e)
    {
        if (_timer is null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33); // 30 fps
            _timer.Tick += (_, _) => InvalidateSurface();
        }
        _timer.Start();
    }

    private void OnPageDisappearing(object sender, EventArgs e) => _timer?.Stop();

    private void DisposeResources()
    {
        _timer?.Stop();
        _bgBitmap?.Dispose();
        _bgBitmap = null;
    }

    private Page? FindParentPage()
    {
        Element? el = this;
        while (el is not null)
        {
            el = el.Parent;
            if (el is Page p) return p;
        }
        return null;
    }

    // ── Paint ────────────────────────────────────────────────────────────────

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        float w = e.Info.Width;
        float h = e.Info.Height;

        EnsureBackground(w, h);
        canvas.DrawBitmap(_bgBitmap, 0, 0);
        if (!_reduceMotion)
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

        // Mjuk diffus bloom — 5 överlappande gradienter med fyra stopp vardera
        // för att undvika synliga ringar i ljuset.
        DrawSoftGlow(c, bounds, w * 0.50f, h * 0.10f, w * 0.80f, s_glow1Inner, 0.11f);
        DrawSoftGlow(c, bounds, w * 0.42f, h * 0.06f, w * 0.60f, s_glow1Inner, 0.08f);
        DrawSoftGlow(c, bounds, w * 0.58f, h * 0.06f, w * 0.60f, s_glow1Inner, 0.08f);
        DrawSoftGlow(c, bounds, w * 0.50f, h * 0.02f, w * 0.45f, s_glow2Inner, 0.10f);
        DrawSoftGlow(c, bounds, w * 0.50f, 0f,        w * 0.30f, s_glow2Inner, 0.08f);

        using (var shader = SKShader.CreateRadialGradient(
                   new SKPoint(w * 0.5f, h * 0.45f), MathF.Max(w, h) * 0.75f,
                   [SKColors.Transparent, SKColors.Transparent, s_vigOuter],
                   [0f, 0.6f, 1f],
                   SKShaderTileMode.Clamp))
        using (var paint = new SKPaint { Shader = shader })
            c.DrawRect(bounds, paint);
    }

    private static void DrawSoftGlow(SKCanvas c, SKRect bounds, float cx, float cy, float radius, SKColor color, float peakAlpha)
    {
        var colors = new[]
        {
            color.WithAlpha((byte)(peakAlpha * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.72f * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.38f * 255)),
            color.WithAlpha((byte)(peakAlpha * 0.12f * 255)),
            SKColors.Transparent,
        };
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy), radius, colors,
            [0f, 0.25f, 0.5f, 0.75f, 1f],
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
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
            if (t < 0.15f)      opacity = t / 0.15f;
            else if (t > 0.75f) opacity = (1f - t) / 0.25f;
            else                opacity = 1f;
            opacity = MathF.Min(opacity, 1f) * 0.90f;

            float sizeMul = t < 0.20f
                ? 0.4f + (t / 0.20f) * 0.6f
                : 1.0f - (t - 0.20f) * 0.50f;
            sizeMul = MathF.Max(0.4f, sizeMul);
            float size = s.SizePx * sizeMul;

            _haloP.Color = s_outerColor.WithAlpha((byte)(opacity * 90));
            canvas.DrawCircle(x, y, size * 5.0f, _haloP);

            _midP.Color = s_midColor.WithAlpha((byte)(opacity * 180));
            canvas.DrawCircle(x, y, size * 2.2f, _midP);

            _coreP.Color = s_coreColor.WithAlpha((byte)(opacity * 255));
            canvas.DrawCircle(x, y, size * 0.9f, _coreP);
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
                StartYrel  = 0.55f + (float)rng.NextDouble() * 0.08f,
                TravelYrel = 0.58f + (float)rng.NextDouble() * 0.08f,
                DriftRel   = (float)(rng.NextDouble() - 0.5) * 0.025f,
                SizePx     = (float)(rng.NextDouble() * 2.2 + 2.0),
                Duration   = (float)(rng.NextDouble() * 4.0 + 11.0),
                Phase      = (float)rng.NextDouble(),
            };
        }
        return arr;
    }
}
