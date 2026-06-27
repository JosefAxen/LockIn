using LockIn.Models;
using LockIn.Resources.Strings;
using System.Globalization;

namespace LockIn.Views;

public class CardioActivityLabelConverter : IValueConverter
{
    public static readonly CardioActivityLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CardioActivityType type)
            return AppResources.Get("Cardio_Activity_" + type.ToString());
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
