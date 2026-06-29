# Design Spec — Social delning

**Datum:** 2026-06-29
**Sida:** PostWorkoutPage → ny dela-knapp
**Motivering:** Ger användaren ett sätt att dela passammanfattning som en stilren bild till sociala medier/vänner, utan att lämna appen.

---

## Bakgrund

Efter ett avslutat pass visar PostWorkoutPage statistik (volym, sets, PR, muskler). Idag kan användaren inte dela detta. Featuren renderar en 1080×1080 px PNG off-screen med SkiaSharp och delar den via MAUI:s `Share`-API. Knapp placeras på PostWorkoutPage som en sekundär action — synlig men utan att stjäla fokus från den primära "KLAR"-knappen.

---

## Design

### Bildformat och layout

**Upplösning:** 1080×1080 px (kvadrat — optimal för Instagram, iMessage-förhandsvisning, Twitter)

**Zoner (uppifrån ned):**

| Zon | Höjd (px) | Innehåll |
|-----|-----------|----------|
| Header | 200 | App-logotyptext "LOCKIN" (BebasNeue, stor) + undertext "Strength Training" |
| Hero | 280 | Passnamn (BebasNeue 72 pt) + datum (DMSans 28 pt, muted) |
| Stats | 280 | Tre kolumner — Volym / Sets / PR — med stortal + etikett |
| Muskler | 220 | Upp till 3 muskler + horisontella bars (samma som UI) |
| Footer | 120 | "@lockin.app" eller anpassad tagline + accent-linje |

**Färgschema (alltid mörkt — ingen light-mode variant för delad bild):**

| Token (DesignTokens.cs) | Hex-backup | Användning |
|-------------------------|------------|------------|
| `ForgeBackground` | `#141418` | Hela bakgrunden |
| `ForgeSurface2` | `#222228` | Stats-kort bakgrund |
| `ForgeAccent` | `#B8B8BC` | Rubriker, bars, accent-linje |
| `ForgeAccentBlue` | `#38BDF8` | Volym-siffra |
| `ForgeAccentPurple` | `#A78BFA` | Sets-siffra |
| `ForgeAccentAmber` | `#FBBF24` | PR-siffra (0 = `ForgeMuted`) |
| `ForgeText` | `#E2E8F0` | Passnamn, stats-värden |
| `ForgeMuted` | `#A2A2A2` | Datum, stat-etiketter, muskeletiketter |

**Typsnitt i SkiaSharp:**
- Rubriker/siffror: `BebasNeue` (registrerat i MauiProgram.cs)
- Brödtext/etiketter: `DMSansMedium` / `DMSansRegular`
- SkiaSharp läser dessa via `SKTypeface.FromFamilyName("BebasNeue")` — font måste finnas i app bundle

### Arkitektur

```
PostWorkoutViewModel
  └── [RelayCommand] ShareWorkoutAsync()
        └── WorkoutShareService.CreateShareImageAsync(ShareImageData)
              └── SKBitmap (1080×1080)
                  SKCanvas.DrawXxx(...)
              └── SKImage.FromBitmap → Encode(SKEncodedImageFormat.Png, 90)
              └── File.WriteAllBytes → temp-fil
              └── Share.Default.RequestAsync(ShareFileRequest)
```

**`ShareImageData`** — en enkel record som samlar ihop all data PostWorkoutViewModel redan har:

```csharp
public record ShareImageData(
    string  TemplateName,
    string  DateDisplay,      // t.ex. "28 jun 2026"
    string  Duration,
    string  TotalVolume,
    string  TotalSets,
    string  PrCount,
    IReadOnlyList<(string Name, double Fraction)> TopMuscles  // max 3
);
```

**`WorkoutShareService`** — ny klass i `LockIn/Services/WorkoutShareService.cs`. Hanterar:
1. Bygga `SKBitmap` + `SKCanvas`
2. Rita alla zoner med SkiaSharp (inline, inga externa resurser förutom typsnitt)
3. Spara PNG till `Path.Combine(FileSystem.CacheDirectory, "share_preview.png")`
4. Returnera filsökvägen

Ingen DI-registrering behövs — `WorkoutShareService` har inga injekterade beroenden och kan instansieras direkt i viewmodeln (`new WorkoutShareService()`), eller registreras som Transient om DI föredras för testbarhet.

**Placering av knapp på PostWorkoutPage:**
- Läggs in i den befintliga floating-knapp-zonen (ZIndex 23) som en andra knapp bredvid "KLAR"
- Layout: `Grid ColumnDefinitions="*,Auto"` — "KLAR" fyller `*`, dela-knapp är `Auto` (56×56 px ikoncirkel)
- Alternativt: knapp i scroll-innehållet under stats-raden — enklare XAML men kräver scroll för att nå

**Vald placering:** Ikoncirkel i floating-zonen, till vänster om KLAR-knappen. Motivering: alltid synlig utan scroll, passar befintligt UI-mönster.

### Felhantering

| Scenario | Beteende |
|----------|----------|
| SkiaSharp-rendering kraschar | Toast: "Kunde inte skapa bild" — ingen krasch |
| `Share.Default.RequestAsync` avbryts av användaren | Tyst — normalt flöde |
| Teckensnittet saknas i SKTypeface.FromFamilyName | Fallback till systemfont (SF Pro) — graceful degradation |
| Temp-filen kan inte skapas (diskutrymme) | Toast med felmeddelande |

### i18n

Nya strängar i `AppResources.resx` (sv) + `AppResources.en.resx` (en):

| Nyckel | Svenska | Engelska |
|--------|---------|----------|
| `PostWorkout_Share` | `Dela` | `Share` |
| `PostWorkout_Share_Error` | `Kunde inte skapa bild` | `Could not create image` |
| `PostWorkout_Share_ImageTitle` | `LockIn pass` | `LockIn workout` |
| `Share_Footer_Tagline` | `Vana Strength` | `Vana Strength` |

### Vad bilden INTE innehåller

- Sparlines, linjegrafer, heatmaps
- Foton från passet
- Achievements
- Logotypfil/PNG-bild (text-rendering räcker — undviker asset-laddning off-screen)

---

## Avgränsningar

- Bilden är alltid mörkt tema oavsett enhetens tema
- Inga valbara bildformat — alltid PNG 1080×1080
- Ingen förhandsvisning i appen — dela-dialogen (system share sheet) ger förhandsvisning
- `WorkoutShareService` är inte återanvändbar för andra sidor (YAGNI) — om det behövs senare extraheras en `IShareImageRenderer`-abstraktion

---

## Beroenden

- SkiaSharp 3.116.1 (redan installerat)
- MAUI `Share.Default` (inbyggt)
- CommunityToolkit.Mvvm `[RelayCommand]` (redan installerat)
- `AppResources` i18n (befintlig infrastruktur)
- Typsnitt `BebasNeue`, `DMSansMedium`, `DMSansRegular` (redan registrerade)
