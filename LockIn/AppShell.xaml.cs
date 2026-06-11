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
        Routing.RegisterRoute(nameof(SessionDetailPage), typeof(SessionDetailPage));
        Routing.RegisterRoute(nameof(ExerciseProgressPage), typeof(ExerciseProgressPage));
        Routing.RegisterRoute(nameof(ProgramDetailPage), typeof(ProgramDetailPage));
        Routing.RegisterRoute(nameof(BodyWeightPage), typeof(BodyWeightPage));
        Routing.RegisterRoute(nameof(PlateCalculatorPage), typeof(PlateCalculatorPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AchievementsPage), typeof(AchievementsPage));
        Routing.RegisterRoute(nameof(ProgressPhotosPage), typeof(ProgressPhotosPage));
    }
}
