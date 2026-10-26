using Foundation;
using LockIn.Navigation;
using UIKit;

namespace LockIn;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		var result = base.FinishedLaunching(application, launchOptions);
		ConfigureNavBarAppearance();
		// Window background and quick actions are handled by SceneDelegate: with the
		// UIScene life cycle the window doesn't exist yet here, launchOptions carries no
		// shortcut item and PerformActionForShortcutItem is never called on the app delegate.
		RegisterShortcutItems(application);
		return result;
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

	// Called from the PerformActionForShortcutItem lifecycle event (see MauiProgram), which
	// MauiUISceneDelegate raises for both cold starts and quick actions while running.
	internal static void HandleShortcut(string type)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			// On a cold start AppShell is set only after App.InitNavigationAsync has
			// initialized the database; wait for it (it never appears during onboarding).
			for (var i = 0; i < 50 && Shell.Current is null; i++)
				await Task.Delay(100);
			if (Shell.Current is not { } shell) return;

			switch (type)
			{
				case "com.josefaxen.lockin.startfreeworkout":
					await shell.GoToAsync(AppRoutes.TrainTab);
					break;
				case "com.josefaxen.lockin.logweight":
					await shell.GoToAsync(AppRoutes.BodyWeight);
					break;
				case "com.josefaxen.lockin.history":
					await shell.GoToAsync(AppRoutes.HistoryTab);
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

	// Called from SceneDelegate.WillConnect once the scene's UIWindow exists.
	// Same color as splash, the startup page and AtmosphericBackgroundView (DesignTokens.SK_Background).
	internal static void ConfigureWindowBackground(UIWindow window)
	{
		window.BackgroundColor = UIColor.FromRGB(0x1E, 0x1E, 0x26);
	}
}
