namespace LockIn.Views;

/// <summary>
/// Marker subclass of Image. AppIconHandler tints with DesignTokens.IconTint
/// (dark: #C8C8D2, light: #3A3A44). Set ForceTint to override — e.g. White
/// for icons on coloured (green) button backgrounds.
/// </summary>
public class AppIcon : Image
{
    public static readonly BindableProperty ForceTintProperty =
        BindableProperty.Create(nameof(ForceTint), typeof(Color), typeof(AppIcon), null);

    public Color? ForceTint
    {
        get => (Color?)GetValue(ForceTintProperty);
        set => SetValue(ForceTintProperty, value);
    }
}
