# Mallkopiering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Låt användaren kopiera en befintlig träningmall (WorkoutTemplate) med ett knapptryck i LibraryPage.

**Architecture:** Ny `DuplicateTemplateAsync`-metod i DatabaseService kopierar mall + alla övningar. LibraryViewModel får ett nytt `DuplicateTemplateCommand` som anropar metoden och navigerar direkt till `TemplateEditPage` med den nya mallen. LibraryPage.xaml får en kopia-knapp bredvid befintliga play/delete-knappar.

**Tech Stack:** sqlite-net-pcl, CommunityToolkit.Mvvm, .NET MAUI 10 XAML

## Global Constraints

- Alla färger via `StaticResource` (XAML) eller `DesignTokens.*` (C#) — inga hårdkodade hex
- `[RelayCommand]` på `async Task`-metoder
- i18n: alla UI-strängar via `AppResources` — lägg till i `.resx` + `.en.resx` + `AppResources.cs`
- Navigation till TemplateEditPage: `Shell.Current.GoToAsync(nameof(TemplateEditPage), new Dictionary<string, object> { { "TemplateId", id } })`
- Commit efter varje task

---

### Task 1: DatabaseService.DuplicateTemplateAsync + i18n-sträng

**Files:**
- Modify: `LockIn/Services/DatabaseService.cs`
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Produces: `public async Task<int> DuplicateTemplateAsync(WorkoutTemplate source, string newName)` — returnerar Id på nya mallen

**Context:**
`GetTemplateExercisesAsync(int templateId)` returnerar `List<TemplateExercise>` sorterade.
`ReplaceTemplateExercisesAsync(int templateId, List<TemplateExercise> exercises)` är atomär transaktion.
`SaveTemplateAsync(WorkoutTemplate)` returnerar Id (int).
`TemplateExercise`-fält att kopiera: `ExerciseId, OrderIndex, Sets, Reps, TargetWeight, DefaultRestSeconds, TargetRepsMin, TargetRepsMax, WeightIncrementKg, AutoProgressMode, SupersetGroupId`.

- [ ] **Step 1: Lägg till `DuplicateTemplateAsync` i DatabaseService.cs**

Lägg till metoden direkt efter `DeleteTemplateAsync`:

```csharp
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
```

- [ ] **Step 2: Lägg till i18n-sträng i AppResources.resx**

I `LockIn/Resources/Strings/AppResources.resx`, lägg till (alfabetiskt bland Library_-nycklar):

```xml
<data name="Library_DuplicateTemplate" xml:space="preserve">
  <value>Kopiera</value>
</data>
```

- [ ] **Step 3: Lägg till i18n-sträng i AppResources.en.resx**

I `LockIn/Resources/Strings/AppResources.en.resx`:

```xml
<data name="Library_DuplicateTemplate" xml:space="preserve">
  <value>Copy</value>
</data>
```

- [ ] **Step 4: Lägg till wrapper-property i AppResources.cs**

```csharp
public static string Library_DuplicateTemplate => Get(nameof(Library_DuplicateTemplate));
```

- [ ] **Step 5: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add LockIn/Services/DatabaseService.cs LockIn/Resources/Strings/AppResources.resx LockIn/Resources/Strings/AppResources.en.resx LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(library): add DuplicateTemplateAsync and i18n string"
```

---

### Task 2: LibraryViewModel + LibraryPage.xaml

**Files:**
- Modify: `LockIn/ViewModels/LibraryViewModel.cs`
- Modify: `LockIn/Views/LibraryPage.xaml`

**Interfaces:**
- Consumes: `DatabaseService.DuplicateTemplateAsync(WorkoutTemplate source, string newName)` (Task 1)
- Consumes: `AppResources.Library_DuplicateTemplate` (Task 1)

**Context:**
Befintlig template-rad i LibraryPage.xaml:
```xml
<Grid ColumnDefinitions="12,*,44,44" ColumnSpacing="10">
    <!-- Col 0: röd dot -->
    <!-- Col 1: Name label -->
    <!-- Col 2: Play-knapp (coral Border + ic_play.png) -->
    <!-- Col 3: Delete-knapp (Button Text="✕") -->
</Grid>
```

Navigation till TemplateEditPage: `Shell.Current.GoToAsync(nameof(TemplateEditPage), new Dictionary<string, object> { { "TemplateId", id } })`

Befintlig `LoadTemplatesAsync()` i LibraryViewModel synkar alla mallar till `Templates`-kollektionen. Anropa den efter kopiering för att ladda om listan, ELLER lägg till manuellt om `WorkoutTemplate`-objektet finns direkt.

Namnmönster för kopian: `$"Kopia av {template.Name}"`.

- [ ] **Step 1: Lägg till `DuplicateTemplateCommand` i LibraryViewModel.cs**

Lägg till direkt efter `EditTemplateAsync`:

```csharp
[RelayCommand]
private async Task DuplicateTemplateAsync(WorkoutTemplate template)
{
    var newName = $"Kopia av {template.Name}";
    var newId = await db.DuplicateTemplateAsync(template, newName);
    await LoadTemplatesAsync();
    await Shell.Current.GoToAsync(nameof(TemplateEditPage), new Dictionary<string, object>
    {
        { "TemplateId", newId }
    });
}
```

- [ ] **Step 2: Uppdatera Grid i LibraryPage.xaml — utöka till 5 kolumner**

Hitta raden:
```xml
<Grid ColumnDefinitions="12,*,44,44" ColumnSpacing="10">
```

Ändra till:
```xml
<Grid ColumnDefinitions="12,*,44,44,44" ColumnSpacing="10">
```

- [ ] **Step 3: Ändra befintlig delete-knapp till kolumn 4**

Hitta:
```xml
<Button Grid.Column="3"
        Text="✕"
```

Ändra `Grid.Column="3"` → `Grid.Column="4"`.

- [ ] **Step 4: Lägg till kopia-knapp i kolumn 3**

Lägg till mellan play-knappen och delete-knappen (dvs direkt efter play-Border-blocket, före Button Text="✕"):

```xml
<Button Grid.Column="3"
        Text="⧉"
        BackgroundColor="Transparent"
        TextColor="{StaticResource ForgeMuted}"
        FontSize="16" HeightRequest="44"
        MinimumWidthRequest="44"
        Padding="0"
        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:LibraryViewModel}}, Path=DuplicateTemplateCommand}"
        CommandParameter="{Binding .}"/>
```

- [ ] **Step 5: Verifiera att projektet bygger**

```bash
cd C:\Users\JosefAxen\Gym
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug 2>&1 | tail -5
```

Förväntat: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add LockIn/ViewModels/LibraryViewModel.cs LockIn/Views/LibraryPage.xaml
git commit -m "feat(library): add template copy button and DuplicateTemplateCommand"
```
