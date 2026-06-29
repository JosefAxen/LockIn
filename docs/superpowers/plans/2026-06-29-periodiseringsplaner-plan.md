# Periodiseringsplaner Implementation Plan
> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development when executing this plan. Each task must be committed before moving to the next.

**Goal:** Implementera MVP för Periodiseringsplaner — en feature som låter användaren skapa mesocykler med veckor, intensitetsprocent och pass knutna till befintliga träningsmallar. Exponeras som Tab 3 "Cykler" i LibraryPage.

**Architecture:** Tre nya SQLite-tabeller (`TrainingCycles`, `CycleWeeks`, `CycleSessions`) hanteras av DatabaseService. Två nya sidor (`PeriodizationPage`, `CycleDetailPage`) med egna ViewModels registreras i AppShell. LibraryPage utökas med en fjärde tabell i pill-kontrollen.

**Tech Stack:** .NET MAUI 10 iOS, SQLite (sqlite-net-pcl), CommunityToolkit.Mvvm, Shell navigation, AppResources i18n

**Spec:** `docs/superpowers/specs/2026-06-29-periodiseringsplaner-design.md`

## Global Constraints

- Inga hårdkodade hex-färger — `StaticResource`/`AppThemeBinding` i XAML, `DesignTokens.cs` i C#
- Alla strängar via `AppResources` (sv + en) — inga hårdkodade svenska strängar
- `Shell.NavBarIsVisible="False"` + `ios:Page.UseSafeArea="False"` på alla nya sidor
- Header-padding: `Padding="16,56,16,8"` (Dynamic Island safe area)
- CommunityToolkit.Mvvm: `[ObservableProperty]`, `[RelayCommand]` på alla VM-properties/kommandon
- `IQueryAttributable` för navigationsparametrar på `CycleDetailPage`
- DatabaseService: alla nya tabeller i `InitCoreAsync`, idempotent via `CreateTableAsync`
- Commit efter varje task — aldrig pusha utan explicit instruktion

---

### Task 1: Data Models — TrainingCycle, CycleWeek, CycleSession

**Filer att skapa:**
- `LockIn/Models/TrainingCycle.cs`
- `LockIn/Models/CycleWeek.cs`
- `LockIn/Models/CycleSession.cs`

**Detaljer:**

`TrainingCycle.cs`:
```csharp
using SQLite;
namespace LockIn.Models;

[Table("TrainingCycles")]
public class TrainingCycle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [NotNull]
    public string Name { get; set; } = "";
    public DateTime StartDate { get; set; }
    public int WeekCount { get; set; }
    public bool IsActive { get; set; }
}
```

`CycleWeek.cs`:
```csharp
using SQLite;
namespace LockIn.Models;

[Table("CycleWeeks")]
public class CycleWeek
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int WeekNumber { get; set; }
    public int IntensityPercent { get; set; }
    public string Label { get; set; } = "";
}
```

`CycleSession.cs`:
```csharp
using SQLite;
namespace LockIn.Models;

[Table("CycleSessions")]
public class CycleSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CycleWeekId { get; set; }
    public int DayOfWeek { get; set; }   // 0=Måndag … 6=Söndag
    public int TemplateId { get; set; }  // 0 = inget pass
    public int SortOrder { get; set; }
}
```

**Commit:** `feat(models): lägg till TrainingCycle, CycleWeek, CycleSession`

---

### Task 2: DatabaseService — tabeller + CRUD-metoder

**Fil att ändra:** `LockIn/Services/DatabaseService.cs`

**Ändringar:**

1. I `InitCoreAsync()`, direkt efter `await _db.CreateTableAsync<CardioSession>();`:
```csharp
await _db.CreateTableAsync<TrainingCycle>();
await _db.CreateTableAsync<CycleWeek>();
await _db.CreateTableAsync<CycleSession>();
```

2. Lägg till följande publika metoder i slutet av klassen (före sista `}`):

```csharp
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

        // Rensa gamla veckor + sessioner
        var oldWeeks = conn.Table<CycleWeek>()
            .Where(w => w.CycleId == cycle.Id).ToList();
        foreach (var w in oldWeeks)
        {
            conn.Table<CycleSession>().Delete(s => s.CycleWeekId == w.Id);
            conn.Delete(w);
        }

        // Skriv nya veckor + sessioner
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
    await _db.ExecuteAsync("UPDATE TrainingCycles SET IsActive = 0");
    await _db.ExecuteAsync(
        "UPDATE TrainingCycles SET IsActive = 1 WHERE Id = ?", cycleId);
}
```

**Commit:** `feat(db): lägg till tabeller och CRUD-metoder för periodiseringscykler`

---

### Task 3: i18n — AppResources.resx (sv) + AppResources.en.resx (en)

**Filer att ändra:**
- `LockIn/Resources/Strings/AppResources.resx` (svenska, master)
- `LockIn/Resources/Strings/AppResources.en.resx` (engelska)

**Strängar att lägga till** (i båda filerna, med rätt värden per språk):

```xml
<!-- Library tab -->
<data name="Library_Tab_Cycles"><value>Cykler</value></data>  <!-- EN: Cycles -->

<!-- PeriodizationPage -->
<data name="Periodization_Title"><value>Cykler</value></data>  <!-- EN: Cycles -->
<data name="Periodization_NoCycles_Title"><value>Ingen cykel ännu</value></data>  <!-- EN: No cycles yet -->
<data name="Periodization_NoCycles_Body"><value>Skapa din första mesocykel för att planera din träning vecka för vecka.</value></data>
<data name="Periodization_NewButton"><value>Ny</value></data>  <!-- EN: New -->
<data name="Periodization_Active_Badge"><value>AKTIV</value></data>  <!-- EN: ACTIVE -->
<data name="Periodization_Weeks_Format"><value>{0} VECKOR</value></data>  <!-- EN: {0} WEEKS -->
<data name="Periodization_Week_Label_Format"><value>VECKA {0}</value></data>  <!-- EN: WEEK {0} -->

<!-- CycleDetailPage -->
<data name="CycleDetail_Title_New"><value>Ny cykel</value></data>  <!-- EN: New cycle -->
<data name="CycleDetail_Title_Edit"><value>Redigera cykel</value></data>  <!-- EN: Edit cycle -->
<data name="CycleDetail_Name_Placeholder"><value>Cykelnamn</value></data>  <!-- EN: Cycle name -->
<data name="CycleDetail_WeekCount_Label"><value>Antal veckor</value></data>  <!-- EN: Number of weeks -->
<data name="CycleDetail_Intensity_Label_Format"><value>Intensitet {0}%</value></data>  <!-- EN: Intensity {0}% -->
<data name="CycleDetail_Save_Button"><value>Spara</value></data>  <!-- EN: Save -->
<data name="CycleDetail_PickTemplate_Title"><value>Välj mall</value></data>  <!-- EN: Pick template -->
<data name="CycleDetail_PickTemplate_Cancel"><value>Avbryt</value></data>  <!-- EN: Cancel -->
<data name="CycleDetail_NoTemplate"><value>Inget pass</value></data>  <!-- EN: No session -->
<data name="CycleDetail_Delete_Title"><value>Ta bort cykel</value></data>  <!-- EN: Delete cycle -->
<data name="CycleDetail_Delete_Body_Format"><value>Ta bort {0}?</value></data>  <!-- EN: Delete {0}? -->

<!-- Weekday names (0=Måndag) -->
<data name="Day_0"><value>Måndag</value></data>  <!-- EN: Monday -->
<data name="Day_1"><value>Tisdag</value></data>   <!-- EN: Tuesday -->
<data name="Day_2"><value>Onsdag</value></data>   <!-- EN: Wednesday -->
<data name="Day_3"><value>Torsdag</value></data>  <!-- EN: Thursday -->
<data name="Day_4"><value>Fredag</value></data>   <!-- EN: Friday -->
<data name="Day_5"><value>Lördag</value></data>   <!-- EN: Saturday -->
<data name="Day_6"><value>Söndag</value></data>   <!-- EN: Sunday -->
```

**Verifiering:** Bygg projektet och kontrollera att `AppResources.cs` genereras utan fel.

**Commit:** `feat(i18n): lägg till strängar för periodiseringscykler`

---

### Task 4: PeriodizationViewModel

**Fil att skapa:** `LockIn/ViewModels/PeriodizationViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class PeriodizationViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<TrainingCycle> Cycles { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var cycles = await db.GetCyclesAsync();
        Cycles.Clear();
        foreach (var c in cycles) Cycles.Add(c);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task CreateCycleAsync()
        => await Shell.Current.GoToAsync(nameof(CycleDetailPage),
            new Dictionary<string, object> { { "CycleId", 0 } });

    [RelayCommand]
    private async Task OpenCycleAsync(TrainingCycle cycle)
        => await Shell.Current.GoToAsync(nameof(CycleDetailPage),
            new Dictionary<string, object> { { "CycleId", cycle.Id } });

    [RelayCommand]
    private async Task DeleteCycleAsync(TrainingCycle cycle)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            AppResources.CycleDetail_Delete_Title,
            string.Format(AppResources.CycleDetail_Delete_Body_Format, cycle.Name),
            AppResources.Common_Delete,
            AppResources.Common_Cancel);
        if (!confirmed) return;
        await db.DeleteCycleAsync(cycle.Id);
        Cycles.Remove(cycle);
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(TrainingCycle cycle)
    {
        if (cycle.IsActive) return;
        await db.SetActiveCycleAsync(cycle.Id);
        await LoadAsync();
    }
}
```

**Commit:** `feat(vm): PeriodizationViewModel med CRUD-kommandon`

---

### Task 5: CycleDetailViewModel

**Fil att skapa:** `LockIn/ViewModels/CycleDetailViewModel.cs`

Klassen implementerar `IQueryAttributable` och tar emot `CycleId` via query-parametrar.

**Interna hjälpklasser i samma fil:**
- `CycleWeekRow : ObservableObject` — `WeekNumber`, `[ObservableProperty] string Label`, `[ObservableProperty] int IntensityPercent`, `ObservableCollection<CycleSessionRow> Sessions`
- `CycleSessionRow : ObservableObject` — `int DayOfWeek`, `[ObservableProperty] int? TemplateId`, `[ObservableProperty] string TemplateName`

**ViewModel-logik:**
- `ApplyQueryAttributes`: läser `CycleId`, laddar om CycleId > 0 annars initierar tom cykel
- `RebuildWeeks()`: när `WeekCount` ändras — lägger till/tar bort `CycleWeekRow` med 7 `CycleSessionRow` per vecka
- `Save`: validerar `Name.Trim()` ej tomt, bygger `List<CycleWeek>` + `List<List<CycleSession>>`, anropar `db.SaveCycleAsync`, navigerar tillbaka med `Shell.Current.GoToAsync("..")`
- `PickTemplate(CycleSessionRow)`: hämtar mallar via `db.GetTemplatesAsync()`, visar `DisplayActionSheet` med mallnamn, sätter `TemplateId` och `TemplateName` på raden
- Hjälpmetod `IntensityColor(int pct)` → `Color` baserat på trösklar i spec (returnerar `DesignTokens`-färger)

**Commit:** `feat(vm): CycleDetailViewModel med vecko/session-byggare`

---

### Task 6: PeriodizationPage.xaml + code-behind

**Filer att skapa:**
- `LockIn/Views/PeriodizationPage.xaml`
- `LockIn/Views/PeriodizationPage.xaml.cs`

**XAML-struktur:**
```xml
<ContentPage Shell.NavBarIsVisible="False" ios:Page.UseSafeArea="False"
             BackgroundColor="Transparent" x:DataType="vm:PeriodizationViewModel">
  <Grid SafeAreaEdges="None">
    <controls:AtmosphericBackgroundView InputTransparent="True"/>
    
    <!-- Loading-band (BebasNeue, ForgeAccent) -->
    
    <!-- ScrollView med VerticalStackLayout -->
    <!--   Header: Grid Padding="16,56,16,8"  -->
    <!--     Label "CYKLER" BebasNeue 34pt  -->
    <!--     Button "NY" SecondaryButton → CreateCycleCommand  -->
    
    <!--   BindableLayout över Cycles  -->
    <!--     CycleSummaryCard (Border, RoundRectangle 18):  -->
    <!--       Grid: aktiv-prick (Ellipse) | Namn + veckor-badge | aktiv-badge  -->
    <!--       TapGestureRecognizer → OpenCycleCommand  -->
    <!--       SwipeView → DeleteCycleCommand  -->
    
    <!--   EmptyView: NoCycles_Title + NoCycles_Body  -->
    
    <!-- Bottom fade BoxView  -->
  </Grid>
</ContentPage>
```

**Code-behind:**
```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    await BindingContext is PeriodizationViewModel vm
        ? vm.LoadAsync()
        : Task.CompletedTask;
    // Fade-in-animation (FadeTo + TranslateTo, samma mönster som övriga sidor)
}
```

**Commit:** `feat(ui): PeriodizationPage — lista med träningscykler`

---

### Task 7: CycleDetailPage.xaml + code-behind

**Filer att skapa:**
- `LockIn/Views/CycleDetailPage.xaml`
- `LockIn/Views/CycleDetailPage.xaml.cs`

**XAML-struktur:**
```xml
<ContentPage Shell.NavBarIsVisible="False" ios:Page.UseSafeArea="False"
             BackgroundColor="Transparent" x:DataType="vm:CycleDetailViewModel">
  <Grid SafeAreaEdges="None" RowDefinitions="*,Auto">
    <controls:AtmosphericBackgroundView InputTransparent="True"/>
    
    <!-- Row 0: ScrollView -->
    <ScrollView>
      <VerticalStackLayout Padding="16,56,16,16" Spacing="20">
        
        <!-- Bakåt-knapp + titel -->
        <Grid ColumnDefinitions="44,*">
          <Button Text="←" BackgroundColor="Transparent" Command="{Binding GoBackCommand}"/>
          <Label Text="{Binding PageTitle}" FontFamily="BebasNeue" FontSize="28"
                 VerticalOptions="Center" Grid.Column="1"/>
        </Grid>
        
        <!-- Namnfält -->
        <Border BackgroundColor="{StaticResource ForgeSurface2}" ...>
          <Entry Text="{Binding Name}" Placeholder="{loc:Localize CycleDetail_Name_Placeholder}"
                 FontFamily="BebasNeue" FontSize="22"/>
        </Border>
        
        <!-- Startdatum-rad -->
        <Grid ColumnDefinitions="*,Auto">
          <Label Text="Startdatum" FontFamily="DMSansRegular"/>
          <DatePicker Date="{Binding StartDate}" Grid.Column="1"/>
        </Grid>
        
        <!-- Veckoantal-stepper -->
        <Grid ColumnDefinitions="*,44,Auto,44">
          <Label Text="{loc:Localize CycleDetail_WeekCount_Label}" VerticalOptions="Center"/>
          <Button Grid.Column="1" Text="−" Command="{Binding DecrementWeeksCommand}"/>
          <Label Grid.Column="2" Text="{Binding WeekCount}" FontFamily="BebasNeue" FontSize="22"
                 MinimumWidthRequest="40" HorizontalTextAlignment="Center"/>
          <Button Grid.Column="3" Text="+" Command="{Binding IncrementWeeksCommand}"/>
        </Grid>
        
        <!-- Veckolista: BindableLayout över Weeks -->
        <StackLayout BindableLayout.ItemsSource="{Binding Weeks}" Spacing="16">
          <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="vm:CycleWeekRow">
              <Border BackgroundColor="{StaticResource ForgeSurface}" ...>
                <!-- Veckonummer + Label-entry + Intensitet-slider -->
                <!-- BindableLayout över Sessions (7 dagar) -->
                <!-- DayRow: Label dag + mall-chip Border → PickTemplateCommand -->
              </Border>
            </DataTemplate>
          </BindableLayout.ItemTemplate>
        </StackLayout>
        
      </VerticalStackLayout>
    </ScrollView>
    
    <!-- Row 1: Spara-knapp (sticky bottom) -->
    <Button Grid.Row="1" Text="{loc:Localize CycleDetail_Save_Button}"
            Style="{StaticResource PrimaryButton}" Margin="16,8,16,32"
            Command="{Binding SaveCommand}"/>
    
    <!-- Bottom gradient -->
  </Grid>
</ContentPage>
```

**Commit:** `feat(ui): CycleDetailPage — skapa/redigera mesocykel`

---

### Task 8: AppShell — registrera routes

**Fil att ändra:** `LockIn/AppShell.xaml.cs`

Lägg till i konstruktorn efter befintliga `Routing.RegisterRoute`-anrop:
```csharp
Routing.RegisterRoute(nameof(PeriodizationPage), typeof(PeriodizationPage));
Routing.RegisterRoute(nameof(CycleDetailPage), typeof(CycleDetailPage));
```

**Commit:** `feat(shell): registrera routes för PeriodizationPage och CycleDetailPage`

---

### Task 9: DI-registrering i MauiProgram.cs

**Fil att ändra:** `LockIn/MauiProgram.cs`

Lägg till i `CreateMauiApp()` (efter övriga `AddTransient`/`AddSingleton`-anrop):
```csharp
builder.Services.AddTransient<PeriodizationViewModel>();
builder.Services.AddTransient<PeriodizationPage>();
builder.Services.AddTransient<CycleDetailViewModel>();
builder.Services.AddTransient<CycleDetailPage>();
```

**Commit:** `feat(di): registrera PeriodizationViewModel/Page och CycleDetailViewModel/Page`

---

### Task 10: LibraryPage — utöka med Tab 3 "Cykler"

**Filer att ändra:**
- `LockIn/Views/LibraryPage.xaml`
- `LockIn/Views/LibraryPage.xaml.cs`
- `LockIn/ViewModels/LibraryViewModel.cs`

**LibraryViewModel.cs — ändringar:**

1. Ändra `ShowPrograms => SelectedTab == 2` → `ShowPrograms => SelectedTab == 2`  
   Lägg till: `public bool ShowCycles => SelectedTab == 3`

2. Ändra `ShowActionButton => SelectedTab < 2` → `ShowActionButton => SelectedTab < 2 || SelectedTab == 3`

3. I `OnSelectedTabChanged`: lägg till `OnPropertyChanged(nameof(ShowCycles))` och `Tab3Fg`-props.

4. Lägg till `Tab3Fg`-property (samma mönster som Tab0–2).

5. I `ActionButtonAsync`:
```csharp
else if (SelectedTab == 3) await Shell.Current.GoToAsync(nameof(PeriodizationPage));
```

**LibraryPage.xaml — ändringar:**

1. I alla tre befintliga tab-pill-kontroller: ändra `ColumnDefinitions="*,*,*"` → `ColumnDefinitions="*,*,*,*"` och lägg till Column 3:
```xml
<Border Grid.Column="3" BackgroundColor="Transparent" StrokeThickness="0" Padding="0,9">
    <Label Text="{loc:Localize Library_Tab_Cycles}" TextColor="{Binding Tab3Fg}"
           FontFamily="BebasNeue" FontSize="13" CharacterSpacing="1"
           HorizontalOptions="Center" VerticalOptions="Center"/>
    <Border.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding SelectTabCommand}" CommandParameter="{x:Int32 3}" Tapped="OnPillTapped"/>
    </Border.GestureRecognizers>
</Border>
```

2. Lägg till `TabIndicator`-Border för Tab 3 (ColSpan=4 i alla tre pill-kontroller).

3. Lägg till Tab 3-innehåll (`IsVisible="{Binding ShowCycles}"`):
```xml
<ScrollView IsVisible="{Binding ShowCycles}" VerticalScrollBarVisibility="Never">
  <VerticalStackLayout Spacing="0">
    <!-- Header (samma struktur som övriga tabs) -->
    <!-- Tab-pill (4 kolumner) -->
    <!-- BindableLayout över Cycles (hämtas från PeriodizationViewModel) -->
    <!-- EmptyView -->
  </VerticalStackLayout>
</ScrollView>
```

**Obs:** LibraryViewModel behöver inte äga cyklerna — Tab 3-innehållet navigerar direkt till `PeriodizationPage` vid tap på "Ny"-knappen. Alternativt: visa en inbäddad lista i LibraryPage (enklare för användaren). Välj det senare: LibraryViewModel håller `ObservableCollection<TrainingCycle> Cycles` och laddar dem i `OnSelectedTabChanged(3)`.

**LibraryPage.xaml.cs:** Lägg till `OnCyclesScrolled`-handler om sticky header behövs.

**Commit:** `feat(ui): utöka LibraryPage med Tab 3 Cykler`

---

### Task 11: Byggtest + CLAUDE.md-uppdatering

**Åtgärder:**
1. Kör `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` och lös eventuella kompileringsfel.
2. Uppdatera sidtabellen i `CLAUDE.md` (push/modala sidor): lägg till `PeriodizationPage` och `CycleDetailPage`, räkna om antalet (22 sidor totalt).

**Commit:** `chore: byggtest + uppdatera CLAUDE.md med nya sidor`
