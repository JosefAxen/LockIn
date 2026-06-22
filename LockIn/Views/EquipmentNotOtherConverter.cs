using System.Globalization;
using LockIn.Models;

namespace LockIn.Views;

public class EquipmentNotOtherConverter : IValueConverter
{
    public static readonly EquipmentNotOtherConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is EquipmentType eq && eq != EquipmentType.Other;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
