# LockIn — Internationalisering till engelska

**Status:** Designspec
**Datum:** 2026-06-24
**Mål:** Stötta engelska som ytterligare språk i LockIn iOS-appen, automatiskt val baserat på iOS-systemspråk.

---

## Sammanfattning

LockIn är idag helt på svenska. Detta dokument specificerar hur appen ska översättas till engelska som ett andra språk, med iOS-systemlocale som källa. Inga in-app språkväxlar.

**Scope-gräns:** övningsnamn (Bänkpress, Marklyft, Knäböj etc) översätts INTE — de förblir svenska oavsett enhetens språk. Allt annat UI, statisk data (achievements), och notiser översätts.

**Implementation:** En spec, tre separat-leveransbara plans (infrastruktur → systematisk UI-översättning → statisk data).

---

## 1. Arkitektur

### 1.1 Resurssystem

Standard .NET `.resx`-mönster. Inga tredjepartspaket.

```
LockIn/Resources/Strings/
├── AppResources.resx       # Defaultspråk — svenska
└── AppResources.en.resx    # Engelska
```

**Fallback:** `ResourceManager` faller automatiskt tillbaka från `en.resx` → `AppResources.resx` om en nyckel saknas. Detta är inbyggt och kräver ingen kod.

**Autogenererad klass:** `LockIn.Resources.Strings.AppResources` får statiska properties för varje nyckel (`AppResources.Library_Title`).

### 1.2 Markup Extension för XAML

För att slippa skriva `{Binding Source={x:Static r:AppResources.Foo}}` överallt skapas en `LocalizeExtension`:

```csharp
// LockIn/Resources/Strings/LocalizeExtension.cs
[ContentProperty(nameof(Key))]
public class LocalizeExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider)
        => string.IsNullOrEmpty(Key)
            ? string.Empty
            : AppResources.ResourceManager.GetString(Key, AppResources.Culture) ?? Key;

    object IMarkupExtension.ProvideValue(IServiceProvider sp) => ProvideValue(sp);
}
```

**XAML-användning:**
```xml
xmlns:loc="clr-namespace:LockIn.Resources.Strings"

<Label Text="{loc:Localize Library_Title}"/>
```

Returnerar nyckelnamnet bokstavligt om nyckeln saknas helt — gör utvecklarmissar synliga utan att krascha.

### 1.3 ViewModels och C#-kod

Direkt referens till autogenererad klass:

```csharp
using LockIn.Resources.Strings;

await Shell.Current.DisplayAlert(
    AppResources.ActiveWorkout_RemoveTitle,
    string.Format(AppResources.ActiveWorkout_RemoveBody, exerciseName, loggedCount),
    AppResources.Common_Remove,
    AppResources.Common_Cancel);
```

### 1.4 Locale-detection

`CultureInfo.CurrentUICulture` sätts automatiskt av .NET MAUI från `NSLocale.PreferredLanguages` vid app-start. **Vi rör det inte.**

Verifiering (görs en gång): logga `CultureInfo.CurrentUICulture.Name` på iOS-enheter med systemspråk svenska respektive engelska och konfirmera att resx väljs korrekt.

---

## 2. Sträng-katalogering

### 2.1 Nyckelkonvention

Mönster: `Område_Element` eller `Område_Action`.

| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Library_Title` | "BIBLIOTEK" | "LIBRARY" |
| `Library_Tab_Exercises` | "ÖVNINGAR" | "EXERCISES" |
| `Library_Tab_Templates` | "MALLAR" | "TEMPLATES" |
| `Library_Tab_Programs` | "PROGRAM" | "PROGRAMS" |
| `ActiveWorkout_Finish` | "AVSLUTA PASS" | "FINISH WORKOUT" |
| `Onboarding_Welcome` | "VÄLKOMMEN" | "WELCOME" |
| `Common_Cancel` | "Avbryt" | "Cancel" |
| `Common_Remove` | "Ta bort" | "Remove" |
| `Common_Save` | "Spara" | "Save" |

**Område-prefix:**
- `Common_` — knappar/strängar som återanvänds (Cancel, Save, Delete, OK)
- `Hem_`, `Train_`, `History_`, `Library_`, `Kropp_` — fem tab-sidor
- `ActiveWorkout_`, `PostWorkout_`, `SessionDetail_`, `ExercisePicker_`, `TemplateEdit_`, `ExerciseProgress_`, `CreateExercise_`, `ProgramDetail_`, `Settings_`, `BodyWeight_`, `Achievements_`, `PlateCalculator_`, `ProgressPhotos_`, `Onboarding_` — modala/push-sidor
- `Achievement_<Id>_Name`, `Achievement_<Id>_Description` — per achievement
- `Notification_*` — push-notifikationer

### 2.2 Parametriserade strängar

Använd `{0}`, `{1}` med `string.Format`. Skriv strängen så att ordningen är naturlig i båda språken.

| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `ActiveWorkout_RemoveBody` | "Ta bort {0}? {1} loggade set försvinner." | "Remove {0}? {1} logged sets will be lost." |
| `KroppPage_DeleteWeight` | "Ta bort {0} kg ({1})?" | "Delete {0} kg ({1})?" |

### 2.3 Pluralisering

Engelska och svenska har olika regler. Lös genom **två varianter per fall där det märks**, suffix `_One` / `_Many`:

| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Common_LoggedSets_One` | "{0} loggat set försvinner." | "{0} logged set will be lost." |
| `Common_LoggedSets_Many` | "{0} loggade set försvinner." | "{0} logged sets will be lost." |

ViewModel väljer rätt baserat på count:
```csharp
var key = count == 1
    ? AppResources.Common_LoggedSets_One
    : AppResources.Common_LoggedSets_Many;
```

Använd `_One`/`_Many` ENDAST där "1 vs N" syns. För strängar som "Förlorade pass: 7" räcker "Lost workouts: {0}" alltid.

### 2.4 Versaler/gemener

Versal-strängar (BIBLIOTEK, FORTSÄTT) skrivs som versaler i resx. BebasNeue-fonten i XAML renderar dem. Engelska motsvarigheter följer samma stil (LIBRARY, CONTINUE).

Vissa svenska strängar har CharacterSpacing — det är XAML-styling, inte sträng-data. Oförändrat.

---

## 3. Vad som INTE översätts

### 3.1 Övningsnamn (definitivt nej)

Övningar i SQLite-databasen (`Exercises`-tabellen) och `WorkoutPrograms.cs` förblir svenska:
- "Bänkpress", "Marklyft", "Knäböj", "Axelpress", "Hängande situps" etc.

Engelska användare ser dessa som är. Ingen databas-migrering, ingen `_en`-kolumn.

### 3.2 Programnamn

Befintliga programnamn i `WorkoutPrograms.cs` är redan engelska ("Starting Strength", "5/3/1 Boring But Big", "Push/Pull/Legs"). Oförändrat.

Program-**beskrivningar** är dock på svenska och översätts → behandlas som statisk data (se 3.4).

### 3.3 Datum- och talformat

**Datum:** `DateTime.ToString("d MMM yyyy")` använder `CultureInfo.CurrentCulture` automatiskt:
- svenska: "24 jun 2026"
- engelska: "Jun 24, 2026"

Ingen kod krävs.

**Tal:**
- *Parsing av Entry-input*: behåll `InvariantCulture` (befintligt mönster). Användaren kan skriva både "80.5" och "80,5".
- *Visning av tal*: ändras från `InvariantCulture` till `CurrentCulture`. Svensk användare ser "80,5 kg", engelsk ser "80.5 kg".

Filer som behöver `CurrentCulture`-visning: `BodyWeightPage`, `KroppPage`, `ExerciseProgressPage`, `ActiveWorkoutPage` (vikt-Entry-placeholders), `SessionDetailPage`.

### 3.4 Vad som ÖVERSÄTTS i statisk data

- **Achievements** (`AchievementService.cs`): `Achievement_<Id>_Name` + `Achievement_<Id>_Description` per achievement.
- **Programbeskrivningar** (`WorkoutPrograms.cs` `Description`-property): nya nycklar `Program_<Id>_Description`.
- **Notification-strängar** (`Plugin.LocalNotification` titlar och kroppar).

---

## 4. Implementation — tre plans

Spec'en täcker hela i18n-systemet. Implementation delas i tre separat-leveransbara plans.

### Plan 1: Infrastruktur (~50 strängar)

**Mål:** Resurssystem på plats, en pilot-sida proof-of-concept.

- Skapa `LockIn/Resources/Strings/AppResources.resx` med ~50 strängar (svenska)
- Skapa `AppResources.en.resx` med samma nycklar (engelska)
- Skapa `LocalizeExtension.cs`
- Konfigurera `.csproj` med `<EmbeddedResource>` för resx
- Översätt `OnboardingPage` + `OnboardingViewModel` som proof
- Verifiera build, kör simulator med svenskt + engelskt locale
- **Leverans:** OnboardingPage på engelska, resten på svenska. Inkrementellt utrullbart.

### Plan 2: UI-text alla sidor (~250–400 strängar)

**Mål:** Alla 19 sidor + tillhörande ViewModels översatta.

Sida för sida (en commit per sida för granskbarhet):

1. HemPage + HemViewModel
2. TrainPage + TrainViewModel
3. HistoryPage + HistoryViewModel
4. LibraryPage + LibraryViewModel
5. KroppPage + KroppViewModel
6. ActiveWorkoutPage + ViewModel
7. PostWorkoutPage + ViewModel
8. SessionDetailPage + ViewModel
9. ExercisePickerPage + ViewModel
10. TemplateEditPage + ViewModel
11. ExerciseProgressPage + ViewModel
12. CreateExercisePage + ViewModel
13. ProgramDetailPage + ViewModel
14. SettingsPage + ViewModel
15. BodyWeightPage + ViewModel
16. AchievementsPage (UI-kromen)
17. PlateCalculatorPage + ViewModel
18. ProgressPhotosPage + ViewModel

(OnboardingPage täcktes i Plan 1.)

**Per-sida-process:**
1. Identifiera alla hårdkodade strängar i XAML
2. Lägg till nycklar i båda `.resx`
3. Byt `Text="Hej"` → `Text="{loc:Localize Hem_Greeting}"`
4. Identifiera DisplayAlert/DisplayPrompt-strängar i ViewModel
5. Byt till `AppResources.Foo`
6. Bygg + verifiera ingen sida-kollision i nycklar

### Plan 3: Statisk data + notiser (~50 strängar)

**Mål:** Sista resterna.

- `AchievementService.cs`: alla `Name` och `Description` byts till nyckel-lookup
- `WorkoutPrograms.cs`: alla `Description` byts till nyckel-lookup (programnamn lämnas)
- `Plugin.LocalNotification` strängar (timer-klar, milestone-meddelanden)
- Eventuella återstående hårdkodade strängar som missades

**Edge cases att hantera:**
- Tal-visning: byt `InvariantCulture` → `CurrentCulture` där tal renderas (5 filer enligt 3.3)
- `ToString("d MMM yyyy")` på datum: redan korrekt, ingen åtgärd

---

## 5. Kritiska filer

| Fil | Roll |
|-----|------|
| `LockIn/Resources/Strings/AppResources.resx` | Defaultspråk, svenska — single source of truth för nycklar |
| `LockIn/Resources/Strings/AppResources.en.resx` | Engelska översättningar |
| `LockIn/Resources/Strings/LocalizeExtension.cs` | XAML markup extension `{loc:Localize Key}` |
| `LockIn/LockIn.csproj` | EmbeddedResource-konfiguration |
| `LockIn/Views/*.xaml` (19 st) | Alla hårdkodade strängar → `{loc:Localize}` |
| `LockIn/ViewModels/*.cs` (10+ st) | DisplayAlert/Prompt-strängar → `AppResources.X` |
| `LockIn/Services/AchievementService.cs` | Statisk achievement-data |
| `LockIn/Data/WorkoutPrograms.cs` | Statiska programbeskrivningar |

---

## 6. Risker och mitigering

| Risk | Mitigering |
|------|-----------|
| Nyckelnamn-kollisioner mellan sidor | Strikt `Område_Element`-prefix |
| Missade hårdkodade strängar | `grep -rE '"[^"\\n]+"' LockIn/Views` per sida + manuell genomgång efter bygg |
| Engelsk översättning blir längre och bryter layout | Tester i simulator med engelskt locale; lyssna efter overflow/truncation |
| `string.Format`-parametrar tappas eller swappas | Strikt namngivning + manuell verifiering vid review |
| Pluralisering missas | Sökning efter `set`, `pass`, `övning`, `mått`, `dagar` i resx — alla där "1 vs N" finns ska ha `_One`/`_Many` |
| iOS-locale ändras runtime (sällsynt) | Acceptera krav på app-restart för språkbyte. Dokumentera i Settings om ett "språk-info"-fält läggs till senare |

---

## 7. Acceptans-kriterier

Spec'en är genomförd när alla tre plans är klara och:

- [ ] Engelsk iOS-enhet visar appen helt på engelska (utom övningsnamn)
- [ ] Svensk iOS-enhet visar appen helt på svenska som idag
- [ ] Inga hårdkodade strängar i XAML eller ViewModels för text som syns för användare
- [ ] Datum visas i locale-anpassat format ("Jun 24, 2026" / "24 jun 2026")
- [ ] Vikt visas med rätt decimaltecken per locale
- [ ] Build är fortfarande 0 errors, inga nya warnings introducerade
- [ ] Inga textsträngar overflowar layout-containers i engelska
- [ ] Existerande svensk användarupplevelse är oförändrad (regression-test: navigera alla 19 sidor på svensk enhet)
