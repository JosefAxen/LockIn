# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project: LockIn

iOS-träningsapp byggd med **.NET MAUI 10**, target `net10.0-ios`. CI publicerar till GitHub Releases via GitHub Actions. SDK `10.0.x` + `dotnet workload install maui-ios`. Appen är på svenska.

## Bygga och köra

```bash
# Bygga för iOS (Release) — kräver Mac med Xcode 26.x
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Release

# Bygga Debug (för simulator)
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Det finns inget test-projekt. Verifiera ändringar genom att bygga och köra appen på enhet/simulator.

## Arkitektur

### Lagerstruktur
```
Models/          — SQLite-entiteter + enums (Exercise, LoggedSet, WorkoutSession, ...)
Services/        — Singleton-tjänster (DatabaseService, PRService, RestTimerService, ...)
ViewModels/      — MVVM med CommunityToolkit.Mvvm ([ObservableProperty], [RelayCommand])
Views/           — XAML-sidor + code-behind; en sida per VM
Controls/        — Custom SkiaSharp-kontroller (WeeklyGoalGauge, SparklineView, LineChartView, ConfettiView)
Platforms/iOS/   — iOS-specifika implementationer (HealthKitService, SoundService, AppIconHandler)
Data/            — Statisk data (WorkoutPrograms — hårdkodade träningsprogram)
```

### Navigation (Shell)
5 tab-sidor registrerade i `AppShell.xaml`: **Hem, Träna, Historik, Bibliotek, Kropp**.

Modala/push-sidor registreras i `AppShell.xaml.cs` och navigeras till med `Shell.Current.GoToAsync(nameof(SidanPage), parametrar)`. Query-attribut används för att skicka data (`IQueryAttributable`).

### Alla sidor (Views/)

**Tab-sidor** (5 st):
| Fil | Titel |
|-----|-------|
| `HemPage.xaml` | Hem |
| `TrainPage.xaml` | Träna |
| `HistoryPage.xaml` | Historik |
| `LibraryPage.xaml` | Bibliotek |
| `KroppPage.xaml` | Kropp |

**Push/modala sidor** (13 st):
| Fil | Nås från |
|-----|----------|
| `ActiveWorkoutPage.xaml` | TrainPage → starta pass |
| `PostWorkoutPage.xaml` | ActiveWorkoutPage → avsluta pass |
| `SessionDetailPage.xaml` | HistoryPage → tap session |
| `ExercisePickerPage.xaml` | TemplateEditPage / ActiveWorkoutPage |
| `TemplateEditPage.xaml` | LibraryPage → redigera mall |
| `ExerciseProgressPage.xaml` | LibraryPage → tap övning |
| `CreateExercisePage.xaml` | ExercisePickerPage → ny övning |
| `ProgramDetailPage.xaml` | LibraryPage → tap program |
| `SettingsPage.xaml` | KroppPage |
| `BodyWeightPage.xaml` | KroppPage |
| `AchievementsPage.xaml` | HistoryPage |
| `PlateCalculatorPage.xaml` | ActiveWorkoutPage |
| `ProgressPhotosPage.xaml` | KroppPage / PostWorkoutPage |
| `OnboardingPage.xaml` | App-start (första körning) |

**Global konvention för alla sidor:**
- Alla sidor har `Shell.NavBarIsVisible="False"` + `ios:Page.UseSafeArea="False"` (bakgrund fyller hela skärmen inkl. Dynamic Island)
- Header-raden har `Padding="...,56,..."` som manuell safe area-offset för Dynamic Island
- Om en ändring misstänks kunna gälla alla sidor: fråga användaren innan du applicerar den på alla 19 sidor
- När en sida läggs till eller tas bort: uppdatera tabellerna ovan OCH räkna om antalet (just nu 19 sidor totalt)

### Databas
`DatabaseService` är en singleton som wrappar `SQLiteAsyncConnection`. Init sker via `Lazy<Task>` — varje publik metod kallar `await InitAsync()` som säkerställer idempotent initiering.

**Migreringspattern** (idempotent via try-catch):
```csharp
try { await _db.ExecuteAsync("ALTER TABLE Foo ADD COLUMN Bar INTEGER NOT NULL DEFAULT 0"); }
catch (SQLiteException ex) when (ex.Message.Contains("duplicate column", ...)) { }
```

Alla migreringar + seeding körs i `InitCoreAsync()`. Ny seed-data för befintliga databaser måste ha en separat `SeedXyzAsync()` med en existens-check.

**Tabeller:** `Exercises`, `WorkoutTemplates`, `TemplateExercises`, `WorkoutSessions`, `SessionExercises`, `LoggedSets`, `AppSettings`, `BodyWeightEntries`, `BodyCompositionEntries`, `UserAchievements`, `WorkoutPhotos`.

### Dataflöde — aktivt pass
```
TrainPage → ActiveWorkoutPage (TemplateId via query)
  → ActiveWorkoutViewModel (Singleton)
    → WorkoutExerciseSection (per övning, ObservableCollection)
      → LoggedSetRow (per set, ObservableObject)
        → CompleteSetAsync() → DatabaseService.SaveLoggedSetAsync()
                             → PRService.IsPRAsync()
                             → RestTimerService.Start()
                             → NotificationService.ScheduleTimer()
```

`ActiveWorkoutViewModel` är **Singleton** (inte Transient) för att överleva tab-navigering under pågående pass. `ForceDeactivate()` återställer all state.

### Restimer
`RestTimerService` använder deadline-baserad timing (`DateTime _deadline`) — `SecondsRemaining` räknas ut från `DateTime.Now` för att vara korrekt efter iOS app-suspend. Pollar var 500ms.

### Design-tokens
Två källor — håll dem i sync:
- **XAML:** `Resources/Styles/Colors.xaml` — `StaticResource`-nycklar (`ForgeAccent`, `ForgeSurface`, etc.)
- **C#-kod (IDrawable, Controls):** `DesignTokens.cs` — statiska properties

Använd aldrig hårdkodade hex-strängar utanför dessa filer.

### Typsnitt
Tre typsnitt registrerade i `MauiProgram.cs`:
- `BebasNeue` — rubriker, siffror, stora displays
- `DMSansMedium` — knappar, viktigare labels
- `DMSansRegular` — brödtext (implicit stil för Label och Entry i Styles.xaml)

### SkiaSharp-kontroller
Alla custom canvas-kontroller i `Controls/` ärver `SKCanvasView` och renderar i `OnPaintSurface`. Använd **SKPaint-baserat text-API** (inte SKFont) — `TextSize`, `TextAlign`, `GetFontMetrics`, `DrawText(string, x, y, SKPaint)`. `InvalidateSurface()` triggar omritning när bindable properties ändras.

### iOS-specifikt
- `HealthKitService` — `RequestAuthorizationToShareAsync(null, new NSSet<HKObjectType>(...))` med try-catch (binding kan saknas i SDK-versioner)
- `SoundService` — `AVAudioPlayer` för ljud vid timer-klar
- `AppIconHandler` — anpassad handler för färgade ikoner i tab-bar
- `AppDelegate` — konfigurerar UITabBar-utseende + tema-ändringar
- `Entitlements.plist` — har `com.apple.developer.healthkit`

### Animationer
- **Knappar:** `AnimatedButtonBehavior` (Pressed/Released → ScaleTo)
- **Gester på Border/custom views:** `PointerGestureRecognizer` med `OnElemPointerPressed/Released` i code-behind (scale 0.93)
- **Sidövergångar (tabs):** `TranslateTo` + `FadeTo` i `OnAppearing` i varje tab-sidas code-behind
- **Set-rader:** `Loaded`-event med fade + translateY (180ms)
- Animera **aldrig** med `transition: all`. Animera bara `transform` och `opacity`-ekvivalenter (`ScaleTo`, `FadeTo`, `TranslateTo`)

### Viktiga ViewModels-mönster
- `[ObservableProperty]` genererar `OnXyzChanged()`-partials — använd dessa för sidoeffekter
- `[RelayCommand]` på `async Task`-metoder genererar `XyzCommand`
- Nested `BindableLayout` + `RelativeSource AncestorType={x:Type vm:ParentVmType}` för att binda till parent-ViewModel inifrån en DataTemplate
- `TapGestureRecognizer.Tapped`-event: `sender` är **vyn** gesturet är kopplat till, INTE `TapGestureRecognizer` — hämta `BindingContext` via `(sender as VisualElement)?.BindingContext`

### MuscleGroup-enum
```csharp
public enum MuscleGroup
{
    Chest=0, Back=1, Shoulders=2, Biceps=3, Triceps=4,
    Legs=5, Core=6, FullBody=7, Other=8, Forearms=9
}
```
Enum-värden lagras som int i SQLite. Lägg **alltid** till nya värden i slutet för att bevara befintlig data.

### Achievements
`AchievementService` (statisk) definierar alla achievements. Triggning sker i `PostWorkoutViewModel` efter avslutat pass. `UserAchievement`-tabellen lagrar unlockat id + timestamp.

### NuGet-paket (viktiga)
- `SkiaSharp 3.116.1` + `SkiaSharp.Views.Maui.Controls 3.116.1` — custom grafik
- `CommunityToolkit.Maui 14.2.0` — Toast, Alerts, Behaviors
- `CommunityToolkit.Mvvm 8.4.2` — ObservableObject, RelayCommand
- `Plugin.LocalNotification 14.1.0` — push-notiser för vilotimer
- `sqlite-net-pcl 1.9.172` — lokal databas

**Varning:** LiveChartsCore är INTE installerat — det kompilerade inte med iOS AOT. Alla grafer implementeras med custom SkiaSharp-kontroller.
