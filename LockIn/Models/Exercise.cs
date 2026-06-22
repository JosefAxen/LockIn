using SQLite;

namespace LockIn.Models;

public enum MuscleGroup
{
    Chest=0, Back=1, Shoulders=2, Biceps=3, Triceps=4,
    Legs=5, Core=6, FullBody=7, Other=8, Forearms=9
}

public enum EquipmentType
{
    Other=0, Barbell=1, Dumbbell=2, Cable=3, Machine=4,
    BodyOnly=5, EZBar=6, Kettlebell=7, Bands=8,
    FoamRoll=9, MedicineBall=10
}

public enum ExerciseForce
{
    Other=0, Push=1, Pull=2, Static=3
}

public enum ExerciseLevel
{
    Beginner=0, Intermediate=1, Expert=2
}

public enum ExerciseMechanic
{
    Other=0, Compound=1, Isolation=2
}

[Table("Exercises")]
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public int DefaultRestSeconds { get; set; } = 120;

    public MuscleGroup MuscleGroup { get; set; }

    public string Notes { get; set; } = "";

    public string Description { get; set; } = "";

    public EquipmentType Equipment { get; set; }

    public string SecondaryMuscles { get; set; } = "";

    public ExerciseForce Force { get; set; }

    public ExerciseLevel Level { get; set; }

    public ExerciseMechanic Mechanic { get; set; }
}
