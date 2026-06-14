using LockIn.Models;
using System.Globalization;

namespace LockIn.ViewModels;

public class MuscleGroupColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MuscleGroup mg)
            return mg switch
            {
                MuscleGroup.Chest     => Color.FromArgb("#FB7185"),
                MuscleGroup.Back      => Color.FromArgb("#38BDF8"),
                MuscleGroup.Shoulders => Color.FromArgb("#A78BFA"),
                MuscleGroup.Biceps    => Color.FromArgb("#4ADE80"),
                MuscleGroup.Triceps   => Color.FromArgb("#FBBF24"),
                MuscleGroup.Legs      => Color.FromArgb("#F97316"),
                MuscleGroup.Core      => Color.FromArgb("#EC4899"),
                MuscleGroup.FullBody  => Color.FromArgb("#EF4444"),
                _                     => Color.FromArgb("#52525E"),
            };
        return Color.FromArgb("#52525E");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class MuscleGroupLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MuscleGroup mg)
            return mg switch
            {
                MuscleGroup.Chest     => "Bröst",
                MuscleGroup.Back      => "Rygg",
                MuscleGroup.Shoulders => "Axlar",
                MuscleGroup.Biceps    => "Biceps",
                MuscleGroup.Triceps   => "Triceps",
                MuscleGroup.Legs      => "Ben",
                MuscleGroup.Core      => "Core",
                MuscleGroup.FullBody  => "Helkropp",
                _                     => "Övrigt",
            };
        return "Övrigt";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
