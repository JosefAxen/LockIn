# Vana Lift – Funktionsöversikt
_Senast uppdaterad: 2026-06-27_

---

## Status idag

### HemPage (Dashboard)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Strain-ring | TRIMP-baserad belastning från HR-zoner (Karvonen), fryst per morgon | HealthKit HR-samples, `AppSettings.MorningRecoveryPct` | ✅ |
| Recovery-ring | HRV 55% + RHR 20% + sömn 15% + ACWR 10% | HealthKit HRV, RestingHR, sömn, `WorkoutSessions` | ✅ |
| Sleep-ring + stages | Sömntimmar + uppdelning Core/Deep/REM/Awake | HealthKit SleepAnalysis | ✅ |
| WeeklyGoalGauge | Cirkulär gauge 0–1 för veckomål | `AppSettings.WeeklyGoal`, `WorkoutSessions` | ✅ |
| SparklineView | Fyra minigraf-trender (steg, kalorier, aktivitet, puls) | HealthKit 7-dagars arrays | ✅ |
| Streak-tracker | Eldikoner per veckodag, dagstreak-counter | `WorkoutSessions` | ✅ |
| Muskelheatmap | Volym per muskelgrupp senaste 7 dagar som färgintensitet | `DatabaseService.GetMuscleScoresAsync` | ✅ |
| Rekommendationsmotor | 8 scenarios (deload, rest, ökad volym etc.) med motiveringstext | Beräknas i `HemViewModel.LoadAsync` | ✅ |
| Coach-prompts | Lista med 3 föreslagna frågor | Statiska strängar i ViewModel, ej renderade i XAML | 🔴 Ingen AI-koppling – ska förbättras |
| "Aktivt pass"-banner | Länk till pågående pass | `ActiveWorkoutStateService.IsActive` | ✅ |

---

### TrainPage (Starta pass)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Program-lista | Expanderbara program med dagar och mallar | `WorkoutPrograms.All`, `WorkoutTemplates` | ✅ |
| Fria mallar | Lista med användarens egna mallar, swipe-to-delete | `WorkoutTemplates` | ✅ |
| Muskelpoängsbalk | Horisontell bar per muskelgrupp för veckans volym | `DatabaseService.GetMuscleScoresAsync` | ✅ |
| Deload-banner | Visas vid 3+ veckors avtagande volym eller hög kronisk belastning | `GetWeeklyVolumeTrendAsync` | ✅ |
| Restday-popup | Popup efter 7+ dagars streak (max 1 gång/vecka) | `GetCurrentStreakAsync` | ✅ |
| Fritt pass (FAB) | Starta pass utan mall | Skapar tom session | ✅ |

---

### HistoryPage (Träningshistorik)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Kalendervy | Månadskalender med markerade tränade dagar | `GetTrainedDaysInMonthAsync` | ✅ |
| Månadsnavigering | Pil-knappar navigerar en månad åt gången | ViewModel + code-behind | 🟡 Kalenderlogik i code-behind (ej ren MVVM) |
| Periodfilter | Alla / Vecka / Månad | `WorkoutSessions` | ✅ |
| Sortering | Datum / Volym | In-memory sort | ✅ |
| Sessionslista | Paginerad lista (5 st per laddning) | `GetCompletedSessionsAsync` | ✅ |
| Achievements-genväg | Knapp till AchievementsPage | Navigation | ✅ |

---

### LibraryPage (Övningar, mallar, program)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Övningssök | Debounced (300ms) fritextsök | `Exercises` | ✅ |
| Muskelgrupp-filter | Chip-filter (single-select) | `Exercises.MuscleGroup` | ✅ |
| Utrustning-filter | Chip-filter (single-select) | `Exercises.Equipment` | ✅ |
| Övningskort | Namn, muskelgrupp, utrustning | `Exercises` | ✅ |
| Mallhantering | Skapa, redigera, radera mallar | `WorkoutTemplates` | ✅ |
| Programlista | 6 färdiga program | `WorkoutPrograms.All` (statisk) | ✅ |
| Lazy group-pagination | Laddar en muskelgrupp i taget för prestanda | In-memory | ✅ |
| Diff-baserad CollectionView-synk | Undviker Clear() för att bevara tangentbordsfokus | ViewModel-logik | ✅ |

---

### KroppPage (Kroppsmätningar)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Viktloggning (overview) | Senaste vikt + trend, top-15 historik | `BodyWeightEntries` | ✅ |
| Viktgraf | LineChartView för vikthistorik | `BodyWeightEntries` | ✅ |
| Kroppsmätningar | Loggning av midja/bröst/höft/armar/lår (5 prompts i sekvens) | `BodyCompositionEntries` | ✅ |
| Muskelheatmap | Volym per muskelgrupp som färgkarta i code-behind | `GetMuscleScoresAsync` | 🟡 Heatmap-logik i code-behind, `HeatmapTiles` är vanlig `List<>` |
| Genväg till BodyWeightPage | Navigerar till dedikerad viktloggningssida | Navigation | ✅ |
| Genväg till ProgressPhotosPage | Navigerar till framstegsfotosida | Navigation | ✅ |

---

### ActiveWorkoutPage (Aktivt pass)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Set-loggning | Entry för vikt + reps per set, RIR (0–4) | `LoggedSets` | ✅ |
| Set-typer | Normal, Warmup, Time, Dropset | `SetType` enum | ✅ |
| Restimer | Deadline-baserad countdown, korrekt efter iOS app-suspend | `RestTimerService` | ✅ |
| Restimer-notis | Push-notis när vilotimern löper ut | `NotificationService` (iOS UNNotification) | ✅ |
| PR-detection | Jämförelse mot bästa Epley-1RM för övningen | `PRService`, `LoggedSets` | ✅ |
| PR-banner + konfetti | Slide-in banner + `ConfettiView`-animation | `ActiveWorkoutViewModel.PRScored` event | ✅ |
| Auto-progression | Förslag om vikthöjning när reps-mål nås | `TemplateExercise.AutoProgressMode` | ✅ |
| Superset-stöd | Round-tracking, auto-scroll till nästa övning i supersetgrupp | `SupersetGroupId` på `SessionExercise` | ✅ |
| Lägg till övning | Öppnar ExercisePickerPage under pågående pass | Navigation + callback | ✅ |
| Plattekalkylator-genväg | Knapp till PlateCalculatorPage | Navigation | ✅ |
| Singleton-VM | Överlever tab-navigering | `ActiveWorkoutViewModel` registrerad som Singleton | ✅ |
| Crash-recovery | Orphan-session rensas upp vid nästa app-start | `ForceDeactivate()` + session-check vid init | ✅ |

---

### PostWorkoutPage (Sammanfattning)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Pass-statistik | Total volym, sets, tid, antal PBs | `LoggedSets`, `WorkoutSession` | ✅ |
| Muskelgrupp-diagram | Horisontellt balkdiagram per muskelgrupp | `GetSessionVolumeByMuscleGroupAsync` | ✅ |
| PR-lista | Lista med nya personliga rekord denna session | `GetPRsForSessionAsync` | ✅ |
| Achievements | Visar nya achievements unlockade i detta pass (20 triggers) | `UserAchievements`, `AchievementService` | ✅ |
| Konfetti-animation | `ConfettiView` vid nya achievements | `ConfettiView` | ✅ |
| Passnotes | Fritext-anteckning sparas till `WorkoutSession.Notes` | `WorkoutSessions` | ✅ |
| Foto-tillägg | Kamera + fotobibliotek, sparas med session-id | `WorkoutPhotos`, `PhotoService` | ✅ |
| HealthKit-sync | Sparar HKWorkout (TraditionalStrengthTraining) med energi | HealthKit | ✅ Sparar riktigt HKWorkout-objekt, syns under Träning i Apple Health |

---

### SessionDetailPage (Historisk session)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Set-data per övning | Tabellvy vikt × reps per set | `LoggedSets` via `GetSessionExerciseDetailsAsync` | ✅ |
| Passnotes | Redigeras och sparas direkt på sidan | `WorkoutSession.Notes` | ✅ Redigerbart via Editor + SaveNotesCommand |
| Foton | Grid med sessionens foton | `WorkoutPhotos` | ✅ |
| Foto-tillägg i efterhand | Lägg till / ta bort foton från historisk session | `WorkoutPhotos`, `PhotoService` | ✅ |

---

### ExercisePickerPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Sök + muskelgrupp + utrustning-filter | Samma logik som LibraryPage | `Exercises` | ✅ |
| Callback-pattern | Returnerar vald övning via `Action<Exercise>` | In-memory | ✅ |
| Skapa ny övning | Navigerar till CreateExercisePage | Navigation | ✅ |

---

### TemplateEditPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Skapa/redigera mall | Namn + övningslista med sets/reps/vikt/vila | `WorkoutTemplates`, `TemplateExercises` | ✅ |
| Auto-progression per övning | Reps-intervall + viktinkrement | `TemplateExercise.AutoProgressMode` | ✅ |
| Superset-toggling | Auto-genererade grupp-ID | `TemplateExercise.SupersetGroupId` | ✅ |
| Spara | Delete + re-insert via `ReplaceTemplateExercisesAsync` | `TemplateExercises` | ✅ |

---

### ExerciseProgressPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| 1RM-progressionsgraf | Epley-baserad, senaste 12 sessioner | `GetBestSetPerSessionForExerciseAsync` | ✅ |
| Bästa lyft (hero-kort) | Max Epley-1RM med datum | `GetMaxEpley1RMAsync` | ✅ |
| Metadata | Utrustning, sekundärmuskel, svårighetsgrad, mekanik, krafttyp | `Exercise`-modellen | ✅ |
| Redigerbara notes | Sparas till `Exercise.Notes` | `Exercises` | ✅ |
| Sessionsräknare | Antal gånger övningen genomförts | `GetSessionCountThisWeekAsync` (möjlig felkälla, se nedan) | 🟡 |

---

### CreateExercisePage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Ny anpassad övning | Namn, muskelgrupp, utrustning, vilotid, notes | `Exercises` | ✅ |
| Navigationshantening | Navigerar `../..` om skapad från aktivt pass, annars `..` | `ActiveWorkoutStateService` | ✅ |

---

### ProgramDetailPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Programöversikt | Dagslista med övningar expanderade | `WorkoutPrograms.All` (statisk) | ✅ |
| Aktivera program | Skapar `WorkoutTemplates` + `TemplateExercises` i databasen | `db.ActivateProgramAsync` | ✅ |
| Lokaliserade beskrivningar | `Program_{id}_Description` från AppResources | `AppResources` | ✅ |

---

### SettingsPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Profilnamn | Redigeras via DisplayPrompt | `AppSettings.UserName` | ✅ |
| Veckomål | Stepper 1–7 | `AppSettings.WeeklyGoal` | ✅ |
| Viktsenhet | kg / lbs toggle | `AppSettings.WeightUnit` | ✅ |
| Haptic feedback | On/off | `Preferences` | ✅ |
| Ljud | On/off | `Preferences` | ✅ |
| HealthKit-sync | Toggle + begär behörigheter | `Preferences`, `HealthKitService.RequestPermissionsAsync` | ✅ |
| Rensa all data | Destructive med bekräftelse, re-seedar DB | `db.DeleteAllDataAsync` | ✅ |
| App-version | Statisk label | Compile-time konstant | ✅ |

---

### BodyWeightPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Logga vikt | DisplayPrompt + spara | `BodyWeightEntries` | ✅ |
| Viktgraf | LineChartView med full historik | `BodyWeightEntries` | ✅ |
| Paginerad historik | 10 poster + "Ladda fler" | `BodyWeightEntries` | ✅ |
| Radera post (swipe) | `DeleteBodyWeightEntryAsync` | `BodyWeightEntries` | ✅ |

---

### AchievementsPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| 20 achievements | 2-kolumns grid, upplåsta vs låsta | `UserAchievements`, `AchievementService.All` | ✅ |
| Visuell distinktion | Dämpad opacity och grå bakgrund för låsta | In-memory beräkning | ✅ |
| Triggers | 20 triggers implementerade i PostWorkoutViewModel | `UserAchievements` | ✅ Alla 20 achievements har triggers |

---

### PlateCalculatorPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Viktskiv-beräkning | Greedy algoritm, 25/20/15/10/5/2.5/1.25 kg-skivor | Lokal beräkning | ✅ |
| BarbellDrawable | Ritar stång med skivor via `IDrawable` + `GraphicsView` | Lokal beräkning | ✅ |
| Anpassad stångvikt | Default 20 kg, går att ändra | Lokal state | ✅ |

---

### ProgressPhotosPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Grupperad lista per månad | `GroupBy` på `TakenAt` | `WorkoutPhotos` | ✅ |
| Lägg till foto (kamera/galleri) | Sparar till app-katalog + DB | `WorkoutPhotos`, `PhotoService` | ✅ |
| Radera foto | Synkar flat + grupperad collection, tar bort tom månad | `WorkoutPhotos` | ✅ |

---

### OnboardingPage
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| 5-stegs flöde | Namn → Veckomål → Erfarenhet → Mål → Programrekommendation | `AppSettings`, `WorkoutPrograms` | ✅ |
| Auto-advance | 220ms fördröjning efter val på steg 1–3 | Animation i code-behind | ✅ |
| Programrekommendation | Matris: erfarenhet × mål → 6 program-ID:n | `WorkoutPrograms.All` | ✅ |
| Hoppa över | Confirm-dialog, sparar ändå insamlad data | `AppSettings` | ✅ |

---

### HealthKitService (iOS-plattform)
| Funktion | Beskrivning | Data | Status |
|----------|-------------|------|--------|
| Steg | Dagens + 7-dagars array | HealthKit StepCount | ✅ |
| Aktiva kalorier | Dagens + 7-dagars array | HealthKit ActiveEnergyBurned | ✅ |
| Maxpuls | Dagens + 7-dagars + 90d estimat för HRmax | HealthKit HeartRate | ✅ |
| HRV | 14d baseline + idag-fallback (senaste 7d) | HealthKit SDNN | ✅ |
| Vilopuls | 14d baseline + idag-fallback | HealthKit RestingHR | ✅ |
| Sömnstages | Core/Deep/REM/Awake mappning | HealthKit SleepAnalysis | ✅ |
| Spara träningspass | Sparar `HKWorkout` (TraditionalStrengthTraining) med energi | HealthKit write | ✅ Syns under "Träning" i Apple Health |
| VO2Max | Saknas | HealthKit VO2Max | 🔴 Ej implementerat |
| Vikt-sync | Saknas | HealthKit BodyMass | 🔴 Ej implementerat |

---

### AchievementService + triggers
| Funktion | Beskrivning | Status |
|----------|-------------|--------|
| 20 achievements definierade | Emoji, titel, beskrivning lokaliserade | ✅ |
| 20 triggers implementerade | Triggning i `PostWorkoutViewModel.LoadAsync` | ✅ |
| "Morgonfågel" (pass innan 07:00) | Definierad | ✅ Trigger via `session.StartedAt.Hour < 7` |
| "Nattuggla" (pass efter 21:00) | Definierad | ✅ Trigger via `session.StartedAt.Hour >= 21` |
| Volym-milstolpar (100 000 / 500 000 / 1 000 000 kg totalt) | Definierad | ✅ Triggers via `GetTotalVolumeAsync` |
| "Fullständig" (alla muskelgrupper samma vecka) | Trigger via `GetAllMuscleGroupsThisWeekAsync` | ✅ |

---

### Lokalisering (i18n)
| Funktion | Beskrivning | Status |
|----------|-------------|--------|
| ~350+ nycklar | Svenska (default) + engelska | ✅ |
| `WorkoutTemplate.LastUsedText` | Hårdkodad svenska ("ALDRIG GJORT", "SENAST IDAG" etc.) | 🔴 Ej lokaliserad |
| `BodyCompositionEntry.Summary` | Hårdkodat format ("M:80 · B:100 cm") | 🔴 Ej lokaliserad |

---

## Backlog – saknade funktioner

### Kärnträning
- [x] **Redigera anteckningar i efterhand** (S) – `SessionDetailPage` har nu Editor + `SaveNotesCommand` via `DatabaseService.SaveSessionAsync`.  
  Prioritet: **Hög**

- [x] **Timerset med sekundinmatning** (S) – `SetType.Time`, ⏱-label, "SEK"-placeholder och `DurationSeconds`-lagring var redan implementerade. Fixat: `DurationSeconds` från föregående session används nu som hint (tidigare visades alltid "SEK"), och auto-fill gäller nu även Time-set vid completing.  
  Prioritet: **Hög**

- [x] **Värme-set-visualisering** (S) – `SessionExerciseDetailRow` fick `SetLabel`/`SetLabelColor`/`WeightDisplay`/`RepsDisplay`. SessionDetailPage visar nu "W" (amber), "↓" (rosa), "⏱" (blå) för respektive set-typ. Time-set visar DurationSeconds i sekunder istället för 0. Volymen i PostWorkoutPage exkluderade redan warmup-set via SQL-filtret.  
  Prioritet: **Medel**

- [~] **Snabb-logg utan mall** (M) – Skippat — ej önskad funktion.  
  Prioritet: **Låg**

- [x] **Cardio-loggning** (M) – Logga löpning, cykling etc. med distans, tid och genomsnittspuls. Koppla mot HealthKit workout-typer.  
  _Beroenden: Ny tabell `CardioSessions`, ny vy, `HKWorkoutActivityType`._  
  Prioritet: **Låg**

- [x] **Dropset- och supermax-UI** (S) – När ett set cyklas till Dropset auto-föreslår ViewModel 80% av föregående sets vikt (avrundat till närmaste 2.5 kg). Set-label-kolumnen får en subtil rosa bakgrundston via `RowAccentColor` på `LoggedSetRow`. `IsDropset`/`RowAccentColor` notifieras via `[NotifyPropertyChangedFor]` vid `SetType`-ändringar.  
  Prioritet: **Medel**

---

### Planering & program
- [x] **Hypertrofi-program** (M) – Lade till PHUL (4 dagar), PHAT (5 dagar) och Arnold Split (3-split × 2) i `WorkoutPrograms.All` med svenska och engelska i18n-nycklar. Totalt 9 program.  
  Prioritet: **Hög**

- [ ] **Anpassningsbar programlängd** (L) – Användaren kan inte ändra antal set/reps i ett inbyggt program utan att skapa en kopia. Lägg till "Anpassa program"-flöde.  
  _Beroenden: Klona-funktion i `db.ActivateProgramAsync`, ny UI._  
  Prioritet: **Medel**

- [ ] **Periodiseringsplaner** (L) – Planera träning vecka för vecka med varierade intensiteter (mesocykler). Krävs för att matcha Strong/Whoop på planering.  
  _Beroenden: Ny tabell `TrainingCycles`, ny vy, komplex VM-logik._  
  Prioritet: **Låg**

- [ ] **Vila-dag-schemaläggning** (S) – Påminnelsenotiser för specifika träningsdagar (t.ex. "Måndag är bent dag").  
  _Beroenden: `NotificationService` utökning, `AppSettings`-fält._  
  Prioritet: **Medel**

- [x] **Mallkopiering** (S) – Kopiera en befintlig mall som bas för en ny. Saknas idag — användaren måste bygga från scratch.  
  _Beroenden: Ny DB-metod, enkel menyknapp i `LibraryPage`._  
  Prioritet: **Medel**

---

### Analys & insikter
- [x] **Volymtrend per muskelgrupp** (M) – Horisontell scrollbar med sparkline-kort per muskelgrupp (senaste 4 veckor) läggs till på HemPage under heatmapen. `GetWeeklyVolumeByMuscleGroupAsync(4)` + `MuscleTrendItem` + `DesignTokens.MuscleColor()`.  
  Prioritet: **Hög**

- [x] **Träningsfrekvens-analys** (S) – Genomsnittlig frekvens per muskelgrupp (senaste 4 veckor) visas som "X/v"-label under scoret i varje heatmap-tile på HemPage. `DatabaseService.GetMuscleFrequencyAsync(4)` + `HeatmapTile.FrequencyText`.  
  Prioritet: **Hög**

- [ ] **VO2Max-estimat** (M) – HealthKit exponerar VO2Max för Apple Watch-användare. Kan visas på HemPage som konditionsmått.  
  _Beroenden: Ny HealthKit-permission, ny `IHealthService`-metod._  
  Prioritet: **Medel**

- [ ] **Vikt-trend och BMI** (S) – Beräkna BMI eller trendriktning (viktnedgång/uppgång) baserat på `BodyWeightEntries`. Appen har all data men visar bara råvärden.  
  _Beroenden: Beräkning i `KroppViewModel` eller `BodyWeightViewModel`._  
  Prioritet: **Medel**

- [ ] **Jämförelse sessionvis** (M) – Visa förra gångens set/vikt bredvid aktuell session i ActiveWorkoutPage (hints visas redan som `PrevWeightHint`/`PrevRepsHint` i `LoggedSetRow`, men ingen "förra sessionen"-summering).  
  _Beroenden: Utökning av `GetLastSessionSetsAsync`, ny UI-sektion._  
  Prioritet: **Medel**

- [x] **Exportera data** (M) – ZIP med sessions.csv + sets.csv via `ExportService`, `Share`-API i `SettingsViewModel`, exportrad i `SettingsPage`. Se specs/2026-06-27-export-data-design.md.  
  Prioritet: **Hög**

- [ ] **Körjournal / progressrapport** (L) – PDF eller delningsbar bild med N veckors progression per övning.  
  _Beroenden: SkiaSharp PDF-rendering eller screenshot-delning._  
  Prioritet: **Låg**

---

### Engagemang & vanor
- [x] **Saknade achievement-triggers** (S) – Alla 20 triggers finns i `PostWorkoutViewModel.CheckAchievementsAsync`. EarlyBird, NightOwl och volym-milstolpar var redan implementerade.  
  Prioritet: **Hög**

- [x] **HKWorkout-typ vid HealthKit-sync** (S) – `HealthKitService.SaveWorkoutAsync` sparar nu `HKWorkout` (TraditionalStrengthTraining) med `totalEnergyBurned`. Write-permission för workout-typen tillagd i `s_writeTypes`.  
  Prioritet: **Hög**

- [ ] **Vikt-sync med Apple Health** (S) – Läs och skriv kroppsvikt via HealthKit. Undviker dubbelregistrering och speglar vad Apple Watch mäter.  
  _Beroenden: Ny HealthKit-permission `HKQuantityType.BodyMass`, ny `IHealthService`-metod._  
  Prioritet: **Medel**

- [ ] **Widget (iOS 18 Live Activity / App Intent)** (L) – Visa pågående restimer eller dagens träningsstatus på hemskärm/Dynamic Island.  
  _Beroenden: Ny iOS Extension-target, `ActivityKit`._  
  Prioritet: **Låg**

- [ ] **Social delning** (M) – Dela passammanfattning som bild (SkiaSharp-render av PostWorkoutPage-statistik).  
  _Beroenden: SkiaSharp off-screen rendering, `Share`-API._  
  Prioritet: **Låg**

- [ ] **Träningspåminnelser** (S) – Schemalagda notiser för träningsdagar (t.ex. "Dags att träna! Benen väntar.").  
  _Beroenden: `NotificationService` utökning, `AppSettings`-fält för dagar + tid._  
  Prioritet: **Medel**

---

### Onboarding & UX
- [x] **Redigerbara sessionsnotes** (S) – Se "Kärnträning" ovan — implementerat med Editor + `SaveNotesCommand`.  
  Prioritet: **Hög**

- [ ] **Töm en enskild övning ur mall** (S) – I `TemplateEditPage` kan användaren ta bort en övning, men det finns inget sätt att snabbt nollställa sets/reps till template-standard om man editerat manuellt.  
  _Beroenden: Minimal UI-ändring._  
  Prioritet: **Låg**

- [ ] **Ångra "avsluta pass"** (M) – Om användaren råkar trycka "avsluta" finns ingen ångra-funktion. Passet sparas men går inte att återuppta.  
  _Beroenden: Sessionsstatus-fält eller "osparad"-state i `ActiveWorkoutViewModel`._  
  Prioritet: **Medel**

- [x] **Omedelbar körguide för övning** (S) – `ShowExerciseInfoAsync` visar övningsbeskrivning via `DisplayAlert` i ActiveWorkoutPage.  
  Prioritet: **Hög**

- [ ] **Onboarding för HealthKit** (S) – HealthKit-behörighet begärs vid toggle i Settings, inte vid förstakörning. Användare missar strain/recovery-data tills de hittar inställningen.  
  _Beroenden: Lägg till HealthKit-steg i `OnboardingPage` (steg 6) eller en contextual prompt på HemPage._  
  Prioritet: **Medel**

- [ ] **Intuitiv coach-prompt-integration** (M) – `HemViewModel.CoachPrompts` har 3 frågesträngar men ingen AI-koppling och inget XAML-element ännu. Ska förbättras med riktig integration.  
  _Beroenden: API-nyckel, nätverksanrop, XAML-UI på HemPage._  
  Prioritet: **Låg**

---

### Data & sync
- [ ] **iCloud-backup / -synk** (L) – Träningsdatan lever lokalt i SQLite utan backup. Användare som byter telefon förlorar all historik.  
  _Beroenden: CloudKit-integration eller iCloud Documents-sökväg för SQLite-filen. Kräver entitlement `com.apple.developer.icloud-container-identifiers`._  
  Prioritet: **Hög**

- [x] **Exportera träningsdata** (M) – Se "Analys & insikter" ovan. Implementerat.  
  Prioritet: **Hög**

- [ ] **Importera från Strong / Hevy** (L) – CSV-import av historiska pass. Höjer switchingkostnaden för konkurrenternas användare.  
  _Beroenden: CSV-parser, mappning till `Exercise`-modell, import-vy._  
  Prioritet: **Medel**

- [ ] **Vikt-sync med Apple Health** (S) – Se "Engagemang & vanor" ovan.  
  _Beroenden: HealthKit write-permission._  
  Prioritet: **Medel**

---

### Monetisering
- [ ] **Freemium-gräns** (M) – Appen har ingen betalvägg. Definiera ett gratis-tier (t.ex. max 3 mallar, 1 program) och ett premium-tier utan gränser.  
  _Beroenden: `StoreKit 2` (In-App Purchase), `AppSettings.IsPremium`-fält, gating-logik i VM:ar._  
  Prioritet: **Hög**

- [ ] **Prenumerationsmodell eller engångsköp** (M) – Välj monetiseringsmodell innan App Store-release. Prenumeration ger återkommande intäkter; engångsköp är enklare att kommunicera.  
  _Beroenden: StoreKit 2, App Store Connect-konfiguration._  
  Prioritet: **Hög**

- [ ] **Gratis provperiod** (S) – Om prenumeration väljs: 7 eller 14 dagars full tillgång utan betaluppgifter (stöds av StoreKit 2 introductory offers).  
  _Beroenden: StoreKit 2 offer-konfiguration._  
  Prioritet: **Medel**

- [ ] **Paywalled premium-innehåll** (M) – Avancerade analyser (volymtrend, frekvensanalys, exportfunktion) och ytterligare program kan låsas bakom premium för att motivera uppgradering.  
  _Beroenden: Freemium-gräns implementerad._  
  Prioritet: **Medel**

---

## Next 5

De fem funktioner som bör byggas härnäst, i prioriteringsordning för en ensam utvecklare med sikte på publik App Store-release.

### 1. iCloud-backup (Hög, L)
**Motivering:** Utan backup kan en enhetsbyte eller oavsiktlig dataradering (*Rensa all data*-knappen) utplåna all träningshistorik. Det är den enda kategorin av buggar som leder till 1-stjärniga recensioner omedelbart efter launch. Implementera via iCloud Documents — flytta SQLite-filen till `NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.DocumentDirectory, ...)` under en iCloud-container-entitlement. Inget CloudKit-schema behövs.

### 2. ~~HKWorkout-typ vid HealthKit-sync~~ ✅ Klart
**Motivering:** `SaveWorkoutAsync` sparar nu ett riktigt `HKWorkout`-objekt (TraditionalStrengthTraining) med `totalEnergyBurned`. Workout-write-permission tillagd i `s_writeTypes`. Passet syns nu under "Träning" i Apple Health. Vikt-sync mot HealthKit återstår (se backlog).

### 3. ~~Saknade achievement-triggers~~ ✅ Klart
**Motivering:** Alla 20 triggers var redan implementerade i `PostWorkoutViewModel.CheckAchievementsAsync`. FEATURES.md var inaktuell.

### 4. ~~Redigerbara sessionsnotes + ShowExerciseInfo~~ ✅ Klart
**Motivering:** Båda implementerade. `SaveNotesCommand` sparar anteckningar via `DatabaseService.SaveSessionAsync`; `ShowExerciseInfoAsync` visar övningsbeskrivning via `DisplayAlert`.

### 5. Exportera träningsdata (Hög, M)
**Motivering:** Dataportering är ett App Store-krav i EU (Digital Markets Act) och ett standardkrav i App Review guidelines för appar som lagrar persondata. CSV-export av `WorkoutSessions` + `LoggedSets` via `Share`-API tar en dag att implementera och skyddar mot App Review-avslag. Det ger också ett konkret svar på den vanligaste onboarding-tveksamheten: "Vad händer med min data?".
