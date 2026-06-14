using Microsoft.Maui.Devices;

namespace LockIn.Views;

internal static class AnimationHelper
{
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
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        await view.ScaleTo(0.93, 65, Easing.CubicOut);
        await view.ScaleTo(1.0, 230, Easing.SpringOut);
    }
}
