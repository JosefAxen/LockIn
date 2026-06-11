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
		return result;
	}

	internal static void ConfigureTabBarAppearance()
	{
		var accent = UIColor.FromRGB(0xFF, 0x5A, 0x1F);
		var unselected = UIColor.FromDynamicProvider(t =>
			t.UserInterfaceStyle == UIUserInterfaceStyle.Dark
				? UIColor.FromRGB(0xA0, 0xA0, 0xA8)
				: UIColor.FromRGB(0x8E, 0x8E, 0x93));
		var tabBg = UIColor.FromDynamicProvider(t =>
			t.UserInterfaceStyle == UIUserInterfaceStyle.Dark
				? UIColor.FromRGB(0x11, 0x11, 0x11)
				: UIColor.White);

		var appearance = new UITabBarAppearance();
		appearance.ConfigureWithOpaqueBackground();
		appearance.BackgroundColor = tabBg;

		var item = new UITabBarItemAppearance();
		item.Normal.IconColor = unselected;
		item.Normal.TitleTextAttributes = new UIStringAttributes { ForegroundColor = unselected };
		item.Selected.IconColor = accent;
		item.Selected.TitleTextAttributes = new UIStringAttributes { ForegroundColor = accent };

		appearance.StackedLayoutAppearance    = item;
		appearance.InlineLayoutAppearance     = item;
		appearance.CompactInlineLayoutAppearance = item;

		UITabBar.Appearance.StandardAppearance = appearance;
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			UITabBar.Appearance.ScrollEdgeAppearance = appearance;
	}
}
