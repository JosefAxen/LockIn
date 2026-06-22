using System.Globalization;
using LockIn.Models;

namespace LockIn.Views;

public class EquipmentLabelConverter : IValueConverter
{
    public static readonly EquipmentLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EquipmentType eq) return "";
        return eq switch
        {
            EquipmentType.Barbell      => "SKIVSTÅNG",
            EquipmentType.Dumbbell     => "HANTEL",
            EquipmentType.Cable        => "KABEL",
            EquipmentType.Machine      => "MASKIN",
            EquipmentType.BodyOnly     => "KROPPSVIKT",
            EquipmentType.EZBar        => "EZ-STÅNG",
            EquipmentType.Kettlebell   => "KETTLEBELL",
            EquipmentType.Bands        => "BAND",
            EquipmentType.FoamRoll     => "FOAM ROLL",
            EquipmentType.MedicineBall => "MEDICINBOLL",
            _                          => ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
