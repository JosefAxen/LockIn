using LockIn.Resources.Strings;

namespace LockIn.Models;

public static class MuscleGroupExtensions
{
    public static string ToLocalizedLabel(this MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest      => AppResources.Muscle_Chest,
        MuscleGroup.Back       => AppResources.Muscle_Back,
        MuscleGroup.Shoulders  => AppResources.Muscle_Shoulders,
        MuscleGroup.Biceps     => AppResources.Muscle_Biceps,
        MuscleGroup.Triceps    => AppResources.Muscle_Triceps,
        MuscleGroup.Legs       => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core       => AppResources.Library_Muscle_Core,
        MuscleGroup.FullBody   => AppResources.Library_Muscle_FullBody,
        MuscleGroup.Forearms   => AppResources.Library_Muscle_Forearms,
        MuscleGroup.Quads      => AppResources.Muscle_Quads,
        MuscleGroup.Hamstrings => AppResources.Muscle_Hamstrings,
        MuscleGroup.Glutes     => AppResources.Muscle_Glutes,
        MuscleGroup.Calves     => AppResources.Muscle_Calves,
        MuscleGroup.Traps      => AppResources.Muscle_Traps,
        MuscleGroup.Abs        => AppResources.Muscle_Abs,
        _                      => AppResources.Library_Muscle_Other,
    };
}
