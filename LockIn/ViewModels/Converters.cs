using System.Globalization;

namespace LockIn.ViewModels;

public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

public class GreaterThanZeroConverter : IValueConverter
{
    public static readonly GreaterThanZeroConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            int i    => i > 0,
            double d => d > 0.0,
            _        => false,
        };
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class PRColorConverter : IValueConverter
{
    public static readonly PRColorConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? Color.FromArgb("#B8B8BC")
            : Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#F2F2F2")
                : Color.FromArgb("#1A1A1A");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class DotWidthConverter : IValueConverter
{
    public static readonly DotWidthConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 20.0 : 6.0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class DotColorConverter : IValueConverter
{
    public static readonly DotColorConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#B8B8BC") : Color.FromArgb("#222228");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class OnboardingButtonColorConverter : IValueConverter
{
    public static readonly OnboardingButtonColorConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Color.FromArgb("#B8B8BC") : Color.FromArgb("#2A2A32");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class CompletedOpacityConverter : IValueConverter
{
    public static readonly CompletedOpacityConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 0.52 : 1.0;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
