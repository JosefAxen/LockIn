using LockIn.Navigation;
using LockIn.Views;

namespace LockIn;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(AppRoutes.TemplatePlanningBoard, typeof(TemplatePlanningBoardPage));
        Routing.RegisterRoute(AppRoutes.ExercisePicker,        typeof(ExercisePickerPage));
        Routing.RegisterRoute(AppRoutes.ActiveWorkout,         typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(AppRoutes.PostWorkout,           typeof(PostWorkoutPage));
        Routing.RegisterRoute(AppRoutes.SessionDetail,         typeof(SessionDetailPage));
        Routing.RegisterRoute(AppRoutes.ExerciseProgress,      typeof(ExerciseProgressPage));
        Routing.RegisterRoute(AppRoutes.ProgramDetail,         typeof(ProgramDetailPage));
        Routing.RegisterRoute(AppRoutes.BodyWeight,            typeof(BodyWeightPage));
        Routing.RegisterRoute(AppRoutes.PlateCalculator,       typeof(PlateCalculatorPage));
        Routing.RegisterRoute(AppRoutes.Settings,              typeof(SettingsPage));
        Routing.RegisterRoute(AppRoutes.Achievements,          typeof(AchievementsPage));
        Routing.RegisterRoute(AppRoutes.ProgressPhotos,        typeof(ProgressPhotosPage));
        Routing.RegisterRoute(AppRoutes.CreateExercise,        typeof(CreateExercisePage));
        Routing.RegisterRoute(AppRoutes.Cardio,                typeof(CardioPage));
        Routing.RegisterRoute(AppRoutes.Periodization,         typeof(PeriodizationPage));
        Routing.RegisterRoute(AppRoutes.MuscleVolume,          typeof(MuscleVolumePage));
        Routing.RegisterRoute(AppRoutes.WeeklyVolume,          typeof(WeeklyVolumePage));
        Routing.RegisterRoute(AppRoutes.MesoBuilder,           typeof(MesoBuilderPage));
        Routing.RegisterRoute(AppRoutes.TemplatePicker,        typeof(TemplatePickerPage));
        Routing.RegisterRoute(AppRoutes.MesocycleStats,        typeof(MesocycleStatsPage));
    }
}
