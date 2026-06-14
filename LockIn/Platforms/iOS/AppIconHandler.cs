using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace LockIn.Platforms.iOS;

public class AppIconHandler : ImageHandler
{
    protected override UIImageView CreatePlatformView()
    {
        var view = base.CreatePlatformView();
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        return view;
    }

    protected override void DisconnectHandler(UIImageView platformView)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        base.DisconnectHandler(platformView);
    }

    // PlatformArrange fires after layout — image is guaranteed to be set by then.
    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        ApplyTint();
    }

    private void ApplyTint()
    {
        if (PlatformView?.Image is null) return;
        if (PlatformView.Image.RenderingMode != UIImageRenderingMode.AlwaysTemplate)
            PlatformView.Image = PlatformView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
        var forceTint = (VirtualView as LockIn.Views.AppIcon)?.ForceTint;
        PlatformView.TintColor = (forceTint ?? DesignTokens.IconTint).ToPlatform();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(ApplyTint);
}
