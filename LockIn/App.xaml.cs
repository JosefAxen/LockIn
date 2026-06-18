using LockIn.Services;
using LockIn.Views;

namespace LockIn;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
#if IOS
        RequestedThemeChanged += (_, _) => AppDelegate.ConfigureTabBarAppearance();
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start with dark blank page to avoid flash, then navigate based on onboarding state
        var splash = new ContentPage { BackgroundColor = Color.FromArgb("#0E0E10") };
        var window = new Window(splash);

        window.HandlerChanged += (_, _) =>
        {
#if IOS
            if (window.Handler?.PlatformView is UIKit.UIView uiView)
            {
                var tap = new UIKit.UITapGestureRecognizer(_ =>
                    UIKit.UIApplication.SharedApplication.SendAction(
                        new ObjCRuntime.Selector("resignFirstResponder"), null, null, null))
                {
                    CancelsTouchesInView = false
                };
                uiView.AddGestureRecognizer(tap);

                // application.Windows is empty in scene-based iOS apps during FinishedLaunching
                // (UIScene connects AFTER that callback), so ConfigureWindowBackground's loop is
                // dead code. Set the UIWindow background here where uiView.Window is guaranteed.
                if (uiView.Window is { } nativeWindow)
                    nativeWindow.BackgroundColor = UIKit.UIColor.FromDynamicProvider(t =>
                        t.UserInterfaceStyle == UIKit.UIUserInterfaceStyle.Dark
                            ? UIKit.UIColor.FromRGB(0x0E, 0x0E, 0x10)
                            : UIKit.UIColor.FromRGB(0xFC, 0xFC, 0xFC));
            }
#endif
        };

        _ = InitNavigationAsync(window);

        return window;
    }

    private static async Task InitNavigationAsync(Window window)
    {
        var services = IPlatformApplication.Current!.Services;
        var db       = services.GetRequiredService<DatabaseService>();
        var notifs   = services.GetRequiredService<NotificationService>();

        await db.InitAsync();
        _ = notifs.RequestPermissionAsync();

        var settings = await db.GetAppSettingsAsync();

        // Auto-complete onboarding for existing installs that already have data
        if (!settings.HasCompletedOnboarding && await db.HasAnyCompletedSessionsAsync())
        {
            settings.HasCompletedOnboarding = true;
            await db.SaveAppSettingsAsync(settings);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            window.Page = settings.HasCompletedOnboarding
                ? (Page)new AppShell()
                : services.GetRequiredService<OnboardingPage>();

        });
    }
}
