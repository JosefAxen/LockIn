# Cardio-loggning Implementation Plan

**Goal:** Logga konditionspass (löpning, cykling, Stairmaster m.fl.) med distans, tid och puls. Egna aktivitetstyper stöds. Integreras i HistoryPage och HealthKit.

**Architecture:** Ny separat tabell `CardioSessions`. `CardioActivityType` enum (16 inbyggda + Custom). Ny `CardioPage.xaml` som modal. Entry-point från TrainPage. Cardio-sessioner visas som separat sektion i HistoryPage.

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Mvvm, sqlite-net-pcl, HealthKit (iOS)

## Global Constraints
- Inga hårdkodade hex-strängar utanför DesignTokens.cs / Colors.xaml
- Inga hårdkodade UI-strängar — alla via AppResources
- DB-migrationer idempotenta (try-catch på duplicate column)
- Shell.NavBarIsVisible="False" + ios:Page.UseSafeArea="False" på alla sidor
- Header-rad: Padding="...,56,..." som safe area offset
- Typsnitt: BebasNeue (rubriker), DMSansMedium (knappar/labels), DMSansRegular (brödtext)
- Design tokens: DesignTokens.cs (C#), Colors.xaml (XAML)
- Inga kommentarer om vad koden gör

---

## Task 1: CardioActivityType enum + CardioSession model + DatabaseService CRUD

**Files:**
- Create: `LockIn/Models/CardioActivityType.cs`
- Create: `LockIn/Models/CardioSession.cs`
- Modify: `LockIn/Services/DatabaseService.cs`

**CardioActivityType enum (exakt dessa värden, i denna ordning):**
```csharp
namespace LockIn.Models;

public enum CardioActivityType
{
    Running = 0,
    OutdoorCycling = 1,
    IndoorCycling = 2,
    Rowing = 3,
    Stairmaster = 4,
    Elliptical = 5,
    Walking = 6,
    Swimming = 7,
    JumpRope = 8,
    Hiit = 9,
    Boxing = 10,
    Padel = 11,
    Dancing = 12,
    Yoga = 13,
    CrossCountrySkiing = 14,
    Other = 15,
    Custom = 16,
}
```

**CardioSession model:**
```csharp
using SQLite;

namespace LockIn.Models;

public class CardioSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public CardioActivityType ActivityType { get; set; } = CardioActivityType.Running;
    public string CustomActivityName { get; set; } = "";

    public DateTime StartedAt { get; set; } = DateTime.Now;
    public int DurationMinutes { get; set; }
    public double DistanceKm { get; set; }
    public int AvgHeartRate { get; set; }
    public int CaloriesBurned { get; set; }
    public string Notes { get; set; } = "";
}
```

**DatabaseService changes:**
1. I `InitCoreAsync()`, efter `await _db.CreateTableAsync<WorkoutPhoto>();` lägg till:
   `await _db.CreateTableAsync<CardioSession>();`

2. Lägg till tre metoder i DatabaseService (after GetBodyCompositionEntriesAsync eller liknande):
```csharp
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
```

**Commit:** `feat(cardio): add CardioActivityType, CardioSession model and DatabaseService CRUD`

---

## Task 2: i18n strings

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Keys att lägga till (svenska / engelska):**

Activity type names (Cardio_Activity_*):
| Key | SV | EN |
|-----|----|----|
| Cardio_Activity_Running | Löpning | Running |
| Cardio_Activity_OutdoorCycling | Cykling utomhus | Outdoor Cycling |
| Cardio_Activity_IndoorCycling | Spinningcykel | Indoor Cycling |
| Cardio_Activity_Rowing | Roddmaskin | Rowing Machine |
| Cardio_Activity_Stairmaster | Stairmaster | Stairmaster |
| Cardio_Activity_Elliptical | Elliptical | Elliptical |
| Cardio_Activity_Walking | Gång | Walking |
| Cardio_Activity_Swimming | Simning | Swimming |
| Cardio_Activity_JumpRope | Hopprep | Jump Rope |
| Cardio_Activity_Hiit | HIIT | HIIT |
| Cardio_Activity_Boxing | Boxning | Boxing |
| Cardio_Activity_Padel | Padel/Tennis | Padel/Tennis |
| Cardio_Activity_Dancing | Dans | Dancing |
| Cardio_Activity_Yoga | Yoga | Yoga |
| Cardio_Activity_CrossCountrySkiing | Skidåkning | Cross-Country Skiing |
| Cardio_Activity_Other | Övrigt | Other |
| Cardio_Activity_Custom | Egen aktivitet | Custom Activity |

UI strings:
| Key | SV | EN |
|-----|----|----|
| Cardio_Title | CARDIO | CARDIO |
| Cardio_ActivityType_Label | AKTIVITET | ACTIVITY |
| Cardio_Duration_Label | TID (MIN) | DURATION (MIN) |
| Cardio_Distance_Label | DISTANS (KM) | DISTANCE (KM) |
| Cardio_HeartRate_Label | SNITTPULS | AVG HEART RATE |
| Cardio_Calories_Label | KALORIER | CALORIES |
| Cardio_Notes_Label | ANTECKNINGAR | NOTES |
| Cardio_CustomName_Label | AKTIVITETSNAMN | ACTIVITY NAME |
| Cardio_Save_Button | SPARA PASS | SAVE SESSION |
| Cardio_Delete_Confirm | Ta bort detta cardiopass? | Delete this cardio session? |
| Cardio_Delete_Yes | Ta bort | Delete |
| Cardio_Delete_No | Avbryt | Cancel |
| History_Cardio_Section | CARDIO | CARDIO |
| History_Cardio_Minutes | min | min |
| History_Cardio_Km | km | km |
| TrainPage_Cardio_Button | Logga cardio | Log cardio |

AppResources.cs wrapper properties (lägg till i alfabetisk ordning):
```csharp
public static string Cardio_Title => Get(nameof(Cardio_Title));
public static string Cardio_ActivityType_Label => Get(nameof(Cardio_ActivityType_Label));
public static string Cardio_Duration_Label => Get(nameof(Cardio_Duration_Label));
public static string Cardio_Distance_Label => Get(nameof(Cardio_Distance_Label));
public static string Cardio_HeartRate_Label => Get(nameof(Cardio_HeartRate_Label));
public static string Cardio_Calories_Label => Get(nameof(Cardio_Calories_Label));
public static string Cardio_Notes_Label => Get(nameof(Cardio_Notes_Label));
public static string Cardio_CustomName_Label => Get(nameof(Cardio_CustomName_Label));
public static string Cardio_Save_Button => Get(nameof(Cardio_Save_Button));
public static string Cardio_Delete_Confirm => Get(nameof(Cardio_Delete_Confirm));
public static string Cardio_Delete_Yes => Get(nameof(Cardio_Delete_Yes));
public static string Cardio_Delete_No => Get(nameof(Cardio_Delete_No));
public static string History_Cardio_Section => Get(nameof(History_Cardio_Section));
public static string History_Cardio_Minutes => Get(nameof(History_Cardio_Minutes));
public static string History_Cardio_Km => Get(nameof(History_Cardio_Km));
public static string TrainPage_Cardio_Button => Get(nameof(TrainPage_Cardio_Button));
// Activity types:
public static string Cardio_Activity_Running => Get(nameof(Cardio_Activity_Running));
public static string Cardio_Activity_OutdoorCycling => Get(nameof(Cardio_Activity_OutdoorCycling));
public static string Cardio_Activity_IndoorCycling => Get(nameof(Cardio_Activity_IndoorCycling));
public static string Cardio_Activity_Rowing => Get(nameof(Cardio_Activity_Rowing));
public static string Cardio_Activity_Stairmaster => Get(nameof(Cardio_Activity_Stairmaster));
public static string Cardio_Activity_Elliptical => Get(nameof(Cardio_Activity_Elliptical));
public static string Cardio_Activity_Walking => Get(nameof(Cardio_Activity_Walking));
public static string Cardio_Activity_Swimming => Get(nameof(Cardio_Activity_Swimming));
public static string Cardio_Activity_JumpRope => Get(nameof(Cardio_Activity_JumpRope));
public static string Cardio_Activity_Hiit => Get(nameof(Cardio_Activity_Hiit));
public static string Cardio_Activity_Boxing => Get(nameof(Cardio_Activity_Boxing));
public static string Cardio_Activity_Padel => Get(nameof(Cardio_Activity_Padel));
public static string Cardio_Activity_Dancing => Get(nameof(Cardio_Activity_Dancing));
public static string Cardio_Activity_Yoga => Get(nameof(Cardio_Activity_Yoga));
public static string Cardio_Activity_CrossCountrySkiing => Get(nameof(Cardio_Activity_CrossCountrySkiing));
public static string Cardio_Activity_Other => Get(nameof(Cardio_Activity_Other));
public static string Cardio_Activity_Custom => Get(nameof(Cardio_Activity_Custom));
```

**Commit:** `feat(cardio): add i18n strings for cardio logging`

---

## Task 3: CardioViewModel + DI + AppShell

**Files:**
- Create: `LockIn/ViewModels/CardioViewModel.cs`
- Modify: `LockIn/MauiProgram.cs`
- Modify: `LockIn/AppShell.xaml.cs`

**CardioViewModel.cs:**
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class CardioViewModel(DatabaseService db) : ObservableObject
{
    public record ActivityOption(CardioActivityType Type, string Name);

    public List<ActivityOption> ActivityOptions { get; } = Enum.GetValues<CardioActivityType>()
        .Select(t => new ActivityOption(t, ActivityTypeLabel(t)))
        .ToList();

    [ObservableProperty] private ActivityOption _selectedActivity = 
        new(CardioActivityType.Running, AppResources.Cardio_Activity_Running);

    [ObservableProperty] private string _durationText = "";
    [ObservableProperty] private string _distanceText = "";
    [ObservableProperty] private string _heartRateText = "";
    [ObservableProperty] private string _caloriesText = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private string _customName = "";
    [ObservableProperty] private bool _isCustom;

    partial void OnSelectedActivityChanged(ActivityOption value)
        => IsCustom = value.Type == CardioActivityType.Custom;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedActivity is null) return;

        int.TryParse(DurationText, out var duration);
        double.TryParse(DistanceText, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var distance);
        int.TryParse(HeartRateText, out var hr);
        int.TryParse(CaloriesText, out var cal);

        var session = new CardioSession
        {
            ActivityType       = SelectedActivity.Type,
            CustomActivityName = IsCustom ? CustomName : "",
            StartedAt          = DateTime.Now,
            DurationMinutes    = duration,
            DistanceKm         = distance,
            AvgHeartRate       = hr,
            CaloriesBurned     = cal,
            Notes              = Notes,
        };

        await db.SaveCardioSessionAsync(session);
        await Shell.Current.GoToAsync("..");
    }

    private static string ActivityTypeLabel(CardioActivityType t) => t switch
    {
        CardioActivityType.Running            => AppResources.Cardio_Activity_Running,
        CardioActivityType.OutdoorCycling     => AppResources.Cardio_Activity_OutdoorCycling,
        CardioActivityType.IndoorCycling      => AppResources.Cardio_Activity_IndoorCycling,
        CardioActivityType.Rowing             => AppResources.Cardio_Activity_Rowing,
        CardioActivityType.Stairmaster        => AppResources.Cardio_Activity_Stairmaster,
        CardioActivityType.Elliptical         => AppResources.Cardio_Activity_Elliptical,
        CardioActivityType.Walking            => AppResources.Cardio_Activity_Walking,
        CardioActivityType.Swimming           => AppResources.Cardio_Activity_Swimming,
        CardioActivityType.JumpRope           => AppResources.Cardio_Activity_JumpRope,
        CardioActivityType.Hiit               => AppResources.Cardio_Activity_Hiit,
        CardioActivityType.Boxing             => AppResources.Cardio_Activity_Boxing,
        CardioActivityType.Padel              => AppResources.Cardio_Activity_Padel,
        CardioActivityType.Dancing            => AppResources.Cardio_Activity_Dancing,
        CardioActivityType.Yoga               => AppResources.Cardio_Activity_Yoga,
        CardioActivityType.CrossCountrySkiing => AppResources.Cardio_Activity_CrossCountrySkiing,
        CardioActivityType.Other              => AppResources.Cardio_Activity_Other,
        CardioActivityType.Custom             => AppResources.Cardio_Activity_Custom,
        _                                     => t.ToString(),
    };
}
```

**MauiProgram.cs:** Lägg till `builder.Services.AddTransient<CardioViewModel>();` efter övriga ViewModels.

**AppShell.xaml.cs:** Lägg till `Routing.RegisterRoute(nameof(CardioPage), typeof(CardioPage));`

**Commit:** `feat(cardio): add CardioViewModel, DI registration and route`

---

## Task 4: CardioPage.xaml + code-behind

**Files:**
- Create: `LockIn/Views/CardioPage.xaml`
- Create: `LockIn/Views/CardioPage.xaml.cs`

**Design:**
- Shell.NavBarIsVisible="False", ios:Page.UseSafeArea="False"
- Header: Padding="22,56,22,0", "←" back-knapp + "CARDIO"-titel (BebasNeue 28)
- ScrollView med formulärfält:
  - Aktivitetstyp: horizontell scrollbar med chip-knappar (en per ActivityOption)
  - Eget namn-fält (Entry, visas bara när IsCustom=true)
  - Tid (min): Entry Keyboard=Numeric
  - Distans (km): Entry Keyboard=Numeric  
  - Snittpuls: Entry Keyboard=Numeric
  - Kalorier: Entry Keyboard=Numeric
  - Anteckningar: Editor
- Spara-knapp (ForgeAccent-bakgrund, BebasNeue, AnimatedButtonBehavior)

**code-behind:** Minimal constructor, `BindingContext = vm`.

**Commit:** `feat(cardio): add CardioPage XAML and code-behind`

---

## Task 5: TrainPage entry + HistoryPage cardio section

**Files:**
- Modify: `LockIn/Views/TrainPage.xaml`
- Modify: `LockIn/ViewModels/TrainViewModel.cs`
- Modify: `LockIn/Services/DatabaseService.cs`
- Modify: `LockIn/ViewModels/HistoryViewModel.cs`
- Modify: `LockIn/Views/HistoryPage.xaml`

**TrainPage.xaml:** Lägg till en "Logga cardio"-rad/knapp i sidan (t.ex. precis ovanför eller under FAB-sektionen). Matcha befintlig card-style.

**TrainViewModel.cs:** Ny `OpenCardioCommand` → `Shell.Current.GoToAsync(nameof(CardioPage))`.

**DatabaseService:** Lägg till `GetCardioSessionsAsync` används redan från Task 1.

**HistoryViewModel.cs:** Lägg till:
```csharp
[ObservableProperty] private IReadOnlyList<CardioSession> _cardioSessions = [];

// I LoadAsync():
CardioSessions = (await db.GetCardioSessionsAsync(20)).AsReadOnly();
```

**HistoryPage.xaml:** Lägg till cardio-sektion under sessionslistan:
- "CARDIO"-sektionsrubrik (SectionLabel-style), dold om CardioSessions.Count == 0
- BindableLayout på CardioSessions med CardioSession DataTemplate
- Varje kort: aktivitetsnamn, tid, distans, datum

**Commit:** `feat(cardio): wire up TrainPage entry point and HistoryPage cardio section`

---

## Task 6: HealthKitService cardio workout saving

**Files:**
- Modify: `LockIn/Services/IHealthService.cs`
- Modify: `LockIn/Platforms/iOS/HealthKitService.cs`

**IHealthService.cs:** Lägg till:
```csharp
Task SaveCardioWorkoutAsync(CardioActivityType type, DateTime start, DateTime end, double kcal, double distanceMeters);
```

**HealthKitService.cs:** Lägg till:
1. `HKWorkoutType.GetWorkoutType()!` i `s_writeTypes`
2. Ny metod `SaveCardioWorkoutAsync` som skapar `HKWorkout` med rätt `HKWorkoutActivityType`

Mappning CardioActivityType → HKWorkoutActivityType:
- Running → HKWorkoutActivityType.Running
- OutdoorCycling → HKWorkoutActivityType.Cycling
- IndoorCycling → HKWorkoutActivityType.Cycling
- Rowing → HKWorkoutActivityType.Rowing
- Stairmaster → HKWorkoutActivityType.StairClimbing
- Elliptical → HKWorkoutActivityType.Elliptical
- Walking → HKWorkoutActivityType.Walking
- Swimming → HKWorkoutActivityType.Swimming
- JumpRope → HKWorkoutActivityType.JumpRope
- Hiit → HKWorkoutActivityType.HighIntensityIntervalTraining
- Boxing → HKWorkoutActivityType.Boxing
- Padel/Tennis → HKWorkoutActivityType.Racquetball
- Dancing → HKWorkoutActivityType.Dance
- Yoga → HKWorkoutActivityType.Yoga
- CrossCountrySkiing → HKWorkoutActivityType.CrossCountrySkiing
- Other/Custom → HKWorkoutActivityType.Other

**CardioViewModel.cs:** Anropa `IHealthService.SaveCardioWorkoutAsync` i SaveAsync efter db-sparning.

**Commit:** `feat(cardio): add HealthKit cardio workout saving with activity type mapping`
