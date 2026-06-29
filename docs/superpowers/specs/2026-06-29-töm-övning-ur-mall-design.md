# Spec: Töm en enskild övning ur mall

**Datum:** 2026-06-29
**Storlek:** S (liten, inga komplexa beroenden)

## Problemformulering

I `TemplateEditPage` kan användaren ta bort en övning men inte snabbt nollställa dess sets/reps/weight om man har editerat manuellt. Funktionen "Töm" ger ett enkelt sätt att återgå till blank state utan att ta bort övningen från mallen.

## Design

### Blank state-värden
- `Sets = 3` (standard-default)
- `Reps = 0` (tom, inte 8 — för att signalera att det är nollställt)
- `TargetWeight = 0` / `WeightText = ""`
- Progression-fält nollställs: `ProgressionEnabled = false`, reps-min/max = "", increment = "2.5"
- Vila (`RestSeconds`) behålls oförändrat — det är inte ett "inmatat värde" på samma sätt

### UI-placering

En liten "töm"-knapp (⊘-ikon eller text "TÖM") placeras i exercise-card header-raden (`Grid ColumnDefinitions="*,44"`) som en tredje knapp. Kolumndefinitionen utökas till `"*,44,44"`.

Knappen placeras:
- **Grid.Column="1"** — ny "Töm"-knapp med text "⌫" eller label "TÖM"
- **Grid.Column="2"** — befintlig "×"-knapp (ta bort) behåller sin plats

Stil: `BackgroundColor="Transparent"`, `TextColor="{StaticResource ForgeMuted}"` — diskret, stör inte flödet.

### ViewModel

`ClearExerciseCommand(TemplateExerciseRow row)` i `TemplateEditViewModel`:
- Sätter `row.SetsText = "3"`
- Sätter `row.RepsText = "0"`
- Sätter `row.WeightText = ""`
- Sätter `row.ProgressionEnabled = false`
- Sätter `row.TargetRepsMinText = ""`
- Sätter `row.TargetRepsMaxText = ""`
- Sätter `row.WeightIncrementText = "2.5"`

Inga DB-anrop behövs — sparandet sker alltid via `SaveCommand`. Ingen ny `DatabaseService`-metod behövs.

### i18n

| Nyckel | sv | en |
|---|---|---|
| `TemplateEdit_ClearExercise` | `TÖM` | `CLEAR` |

### Filer som ändras
1. `LockIn/Resources/Strings/AppResources.resx` — lägg till nyckel
2. `LockIn/Resources/Strings/AppResources.en.resx` — lägg till nyckel
3. `LockIn/Resources/Strings/AppResources.cs` — lägg till property
4. `LockIn/ViewModels/TemplateEditViewModel.cs` — lägg till `ClearExerciseCommand`
5. `LockIn/Views/TemplateEditPage.xaml` — lägg till knapp i exercise-card header
