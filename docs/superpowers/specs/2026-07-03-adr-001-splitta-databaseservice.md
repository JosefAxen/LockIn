# ADR-001: Splitta DatabaseService i repositories per aggregat

**Status:** Proposed
**Date:** 2026-07-03
**Deciders:** Josef Axén

---

## Context

Efter RP-full-klonen (juli 2026) har `LockIn/Services/DatabaseService.cs` vuxit till **1 579 rader** och **111 publika/privata metoder**. Den ansvarar för:

- Connection-ägande + `Lazy<Task>`-baserad initiering
- 30+ idempotenta migreringar (try-catch-mönstret)
- Seeding (`SeedAsync`) för 800+ övningar + template-bibliotek + muskel-landmarks
- CRUD för 13 SQLite-tabeller
- Aggregations- och analysfrågor (`GetRawVolumeForRangeAsync`, `GetNearestPRGapAsync`, `GetLastSessionSetsAsync`, `HasModerateJointPainForSessionAsync`)
- Diverse "hjälpar-queries" som inte hör hemma någon annanstans

**Fan-in:** 11 andra services + 12+ ViewModels beror direkt på `DatabaseService`, med **196 `_db.`-anrop** spridda över services-lagret.

### Krafter som spelar in

1. **Bygg-tid:** SQLite-init går som `Lazy<Task>` — snabb. Ingen brådska.
2. **Efficiency-review flaggade N+1**: `WeeklyVolumeService` gör 85+ DB-anrop per HemPage-load. `TemplateService.GetDetailAsync` gör foreach + await per dag + per slot. `HemViewModel` triggar 60+ round-trips via `SetProgressionEngine`. Grundorsak: repository-lagret saknar batch-metoder — och det finns ingen naturlig plats att lägga dem i en 1 500-radersfil.
3. **Testbarhet är noll**: `DatabaseService` går inte att mocka utan att bygga hela dependency-grafen. Vi har ingen testsvit idag, men om vi någonsin vill lägga en är detta blockerande.
4. **Kognitiv belastning**: Att hitta t.ex. `SaveTemplateWithChildrenAsync` kräver att man scrollar genom migreringar, seed, session-metoder, chip-analytics. Grep fungerar men mental model blir svag.
5. **RP-cloneens nya services** (`TemplateService`, `MesocycleService`, `VolumeProgressionService`) delegerar tunt till `DatabaseService` — de har naturliga ansvarsgränser som redan finns i abstraktionen, men de äger inte sitt eget persistens-lager.
6. **Cachning**: Inget cache-lager finns. `MesocycleService.GetPriorityAsync` laddar hela prioritets-listan från DB varje anrop.

**Icke-funktionella krav:**
- Får inte bryta befintlig funktionalitet
- Måste överleva TestFlight-cyklerna (dvs. inkrementellt införande)
- Ingen ny NuGet-dependency
- Kompatibel med .NET MAUI 10 AOT (iOS-krav)

---

## Decision

**Splitta `DatabaseService` i repositories per aggregat**, där varje repository äger CRUD + batch-hämtning + cache för sin egen tabell-familj. `DatabaseService` behålls som connection-owner + migrations-orkestrerare.

Nya repositories:

| Repository | Ansvar | Tabeller |
|------------|--------|----------|
| `IExerciseRepository` | Övningar + custom exercises | `Exercises` |
| `ITemplateRepository` | Templates + days + muscle slots + template exercises | `WorkoutTemplates`, `TemplateDays`, `TemplateMuscleSlots`, `TemplateExercises` |
| `IMesocycleRepository` | Mesocykler + weeks + priorities | `Mesocycles`, `MesocycleWeeks`, `MesocyclePriorities`, `MuscleVolumeLandmarks` |
| `ISessionRepository` | Sessioner + session-exercises + logged sets + feedback | `WorkoutSessions`, `SessionExercises`, `LoggedSets`, `SessionMuscleFeedback`, `SessionExerciseFeedback` |
| `ISettingsRepository` | AppSettings + achievements + hints | `AppSettings`, `UserAchievements` |
| `IBodyRepository` | Kroppsvikt, bodycomp, foton | `BodyWeightEntries`, `BodyCompositionEntries`, `WorkoutPhotos` |

`DatabaseService` reduceras till:
- `SQLiteAsyncConnection` connection-factory (singleton)
- `InitAsync()` som orkestrerar migrations via `IMigrationRunner`
- Ingen CRUD, inga aggregations-metoder

---

## Options Considered

### Option A: Full big-bang-split (allt-eller-inget)

Skriv om alla 6 repositories + `MigrationRunner` + `DatabaseService` som en enda feature-branch. Migrera alla 11+12 anrops-siter i samma PR.

| Dimension | Assessment |
|-----------|------------|
| Complexity | **Hög** — 6 nya interfaces, 6 impls, MigrationRunner, en gigantisk feature-branch |
| Cost | Uppskattat 3-5 dagars fokuserad implementation + review |
| Scalability | Full ren start — nya features hamnar direkt i rätt repository |
| Team familiarity | Mönstret är standard-Repository — inga nya koncept |
| Risk | **Hög** — hela appen står på en spik under branchens liv, hard att splitta bit-för-bit |

**Pros:**
- En consistent snapshot; ingen övergångsperiod med halvsplittad service
- Alla `_db.`-anrop försvinner ur services-lagret på en gång
- Kan lägga cache-lager direkt i alla repositories från början
- Möjlighet att också införa `UnitOfWork` om det behövs

**Cons:**
- Merge conflicts blir gigantiska mot andra samtidiga branches
- Regressioner svårare att lokalisera — en `git bisect` mellan pre/post-split ger ett massivt commit-fönster
- Vi har precis avslutat en 30-tasks-refaktor — team-energin är mättad
- Kräver att appen inte får nya features under implementationstiden
- Måste re-testa hela appen manuellt (ingen testsvit)

---

### Option B: Inkrementell split, en aggregat i taget ⭐ Rekommenderad

Splitta i 6 sekventiella PR:er, en per aggregat. Ordning enligt beroende-djup (låg först):

1. **`ISettingsRepository`** (isolerad, ingen dependency på andra)
2. **`IBodyRepository`** (isolerad)
3. **`IExerciseRepository`** (fundament för resten)
4. **`ITemplateRepository`** (beror på Exercise)
5. **`IMesocycleRepository`** (beror på Template)
6. **`ISessionRepository`** (störst, beror på allt ovan)
7. **Cleanup:** ta bort de nu-utflyttade metoderna ur `DatabaseService`, gör CRUD-metoderna `internal` och migrera bort residual-anrop

Under övergången samexisterar gamla `_db.GetXxxAsync()` med nya `_xxxRepo.GetXxxAsync()`. Nya services använder repos direkt; äldre services migreras när repository-PR:en för deras aggregat mergas.

| Dimension | Assessment |
|-----------|------------|
| Complexity | **Låg-medel** per PR (100-300 rader) — hanterbart utan risk |
| Cost | 6-8 PR:er över 2-3 veckor kalendertid |
| Scalability | Nya features under övergången kan hamna i "rätt" ställe efter typ av tabell |
| Team familiarity | Standard-mönster |
| Risk | **Låg** — varje PR kan reverteras enskilt utan att bryta något |

**Pros:**
- Varje PR har smal blast radius — enklare att testa manuellt på TestFlight mellan mergena
- Kan pausas mellan PR:er om andra prioriteringar dyker upp
- N+1-fixarna kan komma progressivt (fixa `TemplateRepository.GetDetailBatchAsync` i steg 4, `SessionRepository.GetSessionSummaryBatchAsync` i steg 6)
- Möjliggör en `IRepositoryCache`-abstraktion som införs stegvis

**Cons:**
- Under övergången finns två sätt att göra samma sak (`_db.GetXxx()` och `_repo.GetXxx()`) — förvirrar
- Migrations-koden ligger kvar i `DatabaseService` tills slutet, koncept-brytning
- Behöver disciplinlinje mot att skriva NY kod med `_db.`-mönstret under övergången
- Kalendertidsmässigt mer utdraget

---

### Option C: Läka utan splittring — bara lägg cache + batch-metoder i befintliga services

Behåll `DatabaseService` monolitisk. Fokusera på att ge `TemplateService`, `MesocycleService`, `WeeklyVolumeService` cache + batch-metoder. Ignorera repository-mönstret.

| Dimension | Assessment |
|-----------|------------|
| Complexity | **Låg** — bara nya metoder i befintliga filer |
| Cost | 1-2 dagar för de mest akuta N+1-hot spots |
| Scalability | Adresserar symtom, inte grundorsak. Nästa RP-liknande projekt börjar från samma läge. |
| Team familiarity | Inga nya koncept |
| Risk | **Mycket låg** — inga strukturella ändringar |

**Pros:**
- Snabbast till uppmätt förbättring (HemPage-load-tid, MesocycleStatsPage-load-tid)
- Kräver ingen konceptuell reorganisation
- Kan implementeras utan att röra `DatabaseService` alls (batch-metoder läggs där som separata publika metoder)

**Cons:**
- `DatabaseService` fortsätter växa — snart 2 000 rader
- Testbarhet förblir omöjlig
- Cache-lager blir ad-hoc per service, inte del av en generell arkitektur
- Nya tabeller/aggregat i framtiden kommer också i samma monolit
- Läggs stridsmoral: "vi rensade upp så mycket, varför inte det här?"

---

## Trade-off Analysis

**Snabbhet vs strukturell hälsa:** Option C är snabbast men konsoliderar tekniska skulden. Option A är strukturellt renast men kräver mest fokuserad tid. Option B ligger emellan — ger strukturvinsten men fördelar risken.

**Blast radius:** Option A riskerar en veckas app-instabilitet. Option B har varje PR som smal risk-yta. Option C ändrar ingenting i strukturen, låg risk men lämnar problemet.

**Cache-strategi:** Option A eller B skapar naturligt "aggregate-owner" som kan äga sitt egna cache-lager. Option C tvingar cache in i `DatabaseService` eller sprids ad-hoc i konsumerande services.

**Kognitiv börda:** Option B har den irriterande "två-vägar"-perioden. Om vi tidsätter det snävt (2-3 veckor kalendertid, disciplin mot nya `_db.`-anrop under tiden) är det acceptabelt.

**Utan testsvit:** Alla tre optionerna kräver manuell TestFlight-verifiering. Option B har fördelen att verifieringen kan göras per aggregat.

---

## Recommendation

**Option B — Inkrementell split, en aggregat i taget.**

Motivering:
1. **RP-clonen precis avslutad** — team-energi mättad. Inkrementellt matchar det verkliga tempot bättre än big-bang.
2. **Ingen testsvit** — små PR:er = enkel manuell verifiering.
3. **N+1-fixar följer naturligt** — batch-metoder läggs där de hör hemma när repository byggs, inte som separata patches.
4. **Kan pauseras** — om ett feature-behov dyker upp mitten i, kan vi ta paus utan att lämna appen halv-splittad.
5. **Learning-effekt** — första repository:t (Settings) blir en mall som gör resten snabbare.

Föreslagen kalender:
- **Vecka 1:** SettingsRepo + BodyRepo (isolerade, låg risk)
- **Vecka 2:** ExerciseRepo + TemplateRepo (nu introduceras cache-mönstret)
- **Vecka 3:** MesocycleRepo + SessionRepo (störst, adressera N+1)
- **Vecka 4:** Cleanup — förutbestämt att alla utflyttade metoder ur `DatabaseService` görs `internal` eller raderas

---

## Consequences

**Blir enklare:**
- Lägga cache per aggregat utan att röra kod som inte är relevant
- Batch-metoder får en naturlig hemvist (`SessionRepository.GetSessionSummariesBatchAsync` istället för att slänga en till metod på DatabaseService)
- Framtida testsvit blir realistisk att införa — mocka `ITemplateRepository` istället för hela `DatabaseService`
- Nya features (t.ex. sync med server) kan implementeras genom att lägga till en `ISyncingRepository`-decorator utan att röra domain-services
- Läsa och förstå persistence — 6 filer à 200-400 rader istället för 1 fil à 1 579 rader

**Blir svårare:**
- Migrations-koden behöver hitta en ny hemvist (`IMigrationRunner` i steg 7); tills dess ligger den kvar i `DatabaseService`
- "Två sätt att göra samma sak"-perioden under 2-3 veckor
- Nya utvecklare måste förstå distinktionen repository (persistence) vs service (domain logic)

**Behöver revisiteras:**
- Om vi vill införa server-sync framöver: repositories är förberedda men Unit-of-Work-mönstret kan behövas då
- Cache-invalidering vid write-operationer — enkel version i första PR:erna, kan generaliseras när mönstret stabiliserats
- Om testsvit någonsin införs: `IRepositoryTestDouble` eller in-memory-implementationer

---

## Action Items

- [ ] **PR 1: SettingsRepository** — skapa `ISettingsRepository`, flytta `GetAppSettingsAsync`, `SaveAppSettingsAsync`, `HasSeenXyzHint`-relaterade metoder. Uppdatera 3-4 anrops-siter. Verifiera på simulator (onboarding + settings-flöde).
- [ ] **PR 2: BodyRepository** — flytta body weight/composition/photos-CRUD. Anrops-siter: `BodyWeightViewModel`, `KroppViewModel`, `ProgressPhotosViewModel`.
- [ ] **PR 3: ExerciseRepository** — inkl. custom-exercise-flöde. Anrops-siter: `ExercisePickerViewModel`, `ExerciseRecommendationService`, `ExerciseProgressViewModel`, `CreateExerciseViewModel`, `TemplatePlanningBoardViewModel`.
- [ ] **PR 4: TemplateRepository** — flytta `GetTemplatesAsync/*`-familj + batch-metod `GetDetailBatchAsync` (löser efficiency-finding #8). Anrops-siter: `TemplateService`, `LibraryViewModel`, `ActiveWorkoutViewModel`, `MesoBuilderServiceV2`.
- [ ] **PR 5: MesocycleRepository** — flytta mesocycle-familj + batch prio-lookup (löser efficiency-finding #3). Anrops-siter: `MesocycleService`, `CycleContextService`, `VolumeProgressionService`.
- [ ] **PR 6: SessionRepository** — flytta session/set/feedback CRUD + batch session-summary + `HasModerateJointPainForSessionBatchAsync` (löser efficiency-finding #5). Anrops-siter: `SetProgressionEngine`, `WeeklyVolumeService`, `HemViewModel`, `ActiveWorkoutViewModel`, `PostWorkoutViewModel`, `HistoryViewModel`.
- [ ] **PR 7: MigrationRunner + DatabaseService cleanup** — `IMigrationRunner` äger try-catch-migrationerna. `DatabaseService` reduceras till connection-owner + init-orkestrerare. Radera de utflyttade CRUD-metoderna. Verifiera att kvarvarande yta är minimal.
- [ ] **Follow-up ADR-004: Cache-strategi per repository** — när mönstret stabiliserats, standardisera cache-invalidering + TTL-policy.

---

## Länk till relaterat

- Efficiency-review-findings som denna ADR delvis adresserar: `.superpowers/sdd/simplify-efficiency.md`
- ADR-002 (`WorkoutSessionCoordinator`-splittring) — bör invänta ADR-001 så coordinator kan bero på nya repositories direkt istället för att först bero på `DatabaseService` och sedan migreras.
