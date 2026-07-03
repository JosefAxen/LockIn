using LockIn.Models;
using LockIn.Resources.Strings;
using System.Globalization;

namespace LockIn.ViewModels;

public class MuscleGroupColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MuscleGroup mg ? DesignTokens.MuscleColor(mg) : DesignTokens.Accent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class MuscleGroupLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MuscleGroup mg ? mg.ToLocalizedLabel() : AppResources.Library_Muscle_Other;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
