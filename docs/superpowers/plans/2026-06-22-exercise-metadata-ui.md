# Exercise Metadata UI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Visa Exercise-modellens nya fält (Equipment, Level, Mechanic, Force, SecondaryMuscles) i UI — som filterchips i LibraryPage och ExercisePicker, och som metadatasektion på ExerciseProgressPage.

**Architecture:** Tre scope: (1) Equipment-filterchips i ViewModels + XAML för LibraryPage och ExercisePicker, exakt samma mönster som befintlig MuscleGroupChip. (2) Ny metadatasektion i ExerciseProgressViewModel + XAML. (3) Visuell design via `/frontend-design` innan varje XAML-scope.

**Tech Stack:** .NET MAUI 10, CommunityToolkit.Mvvm, XAML, SkiaSharp (inga nya beroenden)

## Global Constraints

- Inga nya NuGet-paket
- `BebasNeue` för rubriker/labels, `DMSansMedium` för viktigare text, `DMSansRegular` för brödtext
- Hårdkodade hex-färger är förbjudna — använd alltid `StaticResource`/`AppThemeBinding` eller `DesignTokens`
- Alla nya enums läggs i slutet (befintlig konvention: MuscleGroup, EquipmentType, etc.)
- Appen är på svenska — labels som "SKIVSTÅNG", "HANTEL", "NYBÖRJARE" etc.
- Bygg-kommando: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
- Verifiera med 0 errors (warnings OK)

---

### Task 1: EquipmentChip + filter i LibraryViewModel och ExercisePickerViewModel

**Files:**
- Modify: `LockIn/ViewModels/ExercisePickerViewModel.cs` — lägg till `EquipmentChip`-klass + `ExercisePickerRow.EquipmentLabel`
- Modify: `LockIn/ViewModels/LibraryViewModel.cs` — lägg till Equipment-filter
- Modify: `LockIn/ViewModels/ExercisePickerViewModel.cs` — lägg till Equipment-filter

**Interfaces:**
- Produces: `EquipmentChip` klass med `Label`, `Equipment?` (nullable `EquipmentType`), `IsSelected`, `Background`, `Foreground`
- Produces: `LibraryViewModel.EquipmentChips` — `ObservableCollection<EquipmentChip>`
- Produces: `LibraryViewModel.SelectEquipmentChipCommand` — `[RelayCommand]`
- Produces: `ExercisePickerViewModel.EquipmentChips` — `ObservableCollection<EquipmentChip>`
- Produces: `ExercisePickerViewModel.SelectEquipmentChipCommand` — `[RelayCommand]`
- Produces: `ExercisePickerRow.EquipmentLabel` — `string` (t.ex. "SKIVSTÅNG")

- [ ] **Steg 1: Lägg till `EquipmentChip`-klass i slutet av `ExercisePickerViewModel.cs`** (efter `MuscleGroupChip`)

```csharp
public partial class EquipmentChip : ObservableObject
{
    public string Label { get; set; } = "";
    public EquipmentType? Equipment { get; set; }
    [ObservableProperty] private bool _isSelected;

    public Color Background => IsSelected ? DesignTokens.ChipActiveBg : DesignTokens.ChipInactiveBg;
    public Color Foreground => IsSelected ? DesignTokens.ChipActiveFg : DesignTokens.ChipInactiveFg;

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(Foreground));
    }
}
```

- [ ] **Steg 2: Lägg till `EquipmentLabel`-property på `ExercisePickerRow`**

I `ExercisePickerRow`-klassen, lägg till:
```csharp
public string EquipmentLabel { get; }
```
Uppdatera konstruktorn:
```csharp
public ExercisePickerRow(Exercise exercise, string muscleLabel, Color muscleColor)
{
    Exercise = exercise;
    MuscleLabel = muscleLabel;
    MuscleColor = muscleColor;
    EquipmentLabel = EquipmentTypeLabel(exercise.Equipment);
}

private static string EquipmentTypeLabel(EquipmentType e) => e switch
{
    EquipmentType.Barbell     => "SKIVSTÅNG",
    EquipmentType.Dumbbell    => "HANTEL",
    EquipmentType.Cable       => "KABEL",
    EquipmentType.Machine     => "MASKIN",
    EquipmentType.BodyOnly    => "KROPPSVIKT",
    EquipmentType.EZBar       => "EZ-STÅNG",
    EquipmentType.Kettlebell  => "KETTLEBELL",
    EquipmentType.Bands       => "BAND",
    EquipmentType.FoamRoll    => "FOAM ROLL",
    EquipmentType.MedicineBall => "MEDICINBOLL",
    _                         => ""
};
```

- [ ] **Steg 3: Uppdatera `ExercisePickerViewModel` — lägg till chips + filter**

Lägg till fält och collection efter de befintliga:
```csharp
public ObservableCollection<EquipmentChip> EquipmentChips { get; } = new();
private EquipmentType? _selectedEquipment;
```

I `LoadAsync()`, efter MuscleChips-populering, lägg till:
```csharp
EquipmentChips.Clear();
EquipmentChips.Add(new EquipmentChip { Label = "ALLA", Equipment = null, IsSelected = true });
foreach (var eq in _allExercises
    .Select(e => e.Equipment)
    .Where(e => e != EquipmentType.Other)
    .Distinct()
    .OrderBy(e => e.ToString()))
{
    EquipmentChips.Add(new EquipmentChip { Label = EquipmentTypeLabel(eq), Equipment = eq });
}
```

Lägg till RelayCommand:
```csharp
[RelayCommand]
private void SelectEquipmentChip(EquipmentChip chip)
{
    foreach (var c in EquipmentChips) c.IsSelected = false;
    chip.IsSelected = true;
    _selectedEquipment = chip.Equipment;
    ApplyFilter();
}
```

Lägg till `EquipmentTypeLabel`-hjälpmetod (privat statisk, identisk med den i `ExercisePickerRow` ovan):
```csharp
private static string EquipmentTypeLabel(EquipmentType e) => e switch
{
    EquipmentType.Barbell     => "SKIVSTÅNG",
    EquipmentType.Dumbbell    => "HANTEL",
    EquipmentType.Cable       => "KABEL",
    EquipmentType.Machine     => "MASKIN",
    EquipmentType.BodyOnly    => "KROPPSVIKT",
    EquipmentType.EZBar       => "EZ-STÅNG",
    EquipmentType.Kettlebell  => "KETTLEBELL",
    EquipmentType.Bands       => "BAND",
    EquipmentType.FoamRoll    => "FOAM ROLL",
    EquipmentType.MedicineBall => "MEDICINBOLL",
    _                         => "ÖVRIGT"
};
```

Uppdatera `ApplyFilter()` med equipment-filter (lägg till efter muscle-filtret):
```csharp
private void ApplyFilter()
{
    var q = SearchText.Trim().ToLowerInvariant();
    var selected = MuscleChips.FirstOrDefault(c => c.IsSelected);
    var source = _allExercises.AsEnumerable();

    if (selected?.MuscleGroup is MuscleGroup mg)
        source = source.Where(e => e.MuscleGroup == mg);
    if (_selectedEquipment.HasValue)
        source = source.Where(e => e.Equipment == _selectedEquipment.Value);
    if (!string.IsNullOrEmpty(q))
        source = source.Where(e => e.Name.ToLowerInvariant().Contains(q));

    FilteredExercises.Clear();
    foreach (var e in source.OrderBy(e => e.Name))
        FilteredExercises.Add(new ExercisePickerRow(e, MuscleGroupLabel(e.MuscleGroup), GetMuscleColor(e.MuscleGroup)));
}
```

- [ ] **Steg 4: Uppdatera `LibraryViewModel` — lägg till EquipmentChips + filter**

Lägg till fält och collection efter `MuscleChips`:
```csharp
public ObservableCollection<EquipmentChip> EquipmentChips { get; } = new();
private EquipmentType? _selectedEquipment;
```

I `LoadAsync()`, efter MuscleChips-populering:
```csharp
EquipmentChips.Clear();
EquipmentChips.Add(new EquipmentChip { Label = "ALLA", Equipment = null, IsSelected = true });
foreach (var eq in _allExercises
    .Select(e => e.Equipment)
    .Where(e => e != EquipmentType.Other)
    .Distinct()
    .OrderBy(e => e.ToString()))
{
    EquipmentChips.Add(new EquipmentChip { Label = EquipmentTypeLabel(eq), Equipment = eq });
}
```

Lägg till RelayCommand:
```csharp
[RelayCommand]
private void SelectEquipmentChip(EquipmentChip chip)
{
    foreach (var c in EquipmentChips) c.IsSelected = false;
    chip.IsSelected = true;
    _selectedEquipment = chip.Equipment;
    ApplyFilter();
}
```

Lägg till `EquipmentTypeLabel`-hjälpmetod (privat statisk, identisk med ExercisePicker-versionen ovan).

Uppdatera `ApplyFilter()` i LibraryViewModel:
```csharp
private void ApplyFilter()
{
    var q = SearchText.Trim().ToLowerInvariant();
    var source = _allExercises.AsEnumerable();
    if (_selectedMuscleGroup.HasValue)
        source = source.Where(e => e.MuscleGroup == _selectedMuscleGroup.Value);
    if (_selectedEquipment.HasValue)
        source = source.Where(e => e.Equipment == _selectedEquipment.Value);
    if (!string.IsNullOrEmpty(q))
        source = source.Where(e => e.Name.ToLowerInvariant().Contains(q));

    Groups.Clear();
    foreach (var group in source.GroupBy(e => e.MuscleGroup).OrderBy(g => g.Key.ToString()))
    {
        var g = new ExerciseGroup(MuscleGroupLabel(group.Key));
        foreach (var e in group.OrderBy(e => e.Name))
            g.Add(e);
        Groups.Add(g);
    }
}
```

- [ ] **Steg 5: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 6: Commit**

```bash
git add LockIn/ViewModels/LibraryViewModel.cs LockIn/ViewModels/ExercisePickerViewModel.cs
git commit -m "feat(viewmodels): EquipmentChip-filter i Library och ExercisePicker"
```

---

### Task 2: ExerciseProgressViewModel — metadata-properties

**Files:**
- Modify: `LockIn/ViewModels/ExerciseProgressViewModel.cs`

**Interfaces:**
- Consumes: `Exercise.Equipment` (EquipmentType), `Exercise.SecondaryMuscles` (string), `Exercise.Force` (ExerciseForce), `Exercise.Level` (ExerciseLevel), `Exercise.Mechanic` (ExerciseMechanic)
- Produces: `ExerciseProgressViewModel.EquipmentName` — string (t.ex. "SKIVSTÅNG")
- Produces: `ExerciseProgressViewModel.SecondaryMusclesText` — string (t.ex. "Axlar, Triceps")
- Produces: `ExerciseProgressViewModel.LevelName` — string (t.ex. "MEDEL")
- Produces: `ExerciseProgressViewModel.MechanicName` — string (t.ex. "COMPOUND")
- Produces: `ExerciseProgressViewModel.ForceName` — string (t.ex. "PUSH")
- Produces: `ExerciseProgressViewModel.HasMetadata` — bool (true om minst ett fält är ifyllt)

- [ ] **Steg 1: Lägg till ObservableProperties i `ExerciseProgressViewModel`**

```csharp
[ObservableProperty] private string _equipmentName = "";
[ObservableProperty] private string _secondaryMusclesText = "";
[ObservableProperty] private string _levelName = "";
[ObservableProperty] private string _mechanicName = "";
[ObservableProperty] private string _forceName = "";
[ObservableProperty] private bool _hasMetadata;
```

- [ ] **Steg 2: Populera i `IQueryAttributable`-hanteraren**

Befintlig kod i `ExerciseProgressViewModel` laddar övningen via `IQueryAttributable.ApplyQueryAttributes`. Hitta raden `ExerciseDescription = _exercise.Description ?? "";` och lägg till efter den:

```csharp
EquipmentName = EquipmentLabel(_exercise.Equipment);
SecondaryMusclesText = _exercise.SecondaryMuscles ?? "";
LevelName = LevelLabel(_exercise.Level);
MechanicName = MechanicLabel(_exercise.Mechanic);
ForceName = ForceLabel(_exercise.Force);
HasMetadata = EquipmentName.Length > 0 || SecondaryMusclesText.Length > 0
              || LevelName.Length > 0 || MechanicName.Length > 0 || ForceName.Length > 0;
```

- [ ] **Steg 3: Lägg till hjälpmetoder (privata statiska)**

```csharp
private static string EquipmentLabel(EquipmentType e) => e switch
{
    EquipmentType.Barbell      => "Skivstång",
    EquipmentType.Dumbbell     => "Hantel",
    EquipmentType.Cable        => "Kabel",
    EquipmentType.Machine      => "Maskin",
    EquipmentType.BodyOnly     => "Kroppsvikt",
    EquipmentType.EZBar        => "EZ-stång",
    EquipmentType.Kettlebell   => "Kettlebell",
    EquipmentType.Bands        => "Band",
    EquipmentType.FoamRoll     => "Foam roll",
    EquipmentType.MedicineBall => "Medicinboll",
    _                          => ""
};

private static string LevelLabel(ExerciseLevel l) => l switch
{
    ExerciseLevel.Beginner     => "Nybörjare",
    ExerciseLevel.Intermediate => "Medel",
    ExerciseLevel.Expert       => "Avancerad",
    _                          => ""
};

private static string MechanicLabel(ExerciseMechanic m) => m switch
{
    ExerciseMechanic.Compound  => "Compound",
    ExerciseMechanic.Isolation => "Isolering",
    _                          => ""
};

private static string ForceLabel(ExerciseForce f) => f switch
{
    ExerciseForce.Push   => "Push",
    ExerciseForce.Pull   => "Pull",
    ExerciseForce.Static => "Statisk",
    _                    => ""
};
```

- [ ] **Steg 4: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 5: Commit**

```bash
git add LockIn/ViewModels/ExerciseProgressViewModel.cs
git commit -m "feat(viewmodel): ExerciseProgress exponerar Equipment/Level/Mechanic/Force/SecondaryMuscles"
```

---

### Task 3: LibraryPage XAML — equipment-filter + kort-badge

**Files:**
- Modify: `LockIn/Views/LibraryPage.xaml`

**Interfaces:**
- Consumes: `LibraryViewModel.EquipmentChips` (ObservableCollection<EquipmentChip>)
- Consumes: `LibraryViewModel.SelectEquipmentChipCommand`
- Consumes: `Exercise.Equipment` (EquipmentType via `ExercisePickerRow` eller direkt på Exercise)

- [ ] **Steg 1: Invoke `/frontend-design` för detta scope**

Kör: `/frontend-design`
Beskriv scopet: Equipment-filterchips (horisontell scrollview under muskelchips) + utrustnings-badge på övningskort i LibraryPage. Mönstret är identiskt med befintliga muskelchips. Badgen ska vara subtil — samma stil som den befintliga "EGNA"-bagen på kortet. Använd Forge-designsystemet.

- [ ] **Steg 2: Lägg till equipment-chips-rad i Exercises-tabens `CollectionView.Header`**

I `LockIn/Views/LibraryPage.xaml`, hitta muscle-chips ScrollView i `CollectionView.Header > VerticalStackLayout`. Lägg till en identisk ScrollView direkt efter den (innan `</VerticalStackLayout>`):

```xml
<!-- Utrustnings-chips (horisontell scroll) -->
<ScrollView Orientation="Horizontal"
            HorizontalScrollBarVisibility="Never"
            Margin="16,0,16,12">
    <HorizontalStackLayout
        BindableLayout.ItemsSource="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=EquipmentChips}"
        Spacing="6">
        <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="vm:EquipmentChip">
                <Border BackgroundColor="{Binding Background}"
                        StrokeShape="RoundRectangle 22" StrokeThickness="0"
                        Padding="14,0" HeightRequest="34">
                    <Label Text="{Binding Label}"
                           TextColor="{Binding Foreground}"
                           FontFamily="BebasNeue" FontSize="13" CharacterSpacing="1"
                           VerticalOptions="Center"/>
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer
                            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=SelectEquipmentChipCommand}"
                            CommandParameter="{Binding .}"/>
                    </Border.GestureRecognizers>
                </Border>
            </DataTemplate>
        </BindableLayout.ItemTemplate>
    </HorizontalStackLayout>
</ScrollView>
```

- [ ] **Steg 3: Lägg till utrustnings-badge på exercise-kortet**

Hitta `CollectionView.ItemTemplate > DataTemplate` för Exercise-kortet. Inuti `Grid ColumnDefinitions="12,*,Auto"` finns redan en `Border Grid.Column="2"` med "EGNA"-badgen. Byt ut den mot en kombinerad visning (custom + equipment):

```xml
<StackLayout Grid.Column="2" Orientation="Horizontal" Spacing="4" VerticalOptions="Center">
    <Border IsVisible="{Binding IsCustom}"
            BackgroundColor="{StaticResource ForgeAccentOrangeDim}"
            StrokeShape="RoundRectangle 8"
            StrokeThickness="0" Padding="8,4">
        <Label Text="EGNA" TextColor="{StaticResource ForgeAccentOrange}"
               FontSize="10" FontFamily="BebasNeue" CharacterSpacing="1"/>
    </Border>
    <Border IsVisible="{Binding Equipment, Converter={StaticResource EquipmentNotOtherConverter}}"
            BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface2}, Dark={StaticResource ForgeSurface2}}"
            StrokeShape="RoundRectangle 8"
            StrokeThickness="0" Padding="8,4">
        <Label Text="{Binding Equipment, Converter={StaticResource EquipmentLabelConverter}}"
               TextColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
               FontSize="10" FontFamily="BebasNeue" CharacterSpacing="1"/>
    </Border>
</StackLayout>
```

**OBS:** Konverterarna `EquipmentLabelConverter` och `EquipmentNotOtherConverter` behöver läggas till — se Steg 4.

- [ ] **Steg 4: Skapa `EquipmentLabelConverter` och `EquipmentNotOtherConverter` i `LockIn/Views/Converters.cs`**

Hitta filen med befintliga converters (t.ex. `LockIn/Views/LibraryPage.xaml.cs` eller en separat Converters-fil). Lägg till:

```csharp
public class EquipmentLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not EquipmentType eq) return "";
        return eq switch
        {
            EquipmentType.Barbell      => "SKIVSTÅNG",
            EquipmentType.Dumbbell     => "HANTEL",
            EquipmentType.Cable        => "KABEL",
            EquipmentType.Machine      => "MASKIN",
            EquipmentType.BodyOnly     => "KROPPSVIKT",
            EquipmentType.EZBar        => "EZ-STÅNG",
            EquipmentType.Kettlebell   => "KETTLEBELL",
            EquipmentType.Bands        => "BAND",
            EquipmentType.FoamRoll     => "FOAM ROLL",
            EquipmentType.MedicineBall => "MEDICINBOLL",
            _                          => ""
        };
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
}

public class EquipmentNotOtherConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is EquipmentType eq && eq != EquipmentType.Other;
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
}
```

**Sök efter var befintliga converters definieras** med:
```bash
grep -rn "class.*Converter" LockIn/Views/
```
Lägg till i samma fil. Registrera dem i `LibraryPage.xaml`:
```xml
<views:EquipmentLabelConverter x:Key="EquipmentLabelConverter"/>
<views:EquipmentNotOtherConverter x:Key="EquipmentNotOtherConverter"/>
```

- [ ] **Steg 5: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 6: Commit**

```bash
git add LockIn/Views/LibraryPage.xaml LockIn/Views/
git commit -m "feat(ui): equipment-filter och badge på LibraryPage"
```

---

### Task 4: ExercisePickerPage XAML — equipment-filter + badge

**Files:**
- Modify: `LockIn/Views/ExercisePickerPage.xaml`

**Interfaces:**
- Consumes: `ExercisePickerViewModel.EquipmentChips`
- Consumes: `ExercisePickerViewModel.SelectEquipmentChipCommand`
- Consumes: `ExercisePickerRow.EquipmentLabel` (string)
- Consumes: `EquipmentLabelConverter`, `EquipmentNotOtherConverter` (från Task 3)

- [ ] **Steg 1: Lägg till equipment-chips-rad i CollectionView.Header**

Öppna `LockIn/Views/ExercisePickerPage.xaml`. Hitta muscle-chips ScrollView i `CollectionView.Header`. Lägg till equipment-chips ScrollView direkt efter (identisk struktur som Task 3 Steg 2, men med `AncestorType={x:Type vm:ExercisePickerViewModel}`):

```xml
<ScrollView Orientation="Horizontal"
            HorizontalScrollBarVisibility="Never"
            Margin="16,0,16,12">
    <HorizontalStackLayout
        BindableLayout.ItemsSource="{Binding Source={RelativeSource AncestorType={x:Type vm:ExercisePickerViewModel}}, Path=EquipmentChips}"
        Spacing="6">
        <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="vm:EquipmentChip">
                <Border BackgroundColor="{Binding Background}"
                        StrokeShape="RoundRectangle 22" StrokeThickness="0"
                        Padding="14,0" HeightRequest="34">
                    <Label Text="{Binding Label}"
                           TextColor="{Binding Foreground}"
                           FontFamily="BebasNeue" FontSize="13" CharacterSpacing="1"
                           VerticalOptions="Center"/>
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer
                            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ExercisePickerViewModel}}, Path=SelectEquipmentChipCommand}"
                            CommandParameter="{Binding .}"/>
                    </Border.GestureRecognizers>
                </Border>
            </DataTemplate>
        </BindableLayout.ItemTemplate>
    </HorizontalStackLayout>
</ScrollView>
```

Registrera konverterarna i `ExercisePickerPage.xaml` Resources:
```xml
<views:EquipmentLabelConverter x:Key="EquipmentLabelConverter"/>
<views:EquipmentNotOtherConverter x:Key="EquipmentNotOtherConverter"/>
```

- [ ] **Steg 2: Lägg till equipment-label på exercise-kortet**

Hitta exercise-kortets `DataTemplate x:DataType="vm:ExercisePickerRow"`. Hitta raden med `MuscleLabel`-labeln. Lägg till `EquipmentLabel` som en subtil text bredvid eller under muskelgrupps-labeln:

```xml
<!-- Befintlig MuscleLabel — ändra ej -->
<Label Text="{Binding MuscleLabel}" ... />
<!-- Nytt: Utrustningslabel (dold om tom) -->
<Label Text="{Binding EquipmentLabel}"
       IsVisible="{Binding EquipmentLabel, Converter={StaticResource StringNotEmptyConverter}}"
       FontFamily="BebasNeue" FontSize="10" CharacterSpacing="1"
       TextColor="{AppThemeBinding Light={StaticResource LightMuted}, Dark={StaticResource ForgeMuted}}"
       VerticalOptions="Center"/>
```

**OBS:** `StringNotEmptyConverter` bör redan finnas. Kolla med:
```bash
grep -rn "StringNotEmpty\|StringIsEmpty" LockIn/Views/
```
Om den inte finns, lägg till i samma Converters-fil som Task 3:
```csharp
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is string s && s.Length > 0;
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => null;
}
```

- [ ] **Steg 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 4: Commit**

```bash
git add LockIn/Views/ExercisePickerPage.xaml
git commit -m "feat(ui): equipment-filter och badge på ExercisePickerPage"
```

---

### Task 5: ExerciseProgressPage XAML — metadata-sektion

**Files:**
- Modify: `LockIn/Views/ExerciseProgressPage.xaml`

**Interfaces:**
- Consumes: `ExerciseProgressViewModel.EquipmentName` (string)
- Consumes: `ExerciseProgressViewModel.SecondaryMusclesText` (string)
- Consumes: `ExerciseProgressViewModel.LevelName` (string)
- Consumes: `ExerciseProgressViewModel.MechanicName` (string)
- Consumes: `ExerciseProgressViewModel.ForceName` (string)
- Consumes: `ExerciseProgressViewModel.HasMetadata` (bool)

- [ ] **Steg 1: Invoke `/frontend-design` för detta scope**

Kör: `/frontend-design`
Beskriv scopet: Ny metadatasektion på ExerciseProgressPage — ett kort som visar Equipment, Level, Mechanic, Force och SecondaryMuscles i ett strukturerat grid/layout. Ska passa ihop med befintliga kort (CardFrame-stil). Placeras mellan stats-raden och INSTRUKTIONER-kortet. Dold om `HasMetadata = false`.

- [ ] **Steg 2: Lägg till metadata-kort i `ExerciseProgressPage.xaml`**

Hitta kommentaren `<!-- Description (instruktioner) -->` i `VerticalStackLayout`. Lägg till metadata-kortet OVANFÖR den:

```xml
<!-- Metadata (utrustning, nivå, typ) -->
<Border Style="{StaticResource CardFrame}" Padding="16,14"
        IsVisible="{Binding HasMetadata}">
    <VerticalStackLayout Spacing="10">
        <Label Text="ÖVNINGSINFO" Style="{StaticResource SectionLabel}"/>
        <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto,Auto" ColumnSpacing="12" RowSpacing="8">

            <!-- Utrustning -->
            <VerticalStackLayout Grid.Row="0" Grid.Column="0" Spacing="2">
                <Label Text="UTRUSTNING" Style="{StaticResource SectionLabel}" FontSize="10"/>
                <Label Text="{Binding EquipmentName}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
            </VerticalStackLayout>

            <!-- Nivå -->
            <VerticalStackLayout Grid.Row="0" Grid.Column="1" Spacing="2">
                <Label Text="NIVÅ" Style="{StaticResource SectionLabel}" FontSize="10"/>
                <Label Text="{Binding LevelName}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
            </VerticalStackLayout>

            <!-- Rörelsetyp -->
            <VerticalStackLayout Grid.Row="1" Grid.Column="0" Spacing="2">
                <Label Text="TYP" Style="{StaticResource SectionLabel}" FontSize="10"/>
                <Label Text="{Binding MechanicName}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
            </VerticalStackLayout>

            <!-- Force -->
            <VerticalStackLayout Grid.Row="1" Grid.Column="1" Spacing="2">
                <Label Text="RÖRELSE" Style="{StaticResource SectionLabel}" FontSize="10"/>
                <Label Text="{Binding ForceName}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"/>
            </VerticalStackLayout>

            <!-- Sekundärmuskler (full bredd) -->
            <VerticalStackLayout Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" Spacing="2"
                                 IsVisible="{Binding SecondaryMusclesText, Converter={StaticResource StringNotEmptyConverter}}">
                <Label Text="SEKUNDÄRA MUSKLER" Style="{StaticResource SectionLabel}" FontSize="10"/>
                <Label Text="{Binding SecondaryMusclesText}"
                       FontFamily="DMSansRegular" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText}, Dark={StaticResource ForgeText}}"
                       LineBreakMode="WordWrap"/>
            </VerticalStackLayout>

        </Grid>
    </VerticalStackLayout>
</Border>
```

Registrera `StringNotEmptyConverter` i `ExerciseProgressPage.xaml` Resources om den inte redan finns:
```xml
<views:StringNotEmptyConverter x:Key="StringNotEmptyConverter"/>
```

- [ ] **Steg 3: Bygg och verifiera**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 4: Commit**

```bash
git add LockIn/Views/ExerciseProgressPage.xaml
git commit -m "feat(ui): övningsinfo-sektion på ExerciseProgressPage"
```

---

### Task 6: Slutbygge och version bump

**Files:**
- Modify: `LockIn/LockIn.csproj` — öka `ApplicationVersion` med 1

- [ ] **Steg 1: Hämta nuvarande version**

```bash
grep "ApplicationVersion" LockIn/LockIn.csproj
```

- [ ] **Steg 2: Öka `ApplicationVersion` med 1**

Redigera `LockIn/LockIn.csproj`, t.ex. `<ApplicationVersion>33</ApplicationVersion>` → `<ApplicationVersion>34</ApplicationVersion>`.

- [ ] **Steg 3: Slutbygge**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | grep -E "error|Build succeeded"
```
Förväntat: `Build succeeded` med 0 Error(s)

- [ ] **Steg 4: Commit**

```bash
git add LockIn/LockIn.csproj
git commit -m "chore: version bump inför TestFlight-push"
```
