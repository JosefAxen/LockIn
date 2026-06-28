# VO2Max-estimat — Design Spec

**Mål:** Visa användarens senaste VO2Max-värde (från Apple Watch via HealthKit) som ett konditionsmått på HemPage.

## Kontext

Apple Watch loggar VO2Max automatiskt via `HKQuantityTypeIdentifier.VO2Max`. HealthKit exponerar det som ett `HKQuantitySample` i enheten `ml/(kg·min)`. Datan är Apple Watch-exklusiv — finns inte om användaren saknar klocka. Befintlig arkitektur: `IHealthService`-abstraktionen hanterar all HealthKit-åtkomst; `HemViewModel` laddar hälsodata parallellt i `LoadHealthDataAsync`.

## Arkitektur

### 1. IHealthService + HealthKitService + NullHealthService

**Ny metod:**
```csharp
Task<double> GetVO2MaxAsync();
```
Returnerar det senaste VO2Max-värdet i ml/(kg·min), eller `0.0` om ingen data finns.

**HealthKitService-implementation:**
- Lägg till `HKQuantityType.Create(HKQuantityTypeIdentifier.VO2Max)!` i `s_readTypes`
- Ny statisk unit: `private static readonly HKUnit s_vo2MaxUnit = HKUnit.FromString("ml/(kg·min)");`
- Query-mönster: `HKSampleQuery` med `predicate: null` (alla tidpunkter), `limit: 1`, sorterat på `HKSample.SortIdentifierStartDate` descending → tar senaste sample
- Returnerar `sample.Quantity.GetDoubleValue(s_vo2MaxUnit)` eller `0.0`
- Timeout: 10 sekunder (samma som övriga HealthKit-metoder)
- `HKHealthStore.IsHealthDataAvailable`-guard (returnerar `0.0` om ej tillgänglig)

**NullHealthService:** `public Task<double> GetVO2MaxAsync() => Task.FromResult(0.0);`

### 2. i18n

Två nya nycklar i `AppResources.resx` (svenska) och `AppResources.en.resx` (engelska):

| Nyckel | Svenska | Engelska |
|--------|---------|---------|
| `Hem_VO2Max_Label` | `VO2MAX` | `VO2MAX` |
| `Hem_VO2Max_Sub` | `ml/kg/min` | `ml/kg/min` |

Nya C#-properties i `AppResources.cs`:
```csharp
public static string Hem_VO2Max_Label => Get(nameof(Hem_VO2Max_Label));
public static string Hem_VO2Max_Sub   => Get(nameof(Hem_VO2Max_Sub));
```

### 3. HemViewModel

Ny `[ObservableProperty]`:
```csharp
[ObservableProperty] private string _vo2MaxText = "–";
```

Laddning i `LoadHealthDataAsync` — lägg till i det befintliga `Task.WhenAll`-anropet:
```csharp
var vo2MaxTask = health.GetVO2MaxAsync();
// ... (efter Task.WhenAll)
var vo2Max = await vo2MaxTask;
Vo2MaxText = vo2Max > 0 ? $"{vo2Max:F1}" : "–";
```

Exempel-output: `"52.3"` (en decimal, inga enheter i text — enheten visas i sub-labeln på HemPage).

### 4. HemPage.xaml

Nytt full-bredd kort placerat direkt efter den befintliga 2×2-griden (fortfarande inuti samma `VerticalStackLayout`). Layouten matchar de befintliga datakorten:

```xml
<Border Padding="16,14,16,14"
        BackgroundColor="{StaticResource ForgeSurface}"
        StrokeShape="RoundRectangle 16"
        StrokeThickness="0">
    <VerticalStackLayout Spacing="0">
        <Label Text="{loc:Localize Hem_VO2Max_Label}"
               Style="{StaticResource SectionLabel}"/>
        <Label Text="{Binding Vo2MaxText}"
               FontSize="36"
               FontFamily="BebasNeue"
               TextColor="{StaticResource ForgeText}"/>
        <Label Text="{loc:Localize Hem_VO2Max_Sub}"
               Style="{StaticResource MutedLabel}"/>
    </VerticalStackLayout>
</Border>
```

## Kantfall

- Saknar Apple Watch: `GetVO2MaxAsync()` returnerar `0.0` → `Vo2MaxText = "–"` → kortet syns med platshållare (konsekvent med övriga hälsokort vid saknad data)
- HealthKit-timeout (10s): returnerar `0.0`, ingen krasch
- Negativt/noll-värde: `> 0`-guard i VM säkerställer att `"–"` visas

## Inga ändringar i

- DB-schema, databastjänster
- Navigation, sessions-logik
- Befintliga HealthKit-metoder
