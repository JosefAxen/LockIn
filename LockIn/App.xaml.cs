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
        return new Window(new AppShell());
    }
}
