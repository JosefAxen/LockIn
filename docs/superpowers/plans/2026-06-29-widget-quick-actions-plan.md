# iOS Home Screen Quick Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lägg till tre UIApplicationShortcutItems (hemskärms-Quick Actions) i LockIn som visas vid haptic touch på app-ikonen — Starta fritt pass, Logga vikt, Se historik — utan krav på native Swift extension.

**Architecture:** Alla ändringar görs enbart i `Platforms/iOS/AppDelegate.cs`. Tre statiska shortcut-items registreras i `FinishedLaunching`. En override av `PerformActionForShortcutItem` fångar trycket och navigerar via `Shell.Current.GoToAsync`. Kallstart (cold start via shortcut) hanteras med en 500ms-fördröjd navigering för att ge MAUI-runtime tid att initiera Shell.

**Tech Stack:** .NET MAUI 10, `net10.0-ios`, `UIKit.UIApplicationShortcutItem`, `UIKit.UIApplicationShortcutIcon`, `Shell.Current.GoToAsync`, `MainThread.BeginInvokeOnMainThread`.

## Global Constraints

- Target: `net10.0-ios`, .NET MAUI 10 — koden kompileras ENDAST för iOS
- Plattformskod ENBART i `LockIn/Platforms/iOS/AppDelegate.cs` — inga nya filer skapas
- Quick Action type-strängar (exakta): `"com.josefaxen.lockin.startfreeworkout"`, `"com.josefaxen.lockin.logweight"`, `"com.josefaxen.lockin.history"`
- Ikoner: `UIApplicationShortcutIcon.FromSystemImageName("play.fill")`, `UIApplicationShortcutIcon.FromSystemImageName("scalemass.fill")`, `UIApplicationShortcutIcon.FromSystemImageName("calendar")`
- Titlar (hårdkodade svenska, UIKit stöder ej MAUI resx): "Starta fritt pass" / "Träna utan mall", "Logga vikt" / "Lägg till kroppsvikt", "Se historik" / "Dina senaste pass"
- Navigering: `Shell.Current.GoToAsync` wrappat i `MainThread.BeginInvokeOnMainThread`
- `PerformActionForShortcutItem` MÅSTE kalla `completionHandler(true)` alltid
- Inga tester i projektet — verifiera via `dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug`
- Inga hårdkodade hex-strängar utanför `Colors.xaml` / `DesignTokens.cs` (gäller inte UIKit UIColor — de används redan i befintlig kod)

---

## Filstruktur

| Fil | Ändring |
|-----|---------|
| `LockIn/Platforms/iOS/AppDelegate.cs` | Lägg till `RegisterShortcutItems`, `HandleColdStartShortcut`, `PerformActionForShortcutItem`, `HandleShortcut` |

---

### Task 1: UIApplicationShortcutItems i AppDelegate.cs

**Files:**
- Modify: `LockIn/Platforms/iOS/AppDelegate.cs` (79 rader — läs hela filen innan du gör ändringar)
- Verify (läs): `LockIn/AppShell.xaml.cs` — kontrollera att routerna `"TrainPage"`, `"BodyWeightPage"`, `"HistoryPage"` finns registrerade

**Interfaces:**
- Produces: tre hemskärms-shortcut-items synliga vid haptic touch på app-ikonen; navigering till TrainPage / BodyWeightPage / HistoryPage vid tryckning

---

- [ ] **Steg 1: Läs AppShell.xaml.cs och verifiera route-strängar**

Öppna `LockIn/AppShell.xaml.cs`. Bekräfta att dessa routes är registrerade:
- `Routing.RegisterRoute(nameof(BodyWeightPage), typeof(BodyWeightPage))` — ger route `"BodyWeightPage"`

Bekräfta att AppShell.xaml har tab-routes för `TrainPage` och `HistoryPage` (de navigeras med `"//TrainPage"` resp. `"//HistoryPage"`). Om routenamnen avviker, använd de faktiska namnen i Steg 3.

- [ ] **Steg 2: Läs hela AppDelegate.cs och bekanta dig med befintlig struktur**

Filen är 79 rader. Notera att `FinishedLaunching` redan kallar:
- `base.FinishedLaunching(application, launchOptions)` — lagrar returnvärdet i `result`
- `ConfigureTabBarAppearance()`
- `ConfigureNavBarAppearance()`
- `ConfigureWindowBackground(application)`
- returnerar `result`

De nya metoderna läggs till EFTER de befintliga private-metoderna, och `FinishedLaunching` utökas med två anrop.

- [ ] **Steg 3: Ersätt AppDelegate.cs med komplett uppdaterad version**

Skriv den fullständiga nya filen (behåll alla befintliga metoder oförändrade, lägg till de nya):

```csharp
using Foundation;
using UIKit;

namespace LockIn;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        ConfigureTabBarAppearance();
        ConfigureNavBarAppearance();
        ConfigureWindowBackground(application);
        RegisterShortcutItems(application);
        HandleColdStartShortcut(launchOptions);
        return result;
    }

    public override void PerformActionForShortcutItem(
        UIApplication application,
        UIApplicationShortcutItem shortcutItem,
        UIOperationHandler completionHandler)
    {
        HandleShortcut(shortcutItem.Type);
        completionHandler(true);
    }

    private static void RegisterShortcutItems(UIApplication application)
    {
        application.ShortcutItems = new[]
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
    }

    private static void HandleColdStartShortcut(NSDictionary? launchOptions)
    {
        if (launchOptions == null) return;
        if (!launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var val)) return;
        if (val is not UIApplicationShortcutItem item) return;
        var type = item.Type;
        // Shell is not yet initialized at FinishedLaunching time; defer navigation.
        Task.Delay(500).ContinueWith(_ => HandleShortcut(type));
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
                    await Shell.Current.GoToAsync("BodyWeightPage");
                    break;
                case "com.josefaxen.lockin.history":
                    await Shell.Current.GoToAsync("//HistoryPage");
                    break;
            }
        });
    }

    private static void ConfigureNavBarAppearance()
    {
        var appearance = new UINavigationBarAppearance();
        appearance.ConfigureWithTransparentBackground();
        appearance.ShadowColor = UIColor.Clear;
        UINavigationBar.Appearance.StandardAppearance     = appearance;
        UINavigationBar.Appearance.ScrollEdgeAppearance   = appearance;
        if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            UINavigationBar.Appearance.CompactScrollEdgeAppearance = appearance;
    }

    private static void ConfigureWindowBackground(UIApplication application)
    {
        var bgColor = UIColor.FromDynamicProvider(t =>
            t.UserInterfaceStyle == UIUserInterfaceStyle.Dark
                ? UIColor.FromRGB(0x0E, 0x0E, 0x10)
                : UIColor.FromRGB(0xFC, 0xFC, 0xFC));

        // application.Windows is deprecated and empty in scene-based apps at
        // FinishedLaunching time. Use ConnectedScenes instead, deferred to the next
        // run loop when scene connection may have completed.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var scene in application.ConnectedScenes)
                if (scene is UIWindowScene ws)
                    foreach (var win in ws.Windows)
                        win.BackgroundColor = bgColor;
        });
    }

    internal static void ConfigureTabBarAppearance()
    {
        var accent = UIColor.FromRGB(0xFF, 0x5A, 0x1F);
        var unselected = UIColor.FromDynamicProvider(t =>
            t.UserInterfaceStyle == UIUserInterfaceStyle.Dark
                ? UIColor.FromRGB(0xA0, 0xA0, 0xA8)
                : UIColor.FromRGB(0x8E, 0x8E, 0x93));

        var appearance = new UITabBarAppearance();
        // Transparent background — content bleeds behind the tab bar
        appearance.ConfigureWithTransparentBackground();
        appearance.BackgroundColor = UIColor.Clear;
        appearance.ShadowColor     = UIColor.Clear;  // remove top separator line

        var item = new UITabBarItemAppearance();
        item.Normal.IconColor = unselected;
        item.Normal.TitleTextAttributes = new UIStringAttributes { ForegroundColor = unselected };
        item.Selected.IconColor = accent;
        item.Selected.TitleTextAttributes = new UIStringAttributes { ForegroundColor = accent };

        appearance.StackedLayoutAppearance       = item;
        appearance.InlineLayoutAppearance        = item;
        appearance.CompactInlineLayoutAppearance = item;

        UITabBar.Appearance.StandardAppearance = appearance;
        if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            UITabBar.Appearance.ScrollEdgeAppearance = appearance;
    }
}
```

- [ ] **Steg 4: Bygg projektet och verifiera att det kompilerar**

```bash
dotnet build LockIn/LockIn.csproj -f net10.0-ios -c Debug
```

Förväntat utfall: `Build succeeded.` — inga errors. Warnings om deprecated API är acceptabla.

Om build misslyckas: kontrollera att `UIApplicationShortcutItem`-konstruktorn finns tillgänglig i den iOS SDK-version som är installerad. Alternativ konstruktor om den femar:
```csharp
new UIApplicationShortcutItem("com.josefaxen.lockin.startfreeworkout", "Starta fritt pass")
```
(utan ikon/undertitel — funkar men utan SF Symbol)

- [ ] **Steg 5: Commit**

```bash
git add LockIn/Platforms/iOS/AppDelegate.cs
git commit -m "feat(ios): lägg till hemskärms-Quick Actions via UIApplicationShortcutItems"
```
