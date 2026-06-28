# Jämförelse sessionvis — Design Spec

**Mål:** Visa en kompakt sammanfattning av föregående sessions sets per övning i `ActiveWorkoutPage`, direkt under övningsnamnet i varje övningssektion.

## Kontext

Befintlig kod hämtar redan föregående sessions sets via `GetLastSessionSetsAsync(exerciseId, excludeSessionId)` i `ActiveWorkoutViewModel.AddExerciseSectionAsync` — resultatet används idag bara för per-set hints (`PrevWeightHint`/`PrevRepsHint`) som visas som Entry-platshållare. Ingen aggregerad summering finns.

## Arkitektur

Tre lager, inga databas-ändringar:

### 1. WorkoutExerciseSection (ViewModel)

Lägg till två properties:

```csharp
public string PrevSessionSummary { get; set; } = "";
public bool HasPrevSession => !string.IsNullOrEmpty(PrevSessionSummary);
```

`PrevSessionSummary` sätts en gång vid sektionsskapande och ändras inte — `set;` räcker, ingen `[ObservableProperty]`.

### 2. ActiveWorkoutViewModel (Beräkning)

I `AddExerciseSectionAsync`, direkt efter `GetLastSessionSetsAsync`-anropet (rad ~144), beräkna summary innan set-loopen och tilldela `section.PrevSessionSummary`:

```csharp
var prevSets = await db.GetLastSessionSetsAsync(exercise.Id, _session!.Id);

string prevSummary = "";
if (prevSets.Count > 0)
{
    var parts = prevSets
        .Select(s => s.SetType == SetType.Time && s.DurationSeconds > 0
            ? $"{s.DurationSeconds}s"
            : s.WeightKg > 0
                ? $"{s.WeightKg:G}×{s.Reps}"
                : s.Reps > 0
                    ? $"{s.Reps}r"
                    : null)
        .Where(p => p is not null);
    prevSummary = parts.Any() ? "Förra: " + string.Join(" · ", parts) : "";
}
section.PrevSessionSummary = prevSummary;
```

Exempelutdata: `"Förra: 80×8 · 80×8 · 82.5×6"`, `"Förra: 45s · 45s"`, `"Förra: 12r · 10r · 8r"`.

### 3. ActiveWorkoutPage.xaml (UI)

Övningshuvudet har idag `ColumnDefinitions="12,*,Auto,Auto,Auto"` där Column 1 är en enkel `Label`. Ersätt den med `VerticalStackLayout`:

Från:
```xml
<Label Grid.Column="1"
       Text="{Binding ExerciseName}"
       FontFamily="DMSansMedium" FontSize="16"
       VerticalOptions="Center"/>
```

Till:
```xml
<VerticalStackLayout Grid.Column="1" VerticalOptions="Center" Spacing="2">
    <Label Text="{Binding ExerciseName}"
           FontFamily="DMSansMedium" FontSize="16"/>
    <Label Text="{Binding PrevSessionSummary}"
           IsVisible="{Binding HasPrevSession}"
           FontFamily="DMSansRegular" FontSize="11"
           TextColor="{StaticResource ForgeMuted}"
           LineBreakMode="TailTruncation"/>
</VerticalStackLayout>
```

Den andra labeln är osynlig (`IsVisible=False`) när `HasPrevSession` är false — sektionshuvudet ser likadant ut som idag för övningar utan historik.

## Kantfall

- Inga föregående sets → `PrevSessionSummary = ""` → label dold, layout oförändrad
- Alla sets har tomt data (weight=0, reps=0) → `parts` tom → `prevSummary = ""` → label dold
- Lång sträng (många sets) → `LineBreakMode="TailTruncation"` klipper
- Blandade set-typer (t.ex. varm-upp + normala) → varje set formateras individuellt

## Inga ändringar i

- `DatabaseService` — `GetLastSessionSetsAsync` finns och fungerar
- `LoggedSetRow` — per-set hints påverkas inte
- Animationer, navigering, Session/DB-schema
