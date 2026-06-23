using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace LockIn.Controls;

/// <summary>
/// Pulsande coral-dot med gaussian glow för "PASS PÅGÅR"-bannern.
/// Ersätter Grid+Ellipse-stacken som gav hårda kanter.
/// </summary>
public class ForgeLiveDotView : SKCanvasView
{
    private static readonly SKColor s_coral = SKColor.Parse("#FB7185");
    private static readonly SKMaskFilter s_blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f);
    private readonly SKPaint _paint = new() { IsAntialias = true, MaskFilter = s_blur };

    private IDispatcherTimer? _timer;
    private DateTime _pulseStart;
    private bool _isPulsing;

    private const float PulseDuration = 1.4f; // sekunder per cykel

    public ForgeLiveDotView()
    {
        WidthRequest  = 40;
        HeightRequest = 40;
        EnableTouchEvents = false;
        Unloaded += (_, _) => StopPulse();
    }

    public void StartPulse()
    {
        if (_isPulsing) return;
        _isPulsing = true;
        _pulseStart = DateTime.Now;

        if (_timer == null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33); // 30fps
            _timer.Tick += (_, _) => InvalidateSurface();
        }
        _timer.Start();
    }

    public void StopPulse()
    {
        _isPulsing = false;
        _timer?.Stop();
        InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        float cx    = e.Info.Width  * 0.5f;
        float cy    = e.Info.Height * 0.5f;
        float coreR = e.Info.Width  * 0.12f; // ~5px kärna på 40pt-canvas

        if (_isPulsing)
        {
            float elapsed = (float)(DateTime.Now - _pulseStart).TotalSeconds;
            float t = elapsed % PulseDuration / PulseDuration; // 0..1

            // CubicOut-easing: snabb expansion, avtar mot slutet
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);

            float ringR    = e.Info.Width * (0.18f + eased * 0.26f); // 0.18→0.44
            float ringAlpha = 0.65f * (1f - t);

            _paint.Color = s_coral.WithAlpha((byte)(ringAlpha * 255));
            canvas.DrawCircle(cx, cy, ringR, _paint);
        }

        // Statisk glowande kärna — alltid synlig när vyn renderas
        _paint.Color = s_coral.WithAlpha(230);
        canvas.DrawCircle(cx, cy, coreR, _paint);
    }
}
