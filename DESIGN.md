# DESIGN.md — Vana Strength (Forge Design System)

> Visual language, tokens, component patterns, and motion rules for the Vana Strength iOS app (repo: LockIn, .NET MAUI 10).
> Source of truth lives in `Colors.xaml` (XAML tokens) and `DesignTokens.cs` (C# tokens for SkiaSharp controls).
> Hex values below are copied from those files — if they disagree, the code wins. Avoid new hex outside those two files.

**Theme:** dark only. `Info.plist` sets `UIUserInterfaceStyle=Dark` and `App` sets `UserAppTheme=Dark`; the `Light*` tokens and `AppThemeBinding Light=` values are unused until a real light theme is designed (the page background is always the dark `AtmosphericBackgroundView`).
**Orientation:** iPhone, portrait only.

---

## 1. Color Tokens

### Surface Hierarchy

| Token | Hex | Role |
|---|---|---|
| `SK_Background` / `DesignTokens.Background` | `#1E1E26` | Visible page background (AtmosphericBackgroundView, splash, UIWindow, startup page). |
| `ForgeBackground` | `#141418` | `Page.BackgroundColor` behind the (transparent) pages. |
| `ForgeSurface` | `#161618` | Cards, sheets, overlays. |
| `ForgeSurface2` | `#222228` | Elevated cards, input fields. |
| `ForgeSurface3` | `#2E2E36` | Floating elements, tooltips. |
| `GraphGrid` (C#) | `#272732` | Graph grid lines only. |

### Borders

| Token | Hex | Use |
|---|---|---|
| `ForgeBorder` | `#252530` | Always-dark contexts |
| `ForgeBorderLight` | `#323240` | Standard card strokes |

### Text (contrast measured on `#1E1E26` / `#161618`)

| Token | Hex | Use |
|---|---|---|
| `ForgeText` | `#E2E8F0` | Primary text (≈14:1). |
| `ForgeTextSecondary` = `ForgeMuted` | `#A2A2A2` | Secondary labels, section headers (≈6.5–7:1). |
| `DesignTokens.TextMuted` / `GraphAxisText` / `CalNormalText` / `TextDim` | `#8E8E9A` | Lowest text level (≈5:1). Nothing dimmer is allowed for text. |
| `ForgeDestructiveText` / `DesignTokens.DestructiveText` | `#F87171` | Destructive **text** on dark surfaces (≈6:1). |
| `ForgeFabForeground` | `#0E0E10` | Text/icons on silver (`ForgeAccent`, `ForgeFabSilver*`) and on `ForgeSuccess` — never white there. |

### Accents

| Token | Hex | Role | Constraints |
|---|---|---|---|
| `ForgeAccent` | `#B8B8BC` | **Brand accent. Silver/steel.** | Use precisely — color is information |
| `ForgeAccentAmber` | `#FBBF24` | Energy, strain, warmth | Sparingly |
| `ForgeAccentBlue` | `#38BDF8` | Sleep, recovery, calmness | Sparingly |
| `ForgeSuccess` | `#4ADE80` | PR badges, success states | **Semantic only.** Dark foreground on it. |
| `ForgeDestructive` | `#CA3214` | Destructive **fills** (white text ≈5.3:1) | Use `ForgeDestructiveText` for text |

### Tab bar

Selected tab = `ForgeText`, unselected = `ForgeTextSecondary` (set in `AppShell.xaml` / `Styles.xaml`). No UIAppearance overrides.

### Chips

| Token | Hex |
|---|---|
| `ChipInactiveBg` | `#1C1C24` |
| `ChipInactiveFg` | `#A2A2A2` |
| `ChipActiveBg` / `ChipActiveFg` | `#3E3E44` / white |
| `HeatmapInactive` | `#1A1A22` |

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

#### `IconCircleButton` (Border, Style)
- Round icon button (back, close, settings, delete …): 44×44, ellipse, `ForgeOverlayWeak` fill, `ForgeOverlayStroke` 0.5
- Every instance needs `SemanticProperties.Description` (`A11y_*` keys)

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
1. Background arc — stroke, `SkiaTokens.TrackBg`
2. Progress arc — sweep gradient in the logo colors (red `#C0392B` → purple `#8E44AD` → blue `#3498DB`)
3. End dot with white glow
4. Center number — system font (SF Pro), white, bold; sub label `SkiaTokens.AxisText`

### MetricRingView

Three circular rings (Strain, Recovery, Sleep). Bindable: `Progress` (float 0–1), `RingColor` (Color), `CenterText` (string).

- 270° arc from 135°, `StrokeWidth` 14pt × density, `StrokeCap = Round`
- Background arc: `SkiaTokens.RingTrack`
- Active arc: `RingColor`, 800 ms entry animation (skipped with Reduce Motion)
- Center: system font (SF Pro), white, bold

### SparklineView

Compact trend line. Binds to float array. Background: transparent. Line: `ForgeAccent`. No axis labels.

### LineChartView

Multi-series line chart. Grid: `SkiaTokens.ChartSep`. Axis text 11pt. All sizes (margins, text, strokes, dots) are multiplied by the density scale `e.Info.Width / Width` — the canvas is in physical pixels. Same rule for every SkiaSharp control (MetricRingView, WeeklyGoalGauge and SparklineView already do this).

### AtmosphericBackgroundView

**Static** full-screen background — no animation, no particles (removed October 2026 for a cleaner look). Painted only when the size changes:

1. Base fill: `SK_Background` `#1E1E26`
2. Soft silver bloom at the top: five overlapping radial gradients (`SK_AccentGlow1/2`)
3. Vignette: radial `SK_Vignette` towards the edges

`InputTransparent="True"`. ZIndex 0 behind all content. No bindable properties.

### ForgeLiveDotView

Live workout indicator dot with pulsing ring.

- Pulse ring: `SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)` — ONLY on the ring
- Core dot: no blur. Draw LAST (on top) with fresh `SKPaint` without `MaskFilter`
- `StartPulse()` / `StopPulse()` from code-behind on `OnAppearing` / `OnDisappearing`
- With iOS "Reduce Motion" the pulse never starts (static dot only)

---

## 6. Layout Conventions

### Safe Areas

```
Root Grid: SafeAreaEdges="None"          ← required for edge-to-edge
Header padding top: 56pt                ← manual Dynamic Island clearance (portrait iPhone only)
Tab bar clearance: ≥110pt bottom padding ← on all scrollable tab content, more while the "PASS PÅGÅR" banner shows
Push pages with their own bottom CTA     ← Shell.TabBarIsVisible="False"
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

**Reduce Motion is mandatory.** Check `AnimationHelper.ReduceMotion` before every non-essential animation (entrances, count-ups, pulses, confetti); `AnimationHelper.PressAsync` / `PageEntryAsync` / `AnimatedButtonBehavior`, `ConfettiView`, `ForgeLiveDotView` and `MetricRingView` already do. Haptics go through `AnimationHelper.Haptic()` so the user's haptics setting is respected.

**No motivational theatre:** confetti only for real PRs (not first-ever sets, not achievements).

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
