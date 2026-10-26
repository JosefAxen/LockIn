using Microsoft.Maui.Devices;

namespace LockIn.Views;

internal static class AnimationHelper
{
    /// <summary>iOS-inställningen "Minska rörelse". Läses live så ändringar slår igenom direkt.</summary>
    internal static bool ReduceMotion
    {
        get
        {
#if IOS
            return UIKit.UIAccessibility.IsReduceMotionEnabled;
#else
            return false;
#endif
        }
    }

    /// <summary>Haptik som respekterar användarens inställning (Inställningar → Haptik).</summary>
    internal static void Haptic(HapticFeedbackType type = HapticFeedbackType.Click)
    {
        if (Preferences.Default.Get("haptic_enabled", true))
            HapticFeedback.Default.Perform(type);
    }

    internal static async Task PressAsync(object? sender)
    {
        VisualElement? view = sender switch
        {
            TapGestureRecognizer tap when tap.Parent is VisualElement v => v,
            VisualElement ve => ve,
            _ => null
        };
        if (view is null) return;
        await PressAsync(view);
    }

    internal static async Task PressAsync(VisualElement view)
    {
        Haptic();
        if (ReduceMotion) return;
        await view.ScaleTo(0.93, 65, Easing.CubicOut);
        await view.ScaleTo(1.0, 230, Easing.SpringOut);
    }

    internal static async Task PageEntryAsync(ContentPage page)
    {
        if (page.Content is null) return;
        if (ReduceMotion)
        {
            page.Content.Opacity = 1;
            page.Content.TranslationY = 0;
            return;
        }
        page.Content.Opacity = 0;
        page.Content.TranslationY = 14;
        await Task.WhenAll(
            page.Content.FadeTo(1, 280, Easing.CubicOut),
            page.Content.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }
}
