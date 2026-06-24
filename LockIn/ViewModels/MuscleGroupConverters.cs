using LockIn.Models;
using LockIn.Resources.Strings;
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
                MuscleGroup.Chest     => AppResources.Library_Muscle_Chest,
                MuscleGroup.Back      => AppResources.Library_Muscle_Back,
                MuscleGroup.Shoulders => AppResources.Library_Muscle_Shoulders,
                MuscleGroup.Biceps    => AppResources.Library_Muscle_Biceps,
                MuscleGroup.Triceps   => AppResources.Library_Muscle_Triceps,
                MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
                MuscleGroup.Core      => AppResources.Library_Muscle_Core,
                MuscleGroup.FullBody  => AppResources.Library_Muscle_FullBody,
                MuscleGroup.Forearms  => AppResources.Library_Muscle_Forearms,
                _                     => AppResources.Library_Muscle_Other,
            };
        return AppResources.Library_Muscle_Other;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
