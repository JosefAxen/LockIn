using Microsoft.Maui.Devices;

namespace LockIn.Views;

public class AnimatedButtonBehavior : Behavior<Button>
{
    protected override void OnAttachedTo(Button button)
    {
        base.OnAttachedTo(button);
        button.Pressed  += OnPressed;
        button.Released += OnReleased;
    }

    protected override void OnDetachingFrom(Button button)
    {
        button.Pressed  -= OnPressed;
        button.Released -= OnReleased;
        base.OnDetachingFrom(button);
    }

    private static void OnPressed(object? sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is Button btn)
            _ = btn.ScaleTo(0.94, 65, Easing.CubicOut);
    }

    private static void OnReleased(object? sender, EventArgs e)
    {
        if (sender is Button btn)
            _ = btn.ScaleTo(1.0, 230, Easing.SpringOut);
    }
}
