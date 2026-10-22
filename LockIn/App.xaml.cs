using LockIn.Services;
using LockIn.Views;

namespace LockIn;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // Appen är bara designad för mörkt läge (Info.plist sätter också UIUserInterfaceStyle=Dark).
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start with a blank page in the same color as splash/window/page background to avoid
        // visible color steps, then navigate based on onboarding state
        var splash = new ContentPage { BackgroundColor = DesignTokens.Background };
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
                // UIWindow background is set by SceneDelegate.WillConnect.
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
        var settingsRepo = services.GetRequiredService<Data.Repositories.ISettingsRepository>();
        var sessionRepo  = services.GetRequiredService<Data.Repositories.ISessionRepository>();

        // Notification permission is requested on first use (NotificationService), not at launch.
        await db.InitAsync();

        var settings = await settingsRepo.GetAsync();

        // Auto-complete onboarding for existing installs that already have data
        if (!settings.HasCompletedOnboarding && await sessionRepo.HasAnyCompletedAsync())
        {
            settings.HasCompletedOnboarding = true;
            await settingsRepo.SaveAsync(settings);
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            window.Page = settings.HasCompletedOnboarding
                ? (Page)services.GetRequiredService<AppShell>()
                : services.GetRequiredService<OnboardingPage>();
        });
    }
}
