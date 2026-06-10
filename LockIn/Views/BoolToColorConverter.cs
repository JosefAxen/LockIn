using System.Globalization;

namespace LockIn.Views;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool completed = value is true;
        return completed
            ? Color.FromArgb("#FF5A1F")
            : Color.FromArgb("#222224");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
