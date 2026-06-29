# Periodiseringsplaner — Design Spec
**Datum:** 2026-06-29
**Status:** Godkänd

---

## Sammanfattning

Periodiseringsplaner låter användaren strukturera träning i mesocykler — block med flera veckor, varje vecka med en intensitetsprocent och namngivna pass knutna till befintliga mallar. MVP fokuserar på att skapa och visa cykler, inte på automatisk schemaläggning eller kalenderintegration.

---

## Datamodell

### TrainingCycle
```csharp
[Table("TrainingCycles")]
public class TrainingCycle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [NotNull]
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public int WeekCount { get; set; }          // 1–16
    public bool IsActive { get; set; }          // en aktiv cykel åt gången
}
```

### CycleWeek
```csharp
[Table("CycleWeeks")]
public class CycleWeek
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int WeekNumber { get; set; }         // 1-baserat
    public int IntensityPercent { get; set; }   // t.ex. 70, 80, 90, 60 (deload)
    public string Label { get; set; } = "";     // t.ex. "Accumulation", "Intensification", "Deload"
}
```

### CycleSession
```csharp
[Table("CycleSessions")]
public class CycleSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleWeekId { get; set; }
    public int DayOfWeek { get; set; }          // 0=Måndag … 6=Söndag
    public int TemplateId { get; set; }
    public int SortOrder { get; set; }
}
```

**Relationer:**
- `TrainingCycle` 1→N `CycleWeek` (via CycleId)
- `CycleWeek` 1→N `CycleSession` (via CycleWeekId)
- `CycleSession` N→1 `WorkoutTemplate` (via TemplateId, existerande tabell)

**Begränsningar:**
- `WeekCount` är denormaliserat — antalet `CycleWeek`-rader för en cykel MÅSTE matcha `WeekCount`. Vid redigering räknar DatabaseService om.
- DayOfWeek: 0=Måndag (ISO-konvention, konsekvent med hur appen redan räknar träningsdagar).
- IntensityPercent lagras som heltal 0–100. Används enbart för visning i MVP; ingen logik påverkar vikter i aktiva pass.
- IsActive = true på max en cykel. DatabaseService sätter IsActive=false på alla andra vid aktivering.

---

## Navigationsflöde

```
LibraryPage (Tab 3 "Cykler")
  → PeriodizationPage        — lista aktiva/avslutade cykler + "Ny cykel"-knapp
    → CycleDetailPage        — skapa/redigera en cykel (veckor + sessioner)
      → (befintlig) ExercisePickerPage / TemplateEditPage vid behov
```

### Shell-registrering (AppShell.xaml.cs)
```csharp
Routing.RegisterRoute(nameof(PeriodizationPage), typeof(PeriodizationPage));
Routing.RegisterRoute(nameof(CycleDetailPage), typeof(CycleDetailPage));
```

### Tab-utökning i LibraryPage
LibraryPage har idag tre tabbar (Övningar / Mallar / Program). En fjärde tabell "Cykler" läggs till i pill-kontrollens `ColumnDefinitions` och i `LibraryViewModel`.

- Tab 3 visar `PeriodizationListView` — en `BindableLayout` med `TrainingCycle`-kort.
- "Ny cykel"-knapp visas i header när Tab 3 är valt (samma `ShowActionButton`-mönster).

---

## PeriodizationPage

**Syfte:** Listar alla träningscykler. Inget eget nav-bar — Shell.NavBarIsVisible="False".

**Innehåll:**
- Header: rubrik "CYKLER" + "Ny"-knapp
- Lista med `CycleSummaryCard` per cykel:
  - Namn (BebasNeue 20pt)
  - Badges: veckoantal, nuvarande vecka (om aktiv), intensitetsprofil
  - Aktiv-markering: grön `ForgeAccentGreen`-prick + "AKTIV"-badge
  - Swipe-to-delete (via `SwipeView`) eller tap → CycleDetailPage
- Tom-vy med uppmanande text om inga cykler finns

**ViewModel:** `PeriodizationViewModel` (Transient)
- `ObservableCollection<TrainingCycleViewModel> Cycles`
- `[RelayCommand] LoadCycles` — hämtar via `DatabaseService.GetCyclesAsync()`
- `[RelayCommand] CreateCycle` — navigerar till `CycleDetailPage` med `CycleId=0`
- `[RelayCommand] OpenCycle(TrainingCycle)` — navigerar med `CycleId=cycle.Id`
- `[RelayCommand] DeleteCycle(TrainingCycle)` — bekräftelse → `DatabaseService.DeleteCycleAsync`
- `[RelayCommand] ToggleActive(TrainingCycle)` — sätter `IsActive`, DatabaseService deaktiverar övriga

---

## CycleDetailPage

**Syfte:** Skapa eller redigera en cykel med veckor och pass. Navigeras till med `CycleId` (0 = ny).

**UI-struktur:**
```
ScrollView
  VerticalStackLayout
    [Namnfält]  — Entry, FontFamily=BebasNeue, FontSize=22
    [Veckoantal-stepper]  — 1–16, StepperView (minus/plus-knappar + label)
    [Veckolista]  — BindableLayout över CycleWeekRow
      CycleWeekRow (per vecka):
        - Veckonamn-entry (t.ex. "Accumulation")
        - Intensitets-slider: 40–110%, steg 5
        - Pass-lista per dag:
            DayRow: Label (Mån/Tis…) + mall-chip (tappbar → mall-picker inline)
    [Spara-knapp]  — PrimaryButton, fäst längst ned
```

**ViewModel:** `CycleDetailViewModel` (Transient), `IQueryAttributable`
- `[ObservableProperty] string Name`
- `[ObservableProperty] int WeekCount` — partial med `RebuildWeeks()`
- `ObservableCollection<CycleWeekRow> Weeks`
- `[RelayCommand] Save` — validerar + skriver till DB, navigerar tillbaka
- `[RelayCommand] PickTemplate(CycleSessionRow)` — öppnar inline template-picker (modal `DisplayActionSheet` med mallnamn)
- `[RelayCommand] ClearTemplate(CycleSessionRow)` — rensar val

**CycleWeekRow** (ObservableObject):
- `int WeekNumber`, `string Label`, `int IntensityPercent`
- `ObservableCollection<CycleSessionRow> Sessions` (7 rader, en per dag)

**CycleSessionRow** (ObservableObject):
- `int DayOfWeek`, `int? TemplateId`, `string TemplateName`

---

## DatabaseService-metoder (nya)

```csharp
// Hämta alla cykler med summerad metadata
Task<List<TrainingCycle>> GetCyclesAsync();

// Hämta veckor för en cykel (sorterat på WeekNumber)
Task<List<CycleWeek>> GetCycleWeeksAsync(int cycleId);

// Hämta sessioner för en vecka
Task<List<CycleSession>> GetCycleSessionsAsync(int cycleWeekId);

// Spara ny eller uppdatera befintlig cykel + veckor + sessioner (transaktion)
Task SaveCycleAsync(TrainingCycle cycle, List<CycleWeek> weeks, List<List<CycleSession>> sessions);

// Ta bort cykel + kaskadradera veckor + sessioner
Task DeleteCycleAsync(int cycleId);

// Sätt aktiv cykel (sätter IsActive=false på alla andra)
Task SetActiveCycleAsync(int cycleId);
```

**Init (InitCoreAsync):**
```csharp
await _db.CreateTableAsync<TrainingCycle>();
await _db.CreateTableAsync<CycleWeek>();
await _db.CreateTableAsync<CycleSession>();
```
Inga migreringar behövs — tabellerna är nya.

---

## i18n-strängar (svenska + engelska)

| Nyckel | SV | EN |
|--------|----|----|
| `Library_Tab_Cycles` | Cykler | Cycles |
| `Periodization_Title` | Cykler | Cycles |
| `Periodization_NoCycles_Title` | Ingen cykel ännu | No cycles yet |
| `Periodization_NoCycles_Body` | Skapa din första mesocykel för att planera din träning vecka för vecka. | Create your first mesocycle to plan your training week by week. |
| `Periodization_NewButton` | Ny | New |
| `Periodization_Active_Badge` | AKTIV | ACTIVE |
| `Periodization_Weeks_Format` | {0} VECKOR | {0} WEEKS |
| `Periodization_Week_Label_Format` | VECKA {0} | WEEK {0} |
| `CycleDetail_Title_New` | Ny cykel | New cycle |
| `CycleDetail_Title_Edit` | Redigera cykel | Edit cycle |
| `CycleDetail_Name_Placeholder` | Cykelnamn | Cycle name |
| `CycleDetail_WeekCount_Label` | Antal veckor | Number of weeks |
| `CycleDetail_Intensity_Label_Format` | Intensitet {0}% | Intensity {0}% |
| `CycleDetail_Save_Button` | Spara | Save |
| `CycleDetail_PickTemplate_Title` | Välj mall | Pick template |
| `CycleDetail_PickTemplate_Cancel` | Avbryt | Cancel |
| `CycleDetail_NoTemplate` | Inget pass | No session |
| `CycleDetail_Delete_Title` | Ta bort cykel | Delete cycle |
| `CycleDetail_Delete_Body_Format` | Ta bort {0}? | Delete {0}? |

Dagnamn (0=Måndag):
| Nyckel | SV | EN |
|--------|----|----|
| `Day_0` | Måndag | Monday |
| `Day_1` | Tisdag | Tuesday |
| `Day_2` | Onsdag | Wednesday |
| `Day_3` | Torsdag | Thursday |
| `Day_4` | Fredag | Friday |
| `Day_5` | Lördag | Saturday |
| `Day_6` | Söndag | Sunday |

---

## Designbeslut

**Intensitetsfärger:** Slider-track och chip-accent baseras på intensitet:
- ≤65% (deload): `ForgeAccentBlue` (#38BDF8)
- 66–79% (volym): `ForgeAccentPurple` (#A78BFA)
- 80–89%: `ForgeAccent` (#B8B8BC)
- ≥90% (peak): `ForgeAccentCoral` (#FB7185)

**Aktiv cykel:** `IsActive`-flaggan styr enbart visuell markering i MVP. Ingen koppling till `ActiveWorkoutPage`.

**Veckoantal:** Min 1, max 16 veckor. Stepper-knappar i steg om 1. Om antalet minskas och det redan finns pass i avsurna veckor varnas användaren innan de raderas.

**Sparlogik:** `SaveCycleAsync` körs i en SQLite-transaktion. Befintliga `CycleWeek`/`CycleSession`-rader för cykeln raderas och skrivs om (simplare än diff-merge för MVP).

**XAML-konventioner:**
- `Shell.NavBarIsVisible="False"`, `ios:Page.UseSafeArea="False"`
- Header-padding: `Padding="16,56,16,8"`
- Inga hårdkodade hex — `StaticResource` i XAML, `DesignTokens` i C#

---

## Utanför scope (MVP)

- Automatisk schemaläggning (vilken dag som är "idag i cykeln")
- Push-notiser
- Kalenderintegration
- Koppla `IntensityPercent` till faktisk viktberäkning i `ActiveWorkoutPage`
- Progression tracking per cykel (hur många pass genomförts)
