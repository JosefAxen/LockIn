namespace LockIn.Views;

internal static class AnimationHelper
{
    internal static async Task PressAsync(object? sender)
    {
        if (sender is TapGestureRecognizer tap && tap.Parent is VisualElement view)
        {
            await view.ScaleTo(0.96, 80, Easing.CubicOut);
            await view.ScaleTo(1.0, 200, Easing.SpringOut);
        }
    }
}
