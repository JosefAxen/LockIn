namespace LockIn.Data;

public record ProgramExercise(string ExerciseName, int Sets, int Reps, int RestSeconds);
public record ProgramDay(string Label, List<ProgramExercise> Exercises);
public record WorkoutProgram(string Id, string Name, string Description, int DaysPerWeek, List<ProgramDay> Days);

public static class WorkoutPrograms
{
    public static readonly IReadOnlyList<WorkoutProgram> All = [
        new("ppl", "PPL — Push Pull Legs",
            "Klassiskt 6-dagarsprogram. Tre muskelgrupper roteras för maximal frekvens och volym.",
            6,
            [
                new("Push A", [
                    new("Bänkpress", 4, 8, 120),
                    new("Lutande bänkpress", 3, 10, 90),
                    new("Militärpress", 3, 10, 90),
                    new("Sidolyft", 3, 15, 60),
                    new("Tricepspressdown", 3, 12, 60),
                ]),
                new("Pull A", [
                    new("Marklyft", 3, 5, 180),
                    new("Skivstångsrodd", 4, 8, 120),
                    new("Chins", 3, 8, 120),
                    new("Facepull", 3, 15, 60),
                    new("Hammarcurl", 3, 12, 60),
                ]),
                new("Legs A", [
                    new("Knäböj", 4, 8, 180),
                    new("Benpress", 3, 12, 120),
                    new("Rumänska marklyft", 3, 10, 120),
                    new("Benböjning", 3, 12, 90),
                    new("Stående vadpress", 4, 15, 60),
                ]),
                new("Push B", [
                    new("Bänkpress", 3, 5, 180),
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Militärpress", 3, 8, 120),
                    new("Kabelkorsning", 3, 15, 60),
                    new("Skullcrusher", 3, 12, 60),
                ]),
                new("Pull B", [
                    new("Chins", 4, 6, 180),
                    new("Sittande rodd", 4, 10, 90),
                    new("Skivstångsrodd", 3, 10, 90),
                    new("Facepull", 3, 15, 60),
                    new("Skivstångscurl", 3, 12, 60),
                ]),
                new("Legs B", [
                    new("Knäböj", 4, 8, 180),
                    new("Utfall", 3, 10, 90),
                    new("Benböjning", 3, 12, 90),
                    new("Bensträckning", 3, 15, 90),
                    new("Stående vadpress", 4, 15, 60),
                ]),
            ]),

        new("upperlower", "Upper/Lower",
            "Effektivt 4-dagarsprogram som delar kroppen i övre och undre halvan.",
            4,
            [
                new("Upper A — Styrka", [
                    new("Bänkpress", 4, 5, 180),
                    new("Skivstångsrodd", 4, 5, 180),
                    new("Militärpress", 3, 8, 120),
                    new("Chins", 3, 8, 120),
                    new("Skullcrusher", 2, 12, 60),
                    new("Skivstångscurl", 2, 12, 60),
                ]),
                new("Lower A — Styrka", [
                    new("Knäböj", 4, 5, 180),
                    new("Rumänska marklyft", 3, 8, 120),
                    new("Benpress", 3, 10, 90),
                    new("Stående vadpress", 3, 15, 60),
                ]),
                new("Upper B — Hypertrofi", [
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Sittande rodd", 4, 10, 90),
                    new("Militärpress", 3, 12, 90),
                    new("Latsdrag", 3, 12, 90),
                    new("Tricepspressdown", 3, 15, 60),
                    new("Hammarcurl", 3, 15, 60),
                ]),
                new("Lower B — Hypertrofi", [
                    new("Knäböj", 3, 8, 120),
                    new("Benböjning", 4, 12, 90),
                    new("Utfall", 3, 10, 90),
                    new("Bensträckning", 3, 15, 90),
                    new("Stående vadpress", 4, 20, 60),
                ]),
            ]),

        new("fullbody", "Full Body 3×",
            "Träna hela kroppen tre gånger i veckan. Bra för nybörjare och de med lite träningstid.",
            3,
            [
                new("Måndag", [
                    new("Knäböj", 3, 8, 120),
                    new("Bänkpress", 3, 8, 120),
                    new("Skivstångsrodd", 3, 8, 120),
                    new("Militärpress", 2, 10, 90),
                    new("Skivstångscurl", 2, 12, 60),
                    new("Skullcrusher", 2, 12, 60),
                ]),
                new("Onsdag", [
                    new("Marklyft", 3, 5, 180),
                    new("Lutande bänkpress", 3, 10, 90),
                    new("Chins", 3, 8, 120),
                    new("Sidolyft", 3, 15, 60),
                    new("Hammarcurl", 2, 12, 60),
                    new("Tricepspressdown", 2, 12, 60),
                ]),
                new("Fredag", [
                    new("Knäböj", 3, 6, 150),
                    new("Bänkpress", 3, 6, 150),
                    new("Sittande rodd", 3, 10, 90),
                    new("Militärpress", 3, 8, 120),
                    new("Facepull", 3, 15, 60),
                    new("Stående vadpress", 3, 20, 60),
                ]),
            ]),

        new("startingstrength", "Starting Strength",
            "Mark Rippetoes klassiska nybörjarprogram. Fokus på de 5 grundlyften med linjär progression.",
            3,
            [
                new("Pass A", [
                    new("Knäböj", 3, 5, 180),
                    new("Bänkpress", 3, 5, 180),
                    new("Marklyft", 1, 5, 240),
                ]),
                new("Pass B", [
                    new("Knäböj", 3, 5, 180),
                    new("Militärpress", 3, 5, 180),
                    new("Marklyft", 1, 5, 240),
                ]),
            ]),
    ];
}
