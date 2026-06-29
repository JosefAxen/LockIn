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
