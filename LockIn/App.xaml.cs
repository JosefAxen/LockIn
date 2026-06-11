using LockIn.Services;

namespace LockIn;

public partial class App : Application
{
    public App(DatabaseService db)
    {
        InitializeComponent();
        InitDbAsync(db);
    }

    private static async void InitDbAsync(DatabaseService db)
    {
        await db.InitAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        window.HandlerChanged += (_, _) =>
        {
#if IOS
            if (window.Handler?.PlatformView is UIKit.UIView uiView)
            {
                var tap = new UIKit.UITapGestureRecognizer(_ => uiView.EndEditing(true))
                {
                    CancelsTouchesInView = false
                };
                uiView.AddGestureRecognizer(tap);
            }
#endif
        };
        return window;
    }
}
