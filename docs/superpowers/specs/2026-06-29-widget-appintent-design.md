# Widget / App Intent — Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Lägg till tre hemskärms-Quick Actions (UIApplicationShortcutItems) som användaren når via haptic touch (lång tryckning) på app-ikonen, utan krav på native Swift-extension.

**Architecture:** `UIApplicationShortcutItem`-objekt registreras statiskt i `AppDelegate.cs`. `PerformActionForShortcutItem` fångar användarvalet och navigerar via `Shell.Current.GoToAsync` / `Shell.Current.CurrentItem`. Tre i18n-nycklar för titel + undertitel per action.

**Tech Stack:** .NET MAUI 10 iOS, `UIKit.UIApplicationShortcutItem`, `Shell.Current` navigation, CommunityToolkit.Maui Alert (för fritt pass-bekräftelse).

**Motivering till scoping:** Live Activities (ActivityKit) och WidgetKit kräver en separat native Swift App Extension-target som inte stöds direkt av MAUI:s `.csproj`-baserade build-system. Quick Actions levererar "starta träning/se status från hemskärmen" utan denna komplexitet.

---

## Global Constraints

- Target: `net10.0-ios`, .NET MAUI 10
- Plattformskod ENBART i `Platforms/iOS/AppDelegate.cs` — inga nya filer
- Quick Action type-strängar: `"com.josefaxen.lockin.startfreeworkout"`, `"com.josefaxen.lockin.logweight"`, `"com.josefaxen.lockin.history"`
- Ikoner: `UIApplicationShortcutIcon.FromSystemImageName("play.fill")`, `UIApplicationShortcutIcon.FromSystemImageName("scalemass.fill")`, `UIApplicationShortcutIcon.FromSystemImageName("calendar")`
- Navigation görs via `MainThread.BeginInvokeOnMainThread(() => Shell.Current.GoToAsync(...))`
- Inga hårdkodade svenska strängar — alla titlar via `NSString` med lokaliserade värden (hardcoded konstanter är OK här eftersom UIKit ej stöder MAUI resx direkt — se nedan)
- `PerformActionForShortcutItem` returnerar `true` alltid (iOS kräver det)
- Strängar för Quick Actions kan ej hämtas från AppResources vid app-start (AppResources kräver MAUI-runtime) — använd hårdkodade svenska titlar direkt i `AppDelegate.cs` (appen är på svenska)

---

## Quick Actions

| Type-sträng | Titel | Undertitel | Ikon |
|-------------|-------|-----------|------|
| `…startfreeworkout` | Starta fritt pass | Träna utan mall | `play.fill` |
| `…logweight` | Logga vikt | Lägg till kroppsvikt | `scalemass.fill` |
| `…history` | Se historik | Dina senaste pass | `calendar` |

---

## Navigationsmapping

| Type-sträng | Åtgärd |
|-------------|--------|
| `startfreeworkout` | `Shell.Current.GoToAsync("//TrainPage")` — TrainPage hanterar redan "fritt pass"-flödet via sin befintliga FAB-knapp. Navigering till TrainPage räcker. |
| `logweight` | `Shell.Current.GoToAsync(nameof(BodyWeightPage))` |
| `history` | `Shell.Current.GoToAsync("//HistoryPage")` |

---

## Implementationsdetaljer

### AppDelegate.cs — registrering

Registrera shortcuts i `FinishedLaunching` EFTER den befintliga UI-konfigurationen:

```csharp
UIApplication.SharedApplication.ShortcutItems = new[]
{
    new UIApplicationShortcutItem(
        "com.josefaxen.lockin.startfreeworkout",
        "Starta fritt pass",
        "Träna utan mall",
        UIApplicationShortcutIcon.FromSystemImageName("play.fill"),
        null),
    new UIApplicationShortcutItem(
        "com.josefaxen.lockin.logweight",
        "Logga vikt",
        "Lägg till kroppsvikt",
        UIApplicationShortcutIcon.FromSystemImageName("scalemass.fill"),
        null),
    new UIApplicationShortcutItem(
        "com.josefaxen.lockin.history",
        "Se historik",
        "Dina senaste pass",
        UIApplicationShortcutIcon.FromSystemImageName("calendar"),
        null)
};
```

### AppDelegate.cs — hantering

Override `PerformActionForShortcutItem`:

```csharp
public override void PerformActionForShortcutItem(
    UIApplication application,
    UIApplicationShortcutItem shortcutItem,
    UIOperationHandler completionHandler)
{
    HandleShortcut(shortcutItem.Type);
    completionHandler(true);
}

private static void HandleShortcut(string type)
{
    MainThread.BeginInvokeOnMainThread(async () =>
    {
        switch (type)
        {
            case "com.josefaxen.lockin.startfreeworkout":
                await Shell.Current.GoToAsync("//TrainPage");
                break;
            case "com.josefaxen.lockin.logweight":
                await Shell.Current.GoToAsync(nameof(BodyWeightPage));
                break;
            case "com.josefaxen.lockin.history":
                await Shell.Current.GoToAsync("//HistoryPage");
                break;
        }
    });
}
```

### Kallstart (Cold Start)

Om appen startar kall via en Quick Action (ej via resume) skickas shortcut-info i `launchOptions` till `FinishedLaunching`. Hantera det:

```csharp
// I slutet av FinishedLaunching, EFTER base.FinishedLaunching:
if (launchOptions != null &&
    launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var shortcut) &&
    shortcut is UIApplicationShortcutItem item)
{
    // Navigering sker efter Shell är initierat — fördröj
    Task.Delay(500).ContinueWith(_ => HandleShortcut(item.Type));
}
```

---

## Scope ej inkluderat

- Live Activity / Dynamic Island (kräver native Swift extension)
- WidgetKit hemskärmswidget (kräver native Swift extension)
- Siri App Intent (kräver AppIntents framework-bindning)

Dessa kan implementeras i en framtida sprint när MAUI får bättre extension-stöd.
