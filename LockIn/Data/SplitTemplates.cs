using LockIn.Models;

namespace LockIn.Data;

public sealed record SplitDayDef(string Label, int DefaultDayOfWeek, IReadOnlyList<MuscleGroup> Muscles);

public static class SplitTemplates
{
    public static IReadOnlyList<SplitDayDef> GetSplit(int daysPerWeek) => daysPerWeek switch
    {
        2 => new[]
        {
            new SplitDayDef("Full Body A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Abs }),
            new SplitDayDef("Full Body B", 3, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Back, MuscleGroup.Chest }),
        },
        3 => new[]
        {
            new SplitDayDef("Full Body A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Triceps, MuscleGroup.Biceps }),
            new SplitDayDef("Full Body B", 2, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
            new SplitDayDef("Full Body C", 4, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps }),
        },
        4 => new[]
        {
            new SplitDayDef("Upper A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps }),
            new SplitDayDef("Lower A", 1, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
            new SplitDayDef("Upper B", 3, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Triceps, MuscleGroup.Biceps }),
            new SplitDayDef("Lower B", 4, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
        },
        5 => new[]
        {
            new SplitDayDef("Push", 0, new[] { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }),
            new SplitDayDef("Pull", 1, new[] { MuscleGroup.Back, MuscleGroup.Biceps, MuscleGroup.Traps }),
            new SplitDayDef("Legs", 2, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
            new SplitDayDef("Upper", 4, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps }),
            new SplitDayDef("Lower", 5, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
        },
        6 => new[]
        {
            new SplitDayDef("Push A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }),
            new SplitDayDef("Pull A", 1, new[] { MuscleGroup.Back, MuscleGroup.Biceps, MuscleGroup.Traps }),
            new SplitDayDef("Legs A", 2, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
            new SplitDayDef("Push B", 3, new[] { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }),
            new SplitDayDef("Pull B", 4, new[] { MuscleGroup.Back, MuscleGroup.Biceps, MuscleGroup.Traps }),
            new SplitDayDef("Legs B", 5, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(daysPerWeek), "Måste vara 2-6"),
    };
}
