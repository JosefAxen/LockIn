using System.Text.Json;
using System.Text.Json.Serialization;
using LockIn;
using LockIn.Models;
using SQLite;

namespace LockIn.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db = null!;
    private readonly Lazy<Task> _lazyInit;

    public DatabaseService()
    {
        _lazyInit = new Lazy<Task>(() => InitCoreAsync(),
            System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task InitAsync() => _lazyInit.Value;

    /// <summary>
    /// Direktåtkomst till underliggande connection efter InitAsync.
    /// Används av repositories (ADR-001). Icke-repository-kod bör INTE använda denna.
    /// </summary>
    internal SQLiteAsyncConnection Connection => _db;

    /// <summary>Init + connection i ett anrop. Praktiskt för repos.</summary>
    internal async Task<SQLiteAsyncConnection> EnsureConnectionAsync()
    {
        await InitAsync();
        return _db;
    }

    private async Task InitCoreAsync()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lockin.db");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        // RP-clone reset: droppa gamla RP-relaterade tabeller (appen har inga
        // riktiga användare än — historik offras för att kunna bygga om schemat)
        await _db.ExecuteAsync("DROP TABLE IF EXISTS WorkoutTemplates");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS TemplateExercises");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS TrainingCycles");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS CycleWeeks");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS CycleSessions");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS WorkoutSessions");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS SessionExercises");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS LoggedSets");
        await _db.ExecuteAsync("DROP TABLE IF EXISTS MuscleVolumeLandmarks");

        // Ny schema
        await _db.CreateTableAsync<Exercise>();
        await _db.CreateTableAsync<WorkoutTemplate>();
        await _db.CreateTableAsync<TemplateDay>();
        await _db.CreateTableAsync<TemplateMuscleSlot>();
        await _db.CreateTableAsync<TemplateExercise>();
        await _db.CreateTableAsync<Mesocycle>();
        await _db.CreateTableAsync<MesocyclePriority>();
        await _db.CreateTableAsync<MesocycleWeek>();
        await _db.CreateTableAsync<WorkoutSession>();
        await _db.CreateTableAsync<SessionExercise>();
        await _db.CreateTableAsync<LoggedSet>();
        await _db.CreateTableAsync<SessionMuscleFeedback>();
        await _db.CreateTableAsync<SessionExerciseFeedback>();
        await _db.CreateTableAsync<MuscleVolumeLandmarks>();
        await _db.CreateTableAsync<AppSettings>();
        await _db.CreateTableAsync<BodyWeightEntry>();
        await _db.CreateTableAsync<BodyCompositionEntry>();
        await _db.CreateTableAsync<UserAchievement>();
        await _db.CreateTableAsync<WorkoutPhoto>();
        await _db.CreateTableAsync<CardioSession>();

        // AppSettings-migreringar (endast nya fält — idempotent via try-catch)
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN WeeklyWorkoutGoal INTEGER NOT NULL DEFAULT 4"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN UserName TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HasCompletedOnboarding INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN MorningRecoveryDate TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN MorningRecoveryPct REAL NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HeightCm INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderDays INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderTimeMinutes INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN ReminderLabels TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HasSeenRirHint INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HasSeenMesoBuilderTip INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        // Nya RP-clone-fält (Task 5)
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN PreferredEquipmentJson TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN HasSeenStartWeightHint INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE AppSettings ADD COLUMN OnboardingStep INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        await SeedAsync();
        await SeedExerciseDescriptionsAsync();
        await SeedForearmExercisesAsync();
        await SeedExerciseDbAsync();
        await SeedSwedishNamesAsync();
        await SeedMuscleVolumeLandmarksAsync();
        await SeedTemplateLibraryAsync();
    }

    private async Task SeedMuscleVolumeLandmarksAsync()
    {
        var count = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MuscleVolumeLandmarks");
        if (count > 0) return;

        var defaults = new (MuscleGroup Group, int Mev, int Mrv)[]
        {
            (MuscleGroup.Chest,      10, 20),
            (MuscleGroup.Back,       12, 22),
            (MuscleGroup.Shoulders,  10, 20),
            (MuscleGroup.Biceps,      8, 17),
            (MuscleGroup.Triceps,     8, 17),
            (MuscleGroup.Quads,      10, 20),
            (MuscleGroup.Hamstrings,  8, 18),
            (MuscleGroup.Glutes,      8, 16),
            (MuscleGroup.Calves,      8, 16),
            (MuscleGroup.Traps,       6, 14),
            (MuscleGroup.Forearms,    6, 14),
            (MuscleGroup.Abs,         8, 18),
        };
        foreach (var (g, mev, mrv) in defaults)
            await _db.InsertAsync(new MuscleVolumeLandmarks { MuscleGroup = g, MEV = mev, MRV = mrv });
    }

    private async Task SeedTemplateLibraryAsync()
    {
        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutTemplates WHERE IsCustom = 0");
        if (count > 0) return;

        foreach (var def in Data.TemplateLibrary.All)
        {
            var template = new WorkoutTemplate
            {
                Name = def.Name,
                IsCustom = false,
                DefaultDaysPerWeek = def.DaysPerWeek,
                CreatedAt = DateTime.Now,
            };
            await _db.InsertAsync(template);

            for (int i = 0; i < def.Days.Count; i++)
            {
                var d = def.Days[i];
                var day = new TemplateDay
                {
                    TemplateId = template.Id,
                    DayIndex = i,
                    Label = d.Label,
                    DefaultDayOfWeek = d.DefaultDayOfWeek,
                };
                await _db.InsertAsync(day);

                for (int j = 0; j < d.Muscles.Count; j++)
                {
                    var slot = new TemplateMuscleSlot
                    {
                        TemplateDayId = day.Id,
                        MuscleGroupValue = (int)d.Muscles[j],
                        SortOrder = j,
                    };
                    await _db.InsertAsync(slot);
                    // Ingen exercise-tilldelning här — det görs vid StartFromTemplateAsync
                    // med ExerciseRecommendationService.
                }
            }
        }
    }

    private async Task SeedExerciseDescriptionsAsync()
    {
        var hasAny = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Exercises WHERE Description != '' AND IsCustom = 0") > 0;
        if (hasAny) return;

        var descriptions = new Dictionary<string, string>
        {
            ["Bänkpress"]          = "Ligg på bänk med stången vid bröstet. Pressa rakt upp med axlarna nedsänkta och skulderbladen ihoptryckta. Armarna ca 45° från kroppen.",
            ["Lutande bänkpress"]  = "Som bänkpress men bänken lutad 30–45°. Betonar övre bröstmuskeln. Använd lite lättare vikt än vanlig bänkpress.",
            ["Kabelkorsning"]      = "Stå mitt emellan kabelmaskiner. För handtagen i en bred bågrörelse framåt och inåt. Känn sträckningen när armarna är öppna.",
            ["Dips"]               = "Stöd på stänger, sänk kroppen tills armbågarna är 90°. Luta framåt för bröstfokus, håll kroppen rakt för tricepsfokus.",
            ["Marklyft"]           = "Foterna höftbredd, tag om stången. Tryck golvet ifrån dig och sträck höfterna framåt. Håll ryggen rak och stången nära kroppen.",
            ["Skivstångsrodd"]     = "Böj framåt i höften, dra stången mot naveln. Håll ryggen platt och skulderbladen ihoptryckta i toppläget.",
            ["Latsdrag"]           = "Bred fatning, dra stången till övre bröstet. Led med armbågarna — inte händerna. Lätt bakåtlutning i ryggen.",
            ["Chins"]              = "Häng i stången med underhandsfattning. Dra kroppen upp tills hakan passerar stången. Aktivera latsen aktivt från bottenpositionen.",
            ["Sittande rodd"]      = "Dra handtaget mot nedre buken. Håll bröstet upp och armbågarna nära kroppen. Sträck ut ordentligt i startpositionen.",
            ["Militärpress"]       = "Stång vid axlarna, pressa rakt upp tills armarna är helt sträckta. Spänn magen, undvik överdrivet svaj i ländryggen.",
            ["Sidolyft"]           = "Lyft hantlar åt sidorna till axelhöjd med lätt böjda armar. Sänk kontrollerat — den excentriska fasen är minst lika viktig.",
            ["Frontlyft"]          = "Lyft hantlar eller stång rakt framåt till axelhöjd. Lyft inte högre än axlarna för att skona rotatorkuffen.",
            ["Facepull"]           = "Dra kabeln mot ansiktet med armbågarna högt. Rotera axlarna utåt i slutläget. Bra för axelstabilitet och hållning.",
            ["Skivstångscurl"]     = "Håll stången med underhandsfattning, curl mot axlarna. Armbågarna stilla vid sidan. Undvik att svänga med kroppen.",
            ["Hammarcurl"]         = "Neutralt grepp med tummen upp. Tränar brachialis och underarmarna mer än vanlig curl. Bra komplement till skivstångscurl.",
            ["Koncentrationscurl"] = "Sittande med armbågen mot insidan av låret. Curla hela rörelseomfånget. Maximal isolering av biceps.",
            ["Tricepspressdown"]   = "Pressa kabeln rakt ned med armbågarna nära kroppen. Lås armbågarna, spänn triceps hårt i botten.",
            ["Skullcrusher"]       = "Ligg på bänk, sänk stången mot pannan med armbågarna pekande uppåt. Full sträckning i toppen, full böjning i botten.",
            ["Triceps dips"]       = "Händerna bakåt på en bänk, fötterna framåt. Sänk tills armbågarna är 90°. Håll kroppen nära bänken.",
            ["Knäböj"]             = "Fötter axelbredd, tårna lätt utåt. Sätt dig ned tills låren är parallella. Håll knäna i linje med tåriktningen och bröstet upp.",
            ["Benpress"]           = "Tryck plattan ifrån dig med hela foten. Sänk tills knäna är 90°. Undvik att låsa ut knäna helt i toppläget.",
            ["Rumänska marklyft"]  = "Höftgångjärn — för höfterna bakåt, böj framåt med rak rygg och lätt böjda knän. Känn sträckning i baksida lår.",
            ["Utfall"]             = "Ta ett steg framåt, sänk bakre knät mot golvet. Håll överkroppen upprätt och det främre knät bakom tålinjen.",
            ["Benböjning"]         = "Liggande eller sittande maskin — böj knäna och dra hälen mot skinkorna. Fokusera på baksida lår.",
            ["Bensträckning"]      = "Sittande maskin — sträck knäna mot rak position. Fokus på framsida lår. Sänk kontrollerat för excentrisk belastning.",
            ["Stående vadpress"]   = "Full rörelseomfång — sänk hälarna under tåplan och pressa upp på tå. Långsamt tempo för maximalt utbyte.",
            ["Plankan"]            = "Håll kroppen rak från huvud till häl. Spänn mage, skinkor och axlar. Undvik att höfterna sjunker eller skjuter upp.",
            ["Crunches"]           = "Kröker överkroppen mot knäna utan att lyfta hela ryggen från golvet. Fokus på övre magen. Andas ut i toppläget.",
            ["Rysk twist"]         = "Sittande med lätt böjda knän och fötterna lätt lyfta. Rotera överkroppen från sida till sida med händerna samlade.",
        };

        foreach (var (name, desc) in descriptions)
            await _db.ExecuteAsync("UPDATE Exercises SET Description = ? WHERE Name = ? AND IsCustom = 0", desc, name);
    }

    private async Task SeedForearmExercisesAsync()
    {
        var exists = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Exercises WHERE MuscleGroup = ? AND IsCustom = 0",
            (int)MuscleGroup.Forearms) > 0;
        if (exists) return;

        var exercises = new List<Exercise>
        {
            new() { Name = "Handledsrullning", MuscleGroup = MuscleGroup.Forearms, DefaultRestSeconds = 60,
                Description = "Håll stången med underhandsgrepp. Rulla handlederna uppåt och sänk kontrollerat. Tränar armböjarna i underarmen." },
            new() { Name = "Omvänd curl", MuscleGroup = MuscleGroup.Forearms, DefaultRestSeconds = 60,
                Description = "Curla stången med överhandsgrepp (knogarna upp). Betonar brachioradialis och extensorerna i underarmen." },
            new() { Name = "Hantelcurl neutral", MuscleGroup = MuscleGroup.Forearms, DefaultRestSeconds = 60,
                Description = "Neutralt grepp med tummen framåt. Full rörelse — sänk hela vägen. Tränar underarmarna och brachialis." },
        };
        await _db.InsertAllAsync(exercises);
    }

    private async Task SeedAsync()
    {
        var count = await _db.Table<Exercise>().CountAsync();
        if (count > 0) return;

        var exercises = new List<Exercise>
        {
            // Bröst
            new() { Name = "Bänkpress", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 180 },
            new() { Name = "Lutande bänkpress", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 180 },
            new() { Name = "Kabelkorsning", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 90 },
            new() { Name = "Dips", MuscleGroup = MuscleGroup.Chest, DefaultRestSeconds = 120 },

            // Rygg
            new() { Name = "Marklyft", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 240 },
            new() { Name = "Skivstångsrodd", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 180 },
            new() { Name = "Latsdrag", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 120 },
            new() { Name = "Chins", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 150 },
            new() { Name = "Sittande rodd", MuscleGroup = MuscleGroup.Back, DefaultRestSeconds = 120 },

            // Axlar
            new() { Name = "Militärpress", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 180 },
            new() { Name = "Sidolyft", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },
            new() { Name = "Frontlyft", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },
            new() { Name = "Facepull", MuscleGroup = MuscleGroup.Shoulders, DefaultRestSeconds = 90 },

            // Biceps
            new() { Name = "Skivstångscurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 90 },
            new() { Name = "Hammarcurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 90 },
            new() { Name = "Koncentrationscurl", MuscleGroup = MuscleGroup.Biceps, DefaultRestSeconds = 60 },

            // Triceps
            new() { Name = "Tricepspressdown", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },
            new() { Name = "Skullcrusher", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },
            new() { Name = "Triceps dips", MuscleGroup = MuscleGroup.Triceps, DefaultRestSeconds = 90 },

            // Ben
            new() { Name = "Knäböj", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 240 },
            new() { Name = "Benpress", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 180 },
            new() { Name = "Rumänska marklyft", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 180 },
            new() { Name = "Utfall", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 120 },
            new() { Name = "Benböjning", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 90 },
            new() { Name = "Bensträckning", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 90 },
            new() { Name = "Stående vadpress", MuscleGroup = MuscleGroup.Legs, DefaultRestSeconds = 60 },

            // Core
            new() { Name = "Plankan", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
            new() { Name = "Crunches", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
            new() { Name = "Rysk twist", MuscleGroup = MuscleGroup.Core, DefaultRestSeconds = 60 },
        };

        await _db.InsertAllAsync(exercises);

        var settings = await _db.Table<AppSettings>().FirstOrDefaultAsync();
        if (settings is null)
            await _db.InsertAsync(new AppSettings());
    }

    private async Task SeedExerciseDbAsync()
    {
        // Kör om igen om Exercise-rader saknar Equipment-data (ny kolumn, gammal seed)
        var alreadySeeded = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Exercises WHERE Name = 'Bench Press' AND Equipment != 0") > 0;
        if (alreadySeeded) return;

        await using var stream = await FileSystem.OpenAppPackageFileAsync("exercises_db.json");
        var entries = await JsonSerializer.DeserializeAsync<List<ExerciseDbEntry>>(stream)
                      ?? new List<ExerciseDbEntry>();

        var existing = (await _db.Table<Exercise>().ToListAsync())
            .ToDictionary(e => e.Name.ToLowerInvariant(), e => e);

        var gymEquipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "barbell", "dumbbell", "cable", "machine", "body only",
            "e-z curl bar", "kettlebells", "bands"
        };

        var toInsert = new List<Exercise>();
        var toUpdate = new List<Exercise>();

        foreach (var e in entries)
        {
            if (e.Equipment is null || !gymEquipment.Contains(e.Equipment)) continue;
            if (string.Equals(e.Category, "cardio", StringComparison.OrdinalIgnoreCase)) continue;
            if (e.Name is null) continue;

            var muscle    = MapDbMuscle(e.PrimaryMuscles?.FirstOrDefault());
            var equipment = MapDbEquipment(e.Equipment);
            var force     = MapDbForce(e.Force);
            var level     = MapDbLevel(e.Level);
            var mechanic  = MapDbMechanic(e.Mechanic);
            var secondary = e.SecondaryMuscles is { Count: > 0 }
                ? string.Join(", ", e.SecondaryMuscles)
                : "";
            var desc = e.Instructions is { Count: > 0 }
                ? string.Join(" ", e.Instructions.Take(2))
                : "";
            var rest = mechanic == ExerciseMechanic.Compound ? 150 : 90;

            if (existing.TryGetValue(e.Name.ToLowerInvariant(), out var ex))
            {
                ex.Equipment       = equipment;
                ex.SecondaryMuscles = secondary;
                ex.Force           = force;
                ex.Level           = level;
                ex.Mechanic        = mechanic;
                if (string.IsNullOrEmpty(ex.Description) && desc.Length > 0)
                    ex.Description = desc;
                toUpdate.Add(ex);
            }
            else
            {
                toInsert.Add(new Exercise
                {
                    Name             = e.Name,
                    MuscleGroup      = muscle,
                    DefaultRestSeconds = rest,
                    Description      = desc,
                    Equipment        = equipment,
                    SecondaryMuscles = secondary,
                    Force            = force,
                    Level            = level,
                    Mechanic         = mechanic,
                });
            }
        }

        await _db.RunInTransactionAsync(conn =>
        {
            foreach (var ex in toUpdate)
                conn.Update(ex);
            if (toInsert.Count > 0)
                conn.InsertAll(toInsert);
        });
    }

    private async Task SeedSwedishNamesAsync()
    {
        await _db.RunInTransactionAsync(conn =>
        {
            foreach (var (englishName, swedishName) in SwedishExerciseNames.Map)
                conn.Execute(
                    "UPDATE Exercises SET SwedishName = ? WHERE Name = ? AND SwedishName = ''",
                    swedishName, englishName);
        });
    }

    private static MuscleGroup MapDbMuscle(string? m) => m?.ToLowerInvariant() switch
    {
        "chest"                                                      => MuscleGroup.Chest,
        "lats" or "middle back" or "lower back" or "traps"          => MuscleGroup.Back,
        "shoulders"                                                  => MuscleGroup.Shoulders,
        "biceps"                                                     => MuscleGroup.Biceps,
        "triceps"                                                    => MuscleGroup.Triceps,
        "quadriceps" or "hamstrings" or "glutes" or "calves"
            or "adductors" or "abductors"                            => MuscleGroup.Legs,
        "abdominals"                                                 => MuscleGroup.Core,
        "forearms"                                                   => MuscleGroup.Forearms,
        _                                                            => MuscleGroup.Other
    };

    private static EquipmentType MapDbEquipment(string? e) => e?.ToLowerInvariant() switch
    {
        "barbell"       => EquipmentType.Barbell,
        "dumbbell"      => EquipmentType.Dumbbell,
        "cable"         => EquipmentType.Cable,
        "machine"       => EquipmentType.Machine,
        "body only"     => EquipmentType.BodyOnly,
        "e-z curl bar"  => EquipmentType.EZBar,
        "kettlebells"   => EquipmentType.Kettlebell,
        "bands"         => EquipmentType.Bands,
        "foam roll"     => EquipmentType.FoamRoll,
        "medicine ball" => EquipmentType.MedicineBall,
        _               => EquipmentType.Other
    };

    private static ExerciseForce MapDbForce(string? f) => f?.ToLowerInvariant() switch
    {
        "push"   => ExerciseForce.Push,
        "pull"   => ExerciseForce.Pull,
        "static" => ExerciseForce.Static,
        _        => ExerciseForce.Other
    };

    private static ExerciseLevel MapDbLevel(string? l) => l?.ToLowerInvariant() switch
    {
        "intermediate" => ExerciseLevel.Intermediate,
        "expert"       => ExerciseLevel.Expert,
        _              => ExerciseLevel.Beginner
    };

    private static ExerciseMechanic MapDbMechanic(string? m) => m?.ToLowerInvariant() switch
    {
        "compound"  => ExerciseMechanic.Compound,
        "isolation" => ExerciseMechanic.Isolation,
        _           => ExerciseMechanic.Other
    };

    private sealed class ExerciseDbEntry
    {
        [JsonPropertyName("name")]             public string?       Name             { get; set; }
        [JsonPropertyName("equipment")]        public string?       Equipment        { get; set; }
        [JsonPropertyName("mechanic")]         public string?       Mechanic         { get; set; }
        [JsonPropertyName("force")]            public string?       Force            { get; set; }
        [JsonPropertyName("level")]            public string?       Level            { get; set; }
        [JsonPropertyName("category")]         public string?       Category         { get; set; }
        [JsonPropertyName("primaryMuscles")]   public List<string>? PrimaryMuscles   { get; set; }
        [JsonPropertyName("secondaryMuscles")] public List<string>? SecondaryMuscles { get; set; }
        [JsonPropertyName("instructions")]     public List<string>? Instructions     { get; set; }
    }

    /// <summary>
    /// Volym × intensitet för alla loggade set i ett datumintervall.
    /// Per set: WeightKg × Reps × intensity, där intensity = (1 − RIR/6)^1.5.
    /// Icke-linjär RIR-vikt: validitet sjunker längre från failure (RIR 4–6
    /// underrapporteras ofta), så set nära failure väger relativt tyngre.
    /// </summary>
    public async Task<double> GetVolumeIntensityForRangeAsync(DateTime from, DateTime toExclusive)
    {
        await InitAsync();
        // CASE-tabell för (1 − RIR/6)^1.5 — RIR är begränsat till 0..6 av UI.
        return await _db.ExecuteScalarAsync<double>(
            @"SELECT COALESCE(SUM(
                WeightKg * Reps * CASE RIR
                    WHEN 0 THEN 1.0000
                    WHEN 1 THEN 0.7607
                    WHEN 2 THEN 0.5443
                    WHEN 3 THEN 0.3536
                    WHEN 4 THEN 0.1925
                    WHEN 5 THEN 0.0680
                    ELSE 0.0
                END
              ), 0)
              FROM LoggedSets
              WHERE LoggedAt >= ? AND LoggedAt < ?", from, toExclusive);
    }

    public Task<double> GetVolumeIntensityForDateAsync(DateTime date)
        => GetVolumeIntensityForRangeAsync(date.Date, date.Date.AddDays(1));

    public async Task<double> GetRawVolumeForRangeAsync(DateTime from, DateTime toExclusive)
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<double>(
            @"SELECT COALESCE(SUM(ls.WeightKg * ls.Reps), 0)
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL
                AND ws.StartedAt >= ? AND ws.StartedAt < ?
                AND (ls.SetType = 0 OR ls.SetType IS NULL)", from, toExclusive);
    }

    public async Task<(string ExerciseName, double GapKg, double RecentMaxKg, double AllTimeMaxKg)?> GetNearestPRGapAsync(DateTime recentCutoff)
    {
        await InitAsync();
        var rows = await _db.QueryAsync<PRGapRow>(@"
            SELECT e.Name AS ExerciseName,
                   MAX(ls.WeightKg) AS AllTimeMaxKg,
                   MAX(CASE WHEN ws.CompletedAt >= ? THEN CAST(ls.WeightKg AS REAL) END) AS RecentMaxKg,
                   (MAX(ls.WeightKg) - MAX(CASE WHEN ws.CompletedAt >= ? THEN CAST(ls.WeightKg AS REAL) END)) AS GapKg
            FROM LoggedSets ls
            JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
            JOIN WorkoutSessions ws ON ws.Id = se.SessionId
            JOIN Exercises e ON e.Id = se.ExerciseId
            WHERE ws.CompletedAt IS NOT NULL
              AND (ls.SetType = 0 OR ls.SetType IS NULL)
            GROUP BY e.Id, e.Name
            HAVING RecentMaxKg IS NOT NULL
               AND GapKg > 0
               AND GapKg <= 10
            ORDER BY GapKg ASC
            LIMIT 1",
            recentCutoff, recentCutoff);

        var row = rows.FirstOrDefault();
        if (row is null) return null;
        return (row.ExerciseName, row.GapKg, row.RecentMaxKg, row.AllTimeMaxKg);
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        await InitAsync();
        var rows = await _db.QueryAsync<SessionDateRow>(
            "SELECT StartedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL ORDER BY StartedAt DESC");

        var distinctDates = rows
            .Select(r => r.StartedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (distinctDates.Count == 0) return 0;
        var today = DateTime.Today;
        if (distinctDates[0] < today.AddDays(-1)) return 0;

        var expected = distinctDates[0];
        int streak = 0;
        foreach (var d in distinctDates)
        {
            if (d == expected) { streak++; expected = expected.AddDays(-1); }
            else break;
        }
        return streak;
    }

    public async Task<int> DeleteAllDataAsync()
    {
        await InitAsync();
        await _db.DeleteAllAsync<LoggedSet>();
        await _db.DeleteAllAsync<SessionExercise>();
        await _db.DeleteAllAsync<WorkoutSession>();
        await _db.DeleteAllAsync<TemplateExercise>();
        await _db.DeleteAllAsync<WorkoutTemplate>();
        await _db.DeleteAllAsync<Exercise>();
        await _db.DeleteAllAsync<BodyWeightEntry>();
        await _db.DeleteAllAsync<BodyCompositionEntry>();
        await _db.DeleteAllAsync<UserAchievement>();
        await _db.DeleteAllAsync<AppSettings>();
        // Delete photo files and records
        var photos = await _db.Table<WorkoutPhoto>().ToListAsync();
        foreach (var p in photos)
            if (File.Exists(p.FilePath)) File.Delete(p.FilePath);
        await _db.DeleteAllAsync<WorkoutPhoto>();
        await SeedAsync();
        await SeedExerciseDescriptionsAsync();
        await SeedForearmExercisesAsync();
        await SeedExerciseDbAsync();
        await SeedSwedishNamesAsync();
        return 0;
    }

    // ── Muscle scores ──────────────────────────────────────────────────────

    public async Task<Dictionary<MuscleGroup, double>> GetMuscleScoresAsync()
    {
        await InitAsync();
        var cutoff = DateTime.Now.AddDays(-7);
        var rows = await _db.QueryAsync<MuscleSetRow>(
            @"SELECT e.MuscleGroup, ls.RIR
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL
                AND ws.StartedAt >= ?
                AND (ls.SetType = 0 OR ls.SetType IS NULL)", cutoff);

        var result = new Dictionary<MuscleGroup, double>();
        foreach (var g in rows.GroupBy(r => (MuscleGroup)r.MuscleGroup))
        {
            var sets = g.ToList();
            // NULL RIR → treat as moderate effort (RIR 3 = 0.5 intensity) rather than max
            var avgIntensity = sets.Average(s => 1.0 - Math.Max(s.RIR ?? 3, 0) / 6.0);
            var score = Math.Min(sets.Count * avgIntensity, 10.0);
            result[g.Key] = Math.Round(score, 1);
        }
        return result;
    }

    public async Task<Dictionary<MuscleGroup, double[]>> GetWeeklyVolumeByMuscleGroupAsync(int weeks)
    {
        await InitAsync();
        var cutoff = DateTime.Now.AddDays(-7 * weeks);
        var rows = await _db.QueryAsync<VolumeByMuscleRow>(
            @"SELECT e.MuscleGroup, ws.StartedAt, SUM(ls.WeightKg * ls.Reps) AS VolumeKg
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL
                AND ws.StartedAt >= ?
                AND (ls.SetType = 0 OR ls.SetType IS NULL)
              GROUP BY e.MuscleGroup, ws.Id", cutoff);

        var now = DateTime.Now;
        var result = new Dictionary<MuscleGroup, double[]>();
        // Only the 7 primary muscle groups are shown in volume trend (FullBody/Other/Forearms excluded)
        MuscleGroup[] primaryGroups = [MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
            MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Legs, MuscleGroup.Core];
        foreach (var mg in primaryGroups)
        {
            var arr = new double[weeks];
            foreach (var row in rows.Where(r => (MuscleGroup)r.MuscleGroup == mg))
            {
                var weekIndex = (int)((now - row.StartedAt).TotalDays / 7);
                if (weekIndex >= 0 && weekIndex < weeks)
                    arr[weeks - 1 - weekIndex] += row.VolumeKg;
            }
            if (arr.Any(v => v > 0))
                result[mg] = arr;
        }
        return result;
    }

    public async Task<Dictionary<MuscleGroup, int>> GetMuscleFrequencyAsync(int weeks)
    {
        await InitAsync();
        var cutoff = DateTime.Now.AddDays(-7 * weeks);
        var rows = await _db.QueryAsync<MuscleFrequencyRow>(
            @"SELECT e.MuscleGroup, COUNT(DISTINCT ws.Id) AS SessionCount
              FROM SessionExercises se
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL
                AND ws.StartedAt >= ?
              GROUP BY e.MuscleGroup", cutoff);
        return rows.ToDictionary(r => (MuscleGroup)r.MuscleGroup, r => r.SessionCount);
    }

    public async Task<HashSet<int>> GetTrainedDaysInMonthAsync(int year, int month)
    {
        await InitAsync();
        var start = new DateTime(year, month, 1);
        var end   = start.AddMonths(1);
        var rows = await _db.QueryAsync<SessionDateRow>(
            "SELECT StartedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND StartedAt >= ? AND StartedAt < ?",
            start, end);
        return rows.Select(r => r.StartedAt.Day).ToHashSet();
    }

    public class SessionSummaryRow
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TemplateName { get; set; } = "";
        public decimal TotalVolume { get; set; }
        public int TotalSets { get; set; }
        public int PRCount { get; set; }
        public string Notes { get; set; } = "";
    }

    public class SessionExerciseDetailRow
    {
        public string ExerciseName { get; set; } = "";
        public int SetNumber { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
        public int RIR { get; set; }
        public bool IsPR { get; set; }
        public Models.SetType SetType { get; set; } = Models.SetType.Normal;
        public int DurationSeconds { get; set; } = 0;

        public string SetDisplay => SetType == Models.SetType.Time
            ? $"⏱ {DurationSeconds}s"
            : $"{WeightKg:G} kg × {Reps}";

        public string SetLabel => SetType switch
        {
            Models.SetType.Warmup  => "W",
            Models.SetType.Dropset => "↓",
            Models.SetType.Time    => "⏱",
            _                      => SetNumber.ToString()
        };

        public Color SetLabelColor => SetType switch
        {
            Models.SetType.Warmup  => DesignTokens.SetWarmup,
            Models.SetType.Dropset => DesignTokens.SetDropset,
            Models.SetType.Time    => DesignTokens.SetTime,
            _                      => DesignTokens.SetNormal
        };

        public string WeightDisplay => SetType == Models.SetType.Time ? $"{DurationSeconds}s" : $"{WeightKg:G}";
        public string RepsDisplay   => SetType == Models.SetType.Time ? "–" : Reps.ToString();
    }

    public class BestSetRow
    {
        public DateTime SessionDate { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
        public bool IsPR { get; set; }
    }

    public class MuscleVolumeRow
    {
        public int MuscleGroup { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
    }

    private class SessionDateRow
    {
        public DateTime StartedAt { get; set; }
    }

    private class MuscleSetRow
    {
        public int MuscleGroup { get; set; }
        public int? RIR { get; set; }
    }

    private class MuscleFrequencyRow
    {
        public int MuscleGroup { get; set; }
        public int SessionCount { get; set; }
    }

    private class VolumeByMuscleRow
    {
        public int MuscleGroup { get; set; }
        public DateTime StartedAt { get; set; }
        public double VolumeKg { get; set; }
    }

    private class PRGapRow
    {
        public string ExerciseName { get; set; } = "";
        public double AllTimeMaxKg { get; set; }
        public double RecentMaxKg { get; set; }
        public double GapKg { get; set; }
    }

    public async Task<int> GetTotalPRCountAsync()
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM LoggedSets WHERE IsPR = 1");
    }

    public async Task<decimal> GetTotalVolumeAsync()
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(WeightKg * Reps), 0) FROM LoggedSets WHERE SetType = 0 OR SetType IS NULL");
    }

    public async Task<int> GetCurrentWeekStreakAsync()
    {
        await InitAsync();
        var rows = await _db.QueryAsync<SessionDateRow>(
            "SELECT StartedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL ORDER BY StartedAt DESC");

        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var currentMonday = today.AddDays(-daysFromMonday);

        var weeksWithSessions = rows
            .Select(r => r.StartedAt.Date)
            .Select(d => { var dow = (int)d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1; return d.AddDays(-dow); })
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (weeksWithSessions.Count == 0) return 0;
        if (weeksWithSessions[0] < currentMonday.AddDays(-7)) return 0;

        int streak = 0;
        var expected = weeksWithSessions[0];
        foreach (var w in weeksWithSessions)
        {
            if (w == expected) { streak++; expected = expected.AddDays(-7); }
            else break;
        }
        return streak;
    }

    public async Task<bool> GetAllMuscleGroupsThisWeekAsync()
    {
        await InitAsync();
        var cutoff = DateTime.Now.AddDays(-7);
        var rows = await _db.QueryAsync<MuscleSetRow>(
            @"SELECT DISTINCT e.MuscleGroup, 0 as RIR
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE ws.CompletedAt IS NOT NULL AND ws.StartedAt >= ?", cutoff);

        var required = new[] { MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
                               MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Legs, MuscleGroup.Core };
        var trained = rows.Select(r => (MuscleGroup)r.MuscleGroup).ToHashSet();
        return required.All(mg => trained.Contains(mg));
    }

    public async Task<List<decimal>> GetWeeklyVolumeTrendAsync(int weeks)
    {
        await InitAsync();
        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var currentMonday = today.AddDays(-daysFromMonday);

        var result = new List<decimal>();
        for (int i = 1; i <= weeks; i++)
        {
            var weekStart = currentMonday.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);
            var vol = await _db.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(ls.WeightKg * ls.Reps), 0)
                  FROM LoggedSets ls
                  JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
                  JOIN WorkoutSessions ws ON ws.Id = se.SessionId
                  WHERE ws.CompletedAt IS NOT NULL AND ws.StartedAt >= ? AND ws.StartedAt < ?
                    AND (ls.SetType = 0 OR ls.SetType IS NULL)",
                weekStart, weekEnd);
            result.Add(vol);
        }
        return result;
    }

    // ── Export ─────────────────────────────────────────────────────────────────

    internal class ExportSessionRow
    {
        public int SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TemplateName { get; set; } = "";
        public string Notes { get; set; } = "";
        public int TotalSets { get; set; }
        public double TotalVolumeKg { get; set; }
        public int PRCount { get; set; }
    }

    internal class ExportSetRow
    {
        public int SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public string ExerciseName { get; set; } = "";
        public int MuscleGroup { get; set; }
        public int Equipment { get; set; }
        public int SetNumber { get; set; }
        public int SetType { get; set; }
        public double WeightKg { get; set; }
        public int Reps { get; set; }
        public int RIR { get; set; }
        public bool IsPR { get; set; }
        public int DurationSeconds { get; set; }
    }

    internal async Task<(List<ExportSessionRow> Sessions, List<ExportSetRow> Sets)> GetExportDataAsync()
    {
        await InitAsync();

        var sessions = await _db.QueryAsync<ExportSessionRow>(@"
            SELECT
                ws.Id                                                    AS SessionId,
                ws.StartedAt,
                ws.CompletedAt,
                ''                                                       AS TemplateName,
                ws.Notes,
                COUNT(ls.Id)                                             AS TotalSets,
                COALESCE(SUM(CAST(ls.WeightKg AS REAL) * ls.Reps), 0.0) AS TotalVolumeKg,
                COALESCE(SUM(ls.IsPR), 0)                                AS PRCount
            FROM WorkoutSessions ws
            LEFT JOIN SessionExercises se ON se.SessionId = ws.Id
            LEFT JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
            WHERE ws.CompletedAt IS NOT NULL
            GROUP BY ws.Id
            ORDER BY ws.StartedAt");

        var sets = await _db.QueryAsync<ExportSetRow>(@"
            SELECT
                ws.Id           AS SessionId,
                ws.StartedAt    AS SessionDate,
                e.Name          AS ExerciseName,
                e.MuscleGroup,
                e.Equipment,
                ls.SetNumber,
                ls.SetType,
                CAST(ls.WeightKg AS REAL) AS WeightKg,
                ls.Reps,
                ls.RIR,
                ls.IsPR,
                ls.DurationSeconds
            FROM LoggedSets ls
            JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
            JOIN WorkoutSessions ws ON ws.Id = se.SessionId
            JOIN Exercises e ON e.Id = se.ExerciseId
            WHERE ws.CompletedAt IS NOT NULL
            ORDER BY ws.StartedAt, se.OrderIndex, ls.SetNumber");

        return (sessions, sets);
    }

}
