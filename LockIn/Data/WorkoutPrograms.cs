using LockIn.Resources.Strings;

namespace LockIn.Data;

public record ProgramExercise(string ExerciseName, int Sets, int Reps, int RestSeconds)
{
    public string RestLabel => string.Format(AppResources.ProgramDetail_RestSeconds_Format, RestSeconds);
}
public record ProgramDay(string Label, List<ProgramExercise> Exercises)
{
    public string LabelKey      { get; init; } = string.Empty;
    public string LocalizedLabel => string.IsNullOrEmpty(LabelKey) ? Label : AppResources.Get(LabelKey);
}
public record WorkoutProgram(string Id, string Name, string Description, int DaysPerWeek, List<ProgramDay> Days)
{
    public string DaysPerWeekText      => string.Format(AppResources.Library_DaysPerWeek_Format, DaysPerWeek);
    public string LocalizedDescription => AppResources.Get($"Program_{Id}_Description");
}

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
                ]) { LabelKey = "Program_ppl_Day1_Label" },
                new("Pull A", [
                    new("Marklyft", 3, 5, 180),
                    new("Skivstångsrodd", 4, 8, 120),
                    new("Chins", 3, 8, 120),
                    new("Facepull", 3, 15, 60),
                    new("Hammarcurl", 3, 12, 60),
                ]) { LabelKey = "Program_ppl_Day2_Label" },
                new("Legs A", [
                    new("Knäböj", 4, 8, 180),
                    new("Benpress", 3, 12, 120),
                    new("Rumänska marklyft", 3, 10, 120),
                    new("Benböjning", 3, 12, 90),
                    new("Stående vadpress", 4, 15, 60),
                ]) { LabelKey = "Program_ppl_Day3_Label" },
                new("Push B", [
                    new("Bänkpress", 3, 5, 180),
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Militärpress", 3, 8, 120),
                    new("Kabelkorsning", 3, 15, 60),
                    new("Skullcrusher", 3, 12, 60),
                ]) { LabelKey = "Program_ppl_Day4_Label" },
                new("Pull B", [
                    new("Chins", 4, 6, 180),
                    new("Sittande rodd", 4, 10, 90),
                    new("Skivstångsrodd", 3, 10, 90),
                    new("Facepull", 3, 15, 60),
                    new("Skivstångscurl", 3, 12, 60),
                ]) { LabelKey = "Program_ppl_Day5_Label" },
                new("Legs B", [
                    new("Knäböj", 4, 8, 180),
                    new("Utfall", 3, 10, 90),
                    new("Benböjning", 3, 12, 90),
                    new("Bensträckning", 3, 15, 90),
                    new("Stående vadpress", 4, 15, 60),
                ]) { LabelKey = "Program_ppl_Day6_Label" },
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
                ]) { LabelKey = "Program_upperlower_Day1_Label" },
                new("Lower A — Styrka", [
                    new("Knäböj", 4, 5, 180),
                    new("Rumänska marklyft", 3, 8, 120),
                    new("Benpress", 3, 10, 90),
                    new("Stående vadpress", 3, 15, 60),
                ]) { LabelKey = "Program_upperlower_Day2_Label" },
                new("Upper B — Hypertrofi", [
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Sittande rodd", 4, 10, 90),
                    new("Militärpress", 3, 12, 90),
                    new("Latsdrag", 3, 12, 90),
                    new("Tricepspressdown", 3, 15, 60),
                    new("Hammarcurl", 3, 15, 60),
                ]) { LabelKey = "Program_upperlower_Day3_Label" },
                new("Lower B — Hypertrofi", [
                    new("Knäböj", 3, 8, 120),
                    new("Benböjning", 4, 12, 90),
                    new("Utfall", 3, 10, 90),
                    new("Bensträckning", 3, 15, 90),
                    new("Stående vadpress", 4, 20, 60),
                ]) { LabelKey = "Program_upperlower_Day4_Label" },
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
                ]) { LabelKey = "Program_fullbody_Day1_Label" },
                new("Onsdag", [
                    new("Marklyft", 3, 5, 180),
                    new("Lutande bänkpress", 3, 10, 90),
                    new("Chins", 3, 8, 120),
                    new("Sidolyft", 3, 15, 60),
                    new("Hammarcurl", 2, 12, 60),
                    new("Tricepspressdown", 2, 12, 60),
                ]) { LabelKey = "Program_fullbody_Day2_Label" },
                new("Fredag", [
                    new("Knäböj", 3, 6, 150),
                    new("Bänkpress", 3, 6, 150),
                    new("Sittande rodd", 3, 10, 90),
                    new("Militärpress", 3, 8, 120),
                    new("Facepull", 3, 15, 60),
                    new("Stående vadpress", 3, 20, 60),
                ]) { LabelKey = "Program_fullbody_Day3_Label" },
            ]),

        new("startingstrength", "Starting Strength",
            "Mark Rippetoes klassiska nybörjarprogram. Fokus på de 5 grundlyften med linjär progression.",
            3,
            [
                new("Pass A", [
                    new("Knäböj", 3, 5, 180),
                    new("Bänkpress", 3, 5, 180),
                    new("Marklyft", 1, 5, 240),
                ]) { LabelKey = "Program_startingstrength_Day1_Label" },
                new("Pass B", [
                    new("Knäböj", 3, 5, 180),
                    new("Militärpress", 3, 5, 180),
                    new("Marklyft", 1, 5, 240),
                ]) { LabelKey = "Program_startingstrength_Day2_Label" },
            ]),

        new("texasmethod", "Texas Method",
            "Klassiskt mellannivåprogram med volymdag, återhämtningsdag och intensitetsdag. Linjär progression vecka för vecka.",
            3,
            [
                new("Måndag — Volym", [
                    new("Knäböj", 5, 5, 180),
                    new("Bänkpress", 5, 5, 150),
                    new("Skivstångsrodd", 4, 5, 120),
                    new("Facepull", 3, 15, 60),
                ]) { LabelKey = "Program_texasmethod_Day1_Label" },
                new("Onsdag — Återhämtning", [
                    new("Knäböj", 2, 5, 150),
                    new("Militärpress", 3, 5, 150),
                    new("Marklyft", 1, 5, 240),
                    new("Chins", 3, 8, 90),
                ]) { LabelKey = "Program_texasmethod_Day2_Label" },
                new("Fredag — Intensitet", [
                    new("Knäböj", 1, 5, 240),
                    new("Bänkpress", 1, 5, 240),
                    new("Marklyft", 1, 3, 300),
                    new("Skivstångsrodd", 3, 5, 120),
                ]) { LabelKey = "Program_texasmethod_Day3_Label" },
            ]),

        new("531bbb", "5/3/1 BBB",
            "Jim Wendlers 5/3/1 med Boring But Big-tillägget. Bygg styrka med de tunga seten och massa med 5×10-volymarbetet.",
            3,
            [
                new("Pass A — Knäböj & Bänkpress", [
                    new("Knäböj", 3, 5, 240),
                    new("Knäböj", 5, 10, 120),
                    new("Bänkpress", 3, 5, 180),
                    new("Bänkpress", 5, 10, 90),
                    new("Chins", 5, 10, 60),
                ]) { LabelKey = "Program_531bbb_Day1_Label" },
                new("Pass B — Marklyft & Militärpress", [
                    new("Marklyft", 3, 5, 300),
                    new("Marklyft", 5, 10, 180),
                    new("Militärpress", 3, 5, 180),
                    new("Militärpress", 5, 10, 90),
                    new("Skivstångsrodd", 5, 10, 60),
                ]) { LabelKey = "Program_531bbb_Day2_Label" },
                new("Pass C — Styrka & Accessoarer", [
                    new("Knäböj", 1, 1, 300),
                    new("Bänkpress", 1, 1, 300),
                    new("Marklyft", 1, 1, 300),
                    new("Tricepspressdown", 4, 15, 60),
                    new("Hammarcurl", 4, 15, 60),
                ]) { LabelKey = "Program_531bbb_Day3_Label" },
            ]),

        new("phul", "PHUL — Power Hypertrophy Upper Lower",
            "4-dagars split med två styrkefokuserade och två hypertrofifokuserade pass. Effektiv kombination för styrka och muskeltillväxt.",
            4,
            [
                new("Upper — Styrka", [
                    new("Bänkpress", 3, 3, 180),
                    new("Skivstångsrodd", 3, 3, 180),
                    new("Militärpress", 3, 5, 150),
                    new("Chins", 3, 6, 150),
                    new("Skivstångscurl", 2, 8, 90),
                    new("Skullcrusher", 2, 8, 90),
                ]) { LabelKey = "Program_phul_Day1_Label" },
                new("Lower — Styrka", [
                    new("Knäböj", 4, 3, 240),
                    new("Marklyft", 3, 3, 240),
                    new("Rumänska marklyft", 3, 8, 120),
                    new("Benpress", 3, 10, 90),
                    new("Stående vadpress", 4, 10, 60),
                ]) { LabelKey = "Program_phul_Day2_Label" },
                new("Upper — Hypertrofi", [
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Sittande rodd", 4, 10, 90),
                    new("Kabelkorsning", 3, 12, 60),
                    new("Latsdrag", 3, 12, 60),
                    new("Hammarcurl", 3, 12, 60),
                    new("Tricepspressdown", 3, 12, 60),
                    new("Sidolyft", 3, 15, 60),
                ]) { LabelKey = "Program_phul_Day3_Label" },
                new("Lower — Hypertrofi", [
                    new("Knäböj", 4, 10, 120),
                    new("Benböjning", 4, 12, 90),
                    new("Bensträckning", 4, 12, 90),
                    new("Utfall", 3, 10, 90),
                    new("Stående vadpress", 5, 15, 60),
                ]) { LabelKey = "Program_phul_Day4_Label" },
            ]),

        new("phat", "PHAT — Power Hypertrophy Adaptive Training",
            "5-dagars program med 2 styrkdagar och 3 hypertrofidagar. Inspirerat av Layne Nortons träningsfilosofi.",
            5,
            [
                new("Upper — Styrka", [
                    new("Bänkpress", 3, 3, 180),
                    new("Skivstångsrodd", 3, 3, 180),
                    new("Militärpress", 2, 5, 150),
                    new("Chins", 3, 5, 150),
                    new("Hammarcurl", 2, 6, 90),
                    new("Skullcrusher", 2, 6, 90),
                ]) { LabelKey = "Program_phat_Day1_Label" },
                new("Lower — Styrka", [
                    new("Knäböj", 3, 3, 240),
                    new("Marklyft", 3, 3, 300),
                    new("Rumänska marklyft", 2, 8, 150),
                    new("Benpress", 2, 10, 90),
                ]) { LabelKey = "Program_phat_Day2_Label" },
                new("Rygg & Axlar", [
                    new("Chins", 4, 10, 90),
                    new("Sittande rodd", 4, 10, 90),
                    new("Latsdrag", 3, 12, 60),
                    new("Facepull", 4, 15, 60),
                    new("Militärpress", 3, 12, 90),
                    new("Sidolyft", 3, 15, 60),
                ]) { LabelKey = "Program_phat_Day3_Label" },
                new("Bröst & Armar", [
                    new("Bänkpress", 4, 10, 90),
                    new("Lutande bänkpress", 4, 10, 90),
                    new("Kabelkorsning", 3, 15, 60),
                    new("Skivstångscurl", 4, 12, 60),
                    new("Hammarcurl", 3, 12, 60),
                    new("Skullcrusher", 3, 12, 60),
                    new("Tricepspressdown", 3, 15, 60),
                ]) { LabelKey = "Program_phat_Day4_Label" },
                new("Ben", [
                    new("Knäböj", 4, 10, 120),
                    new("Benpress", 3, 12, 90),
                    new("Benböjning", 4, 15, 90),
                    new("Bensträckning", 4, 15, 90),
                    new("Stående vadpress", 5, 20, 60),
                ]) { LabelKey = "Program_phat_Day5_Label" },
            ]),

        new("arnoldsplit", "Arnold Split",
            "Arnold Schwarzeneggers klassiska 3-split. Kör varje pass 2 gånger i veckan för 6 träningsdagar totalt.",
            6,
            [
                new("Bröst & Rygg", [
                    new("Bänkpress", 4, 8, 120),
                    new("Lutande bänkpress", 4, 8, 90),
                    new("Kabelkorsning", 3, 12, 60),
                    new("Chins", 4, 8, 120),
                    new("Skivstångsrodd", 4, 8, 90),
                    new("Sittande rodd", 3, 12, 60),
                ]) { LabelKey = "Program_arnoldsplit_Day1_Label" },
                new("Axlar & Armar", [
                    new("Militärpress", 4, 8, 120),
                    new("Sidolyft", 4, 12, 60),
                    new("Facepull", 3, 15, 60),
                    new("Skivstångscurl", 4, 8, 90),
                    new("Hammarcurl", 3, 12, 60),
                    new("Skullcrusher", 4, 10, 90),
                    new("Tricepspressdown", 3, 12, 60),
                ]) { LabelKey = "Program_arnoldsplit_Day2_Label" },
                new("Ben", [
                    new("Knäböj", 4, 8, 180),
                    new("Benpress", 4, 10, 120),
                    new("Rumänska marklyft", 3, 10, 120),
                    new("Benböjning", 4, 12, 90),
                    new("Bensträckning", 4, 12, 90),
                    new("Stående vadpress", 5, 15, 60),
                ]) { LabelKey = "Program_arnoldsplit_Day3_Label" },
            ]),
    ];
}
