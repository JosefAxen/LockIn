# Design Spec — Contextual Data Chips (Coach Prompts ersättning)

**Datum:** 2026-06-27  
**Sida:** HemPage  
**Erstätter:** `CoachPrompts` (statisk IReadOnlyList<string>, oanvänd i XAML)

---

## Bakgrund

HemViewModel har en statisk `CoachPrompts`-property med 3 hårdkodade strängar som inte är ansluten till något XAML-UI. Brainstorming + impeccable critique ledde till följande slutsats:

- Frågeformat ("hur återhämtar jag mig bäst?") bryter mot brand-principen "Ingen motivationsteater"
- Redundans med befintligt `TodayRecommendation`-kort som redan täcker recovery/load
- Lösning: **Data-chips** — korta faktapåståenden som expanderar vid tryck, ingen coachingkänsla

---

## Designprincip

Chips är **precisionsinstrument**, inte coach. De visar fakta som är **komplementära** till TodayRecommendation-kortet:

| TodayRecommendation | Data-chips |
|---------------------|-----------|
| Recovery-nivå idag | Vilken muskelgrupp är mest eftersatt |
| Rekommendation (vila/träna) | Hur nära ett PR du är |
| Antal dagar i rad | Volymtrend vecka-mot-vecka |
| Uppehållslängd | Veckosammanfattning |

Chips har **ingen sektionsrubrik** — de platsar efter rekommendationskortet som stilla extra kontext, inte en sektionsenhet.

---

## Arkitektur

### Nya filer

| Fil | Roll |
|-----|------|
| `LockIn/Services/CoachPromptEngine.cs` | Statisk klass, utvärderar kontext → chips |
| `LockIn/Models/CoachChip.cs` | Record: PromptId, ChipText, DetailHeader, DetailBody |
| `LockIn/Models/CoachContext.cs` | Record: all input-data till engine |
| `LockIn/Views/CoachChipSheet.xaml` | CommunityToolkit.Maui Popup (bottom sheet) |
| `LockIn/Views/CoachChipSheet.xaml.cs` | Code-behind |

### Ändrade filer

| Fil | Ändring |
|-----|---------|
| `HemViewModel.cs` | Byt `IReadOnlyList<string> CoachPrompts` → `ObservableCollection<CoachChip> CoachChips` + LoadCoachChipsAsync() |
| `HemPage.xaml` | Lägg till horisontellt chip-rad efter IDAG-REKOMMENDATION |
| `AppResources.resx` / `.en.resx` | Chip-texter, detail-texter |
| `AppResources.cs` | Wrapper-properties |

---

## CoachContext

```csharp
public record CoachContext(
    IReadOnlyList<WorkoutSession> RecentSessions,       // senaste 7 dagar
    IReadOnlyList<WorkoutSession> WeekSessions,         // denna vecka (mån–nu)
    IReadOnlyList<WorkoutSession> PrevWeekSessions,     // förra veckan (mån–sön)
    IReadOnlyDictionary<MuscleGroup, double> MuscleScores, // från GetMuscleScoresAsync
    double RecoveryPct,                                 // 0–100, från befintlig beräkning
    double? NearestPRGapKg,                             // null = inget nära PR, annars kg kvar
    string? NearestPRExerciseName,
    int DaysSinceLastWorkout,
    double ThisWeekVolumeKg,
    double PrevWeekVolumeKg
);
```

`NearestPRGapKg` kräver en ny `DatabaseService.GetNearestPRGapAsync()`:
1. Hämta alla övningar med loggade sets senaste 30 dagarna
2. Per övning: `recentMax = MAX(Weight) WHERE CompletedAt > -30d`, `allTimeMax = MAX(Weight) alltid`
3. `gap = allTimeMax - recentMax`
4. Returnera övningen med minst `gap` WHERE `0 < gap <= 10`
5. Om ingen ≤ 10 kg: returnera null

---

## CoachChip

```csharp
public record CoachChip(
    string PromptId,       // stabil nyckel, t.ex. "muscle-gap-legs"
    string ChipText,       // kort faktapåstående, max ~25 tecken
    string DetailHeader,   // bottom sheet H1
    string DetailBody      // bottom sheet body-text
);
```

---

## CoachPromptEngine — Chip-pool

### Chip 1 — PR-proximity  
- **Id:** `pr-proximity`  
- **Aktivering:** `NearestPRGapKg != null && NearestPRGapKg <= 10`  
- **Text:** `"{ExerciseName}: {gap:F1} kg från PR"`  
- **Detail:** Nuvarande max-vikt + PR + senaste sets för den övningen  
- **Cooldown:** 24h (återkommer dagligen — relevansen ändras snabbt)  
- **Prioritet:** 1 (högst — konkret, actionbar)

### Chip 2 — Muskelgap  
- **Id:** `muscle-gap-{muscleGroup}`  
- **Aktivering:** Muskelgrupp med score == 0.0 (ingen träning i 7 dagar), exkluderar `FullBody` och `Other`. Om flera grupper uppfyller: välj "tyngre" grupp (prioritetsordning: Legs > Back > Chest > Shoulders > Arms/Core).  
- **Notering:** `GetMuscleScoresAsync()` tittar endast på senaste 7 dagarna — score 0.0 = 0 sets den veckan.  
- **Text:** `"{MuskleName}: länge sedan"`  
- **Detail:** Förklaring + estimerat antal dagar (om session-data finns via `RecentSessions`)  
- **Cooldown:** 48h, men nyckel innehåller muskelgrupp — ny muskel = nytt chip  
- **Prioritet:** 2

### Chip 3 — Volymtrend  
- **Id:** `volume-trend`  
- **Aktivering:** `PrevWeekVolumeKg > 0 && |Δ| / PrevWeekVolumeKg > 0.15` (>15% förändring)  
- **Text (upp):** `"Volym: +{pct}% mot förra veckan"`  
- **Text (ned):** `"Volym: −{pct}% mot förra veckan"`  
- **Detail:** Denna vecka total vs förra veckan total  
- **Cooldown:** 48h  
- **Prioritet:** 3

### Chip 4 — Veckosammanfattning  
- **Id:** `week-summary`  
- **Aktivering:** `DayOfWeek >= DayOfWeek.Thursday && WeekSessions.Count >= 2`  
- **Text:** `"Veckan: {N} pass, {totalKg:N0} kg"`  
- **Detail:** Alla pass denna vecka med datum + volym  
- **Cooldown:** 24h  
- **Prioritet:** 4

### Chip 5 — Veckostreak  
- **Id:** `streak-weeks`  
- **Aktivering:** Minst 2 konsekutiva veckor med ≥1 pass (`CalculateWeekStreak()`)  
- **Text:** `"{N} veckor i rad"`  
- **Detail:** Veckostreakhistorik  
- **Cooldown:** 72h  
- **Prioritet:** 5 (lägst)

### Fallback  
Om noll chips aktiveras: visa inga chips alls. Sektionen är tom/dold. **Ingen placeholder.**

---

## Cooldown-mekanism

```csharp
// Spara i Preferences:
// Key: "coach_chip_{promptId}_shown_at"  
// Value: DateTime.UtcNow.ToString("O")

// Vid evaluering:
if (Preferences.ContainsKey(key))
{
    var lastShown = DateTime.Parse(Preferences.Get(key, ""));
    if (DateTime.UtcNow - lastShown < cooldown) skip;
}
```

Markeras som visad vid **tap** (inte vid rendering) — en chip som scrollas förbi räknas inte som läst.

---

## EvaluateAsync

```
CoachPromptEngine.EvaluateAsync(CoachContext ctx) → IReadOnlyList<CoachChip>
```

1. Skapa candidates-lista (alla chips vars villkor är uppfyllda)
2. Filtrera bort chips vars cooldown inte passerat
3. Sortera efter prioritet
4. Returnera upp till **2 chips** (max 2 för att inte ta för mycket plats)

---

## HemViewModel-ändringar

```csharp
// Ersätt:
public IReadOnlyList<string> CoachPrompts { get; } = new[] { ... };

// Med:
[ObservableProperty]
private ObservableCollection<CoachChip> _coachChips = new();
```

`LoadCoachChipsAsync()` kallas i slutet av `LoadAsync()`. Bygger `CoachContext` från data som redan är laddad + 2 extra queries (PrevWeekSessions + NearestPRGap).

---

## HemPage.xaml — UI-placering

**Placeras:** direkt efter IDAG-REKOMMENDATION-kortet (rad 129), före ringarna.

```xml
<!-- ═══ DATA-CHIPS ═══ -->
<CollectionView ItemsSource="{Binding CoachChips}"
                IsVisible="{Binding CoachChips.Count, Converter={x:Static views:GreaterThanZeroConverter.Instance}}"
                Margin="16,10,0,0" HeightRequest="44"
                HorizontalScrollBarVisibility="Never">
    <CollectionView.ItemsLayout>
        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="8"/>
    </CollectionView.ItemsLayout>
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:CoachChip">
            <Border StrokeShape="RoundRectangle 10"
                    Padding="12,0"
                    HeightRequest="36"
                    BackgroundColor="{AppThemeBinding Light={StaticResource LightSurface2},
                                                     Dark={StaticResource ForgeSurface2}}"
                    Stroke="{AppThemeBinding Light={StaticResource LightBorder},
                                            Dark={StaticResource ForgeBorderLight}}"
                    StrokeThickness="1">
                <Label Text="{Binding ChipText}"
                       FontFamily="DMSansMedium" FontSize="13"
                       TextColor="{AppThemeBinding Light={StaticResource LightText},
                                                  Dark={StaticResource ForgeText}}"
                       VerticalOptions="Center"/>
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnChipTapped"/>
                </Border.GestureRecognizers>
            </Border>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**OnChipTapped** i code-behind:
1. Hämta `CoachChip` från `(sender as VisualElement)?.BindingContext`
2. Markera chip-id som visat i Preferences
3. Öppna `CoachChipSheet` via `MauiPopup.ShowPopupAsync`

---

## CoachChipSheet (Bottom Sheet)

`CommunityToolkit.Maui.Popup` med:
- `CanBeDismissedByTappingOutsideOfPopup = true`
- Dark surface bakgrund (`ForgeSurface`)
- Header: `DetailHeader` i `BebasNeue` 28px
- Body: `DetailBody` i `DMSansRegular` 14px
- Stäng-knapp (×) uppe till höger

Inga knappar som "Visa i biblioteket" etc. — Scope: information, inte navigation.

---

## i18n-nycklar (nya)

Chip-texterna byggs dynamiskt i engine med string.Format. Separata nycklar per chip:

```
Hem_Chip_PRProximity_Text     = "{0}: {1:F1} kg från PR"
Hem_Chip_MuscleGap_Text       = "{0}: länge sedan"
Hem_Chip_VolumeTrendUp_Text   = "Volym: +{0}% mot förra"
Hem_Chip_VolumeTrendDown_Text = "Volym: −{0}% mot förra"
Hem_Chip_WeekSummary_Text     = "Veckan: {0} pass, {1:N0} kg"
Hem_Chip_StreakWeeks_Text      = "{0} veckor i rad"

Hem_Chip_PRProximity_Header   = "{0} — nära PR"
Hem_Chip_PRProximity_Body     = "{0}: {1:F1} kg kvar till nytt PR.\nNuvarande max: {2:F1} kg\nPR: {3:F1} kg"

Hem_Chip_MuscleGap_Header     = "{0} — länge sedan"
Hem_Chip_MuscleGap_Body       = "Senaste passet för {0} var för mer än {1} dagar sedan."

Hem_Chip_VolumeTrend_Header   = "Volymtrend"
Hem_Chip_VolumeTrend_Body     = "Den här veckan: {0:N0} kg\nFörra veckan: {1:N0} kg"

Hem_Chip_WeekSummary_Header   = "Veckans träning"
Hem_Chip_WeekSummary_Body     = "{0} pass · {1:N0} kg total volym"

Hem_Chip_StreakWeeks_Header   = "Veckostreak"
Hem_Chip_StreakWeeks_Body     = "{0} veckor i rad med minst ett pass."
```

Engelska-nycklar läggs till i `AppResources.en.resx` parallellt.

---

## Kantsäkring

| Kantfall | Hantering |
|----------|-----------|
| Inga chips aktiveras | CollectionView dold (`IsVisible=false`) — ingen tom sektion |
| `NearestPRGapKg` query kastar | try-catch, returnera null |
| `PrevWeekVolumeKg = 0` | Volume-trend-chip aktiveras inte |
| Chip tappas snabbt 2× | Popup-öppning ignoreras om redan öppen |
| Muskelgrupp utan namn-mappning | Fallback: `Other`-chip visas aldrig |

---

## CalculateWeekStreak

Används för Chip 5. Beräknar antal **konsekutiva veckor** (inte dagar) med minst 1 avklarat pass:

```
WeekStreak = 0
currentWeekStart = MondayOfCurrentWeek()
loop backwards week by week:
    om sessions.Any(s => s.CompletedAt i [weekStart, weekStart+7)):
        WeekStreak++
    annars: break
```

Kräver sessions från ~12 veckors historik (84 dagar). En ny `db`-query i `LoadCoachChipsAsync` med 90-dagars cutoff.

---

## Vad som INTE ingår

- AI-genererade texter — allt är regel-baserat med formatsträngar
- Navigation in i biblioteket eller övning från bottom sheet
- Användarkonfiguration av vilka chips som syns
- Animering av chip-listan (kan läggas till i polish-pass)
