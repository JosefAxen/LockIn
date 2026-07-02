using LockIn.Models;

namespace LockIn.Data;

public sealed record TemplateDef(
    string Name,
    int DaysPerWeek,
    IReadOnlyList<TemplateDayDef> Days);

public sealed record TemplateDayDef(
    string Label,
    int DefaultDayOfWeek,
    IReadOnlyList<MuscleGroup> Muscles);

public static class TemplateLibrary
{
    public static IReadOnlyList<TemplateDef> All => new[]
    {
        new TemplateDef("PPL 6-dagar", 6, new[]
        {
            new TemplateDayDef("Push A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }),
            new TemplateDayDef("Pull A", 1, new[] { MuscleGroup.Back, MuscleGroup.Biceps, MuscleGroup.Traps }),
            new TemplateDayDef("Legs A", 2, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
            new TemplateDayDef("Push B", 3, new[] { MuscleGroup.Chest, MuscleGroup.Shoulders, MuscleGroup.Triceps }),
            new TemplateDayDef("Pull B", 4, new[] { MuscleGroup.Back, MuscleGroup.Biceps, MuscleGroup.Traps }),
            new TemplateDayDef("Legs B", 5, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
        }),
        new TemplateDef("Upper/Lower 4-dagar", 4, new[]
        {
            new TemplateDayDef("Upper A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Biceps, MuscleGroup.Triceps }),
            new TemplateDayDef("Lower A", 1, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
            new TemplateDayDef("Upper B", 3, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Triceps, MuscleGroup.Biceps }),
            new TemplateDayDef("Lower B", 4, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Abs }),
        }),
        new TemplateDef("Full Body 3-dagar", 3, new[]
        {
            new TemplateDayDef("Full Body A", 0, new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders, MuscleGroup.Quads, MuscleGroup.Triceps }),
            new TemplateDayDef("Full Body B", 2, new[] { MuscleGroup.Back, MuscleGroup.Chest, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Biceps }),
            new TemplateDayDef("Full Body C", 4, new[] { MuscleGroup.Shoulders, MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Calves, MuscleGroup.Abs }),
        }),
        new TemplateDef("Bro Split 5-dagar", 5, new[]
        {
            new TemplateDayDef("Chest", 0, new[] { MuscleGroup.Chest, MuscleGroup.Triceps }),
            new TemplateDayDef("Back", 1, new[] { MuscleGroup.Back, MuscleGroup.Traps }),
            new TemplateDayDef("Shoulders", 2, new[] { MuscleGroup.Shoulders, MuscleGroup.Abs }),
            new TemplateDayDef("Legs", 3, new[] { MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Calves }),
            new TemplateDayDef("Arms", 4, new[] { MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Forearms }),
        }),
    };
}
