using System.Globalization;

namespace LockIn.Views;

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is false;
}
