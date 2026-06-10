using LockIn.Views;

namespace LockIn;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(TemplateEditPage), typeof(TemplateEditPage));
        Routing.RegisterRoute(nameof(ExercisePickerPage), typeof(ExercisePickerPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(PostWorkoutPage), typeof(PostWorkoutPage));
    }
}
