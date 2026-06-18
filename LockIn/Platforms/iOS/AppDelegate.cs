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
		return result;
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

	// Walk the VC tree and set ExtendedLayoutIncludesOpaqueBars so content
	// fills behind the (hidden) nav bar and the transparent tab bar.
	// Container VCs (UINavigationController, UITabBarController) get a transparent
	// background so their system-gray default never bleeds through.
	internal static void ApplyEdgeToEdge(UIViewController? root)
	{
		if (root is null) return;
		root.ExtendedLayoutIncludesOpaqueBars = true;
		root.EdgesForExtendedLayout = UIRectEdge.All;
		if (root is UINavigationController or UITabBarController)
			root.View?.BackgroundColor = UIColor.Clear;
		foreach (var child in root.ChildViewControllers)
			ApplyEdgeToEdge(child);
	}

	private static void ConfigureWindowBackground(UIApplication application)
	{
		var bgColor = UIColor.FromDynamicProvider(t =>
			t.UserInterfaceStyle == UIUserInterfaceStyle.Dark
				? UIColor.FromRGB(0x0E, 0x0E, 0x10)
				: UIColor.FromRGB(0xFC, 0xFC, 0xFC));

		MainThread.BeginInvokeOnMainThread(() =>
		{
			foreach (var win in application.Windows)
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
