using Foundation;
using UIKit;

namespace LockIn;

// Required by the UIScene life cycle (Xcode 27 SDK / Apple TN3187), registered in
// Info.plist under UIApplicationSceneManifest. Home Screen quick actions arrive here
// (connectionOptions.ShortcutItem / windowScene:performActionForShortcutItem:) and
// MauiUISceneDelegate forwards them to the PerformActionForShortcutItem lifecycle
// event registered in MauiProgram.
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		base.WillConnect(scene, session, connectionOptions);

		// The UIWindow is created by base.WillConnect, after AppDelegate.FinishedLaunching.
		if (Window is { } window)
			AppDelegate.ConfigureWindowBackground(window);
	}
}
