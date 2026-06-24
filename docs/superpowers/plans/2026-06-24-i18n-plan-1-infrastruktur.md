# i18n Plan 1 — Infrastruktur + OnboardingPage som pilot

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sätt upp .resx-resurssystem + `LocalizeExtension` + översätt `OnboardingPage` som proof-of-concept så LockIn talar engelska på en enhet med engelskt iOS-locale.

**Architecture:** Standard .NET `.resx`-mönster — `AppResources.resx` (svenska, default) + `AppResources.en.resx` (engelska). Hand-skriven statisk wrapper `AppResources.cs` läser via `ResourceManager` (vi förlitar oss inte på IDE-genererad designer-fil för CI-stabilitet). Markup extension `{loc:Localize Key}` för XAML, `AppResources.Key` direkt i C#. `CultureInfo.CurrentUICulture` sätts automatiskt av .NET MAUI från iOS-locale.

**Tech Stack:** .NET MAUI 10, net10.0-ios, System.Resources.ResourceManager (.NET BCL).

## Global Constraints

- Övningsnamn översätts INTE — `Exercises`-tabellen och `WorkoutPrograms.cs` `Name`-fält rörs ej.
- Nyckelkonvention: `Område_Element` eller `Område_Action`. Område-prefix för Onboarding: `Onboarding_`. Återanvändbara knappar: `Common_`.
- Versal-strängar (VÄLKOMMEN, FORTSÄTT) lagras som versaler i resx.
- Vid pluralisering där "1 vs N" syns: suffix `_One` / `_Many`. Plan 1 berör inte detta (Onboarding har inga pluraliseringsfall).
- Saknad nyckel: `LocalizeExtension` returnerar nyckelnamnet bokstavligt så utvecklarmissar syns utan att krascha.
- Inga unit-tests finns i projektet — verifiering = `dotnet build` + manuell körning i simulator/enhet.
- Hårdkodade hex-strängar förbjudna utanför `Colors.xaml` och `DesignTokens.cs` (oförändrat).
- Pusha aldrig utan explicit instruktion. `ApplicationVersion` bumps med +1 inför push.

---

## Filöversikt

| Fil | Roll | Status |
|-----|------|--------|
| `LockIn/Resources/Strings/AppResources.resx` | Default = svenska. Källa för alla nycklar. | Skapas |
| `LockIn/Resources/Strings/AppResources.en.resx` | Engelska översättningar. Samma nycklar som default. | Skapas |
| `LockIn/Resources/Strings/AppResources.cs` | Hand-skriven static wrapper. Properties + `Culture`-prop. | Skapas |
| `LockIn/Resources/Strings/LocalizeExtension.cs` | XAML markup extension `{loc:Localize Key}`. | Skapas |
| `LockIn/LockIn.csproj` | Inga ändringar krävs — .NET SDK auto-inkluderar `.resx` under projektroten som `EmbeddedResource`. | Verifieras |
| `LockIn/Views/OnboardingPage.xaml` | Alla hårdkodade strängar → `{loc:Localize}`. | Modifieras |
| `LockIn/ViewModels/OnboardingViewModel.cs` | DisplayAlert-strängar + `RecommendedDescription` format-sträng → `AppResources`. | Modifieras |

---

## Nyckel-katalog för Plan 1

Plan 1 introducerar dessa nycklar (sv / en):

**Common (delas med framtida plans):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Common_Cancel` | "Avbryt" | "Cancel" |
| `Common_Skip` | "Hoppa över" | "Skip" |

**Onboarding — top bar:**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_SkipLink` | "Hoppa över" | "Skip" |
| `Onboarding_SkipDialogTitle` | "Hoppa över?" | "Skip?" |
| `Onboarding_SkipDialogBody` | "Du kan välja program senare i Bibliotek." | "You can pick a program later in Library." |

**Onboarding — Step 0 (Namn):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Step0_Welcome` | "VÄLKOMMEN" | "WELCOME" |
| `Onboarding_Step0_Question` | "Vad heter du?" | "What's your name?" |
| `Onboarding_Step0_Privacy` | "Vi använder bara namnet för att hälsa på dig — inget delas." | "We only use your name to greet you — nothing is shared." |
| `Onboarding_Step0_Placeholder` | "Ditt namn..." | "Your name..." |

**Onboarding — Step 1 (Veckomål):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Step1_Title` | "VECKANS MÅL" | "WEEKLY GOAL" |
| `Onboarding_Step1_Question` | "Hur många pass per vecka?" | "How many sessions per week?" |
| `Onboarding_Step1_Wk2_Title` | "2 PASS / VECKA" | "2 SESSIONS / WEEK" |
| `Onboarding_Step1_Wk2_Sub` | "Lätt start — bygg vanan" | "Easy start — build the habit" |
| `Onboarding_Step1_Wk3_Title` | "3 PASS / VECKA" | "3 SESSIONS / WEEK" |
| `Onboarding_Step1_Wk3_Sub` | "Klassisk balans — stabil utveckling" | "Classic balance — steady progress" |
| `Onboarding_Step1_Wk4_Title` | "4 PASS / VECKA" | "4 SESSIONS / WEEK" |
| `Onboarding_Step1_Wk4_Sub` | "Snabbare progress — kräver återhämtning" | "Faster progress — requires recovery" |
| `Onboarding_Step1_Wk5_Title` | "5 PASS / VECKA" | "5 SESSIONS / WEEK" |
| `Onboarding_Step1_Wk5_Sub` | "Intensiv — för engagerade lyftare" | "Intense — for committed lifters" |
| `Onboarding_Step1_Wk6_Title` | "6 PASS / VECKA" | "6 SESSIONS / WEEK" |
| `Onboarding_Step1_Wk6_Sub` | "Hardcore — bara om du vet vad du gör" | "Hardcore — only if you know what you're doing" |

**Onboarding — Step 2 (Erfarenhet):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Step2_Title` | "ERFARENHET" | "EXPERIENCE" |
| `Onboarding_Step2_Question` | "Hur länge har du tränat?" | "How long have you been training?" |
| `Onboarding_Step2_Exp0_Title` | "NYBÖRJARE" | "BEGINNER" |
| `Onboarding_Step2_Exp0_Sub` | "Under 1 år · Linjär progression vecka för vecka" | "Under 1 year · Linear progression week by week" |
| `Onboarding_Step2_Exp1_Title` | "MELLANNIVÅ" | "INTERMEDIATE" |
| `Onboarding_Step2_Exp1_Sub` | "1–3 år · Periodisering och tyngre lyft" | "1–3 years · Periodization and heavier lifts" |
| `Onboarding_Step2_Exp2_Title` | "AVANCERAD" | "ADVANCED" |
| `Onboarding_Step2_Exp2_Sub` | "3+ år · Avancerade scheman med deload" | "3+ years · Advanced programs with deloads" |

**Onboarding — Step 3 (Mål):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Step3_Title` | "DITT MÅL" | "YOUR GOAL" |
| `Onboarding_Step3_Question` | "Vad vill du uppnå?" | "What do you want to achieve?" |
| `Onboarding_Step3_Strength_Title` | "STYRKA" | "STRENGTH" |
| `Onboarding_Step3_Strength_Sub` | "Bli starkare och lyfta tyngre" | "Get stronger and lift heavier" |
| `Onboarding_Step3_Hyper_Title` | "HYPERTROFI" | "HYPERTROPHY" |
| `Onboarding_Step3_Hyper_Sub` | "Bygga muskler och förändra kroppen" | "Build muscle and reshape your body" |

**Onboarding — Step 4 (Rekommendation):**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Step4_Title` | "PERFEKT MATCH" | "PERFECT MATCH" |
| `Onboarding_Step4_Subtitle` | "Vi rekommenderar:" | "We recommend:" |
| `Onboarding_Step4_Activate` | "AKTIVERA PROGRAM →" | "ACTIVATE PROGRAM →" |
| `Onboarding_Step4_SkipProgram` | "Hoppa över, jag väljer själv" | "Skip, I'll pick myself" |
| `Onboarding_RecommendedFormat` | "{0} dagar/vecka · {1} pass" | "{0} days/week · {1} sessions" |

**Onboarding — Bottom CTA:**
| Nyckel | sv-SE | en-US |
|--------|-------|-------|
| `Onboarding_Continue` | "FORTSÄTT →" | "CONTINUE →" |

Totalt: 41 nycklar (2 Common + 39 Onboarding).

---

### Task 1: Resurssystem-skelett (resx + wrapper)

Skapa de tre filerna som utgör resurssystemet. Inga UI-ändringar — bara infrastruktur som senare tasks fyller på.

**Files:**
- Create: `LockIn/Resources/Strings/AppResources.resx`
- Create: `LockIn/Resources/Strings/AppResources.en.resx`
- Create: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: inget
- Produces: `LockIn.Resources.Strings.AppResources` — static class med:
  - `static CultureInfo? Culture { get; set; }` — overridable, default `null` = `CultureInfo.CurrentUICulture`
  - `static ResourceManager ResourceManager { get; }` — exponeras för `LocalizeExtension`
  - `static string Common_Cancel` (read-only property) — och en property per nyckel som tasks lägger till

- [ ] **Step 1: Skapa `AppResources.resx` med två common-nycklar**

Skapa `LockIn/Resources/Strings/AppResources.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Common_Cancel" xml:space="preserve">
    <value>Avbryt</value>
  </data>
  <data name="Common_Skip" xml:space="preserve">
    <value>Hoppa över</value>
  </data>
</root>
```

- [ ] **Step 2: Skapa `AppResources.en.resx` med samma två nycklar översatta**

Skapa `LockIn/Resources/Strings/AppResources.en.resx`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <data name="Common_Cancel" xml:space="preserve">
    <value>Cancel</value>
  </data>
  <data name="Common_Skip" xml:space="preserve">
    <value>Skip</value>
  </data>
</root>
```

- [ ] **Step 3: Skapa `AppResources.cs` — hand-skriven static wrapper**

Skapa `LockIn/Resources/Strings/AppResources.cs`:

```csharp
using System.Globalization;
using System.Resources;

namespace LockIn.Resources.Strings;

/// <summary>
/// Hand-written wrapper kring AppResources.resx satellitassemblier.
/// Lägg till nya properties här när nycklar läggs till i .resx-filerna.
/// </summary>
public static class AppResources
{
    public static ResourceManager ResourceManager { get; } = new(
        "LockIn.Resources.Strings.AppResources",
        typeof(AppResources).Assembly);

    /// <summary>
    /// Override för tester / framtida språkval. När null används CurrentUICulture.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static string Get(string key) =>
        ResourceManager.GetString(key, Culture) ?? key;

    // ── Common ─────────────────────────────────────────────────────────
    public static string Common_Cancel => Get(nameof(Common_Cancel));
    public static string Common_Skip   => Get(nameof(Common_Skip));
}
```

- [ ] **Step 4: Bygg och verifiera att resurserna inkluderas korrekt**

Kör: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
Förväntat: BUILD SUCCEEDED, 0 errors. Inga warnings om `AppResources`.

Om .resx-filerna inte plockas upp automatiskt: kontrollera output efter `AppResources.resources` i `bin/Debug/net10.0-ios/`. Om de saknas, lägg till explicit i `LockIn/LockIn.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Update="Resources\Strings\AppResources.resx">
    <Generator></Generator>
  </EmbeddedResource>
  <EmbeddedResource Update="Resources\Strings\AppResources.en.resx">
    <Generator></Generator>
  </EmbeddedResource>
</ItemGroup>
```

(`Generator` lämnas tom för att inte trigga IDE-baserad designer-generering — vi äger `AppResources.cs` manuellt.)

- [ ] **Step 5: Commit**

```bash
git add LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs \
        LockIn/LockIn.csproj
git commit -m "feat(i18n): add resx infrastructure with sv default + en satellite"
```

---

### Task 2: LocalizeExtension för XAML

Markup extension så XAML kan skriva `{loc:Localize Key}` istället för långa `{Binding Source={x:Static ...}}`-uttryck.

**Files:**
- Create: `LockIn/Resources/Strings/LocalizeExtension.cs`

**Interfaces:**
- Consumes: `AppResources.ResourceManager`, `AppResources.Culture` (från Task 1)
- Produces: `LockIn.Resources.Strings.LocalizeExtension` — `IMarkupExtension<string>` med string-property `Key`. Returnerar `key` bokstavligt om resurs saknas.

- [ ] **Step 1: Skapa `LocalizeExtension.cs`**

Skapa `LockIn/Resources/Strings/LocalizeExtension.cs`:

```csharp
using System.Globalization;

namespace LockIn.Resources.Strings;

/// <summary>
/// XAML markup extension: <c>{loc:Localize Onboarding_Step0_Welcome}</c>
/// Saknad nyckel returnerar nyckelnamnet bokstavligt så missar syns utan att krascha.
/// </summary>
[ContentProperty(nameof(Key))]
public class LocalizeExtension : IMarkupExtension<string>
{
    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key)) return string.Empty;
        var culture = AppResources.Culture ?? CultureInfo.CurrentUICulture;
        return AppResources.ResourceManager.GetString(Key, culture) ?? Key;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
```

- [ ] **Step 2: Bygg och verifiera**

Kör: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add LockIn/Resources/Strings/LocalizeExtension.cs
git commit -m "feat(i18n): add LocalizeExtension XAML markup extension"
```

---

### Task 3: Lägg till alla Onboarding-nycklar i resurssystemet

Fyll resx-filerna och `AppResources.cs` med alla nycklar OnboardingPage + ViewModel behöver. Ingen XAML/ViewModel ändras än — nästa task konsumerar dem.

**Files:**
- Modify: `LockIn/Resources/Strings/AppResources.resx`
- Modify: `LockIn/Resources/Strings/AppResources.en.resx`
- Modify: `LockIn/Resources/Strings/AppResources.cs`

**Interfaces:**
- Consumes: `AppResources` (Task 1)
- Produces: Nya static properties på `AppResources` — alla 38 nycklar från katalogen ovan. Common-namespace utökat med `Common_Skip` (redan från Task 1). Övriga är Onboarding-prefixade.

- [ ] **Step 1: Lägg till alla nya nycklar i `AppResources.resx`**

Lägg till följande `<data>`-element inuti `<root>` i `LockIn/Resources/Strings/AppResources.resx` (efter de två Common-nycklarna):

```xml
  <!-- Onboarding — top bar -->
  <data name="Onboarding_SkipLink" xml:space="preserve">
    <value>Hoppa över</value>
  </data>
  <data name="Onboarding_SkipDialogTitle" xml:space="preserve">
    <value>Hoppa över?</value>
  </data>
  <data name="Onboarding_SkipDialogBody" xml:space="preserve">
    <value>Du kan välja program senare i Bibliotek.</value>
  </data>

  <!-- Onboarding — Step 0 -->
  <data name="Onboarding_Step0_Welcome" xml:space="preserve">
    <value>VÄLKOMMEN</value>
  </data>
  <data name="Onboarding_Step0_Question" xml:space="preserve">
    <value>Vad heter du?</value>
  </data>
  <data name="Onboarding_Step0_Privacy" xml:space="preserve">
    <value>Vi använder bara namnet för att hälsa på dig — inget delas.</value>
  </data>
  <data name="Onboarding_Step0_Placeholder" xml:space="preserve">
    <value>Ditt namn...</value>
  </data>

  <!-- Onboarding — Step 1 -->
  <data name="Onboarding_Step1_Title" xml:space="preserve">
    <value>VECKANS MÅL</value>
  </data>
  <data name="Onboarding_Step1_Question" xml:space="preserve">
    <value>Hur många pass per vecka?</value>
  </data>
  <data name="Onboarding_Step1_Wk2_Title" xml:space="preserve">
    <value>2 PASS / VECKA</value>
  </data>
  <data name="Onboarding_Step1_Wk2_Sub" xml:space="preserve">
    <value>Lätt start — bygg vanan</value>
  </data>
  <data name="Onboarding_Step1_Wk3_Title" xml:space="preserve">
    <value>3 PASS / VECKA</value>
  </data>
  <data name="Onboarding_Step1_Wk3_Sub" xml:space="preserve">
    <value>Klassisk balans — stabil utveckling</value>
  </data>
  <data name="Onboarding_Step1_Wk4_Title" xml:space="preserve">
    <value>4 PASS / VECKA</value>
  </data>
  <data name="Onboarding_Step1_Wk4_Sub" xml:space="preserve">
    <value>Snabbare progress — kräver återhämtning</value>
  </data>
  <data name="Onboarding_Step1_Wk5_Title" xml:space="preserve">
    <value>5 PASS / VECKA</value>
  </data>
  <data name="Onboarding_Step1_Wk5_Sub" xml:space="preserve">
    <value>Intensiv — för engagerade lyftare</value>
  </data>
  <data name="Onboarding_Step1_Wk6_Title" xml:space="preserve">
    <value>6 PASS / VECKA</value>
  </data>
  <data name="Onboarding_Step1_Wk6_Sub" xml:space="preserve">
    <value>Hardcore — bara om du vet vad du gör</value>
  </data>

  <!-- Onboarding — Step 2 -->
  <data name="Onboarding_Step2_Title" xml:space="preserve">
    <value>ERFARENHET</value>
  </data>
  <data name="Onboarding_Step2_Question" xml:space="preserve">
    <value>Hur länge har du tränat?</value>
  </data>
  <data name="Onboarding_Step2_Exp0_Title" xml:space="preserve">
    <value>NYBÖRJARE</value>
  </data>
  <data name="Onboarding_Step2_Exp0_Sub" xml:space="preserve">
    <value>Under 1 år · Linjär progression vecka för vecka</value>
  </data>
  <data name="Onboarding_Step2_Exp1_Title" xml:space="preserve">
    <value>MELLANNIVÅ</value>
  </data>
  <data name="Onboarding_Step2_Exp1_Sub" xml:space="preserve">
    <value>1–3 år · Periodisering och tyngre lyft</value>
  </data>
  <data name="Onboarding_Step2_Exp2_Title" xml:space="preserve">
    <value>AVANCERAD</value>
  </data>
  <data name="Onboarding_Step2_Exp2_Sub" xml:space="preserve">
    <value>3+ år · Avancerade scheman med deload</value>
  </data>

  <!-- Onboarding — Step 3 -->
  <data name="Onboarding_Step3_Title" xml:space="preserve">
    <value>DITT MÅL</value>
  </data>
  <data name="Onboarding_Step3_Question" xml:space="preserve">
    <value>Vad vill du uppnå?</value>
  </data>
  <data name="Onboarding_Step3_Strength_Title" xml:space="preserve">
    <value>STYRKA</value>
  </data>
  <data name="Onboarding_Step3_Strength_Sub" xml:space="preserve">
    <value>Bli starkare och lyfta tyngre</value>
  </data>
  <data name="Onboarding_Step3_Hyper_Title" xml:space="preserve">
    <value>HYPERTROFI</value>
  </data>
  <data name="Onboarding_Step3_Hyper_Sub" xml:space="preserve">
    <value>Bygga muskler och förändra kroppen</value>
  </data>

  <!-- Onboarding — Step 4 -->
  <data name="Onboarding_Step4_Title" xml:space="preserve">
    <value>PERFEKT MATCH</value>
  </data>
  <data name="Onboarding_Step4_Subtitle" xml:space="preserve">
    <value>Vi rekommenderar:</value>
  </data>
  <data name="Onboarding_Step4_Activate" xml:space="preserve">
    <value>AKTIVERA PROGRAM →</value>
  </data>
  <data name="Onboarding_Step4_SkipProgram" xml:space="preserve">
    <value>Hoppa över, jag väljer själv</value>
  </data>
  <data name="Onboarding_RecommendedFormat" xml:space="preserve">
    <value>{0} dagar/vecka · {1} pass</value>
  </data>

  <!-- Onboarding — Bottom CTA -->
  <data name="Onboarding_Continue" xml:space="preserve">
    <value>FORTSÄTT →</value>
  </data>
```

- [ ] **Step 2: Lägg till samma nycklar med engelska värden i `AppResources.en.resx`**

Lägg till efter de två Common-nycklarna i `LockIn/Resources/Strings/AppResources.en.resx`:

```xml
  <!-- Onboarding — top bar -->
  <data name="Onboarding_SkipLink" xml:space="preserve">
    <value>Skip</value>
  </data>
  <data name="Onboarding_SkipDialogTitle" xml:space="preserve">
    <value>Skip?</value>
  </data>
  <data name="Onboarding_SkipDialogBody" xml:space="preserve">
    <value>You can pick a program later in Library.</value>
  </data>

  <!-- Onboarding — Step 0 -->
  <data name="Onboarding_Step0_Welcome" xml:space="preserve">
    <value>WELCOME</value>
  </data>
  <data name="Onboarding_Step0_Question" xml:space="preserve">
    <value>What's your name?</value>
  </data>
  <data name="Onboarding_Step0_Privacy" xml:space="preserve">
    <value>We only use your name to greet you — nothing is shared.</value>
  </data>
  <data name="Onboarding_Step0_Placeholder" xml:space="preserve">
    <value>Your name...</value>
  </data>

  <!-- Onboarding — Step 1 -->
  <data name="Onboarding_Step1_Title" xml:space="preserve">
    <value>WEEKLY GOAL</value>
  </data>
  <data name="Onboarding_Step1_Question" xml:space="preserve">
    <value>How many sessions per week?</value>
  </data>
  <data name="Onboarding_Step1_Wk2_Title" xml:space="preserve">
    <value>2 SESSIONS / WEEK</value>
  </data>
  <data name="Onboarding_Step1_Wk2_Sub" xml:space="preserve">
    <value>Easy start — build the habit</value>
  </data>
  <data name="Onboarding_Step1_Wk3_Title" xml:space="preserve">
    <value>3 SESSIONS / WEEK</value>
  </data>
  <data name="Onboarding_Step1_Wk3_Sub" xml:space="preserve">
    <value>Classic balance — steady progress</value>
  </data>
  <data name="Onboarding_Step1_Wk4_Title" xml:space="preserve">
    <value>4 SESSIONS / WEEK</value>
  </data>
  <data name="Onboarding_Step1_Wk4_Sub" xml:space="preserve">
    <value>Faster progress — requires recovery</value>
  </data>
  <data name="Onboarding_Step1_Wk5_Title" xml:space="preserve">
    <value>5 SESSIONS / WEEK</value>
  </data>
  <data name="Onboarding_Step1_Wk5_Sub" xml:space="preserve">
    <value>Intense — for committed lifters</value>
  </data>
  <data name="Onboarding_Step1_Wk6_Title" xml:space="preserve">
    <value>6 SESSIONS / WEEK</value>
  </data>
  <data name="Onboarding_Step1_Wk6_Sub" xml:space="preserve">
    <value>Hardcore — only if you know what you're doing</value>
  </data>

  <!-- Onboarding — Step 2 -->
  <data name="Onboarding_Step2_Title" xml:space="preserve">
    <value>EXPERIENCE</value>
  </data>
  <data name="Onboarding_Step2_Question" xml:space="preserve">
    <value>How long have you been training?</value>
  </data>
  <data name="Onboarding_Step2_Exp0_Title" xml:space="preserve">
    <value>BEGINNER</value>
  </data>
  <data name="Onboarding_Step2_Exp0_Sub" xml:space="preserve">
    <value>Under 1 year · Linear progression week by week</value>
  </data>
  <data name="Onboarding_Step2_Exp1_Title" xml:space="preserve">
    <value>INTERMEDIATE</value>
  </data>
  <data name="Onboarding_Step2_Exp1_Sub" xml:space="preserve">
    <value>1–3 years · Periodization and heavier lifts</value>
  </data>
  <data name="Onboarding_Step2_Exp2_Title" xml:space="preserve">
    <value>ADVANCED</value>
  </data>
  <data name="Onboarding_Step2_Exp2_Sub" xml:space="preserve">
    <value>3+ years · Advanced programs with deloads</value>
  </data>

  <!-- Onboarding — Step 3 -->
  <data name="Onboarding_Step3_Title" xml:space="preserve">
    <value>YOUR GOAL</value>
  </data>
  <data name="Onboarding_Step3_Question" xml:space="preserve">
    <value>What do you want to achieve?</value>
  </data>
  <data name="Onboarding_Step3_Strength_Title" xml:space="preserve">
    <value>STRENGTH</value>
  </data>
  <data name="Onboarding_Step3_Strength_Sub" xml:space="preserve">
    <value>Get stronger and lift heavier</value>
  </data>
  <data name="Onboarding_Step3_Hyper_Title" xml:space="preserve">
    <value>HYPERTROPHY</value>
  </data>
  <data name="Onboarding_Step3_Hyper_Sub" xml:space="preserve">
    <value>Build muscle and reshape your body</value>
  </data>

  <!-- Onboarding — Step 4 -->
  <data name="Onboarding_Step4_Title" xml:space="preserve">
    <value>PERFECT MATCH</value>
  </data>
  <data name="Onboarding_Step4_Subtitle" xml:space="preserve">
    <value>We recommend:</value>
  </data>
  <data name="Onboarding_Step4_Activate" xml:space="preserve">
    <value>ACTIVATE PROGRAM →</value>
  </data>
  <data name="Onboarding_Step4_SkipProgram" xml:space="preserve">
    <value>Skip, I'll pick myself</value>
  </data>
  <data name="Onboarding_RecommendedFormat" xml:space="preserve">
    <value>{0} days/week · {1} sessions</value>
  </data>

  <!-- Onboarding — Bottom CTA -->
  <data name="Onboarding_Continue" xml:space="preserve">
    <value>CONTINUE →</value>
  </data>
```

- [ ] **Step 3: Lägg till motsvarande C#-properties i `AppResources.cs`**

Lägg till följande properties i `LockIn/Resources/Strings/AppResources.cs` (efter Common-blocket, före klassen stängs):

```csharp
    // ── Onboarding ─────────────────────────────────────────────────────
    public static string Onboarding_SkipLink           => Get(nameof(Onboarding_SkipLink));
    public static string Onboarding_SkipDialogTitle    => Get(nameof(Onboarding_SkipDialogTitle));
    public static string Onboarding_SkipDialogBody     => Get(nameof(Onboarding_SkipDialogBody));

    public static string Onboarding_Step0_Welcome      => Get(nameof(Onboarding_Step0_Welcome));
    public static string Onboarding_Step0_Question     => Get(nameof(Onboarding_Step0_Question));
    public static string Onboarding_Step0_Privacy      => Get(nameof(Onboarding_Step0_Privacy));
    public static string Onboarding_Step0_Placeholder  => Get(nameof(Onboarding_Step0_Placeholder));

    public static string Onboarding_Step1_Title        => Get(nameof(Onboarding_Step1_Title));
    public static string Onboarding_Step1_Question     => Get(nameof(Onboarding_Step1_Question));
    public static string Onboarding_Step1_Wk2_Title    => Get(nameof(Onboarding_Step1_Wk2_Title));
    public static string Onboarding_Step1_Wk2_Sub      => Get(nameof(Onboarding_Step1_Wk2_Sub));
    public static string Onboarding_Step1_Wk3_Title    => Get(nameof(Onboarding_Step1_Wk3_Title));
    public static string Onboarding_Step1_Wk3_Sub      => Get(nameof(Onboarding_Step1_Wk3_Sub));
    public static string Onboarding_Step1_Wk4_Title    => Get(nameof(Onboarding_Step1_Wk4_Title));
    public static string Onboarding_Step1_Wk4_Sub      => Get(nameof(Onboarding_Step1_Wk4_Sub));
    public static string Onboarding_Step1_Wk5_Title    => Get(nameof(Onboarding_Step1_Wk5_Title));
    public static string Onboarding_Step1_Wk5_Sub      => Get(nameof(Onboarding_Step1_Wk5_Sub));
    public static string Onboarding_Step1_Wk6_Title    => Get(nameof(Onboarding_Step1_Wk6_Title));
    public static string Onboarding_Step1_Wk6_Sub      => Get(nameof(Onboarding_Step1_Wk6_Sub));

    public static string Onboarding_Step2_Title        => Get(nameof(Onboarding_Step2_Title));
    public static string Onboarding_Step2_Question     => Get(nameof(Onboarding_Step2_Question));
    public static string Onboarding_Step2_Exp0_Title   => Get(nameof(Onboarding_Step2_Exp0_Title));
    public static string Onboarding_Step2_Exp0_Sub     => Get(nameof(Onboarding_Step2_Exp0_Sub));
    public static string Onboarding_Step2_Exp1_Title   => Get(nameof(Onboarding_Step2_Exp1_Title));
    public static string Onboarding_Step2_Exp1_Sub     => Get(nameof(Onboarding_Step2_Exp1_Sub));
    public static string Onboarding_Step2_Exp2_Title   => Get(nameof(Onboarding_Step2_Exp2_Title));
    public static string Onboarding_Step2_Exp2_Sub     => Get(nameof(Onboarding_Step2_Exp2_Sub));

    public static string Onboarding_Step3_Title        => Get(nameof(Onboarding_Step3_Title));
    public static string Onboarding_Step3_Question     => Get(nameof(Onboarding_Step3_Question));
    public static string Onboarding_Step3_Strength_Title => Get(nameof(Onboarding_Step3_Strength_Title));
    public static string Onboarding_Step3_Strength_Sub   => Get(nameof(Onboarding_Step3_Strength_Sub));
    public static string Onboarding_Step3_Hyper_Title    => Get(nameof(Onboarding_Step3_Hyper_Title));
    public static string Onboarding_Step3_Hyper_Sub      => Get(nameof(Onboarding_Step3_Hyper_Sub));

    public static string Onboarding_Step4_Title         => Get(nameof(Onboarding_Step4_Title));
    public static string Onboarding_Step4_Subtitle      => Get(nameof(Onboarding_Step4_Subtitle));
    public static string Onboarding_Step4_Activate      => Get(nameof(Onboarding_Step4_Activate));
    public static string Onboarding_Step4_SkipProgram   => Get(nameof(Onboarding_Step4_SkipProgram));
    public static string Onboarding_RecommendedFormat   => Get(nameof(Onboarding_RecommendedFormat));

    public static string Onboarding_Continue            => Get(nameof(Onboarding_Continue));
```

- [ ] **Step 4: Bygg och verifiera**

Kör: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add LockIn/Resources/Strings/AppResources.resx \
        LockIn/Resources/Strings/AppResources.en.resx \
        LockIn/Resources/Strings/AppResources.cs
git commit -m "feat(i18n): add Onboarding keys to resx (sv + en)"
```

---

### Task 4: Översätt OnboardingPage.xaml

Byt ut alla hårdkodade strängar i OnboardingPage mot `{loc:Localize ...}`. Lägg till `xmlns:loc`-deklaration.

**Files:**
- Modify: `LockIn/Views/OnboardingPage.xaml`

**Interfaces:**
- Consumes: `LocalizeExtension` (Task 2), nycklar (Task 3)
- Produces: inget (slutkonsument)

- [ ] **Step 1: Lägg till `xmlns:loc`-deklaration i ContentPage-rot**

Modifiera `LockIn/Views/OnboardingPage.xaml` rad 2-11 — lägg till `xmlns:loc`:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:LockIn.ViewModels"
             xmlns:controls="clr-namespace:LockIn.Controls"
             xmlns:loc="clr-namespace:LockIn.Resources.Strings"
             xmlns:ios="clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls"
             x:Class="LockIn.Views.OnboardingPage"
             x:DataType="vm:OnboardingViewModel"
             Shell.NavBarIsVisible="False"
             BackgroundColor="Transparent"
             ios:Page.UseSafeArea="False">
```

- [ ] **Step 2: Top bar — byt "Hoppa över"-label**

Hitta i `OnboardingPage.xaml`:
```xml
<Label Text="Hoppa över"
       FontFamily="DMSansMedium" FontSize="13"
```
Ändra till:
```xml
<Label Text="{loc:Localize Onboarding_SkipLink}"
       FontFamily="DMSansMedium" FontSize="13"
```

- [ ] **Step 3: Step 0 — byt alla fyra strängar**

Ändra `Text="VÄLKOMMEN"` → `Text="{loc:Localize Onboarding_Step0_Welcome}"`.
Ändra `Text="Vad heter du?"` → `Text="{loc:Localize Onboarding_Step0_Question}"`.
Ändra `Text="Vi använder bara namnet för att hälsa på dig — inget delas."` → `Text="{loc:Localize Onboarding_Step0_Privacy}"`.
Ändra `Placeholder="Ditt namn..."` → `Placeholder="{loc:Localize Onboarding_Step0_Placeholder}"`.

- [ ] **Step 4: Step 1 — byt rubriker och alla 5 veckomålskort**

Ändra `Text="VECKANS MÅL"` → `Text="{loc:Localize Onboarding_Step1_Title}"`.
Ändra `Text="Hur många pass per vecka?"` → `Text="{loc:Localize Onboarding_Step1_Question}"`.

För varje Wk2…Wk6-kort, byt de två texterna:
- `Text="2 PASS / VECKA"` → `Text="{loc:Localize Onboarding_Step1_Wk2_Title}"`
- `Text="Lätt start — bygg vanan"` → `Text="{loc:Localize Onboarding_Step1_Wk2_Sub}"`
- `Text="3 PASS / VECKA"` → `Text="{loc:Localize Onboarding_Step1_Wk3_Title}"`
- `Text="Klassisk balans — stabil utveckling"` → `Text="{loc:Localize Onboarding_Step1_Wk3_Sub}"`
- `Text="4 PASS / VECKA"` → `Text="{loc:Localize Onboarding_Step1_Wk4_Title}"`
- `Text="Snabbare progress — kräver återhämtning"` → `Text="{loc:Localize Onboarding_Step1_Wk4_Sub}"`
- `Text="5 PASS / VECKA"` → `Text="{loc:Localize Onboarding_Step1_Wk5_Title}"`
- `Text="Intensiv — för engagerade lyftare"` → `Text="{loc:Localize Onboarding_Step1_Wk5_Sub}"`
- `Text="6 PASS / VECKA"` → `Text="{loc:Localize Onboarding_Step1_Wk6_Title}"`
- `Text="Hardcore — bara om du vet vad du gör"` → `Text="{loc:Localize Onboarding_Step1_Wk6_Sub}"`

- [ ] **Step 5: Step 2 — byt rubriker och alla 3 erfarenhetskort**

- `Text="ERFARENHET"` → `Text="{loc:Localize Onboarding_Step2_Title}"`
- `Text="Hur länge har du tränat?"` → `Text="{loc:Localize Onboarding_Step2_Question}"`
- `Text="NYBÖRJARE"` → `Text="{loc:Localize Onboarding_Step2_Exp0_Title}"`
- `Text="Under 1 år · Linjär progression vecka för vecka"` → `Text="{loc:Localize Onboarding_Step2_Exp0_Sub}"`
- `Text="MELLANNIVÅ"` → `Text="{loc:Localize Onboarding_Step2_Exp1_Title}"`
- `Text="1–3 år · Periodisering och tyngre lyft"` → `Text="{loc:Localize Onboarding_Step2_Exp1_Sub}"`
- `Text="AVANCERAD"` → `Text="{loc:Localize Onboarding_Step2_Exp2_Title}"`
- `Text="3+ år · Avancerade scheman med deload"` → `Text="{loc:Localize Onboarding_Step2_Exp2_Sub}"`

- [ ] **Step 6: Step 3 — byt rubriker och båda målkorten**

- `Text="DITT MÅL"` → `Text="{loc:Localize Onboarding_Step3_Title}"`
- `Text="Vad vill du uppnå?"` → `Text="{loc:Localize Onboarding_Step3_Question}"`
- `Text="STYRKA"` → `Text="{loc:Localize Onboarding_Step3_Strength_Title}"`
- `Text="Bli starkare och lyfta tyngre"` → `Text="{loc:Localize Onboarding_Step3_Strength_Sub}"`
- `Text="HYPERTROFI"` → `Text="{loc:Localize Onboarding_Step3_Hyper_Title}"`
- `Text="Bygga muskler och förändra kroppen"` → `Text="{loc:Localize Onboarding_Step3_Hyper_Sub}"`

- [ ] **Step 7: Step 4 — byt rubrik + CTA + skip-link**

- `Text="PERFEKT MATCH"` → `Text="{loc:Localize Onboarding_Step4_Title}"`
- `Text="Vi rekommenderar:"` → `Text="{loc:Localize Onboarding_Step4_Subtitle}"`
- `Text="AKTIVERA PROGRAM →"` → `Text="{loc:Localize Onboarding_Step4_Activate}"`
- `Text="Hoppa över, jag väljer själv"` → `Text="{loc:Localize Onboarding_Step4_SkipProgram}"`

- [ ] **Step 8: Bottom CTA — byt "FORTSÄTT →"**

- `Text="FORTSÄTT →"` → `Text="{loc:Localize Onboarding_Continue}"`

- [ ] **Step 9: Bygg och verifiera 0 errors**

Kör: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
Förväntat: BUILD SUCCEEDED, 0 errors. Inga XamlC-fel — alla `{loc:Localize ...}`-referenser kompilerar.

Om XamlC klagar på att `loc:LocalizeExtension` inte finns: kontrollera att `LocalizeExtension`-klassen ligger i namespace `LockIn.Resources.Strings` (Task 2) och att `xmlns:loc="clr-namespace:LockIn.Resources.Strings"` är korrekt.

- [ ] **Step 10: Sök efter eventuella missade strängar i filen**

Kör: `grep -E 'Text="[A-ZÅÄÖa-zåäö]' LockIn/Views/OnboardingPage.xaml`
Förväntat: Endast bindningar som börjar med `{` (t.ex. `{Binding ...}`, `{loc:Localize ...}`). Inga hårdkodade svenska/engelska strängar kvar i Text-attribut.

Om något hittas: lägg till en nyckel och översätt (gå tillbaka till Task 3 och utöka).

- [ ] **Step 11: Commit**

```bash
git add LockIn/Views/OnboardingPage.xaml
git commit -m "feat(i18n): translate OnboardingPage XAML to use LocalizeExtension"
```

---

### Task 5: Översätt OnboardingViewModel + locale-verifiering i simulator

Byt ut hårdkodade strängar i ViewModel (DisplayAlert + `RecommendedDescription` format-sträng). Verifiera sedan slutresultatet i iOS-simulator på både svenskt och engelskt locale.

**Files:**
- Modify: `LockIn/ViewModels/OnboardingViewModel.cs`

**Interfaces:**
- Consumes: `AppResources.Onboarding_SkipDialogTitle`, `Onboarding_SkipDialogBody`, `Onboarding_SkipLink`, `Common_Cancel`, `Onboarding_RecommendedFormat` (alla från Task 3)
- Produces: inget (slutkonsument)

- [ ] **Step 1: Lägg till using-direktiv för AppResources**

Modifiera översta block i `LockIn/ViewModels/OnboardingViewModel.cs`. Före befintliga usings, lägg till:

```csharp
using LockIn.Resources.Strings;
```

- [ ] **Step 2: Byt ut DisplayAlert-strängarna i `SkipOnboardingAsync`**

I `LockIn/ViewModels/OnboardingViewModel.cs`, hitta metoden `SkipOnboardingAsync` (rad ~119) och ändra:

```csharp
    [RelayCommand]
    private async Task SkipOnboardingAsync()
    {
        var ok = await Shell.Current.DisplayAlert(
            "Hoppa över?",
            "Du kan välja program senare i Bibliotek.",
            "Hoppa över", "Avbryt");
        if (!ok) return;
        await FinishOnboardingAsync(activateProgram: false);
    }
```

Till:

```csharp
    [RelayCommand]
    private async Task SkipOnboardingAsync()
    {
        var ok = await Shell.Current.DisplayAlert(
            AppResources.Onboarding_SkipDialogTitle,
            AppResources.Onboarding_SkipDialogBody,
            AppResources.Onboarding_SkipLink,
            AppResources.Common_Cancel);
        if (!ok) return;
        await FinishOnboardingAsync(activateProgram: false);
    }
```

- [ ] **Step 3: Byt ut `RecommendedDescription` format-sträng**

Hitta i `LockIn/ViewModels/OnboardingViewModel.cs` (rad ~150):

```csharp
    public string RecommendedDescription =>
        RecommendedProgram is { } p ? $"{p.DaysPerWeek} dagar/vecka · {p.Days.Count} pass" : "";
```

Ändra till:

```csharp
    public string RecommendedDescription =>
        RecommendedProgram is { } p
            ? string.Format(AppResources.Onboarding_RecommendedFormat, p.DaysPerWeek, p.Days.Count)
            : "";
```

- [ ] **Step 4: Bygg och verifiera 0 errors**

Kör: `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
Förväntat: BUILD SUCCEEDED, 0 errors.

- [ ] **Step 5: Sök efter eventuella missade svenska strängar i OnboardingViewModel**

Kör: `grep -nE '"[A-ZÅÄÖa-zåäö][^"]*"' LockIn/ViewModels/OnboardingViewModel.cs`
Förväntat: Endast tekniska strängar (program-id `"startingstrength"`, `"fullbody"`, etc.) — inga användarsynliga svenska fraser.

Om en svensk fras hittas: lägg till nyckel + översättning (gå tillbaka till Task 3).

- [ ] **Step 6: Manuell verifiering — bygg och kör på iOS-simulator med svenskt locale**

Bygg och kör i Xcode-simulator (kräver Mac med Xcode 26.x). I simulatorn:

1. Settings → General → Language & Region → iPhone Language = Swedish
2. Starta LockIn
3. Tvinga onboarding (rensa app-data eller sätt `HasCompletedOnboarding = false` i debugger)
4. Verifiera att alla strängar på alla 5 steg visas på svenska som idag (VÄLKOMMEN, VECKANS MÅL, ERFARENHET, DITT MÅL, PERFEKT MATCH)
5. Tryck "Hoppa över" → bekräfta att dialog visar "Hoppa över?" / "Du kan välja program senare i Bibliotek." / "Hoppa över" / "Avbryt"

Förväntat: Identisk svensk text som innan i18n-arbetet. Regression = noll.

- [ ] **Step 7: Manuell verifiering — växla till engelskt locale och starta om appen**

I simulatorn:

1. Settings → General → Language & Region → iPhone Language = English
2. Tvinga app-omstart (svep upp och döda appen, starta om)
3. Starta LockIn
4. Verifiera att alla strängar nu visas på engelska:
   - Step 0: "WELCOME" / "What's your name?" / "We only use your name..." / "Your name..."
   - Step 1: "WEEKLY GOAL" / "How many sessions per week?" / "2 SESSIONS / WEEK" / "Easy start..." (samtliga 5 kort)
   - Step 2: "EXPERIENCE" / "How long have you been training?" / "BEGINNER" / "INTERMEDIATE" / "ADVANCED"
   - Step 3: "YOUR GOAL" / "What do you want to achieve?" / "STRENGTH" / "HYPERTROPHY"
   - Step 4: "PERFECT MATCH" / "We recommend:" / "ACTIVATE PROGRAM →" / "Skip, I'll pick myself"
   - Bottom CTA: "CONTINUE →"
   - Skip-länk i topp: "Skip"
   - Skip-dialog: "Skip?" / "You can pick a program later in Library." / "Skip" / "Cancel"
5. Programnamn på Step 4 (t.ex. "Starting Strength", "Push/Pull/Legs") — ska vara oförändrade, eftersom WorkoutPrograms inte ingår i Plan 1.
6. `RecommendedDescription` på Step 4 — ska visa t.ex. "3 days/week · 4 sessions" istället för "3 dagar/vecka · 4 pass".

Förväntat: Hela OnboardingPage på engelska. Inga avhuggna texter eller layoutkrockar (engelska är ofta längre — håll utkik).

- [ ] **Step 8: Om layout-overflow upptäcks — dokumentera**

Om någon engelsk sträng overflowar (typiskt: längre titel på 2 PASS-kort, eller skip-dialog-knappar) — notera det här, men ändra inte layout i denna task. Skapa istället en uppföljande commit senare. För denna task räcker det att Plan 1 levererar en funktionell engelsk version, även om någon sträng är trång.

- [ ] **Step 9: Commit**

```bash
git add LockIn/ViewModels/OnboardingViewModel.cs
git commit -m "feat(i18n): translate OnboardingViewModel — Plan 1 complete"
```

---

## Verifierings-checklista (efter Task 5)

- [ ] `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug` → 0 errors, 0 nya warnings
- [ ] Svensk simulator visar OnboardingPage identiskt med pre-i18n (regression = noll)
- [ ] Engelsk simulator visar OnboardingPage helt på engelska (utom programnamn)
- [ ] Skip-dialog (alert) översätts korrekt på båda locales
- [ ] `RecommendedDescription` på Step 4 formateras med rätt språk
- [ ] Inga andra sidor påverkas — Hem/Träna/Historik/Bibliotek/Kropp förblir på svenska oavsett locale (väntar på Plan 2)

---

## Vad som NOT ingår i Plan 1

För att hålla Plan 1 leverabel:

- **Inga andra sidor** översätts — Plan 2 hanterar de övriga 18 sidorna.
- **Inga achievements / programbeskrivningar / notification-strängar** — Plan 3.
- **Inga tal- eller datumformat-ändringar** — Plan 3 (5 specifika filer).
- **Ingen version bump** efter Plan 1 om vi inte pushar. Plan 1 är inkrementellt utrullbart, men användaren beslutar om push (per `feedback_git_push`).

---

## Risker och mitigering

| Risk | Mitigering |
|------|-----------|
| `.resx` plockas inte upp av SDK | Step 4 i Task 1 har fallback med explicit `<EmbeddedResource>` i `.csproj` |
| `LocalizeExtension` namespace-mismatch i XamlC | Step 9 i Task 4 har felsökningsanvisning |
| Hand-skriven `AppResources.cs` glömmer en property | Step 10 i Task 4 + Step 5 i Task 5 gör grep-pass efter missade strängar |
| `CultureInfo.CurrentUICulture` är inte engelska trots iOS-locale = English | Step 7 i Task 5: om engelska inte slår igenom, lägg till `Thread.CurrentThread.CurrentUICulture` -loggning i `App.xaml.cs` `OnStart` och utred. Detta dokumenteras i spec'en som "verifiering görs en gång" |
| iOS cachear satellite-assembly | Tvinga app-omstart (Step 7) — inte bara reload |
