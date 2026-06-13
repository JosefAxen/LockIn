using System.Globalization;

namespace LockIn.Views;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool completed = value is true;
        return completed
            ? Color.FromArgb("#4ADE80")
            : Color.FromArgb("#222224");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
