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

    private async Task InitCoreAsync()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lockin.db");
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<Exercise>();
        await _db.CreateTableAsync<WorkoutTemplate>();
        await _db.CreateTableAsync<TemplateExercise>();
        await _db.CreateTableAsync<WorkoutSession>();
        await _db.CreateTableAsync<SessionExercise>();
        await _db.CreateTableAsync<LoggedSet>();
        await _db.CreateTableAsync<AppSettings>();
        await _db.CreateTableAsync<BodyWeightEntry>();
        await _db.CreateTableAsync<BodyCompositionEntry>();
        await _db.CreateTableAsync<UserAchievement>();
        await _db.CreateTableAsync<WorkoutPhoto>();
        await _db.CreateTableAsync<CardioSession>();
        await _db.CreateTableAsync<TrainingCycle>();
        await _db.CreateTableAsync<CycleWeek>();
        await _db.CreateTableAsync<CycleSession>();

        try { await _db.ExecuteAsync("ALTER TABLE WorkoutTemplates ADD COLUMN ProgramId TEXT NULL"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN Description TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        // Plan 3: SetType migrations (idempotent — only swallows duplicate-column errors)
        try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN SetType INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN DurationSeconds INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        // Backfill NULLs from schema versions that lacked DEFAULT constraints
        await _db.ExecuteAsync("UPDATE LoggedSets SET SetType = 0 WHERE SetType IS NULL");
        await _db.ExecuteAsync("UPDATE LoggedSets SET DurationSeconds = 0 WHERE DurationSeconds IS NULL");

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

        // Fas 2: Auto-progression per övning
        try { await _db.ExecuteAsync("ALTER TABLE TemplateExercises ADD COLUMN TargetRepsMin INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE TemplateExercises ADD COLUMN TargetRepsMax INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE TemplateExercises ADD COLUMN WeightIncrementKg REAL NOT NULL DEFAULT 2.5"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE TemplateExercises ADD COLUMN AutoProgressMode INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        // Fas 3: Superset
        try { await _db.ExecuteAsync("ALTER TABLE TemplateExercises ADD COLUMN SupersetGroupId INTEGER NULL"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE SessionExercises ADD COLUMN SupersetGroupId INTEGER NULL"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        // Fas 4: RIR explicit column (äldre rader kan ha NULL utan denna migration)
        try { await _db.ExecuteAsync("ALTER TABLE LoggedSets ADD COLUMN RIR INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        // Fas 5: Exercise metadata från free-exercise-db
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN Equipment INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN SecondaryMuscles TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN Force INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN Level INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN Mechanic INTEGER NOT NULL DEFAULT 0"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        // Fas 6: Svenskt namn för sökning
        try { await _db.ExecuteAsync("ALTER TABLE Exercises ADD COLUMN SwedishName TEXT NOT NULL DEFAULT ''"); }
        catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)) { }

        await SeedAsync();
        await SeedExerciseDescriptionsAsync();
        await SeedForearmExercisesAsync();
        await SeedExerciseDbAsync();
        await SeedSwedishNamesAsync();
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

    // ── Exercises ──────────────────────────────────────────────────────────

    public async Task<List<Exercise>> GetExercisesAsync()
    {
        await InitAsync();
        return await _db.Table<Exercise>().OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<Exercise?> GetExerciseAsync(int id)
    {
        await InitAsync();
        return await _db.Table<Exercise>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return exercise.Id == 0 ? await _db.InsertAsync(exercise) : await _db.UpdateAsync(exercise);
    }

    public async Task<int> DeleteExerciseAsync(Exercise exercise)
    {
        await InitAsync();
        return await _db.DeleteAsync(exercise);
    }

    // ── Templates ──────────────────────────────────────────────────────────

    public async Task<List<WorkoutTemplate>> GetTemplatesAsync()
    {
        await InitAsync();
        return await _db.Table<WorkoutTemplate>().ToListAsync();
    }

    public async Task<int> SaveTemplateAsync(WorkoutTemplate template)
    {
        await InitAsync();
        return template.Id == 0 ? await _db.InsertAsync(template) : await _db.UpdateAsync(template);
    }

    public async Task<int> DeleteTemplateAsync(WorkoutTemplate template)
    {
        await InitAsync();
        return await _db.DeleteAsync(template);
    }

    public async Task<int> DuplicateTemplateAsync(WorkoutTemplate source, string newName)
    {
        await InitAsync();
        var copy = new WorkoutTemplate { Name = newName };
        await _db.InsertAsync(copy);

        var sourceExercises = await GetTemplateExercisesAsync(source.Id);
        var copies = sourceExercises.Select(te => new TemplateExercise
        {
            TemplateId         = copy.Id,
            ExerciseId         = te.ExerciseId,
            OrderIndex         = te.OrderIndex,
            Sets               = te.Sets,
            Reps               = te.Reps,
            TargetWeight       = te.TargetWeight,
            DefaultRestSeconds = te.DefaultRestSeconds,
            TargetRepsMin      = te.TargetRepsMin,
            TargetRepsMax      = te.TargetRepsMax,
            WeightIncrementKg  = te.WeightIncrementKg,
            AutoProgressMode   = te.AutoProgressMode,
            SupersetGroupId    = te.SupersetGroupId,
        }).ToList();

        await ReplaceTemplateExercisesAsync(copy.Id, copies);
        return copy.Id;
    }

    public async Task<Dictionary<int, DateTime>> GetLastSessionDatePerTemplateAsync()
    {
        await InitAsync();
        var rows = await _db.QueryAsync<TemplateLastUsedRow>(
            "SELECT TemplateId, MAX(CompletedAt) as LastUsedAt FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND TemplateId > 0 GROUP BY TemplateId");
        return rows.ToDictionary(r => r.TemplateId, r => r.LastUsedAt);
    }

    private class TemplateLastUsedRow
    {
        public int TemplateId { get; set; }
        public DateTime LastUsedAt { get; set; }
    }

    public async Task<List<TemplateExercise>> GetTemplateExercisesAsync(int templateId)
    {
        await InitAsync();
        return await _db.Table<TemplateExercise>()
            .Where(te => te.TemplateId == templateId)
            .OrderBy(te => te.OrderIndex)
            .ToListAsync();
    }

    public async Task<int> SaveTemplateExerciseAsync(TemplateExercise te)
    {
        await InitAsync();
        return te.Id == 0 ? await _db.InsertAsync(te) : await _db.UpdateAsync(te);
    }

    public async Task<int> DeleteTemplateExerciseAsync(TemplateExercise te)
    {
        await InitAsync();
        return await _db.DeleteAsync(te);
    }

    public async Task ReplaceTemplateExercisesAsync(int templateId, List<TemplateExercise> exercises)
    {
        await InitAsync();
        await _db.RunInTransactionAsync(db =>
        {
            db.Execute("DELETE FROM TemplateExercises WHERE TemplateId = ?", templateId);
            foreach (var te in exercises)
                db.Insert(te);
        });
    }

    // ── Sessions ───────────────────────────────────────────────────────────

    public async Task<int> SaveSessionAsync(WorkoutSession session)
    {
        await InitAsync();
        return session.Id == 0 ? await _db.InsertAsync(session) : await _db.UpdateAsync(session);
    }

    public async Task<WorkoutSession?> GetSessionAsync(int id)
    {
        await InitAsync();
        return await _db.Table<WorkoutSession>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<WorkoutSession>> GetSessionsAsync()
    {
        await InitAsync();
        return await _db.Table<WorkoutSession>().OrderByDescending(s => s.StartedAt).ToListAsync();
    }

    public async Task<int> SaveSessionExerciseAsync(SessionExercise se)
    {
        await InitAsync();
        return se.Id == 0 ? await _db.InsertAsync(se) : await _db.UpdateAsync(se);
    }

    public async Task<List<SessionExercise>> GetSessionExercisesAsync(int sessionId)
    {
        await InitAsync();
        return await _db.Table<SessionExercise>()
            .Where(se => se.SessionId == sessionId)
            .OrderBy(se => se.OrderIndex)
            .ToListAsync();
    }

    public async Task DeleteSessionExerciseWithSetsAsync(int sessionExerciseId)
    {
        await InitAsync();
        await _db.ExecuteAsync("DELETE FROM LoggedSets WHERE SessionExerciseId = ?", sessionExerciseId);
        await _db.ExecuteAsync("DELETE FROM SessionExercises WHERE Id = ?", sessionExerciseId);
    }

    // ── Logged Sets ────────────────────────────────────────────────────────

    public async Task<int> SaveLoggedSetAsync(LoggedSet set)
    {
        await InitAsync();
        return set.Id == 0 ? await _db.InsertAsync(set) : await _db.UpdateAsync(set);
    }

    public async Task<List<LoggedSet>> GetSetsForSessionExerciseAsync(int sessionExerciseId)
    {
        await InitAsync();
        return await _db.Table<LoggedSet>()
            .Where(s => s.SessionExerciseId == sessionExerciseId)
            .OrderBy(s => s.SetNumber)
            .ToListAsync();
    }

    public async Task<List<LoggedSet>> GetAllSetsForSessionAsync(int sessionId)
    {
        await InitAsync();
        return await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ?
              ORDER BY se.Id, ls.SetNumber", sessionId);
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

    public async Task<double> GetMaxEpley1RMAsync(int exerciseId, int excludeLoggedSetId = 0)
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<double>(
            @"SELECT COALESCE(MAX(WeightKg * (1.0 + CAST(Reps AS REAL) / 30.0)), 0)
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.ExerciseId = ? AND ls.Id != ?",
            exerciseId, excludeLoggedSetId);
    }

    public async Task<List<LoggedSet>> GetAllSetsForExerciseAsync(int exerciseId)
    {
        await InitAsync();
        return await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ls.LoggedAt DESC", exerciseId);
    }

    public async Task<List<LoggedSet>> GetLastSessionSetsAsync(int exerciseId, int excludeSessionId)
    {
        await InitAsync();
        var sets = await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ? AND ws.Id != ? AND ws.CompletedAt IS NOT NULL
              ORDER BY ws.StartedAt DESC, ls.SetNumber ASC",
            exerciseId, excludeSessionId);

        if (sets.Count == 0) return sets;
        var latestSeId = sets[0].SessionExerciseId;
        return sets.Where(s => s.SessionExerciseId == latestSeId).ToList();
    }

    // ── History ────────────────────────────────────────────────────────────

    public async Task<List<(DateTime Date, decimal WeightKg, int Reps, double Epley1RM, bool IsPR)>> GetBestSetPerSessionForExerciseAsync(int exerciseId)
    {
        await InitAsync();
        var rows = await _db.QueryAsync<BestSetRow>(
            @"SELECT ws.StartedAt as SessionDate, ls.WeightKg, ls.Reps, ls.IsPR
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN WorkoutSessions ws ON ws.Id = se.SessionId
              WHERE se.ExerciseId = ?
              ORDER BY ws.StartedAt DESC", exerciseId);

        return rows
            .GroupBy(r => r.SessionDate.Date)
            .Select(g =>
            {
                var best = g.OrderByDescending(r => (double)r.WeightKg * (1 + r.Reps / 30.0)).First();
                return (best.SessionDate, best.WeightKg, best.Reps,
                    (double)best.WeightKg * (1 + best.Reps / 30.0), best.IsPR);
            })
            .OrderBy(r => r.SessionDate)
            .ToList();
    }

    public async Task<Dictionary<MuscleGroup, (decimal Volume, int Sets)>> GetSessionVolumeByMuscleGroupAsync(int sessionId)
    {
        await InitAsync();
        var rows = await _db.QueryAsync<MuscleVolumeRow>(
            @"SELECT e.MuscleGroup, ls.WeightKg, ls.Reps
              FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              JOIN Exercises e ON e.Id = se.ExerciseId
              WHERE se.SessionId = ? AND (ls.SetType = 0 OR ls.SetType IS NULL)", sessionId);

        return rows
            .GroupBy(r => (MuscleGroup)r.MuscleGroup)
            .ToDictionary(
                g => g.Key,
                g => (g.Sum(r => r.WeightKg * r.Reps), g.Count()));
    }

    public async Task<List<LoggedSet>> GetPRsForSessionAsync(int sessionId)
    {
        await InitAsync();
        return await _db.QueryAsync<LoggedSet>(
            @"SELECT ls.* FROM LoggedSets ls
              JOIN SessionExercises se ON se.Id = ls.SessionExerciseId
              WHERE se.SessionId = ? AND ls.IsPR = 1", sessionId);
    }

    public async Task<bool> HasAnyCompletedSessionsAsync()
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL") > 0;
    }

    public async Task<List<SessionSummaryRow>> GetCompletedSessionsAsync()
    {
        await InitAsync();
        return await _db.QueryAsync<SessionSummaryRow>(
            @"SELECT ws.Id, ws.StartedAt, ws.CompletedAt, ws.Notes,
                     COALESCE(wt.Name, 'Fritt pass') as TemplateName,
                     COALESCE(SUM(ls.WeightKg * ls.Reps), 0) as TotalVolume,
                     COUNT(ls.Id) as TotalSets,
                     SUM(CASE WHEN ls.IsPR = 1 THEN 1 ELSE 0 END) as PRCount
              FROM WorkoutSessions ws
              LEFT JOIN WorkoutTemplates wt ON wt.Id = ws.TemplateId
              LEFT JOIN SessionExercises se ON se.SessionId = ws.Id
              LEFT JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE ws.CompletedAt IS NOT NULL
              GROUP BY ws.Id
              ORDER BY ws.StartedAt DESC");
    }

    public async Task<List<SessionExerciseDetailRow>> GetSessionExerciseDetailsAsync(int sessionId)
    {
        await InitAsync();
        return await _db.QueryAsync<SessionExerciseDetailRow>(
            @"SELECT e.Name as ExerciseName, ls.SetNumber, ls.WeightKg, ls.Reps, ls.RIR, ls.IsPR,
                     ls.SetType, ls.DurationSeconds
              FROM SessionExercises se
              JOIN Exercises e ON e.Id = se.ExerciseId
              JOIN LoggedSets ls ON ls.SessionExerciseId = se.Id
              WHERE se.SessionId = ?
              ORDER BY se.OrderIndex, ls.SetNumber", sessionId);
    }

    // ── Train stats ────────────────────────────────────────────────────────

    public async Task<int> GetTotalCompletedSessionCountAsync()
    {
        await InitAsync();
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL");
    }

    public async Task<int> GetSessionCountThisWeekAsync()
    {
        await InitAsync();
        var today = DateTime.Today;
        var daysFromMonday = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
        var monday = today.AddDays(-daysFromMonday);
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND StartedAt >= ?",
            monday);
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

    // ── Body weight ────────────────────────────────────────────────────────

    public async Task<List<BodyWeightEntry>> GetBodyWeightEntriesAsync()
    {
        await InitAsync();
        return await _db.Table<BodyWeightEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveBodyWeightEntryAsync(BodyWeightEntry entry)
    {
        await InitAsync();
        return entry.Id == 0 ? await _db.InsertAsync(entry) : await _db.UpdateAsync(entry);
    }

    public async Task<int> DeleteBodyWeightEntryAsync(BodyWeightEntry entry)
    {
        await InitAsync();
        return await _db.DeleteAsync(entry);
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

    // ── App settings ──────────────────────────────────────────────────────

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        await InitAsync();
        return await _db.Table<AppSettings>().FirstOrDefaultAsync() ?? new AppSettings();
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        await InitAsync();
        await _db.UpdateAsync(settings);
    }

    // ── Session range queries ─────────────────────────────────────────────

    public async Task<List<WorkoutSession>> GetCompletedSessionsInRangeAsync(DateTime from, DateTime to)
    {
        await InitAsync();
        return await _db.QueryAsync<WorkoutSession>(
            "SELECT * FROM WorkoutSessions WHERE CompletedAt IS NOT NULL AND CompletedAt >= ? AND CompletedAt <= ? ORDER BY StartedAt DESC",
            from, to);
    }

    // ── Cardio sessions ───────────────────────────────────────────────────

    public async Task SaveCardioSessionAsync(CardioSession session)
    {
        await InitAsync();
        if (session.Id == 0)
            await _db.InsertAsync(session);
        else
            await _db.UpdateAsync(session);
    }

    public async Task<List<CardioSession>> GetCardioSessionsAsync(int limit = 50)
    {
        await InitAsync();
        return await _db.Table<CardioSession>()
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteCardioSessionAsync(int id)
    {
        await InitAsync();
        await _db.DeleteAsync<CardioSession>(id);
    }

    // ── Body composition ───────────────────────────────────────────────────

    public async Task<List<BodyCompositionEntry>> GetBodyCompositionEntriesAsync()
    {
        await InitAsync();
        return await _db.Table<BodyCompositionEntry>().OrderByDescending(e => e.LoggedAt).ToListAsync();
    }

    public async Task<int> SaveBodyCompositionEntryAsync(BodyCompositionEntry entry)
    {
        await InitAsync();
        return entry.Id == 0 ? await _db.InsertAsync(entry) : await _db.UpdateAsync(entry);
    }

    public async Task<int> DeleteBodyCompositionEntryAsync(BodyCompositionEntry entry)
    {
        await InitAsync();
        return await _db.DeleteAsync(entry);
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

    private class BestSetRow
    {
        public DateTime SessionDate { get; set; }
        public decimal WeightKg { get; set; }
        public int Reps { get; set; }
        public bool IsPR { get; set; }
    }

    private class MuscleVolumeRow
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

    // ── Achievements ──────────────────────────────────────────────────────

    public async Task<HashSet<int>> GetUnlockedAchievementIdsAsync()
    {
        await InitAsync();
        var rows = await _db.Table<UserAchievement>().ToListAsync();
        return rows.Select(r => r.Id).ToHashSet();
    }

    public async Task<List<UserAchievement>> GetUnlockedAchievementsAsync()
    {
        await InitAsync();
        return await _db.Table<UserAchievement>().ToListAsync();
    }

    public async Task<bool> UnlockAchievementAsync(AchievementId id)
    {
        await InitAsync();
        var existing = await _db.Table<UserAchievement>().Where(a => a.Id == (int)id).FirstOrDefaultAsync();
        if (existing is not null) return false;
        await _db.InsertAsync(new UserAchievement { Id = (int)id, UnlockedAt = DateTime.Now });
        return true;
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

    // ── Photos ─────────────────────────────────────────────────────────────

    public async Task<List<WorkoutPhoto>> GetPhotosForSessionAsync(int sessionId)
    {
        await InitAsync();
        return await _db.Table<WorkoutPhoto>()
            .Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.TakenAt)
            .ToListAsync();
    }

    public async Task<List<WorkoutPhoto>> GetAllPhotosAsync()
    {
        await InitAsync();
        return await _db.Table<WorkoutPhoto>().OrderByDescending(p => p.TakenAt).ToListAsync();
    }

    public async Task<int> SavePhotoAsync(WorkoutPhoto photo)
    {
        await InitAsync();
        return photo.Id == 0 ? await _db.InsertAsync(photo) : await _db.UpdateAsync(photo);
    }

    public async Task<int> DeletePhotoAsync(WorkoutPhoto photo)
    {
        await InitAsync();
        if (File.Exists(photo.FilePath))
            File.Delete(photo.FilePath);
        return await _db.DeleteAsync(photo);
    }

    // ── Program activation ─────────────────────────────────────────────────

    public async Task ActivateProgramAsync(Data.WorkoutProgram program)
    {
        await InitAsync();
        var allExercises = await GetExercisesAsync();
        var exerciseMap  = allExercises.ToDictionary(e => e.Name.ToLowerInvariant(), e => e);

        foreach (var day in program.Days)
        {
            var template = new Models.WorkoutTemplate { Name = day.Label, ProgramId = program.Id };
            await SaveTemplateAsync(template);

            for (int i = 0; i < day.Exercises.Count; i++)
            {
                var pe  = day.Exercises[i];
                var key = pe.ExerciseName.ToLowerInvariant();

                if (!exerciseMap.TryGetValue(key, out var exercise))
                {
                    exercise = new Models.Exercise
                    {
                        Name               = pe.ExerciseName,
                        IsCustom           = true,
                        DefaultRestSeconds = pe.RestSeconds,
                        MuscleGroup        = Models.MuscleGroup.Other
                    };
                    await SaveExerciseAsync(exercise);
                    exerciseMap[key] = exercise;
                }

                await SaveTemplateExerciseAsync(new Models.TemplateExercise
                {
                    TemplateId         = template.Id,
                    ExerciseId         = exercise.Id,
                    OrderIndex         = i,
                    Sets               = pe.Sets,
                    Reps               = pe.Reps,
                    DefaultRestSeconds = pe.RestSeconds
                });
            }
        }
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
                COALESCE(wt.Name, '')                                    AS TemplateName,
                ws.Notes,
                COUNT(ls.Id)                                             AS TotalSets,
                COALESCE(SUM(CAST(ls.WeightKg AS REAL) * ls.Reps), 0.0) AS TotalVolumeKg,
                COALESCE(SUM(ls.IsPR), 0)                                AS PRCount
            FROM WorkoutSessions ws
            LEFT JOIN WorkoutTemplates wt ON wt.Id = ws.TemplateId
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

    // ── Periodisering ──────────────────────────────────────────────────────────

    public async Task<List<TrainingCycle>> GetCyclesAsync()
    {
        await InitAsync();
        return await _db.Table<TrainingCycle>()
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<List<CycleWeek>> GetCycleWeeksAsync(int cycleId)
    {
        await InitAsync();
        return await _db.Table<CycleWeek>()
            .Where(w => w.CycleId == cycleId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync();
    }

    public async Task<List<CycleSession>> GetCycleSessionsAsync(int cycleWeekId)
    {
        await InitAsync();
        return await _db.Table<CycleSession>()
            .Where(s => s.CycleWeekId == cycleWeekId)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync();
    }

    public async Task<int> SaveCycleAsync(
        TrainingCycle cycle,
        List<CycleWeek> weeks,
        List<List<CycleSession>> sessionsByWeek)
    {
        await InitAsync();
        await _db.RunInTransactionAsync(conn =>
        {
            if (cycle.Id == 0)
                conn.Insert(cycle);
            else
                conn.Update(cycle);

            var oldWeeks = conn.Table<CycleWeek>()
                .Where(w => w.CycleId == cycle.Id).ToList();
            foreach (var w in oldWeeks)
            {
                conn.Table<CycleSession>().Delete(s => s.CycleWeekId == w.Id);
                conn.Delete(w);
            }

            for (int i = 0; i < weeks.Count; i++)
            {
                var week = weeks[i];
                week.CycleId = cycle.Id;
                conn.Insert(week);

                var sessions = sessionsByWeek[i];
                foreach (var s in sessions)
                {
                    s.CycleWeekId = week.Id;
                    conn.Insert(s);
                }
            }
        });
        return cycle.Id;
    }

    public async Task DeleteCycleAsync(int cycleId)
    {
        await InitAsync();
        await _db.RunInTransactionAsync(conn =>
        {
            var weeks = conn.Table<CycleWeek>()
                .Where(w => w.CycleId == cycleId).ToList();
            foreach (var w in weeks)
                conn.Table<CycleSession>().Delete(s => s.CycleWeekId == w.Id);
            conn.Table<CycleWeek>().Delete(w => w.CycleId == cycleId);
            conn.Table<TrainingCycle>().Delete(c => c.Id == cycleId);
        });
    }

    public async Task SetActiveCycleAsync(int cycleId)
    {
        await InitAsync();
        await _db.RunInTransactionAsync(conn =>
        {
            conn.Execute("UPDATE TrainingCycles SET IsActive = 0");
            conn.Execute("UPDATE TrainingCycles SET IsActive = 1 WHERE Id = ?", cycleId);
        });
    }
}
