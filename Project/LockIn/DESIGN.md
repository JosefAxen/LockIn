# DESIGN.md — LockIn Forge Design System

> Visual language, tokens, component patterns, and motion rules for the LockIn iOS app (.NET MAUI 10).
> Source of truth lives in `Colors.xaml` (XAML tokens) and `DesignTokens.cs` (C# tokens for SkiaSharp controls).
> Never hardcode hex values outside those two files.

---

## 1. Color Tokens

### Surface Hierarchy

Five depth levels. Each layer sits visually above the previous — never flat.

| Token (XAML) | Hex | Role |
|---|---|---|
| `ForgeBackground` | `#0E0E10` | Page background. The floor. |
| `ForgeSurface` | `#1C1C24` | Cards, sheets, overlays. |
| `ForgeSurface2` | `#252530` | Elevated cards, input fields. |
| `ForgeSurface3` | `#2E2E3A` | Floating elements, tooltips. |
| `GraphGrid` (C#) | `#272732` | Graph grid lines only. |

### Borders

| Token | Hex | Use |
|---|---|---|
| `ForgeBorder` | `#3A3A46` | Always-dark contexts (ActiveWorkout) |
| `ForgeBorderLight` | `#2A2A36` | Standard card strokes |

### Text

| Token | Hex | Use |
|---|---|---|
| `ForgeText` | `#E8E8EC` | Primary text. ≥4.5:1 on all surfaces. |
| `ForgeTextSecondary` | `#8E8E9A` | Secondary labels, sublabels |
| `ForgeMuted` / `TextMuted` | `#52525E` | Section headers, axis labels, placeholders |
| `GraphAxisText` (C#) | `#52525E` | Chart axis text only |

### Accents

| Token | Hex | Role | Constraints |
|---|---|---|---|
| `ForgeAccent` | `#B8B8BC` | **Brand accent. Silver/steel.** | Use precisely — color is information |
| `ForgeAccentAmber` | `#F59E0B` | Energy, strain, warmth | Sparingly — high heat |
| `ForgeAccentBlue` | `#3B82F6` | Sleep, recovery, calmness | Sparingly |
| `ForgeSuccess` | `#4ADE80` | PR badges, success states | **Semantic only. Never brand accent.** |
| `ForgeDestructive` | `#EF4444` | Delete, errors | Semantic only |

### Calendar tokens (C# only)

| Token | Hex |
|---|---|
| `CalTodayStroke` | Defined in DesignTokens.cs |
| `CalTodayText` | Defined in DesignTokens.cs |
| `CalNormalText` | Defined in DesignTokens.cs |

### Chips

| Token | Hex |
|---|---|
| `ChipInactiveBg` | `#1C1C24` |
| `HeatmapInactive` | `#1A1A22` |
| `ChipActiveBg` | Defined in DesignTokens.cs |
| `ChipInactiveFg` / `ChipActiveFg` | Defined in DesignTokens.cs |

---

## 2. Typography

Three fonts, three roles. Never use a font outside its role.

| Font | File | Role |
|---|---|---|
| `BebasNeue` | Resources/Fonts/ | Headings, numbers, big displays. Condensed, uppercase, industrial. |
| `DMSansMedium` | Resources/Fonts/ | Buttons, important labels, interactive text. |
| `DMSansRegular` | Resources/Fonts/ | Body text. Applied implicitly via Styles.xaml. |

### Named text styles

| Key | Font | Size | Tracking | Color |
|---|---|---|---|---|
| `DisplayLabel` | BebasNeue | — | `CharacterSpacing="2"` | ForgeText |
| `HeroNumber` | BebasNeue | 44 | `CharacterSpacing="1"` | ForgeText |
| `AccentHeroNumber` | BebasNeue | 44 | `CharacterSpacing="1"` | ForgeAccent |
| `SectionLabel` | BebasNeue | 11 | `CharacterSpacing="3"` | ForgeMuted |
| `MutedLabel` | DMSansRegular | 12 | — | ForgeTextSecondary |
| `AccentLabel` | DMSansMedium | — | — | ForgeAccent |
| *(implicit Label)* | DMSansRegular | 14 | — | ForgeText |

### Rules

- Large numbers dominate — `HeroNumber` and `DisplayLabel` draw the eye first.
- Section kickers use `SectionLabel` (11px Bebas, 3-letter-spacing, muted). One per group, not one per element.
- `ForgeText` on `ForgeBackground` = ~10:1 contrast. Never reduce below 4.5:1.
- LineHeight on body: keep MAUI default (`LineHeight="1"` on hero numbers to tighten; no explicit override on body).

---

## 3. Visual Hierarchy Pattern

Inspired by the reference: the data is the hero, not the chrome.

```
1. HERO NUMBER — giant, BebasNeue, white or ForgeAccent
   └─ status label — small, semantic color (Amber=high strain, Blue=recovery)

2. GAUGE / ARC — semicircular or circular progress, gradient-stroked
   └─ center value — BebasNeue medium-large inside

3. METRIC CARDS — 2-column grid
   └─ top: SectionLabel (muted, upper)
   └─ center: large number + unit
   └─ bottom: colored status or sparkline

4. TIME-SERIES — bar or line chart, full width
   └─ axis labels: GraphAxisText (#52525E)
   └─ grid: GraphGrid (#272732)
```

This hierarchy maps exactly to LockIn's HemPage: TrainingScore → MetricRingViews → stat cards → SparklineView / LineChartView.

---

## 4. Component Catalogue

### 4.1 Cards

#### `CardFrame` (Border, Style)
- Background: `ForgeSurface`
- Corner: 16px
- Stroke: `ForgeBorderLight` @ 1px
- Shadow: `Black` offset(0,3) radius 12 opacity 0.12

#### `DarkCardFrame` (Border, Style)
- Always-dark. Background: `ForgeSurface` (no AppTheme binding)
- Stroke: `ForgeBorder`
- Use in: ActiveWorkoutPage, PostWorkoutPage

#### `AccentCard` (Border, Style)
- Stroke: `ForgeAccent` (silver glow)
- Shadow: `#B8B8BC` offset(0,4) radius 20 opacity 0.15
- Reserved for focus/selected states

---

### 4.2 Buttons

#### `PrimaryButton`
- Background: `ForgePrimary`
- Text: `ForgePrimaryForeground`, BebasNeue 20px, tracking 3
- Height: 56, corner radius: 26 (pill)
- Shadow: `#B8B8BC` offset(0,4) radius 20 opacity 0.28

#### `SecondaryButton`
- Background: `ForgeSurface2`, DMSansMedium 14px
- Height: 44, corner radius: 14

#### `DestructiveButton`
- Transparent bg, `ForgeDestructive` text, DMSansMedium 14px

---

### 4.3 Filter Chips

Used in LibraryPage and ExercisePickerPage for MuscleGroup and Equipment filtering.

- Inactive: bg `ChipInactiveBg` (#1C1C24), fg `ChipInactiveFg`
- Active: bg `ChipActiveBg`, fg `ChipActiveFg`
- Colors are `[ObservableProperty]` fields — NOT computed props — so binding is set correctly at initial render.
- Press animation: `AnimationHelper.PressAsync` (ScaleTo 0.93 → spring-back)

---

### 4.4 Navigation Tabs (sliding indicator)

Used in LibraryPage (3 tabs) and HistoryPage (period + sort selectors).

- Container fires `SizeChanged` → sets `_tabColumnWidth = containerWidth / tabCount`
- Indicator `WidthRequest` = column width; `TranslationX` = `selectedIndex × columnWidth`
- Animated: `TranslateTo(x, 0, 280, Easing.SpringOut)`
- Critical: `UpdateXxxIndicator(animated: false)` called in `OnAppearing` — not just in `SizeChanged` — or indicators are wrong on re-appearance.

---

### 4.5 Page Header

All pages: `Shell.NavBarIsVisible="False"` + `ios:Page.UseSafeArea="False"`. Background fills edge-to-edge including Dynamic Island.

```xml
<Grid>
    <!-- Root grid: SafeAreaEdges="None" for edge-to-edge -->
    <controls:AtmosphericBackgroundView ZIndex="0" InputTransparent="True"/>
    <Grid ZIndex="1">
        <!-- Header row: top Padding = 56 (manual Dynamic Island offset) -->
        <Grid Padding="20,56,20,0" ...>
```

Sticky header: `Opacity` driven by scroll offset — `Math.Clamp((scrollY - 80) / 40, 0, 1)`.

---

### 4.6 "PASS PÅGÅR" Workout Banner

Live workout indicator shown as a floating banner on all 5 tab pages. Visibility driven by `ActiveWorkoutStateService.IsActive`.

Current pattern: `ForgeLiveDotView` (SkiaSharp) + tap → `ActiveWorkoutPage`.

`ForgeLiveDotView` renders a pulsing ring + core dot using `SKMaskFilter.CreateBlur`. **The blur MUST be applied only to the pulse ring, not the core dot** — otherwise the core appears blurry. Draw core without blur last, on top.

---

### 4.7 Set Rows (ActiveWorkoutPage)

Entrance animation: `Loaded` event → fade in + `TranslateY` 12→0, 180ms, `CubicOut`. One-shot, not repeating.

---

## 5. SkiaSharp Controls

All custom canvas controls in `Controls/`. Pattern: inherit `SKCanvasView`, use `[BindableProperty]`, call `InvalidateSurface()` on change. Use **SKPaint-based text API** (not SKFont): `TextSize`, `TextAlign`, `GetFontMetrics`, `DrawText(string, x, y, SKPaint)`.

### WeeklyGoalGauge

Semicircular arc gauge. Renders TrainingScore as a 300° swept arc with gradient fill. Start angle: 120°, sweep: 300° × progress.

Layers (bottom to top):
1. Background arc — stroke, `ForgeSurface3` color
2. Progress arc — `SKShader.CreateLinearGradient`, `ForgeAccentAmber` → amber-light
3. Center number — BebasNeue (or system font via `SKTypeface.FromFamilyName(null)`)
4. Label text — DMSans, `ForgeMuted`

### MetricRingView

Three circular rings (Strain, Recovery, Sleep). Bindable: `Progress` (float 0–1), `RingColor` (Color), `CenterText` (string).

- 300° arc, `StrokeWidth` ~14, `StrokeCap = Round`
- Background arc: `ForgeSurface3`
- Active arc: `SKShader.CreateLinearGradient` (RingColor → lighter variant)
- Center: BebasNeue white

### SparklineView

Compact trend line. Binds to float array. Background: transparent. Line: `ForgeAccent`. No axis labels.

### LineChartView

Multi-series line chart. Grid: `GraphGrid` (#272732). Axis text: `GraphAxisText` (#52525E). Series colors match semantic tokens (Amber for strain, Blue for recovery/sleep, ForgeAccent for neutral).

### AtmosphericBackgroundView

Full-screen background with layered radial glows + Perlin grain. Renders 4 passes:

1. Base fill: `#0E0E10`
2. Green radial glow (top-center, behind gauge zone): `#254ADE80` → transparent
3. Blue ambient bottom gradient: `#1A3B82E6` → transparent
4. Perlin grain overlay: turbulence `BlendMode.Screen`, alpha 12

`InputTransparent="True"`. ZIndex 0 behind all content. No bindable properties.

### ForgeLiveDotView

Live workout indicator dot with pulsing ring.

- Pulse ring: `SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)` — ONLY on the ring
- Core dot: no blur. Draw LAST (on top) with fresh `SKPaint` without `MaskFilter`
- `StartPulse()` / `StopPulse()` from code-behind on `OnAppearing` / `OnDisappearing`

---

## 6. Layout Conventions

### Safe Areas

```
Root Grid: SafeAreaEdges="None"          ← required for edge-to-edge
Header padding top: 56pt                ← manual Dynamic Island clearance
Tab bar clearance: 90pt bottom padding  ← on all scrollable content
```

### Spacing Rhythm

No arbitrary values. Use these intentional steps:

| Role | Value |
|---|---|
| Section gap | 24 |
| Card internal padding | 16 or 20 |
| Card gap | 12 |
| Row gap within card | 8 |
| Inline icon-text gap | 6 |
| Touch target minimum | 44×44pt |

### Grid Patterns

- Metric cards: `ColumnDefinitions="*,*"` or `"*,*,*"` — equal columns, `ColumnSpacing="12"`
- Stats row: `ColumnDefinitions="*,*,*,*"`, `ColumnSpacing="8"`
- Filter chips: horizontal `CollectionView` or `FlexLayout Wrap="Wrap"` with 8px gap

---

## 7. Motion

**Animate only `transform` (Scale, Translate) and `opacity`. Never layout properties.**

### Button press
```csharp
// AnimationHelper.PressAsync or AnimatedButtonBehavior
ScaleTo(0.93, 65, Easing.CubicOut)
ScaleTo(1.0, 230, Easing.SpringOut)
```

### Pointer gestures (Border/custom views)
```csharp
OnPointerPressed → ScaleTo(0.93, 65, Easing.CubicOut)
OnPointerReleased → ScaleTo(1.0, 230, Easing.SpringOut)
```

### Page entrance (tab pages)
```csharp
Content.Opacity = 0; Content.TranslationY = 16;
await Task.WhenAll(
    Content.FadeTo(1, 400, Easing.CubicOut),
    Content.TranslateTo(0, 0, 400, Easing.CubicOut));
```

### Sliding indicator
```csharp
element.TranslateTo(targetX, 0, 280, Easing.SpringOut)
```

### Set row entrance (ActiveWorkoutPage)
- `Loaded` event, fade + translateY 12→0, 180ms, `CubicOut`

### Easing reference

| Easing | Use case |
|---|---|
| `Easing.CubicOut` | Entrances, fades, content reveals |
| `Easing.SpringOut` | Sliding indicators, bouncy press-release |
| `Easing.SinInOut` | Pulsing loaders |
| `Easing.Linear` | Looping animations (loader highlight sweep) |

---

## 8. Reference Image Analysis

The water-quality iOS app that inspired LockIn's visual direction demonstrates:

| Pattern | Implementation in LockIn |
|---|---|
| **Giant hero number** with semantic color status label | `HeroNumber` style + `ForgeAccentAmber`/`ForgeAccentBlue` label below |
| **Semicircular arc gauge** with gradient stroke | `WeeklyGoalGauge` (SkiaSharp) |
| **2×2 metric card grid** — muted label, colored status, large number | Two-column `Grid` with `CardFrame` cards |
| **Line chart** with dual-color series | `LineChartView` (SkiaSharp) |
| **Inline sparklines** inside metric cards | `SparklineView` (SkiaSharp) |
| **Bottom bar chart / time series** | Custom SkiaSharp bar rendering inside `WeeklyGoalGauge` or standalone |
| **Dark surface layering** — not flat black | `ForgeBackground` → `ForgeSurface` → `ForgeSurface2` depth stack |
| **Muted axis labels** | `GraphAxisText` (#52525E), `SectionLabel` style |
| **Atmospheric background glow** | `AtmosphericBackgroundView` |

Key insight: the reference app uses **color as a reading aid** — red means bad, blue means good, orange means warning — not decoration. LockIn follows the same rule: Amber = energy/strain, Blue = sleep/recovery, ForgeAccent silver = neutral data.

---

## 9. Dos and Don'ts

### Do
- Use `SectionLabel` (BebasNeue 11px, tracking 3, muted) for category kickers — sparingly
- Let big numbers dominate empty space — resist adding filler elements
- Layer `ForgeSurface` cards on `ForgeBackground` for depth
- Add `Shadow` to cards: use `#B8B8BC` at low opacity, not pure black
- Call `UpdateXxxIndicator(animated: false)` in `OnAppearing` for all sliding indicators
- Test every interactive element at ≥44×44pt

### Don't
- Hardcode hex strings outside `Colors.xaml` / `DesignTokens.cs`
- Use `ForgeSuccess` (#4ADE80) as brand color — semantic only
- Blur the core dot in `ForgeLiveDotView` — blur only the pulse ring
- Use `transition: all` equivalent — animate each property explicitly
- Add emoji or motivational language to UI text
- Add sections, features, or cards not requested — tystnad är en feature
- Use equal-weight typography across hierarchy levels
