using System.Globalization;
namespace LockIn.Views;
public class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s.Length > 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
