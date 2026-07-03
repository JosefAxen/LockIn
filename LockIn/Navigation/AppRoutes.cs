using LockIn.Models;

namespace LockIn.Navigation;

/// <summary>
/// Typade route-konstanter och navigations-helpers för Shell.
/// Ersätter magic strings spridda över hela kodbasen (ADR-003).
///
/// Konvention:
/// - Push-sidor: nameof(SomePage) → matchar Routing.RegisterRoute(nameof(SomePage), ...) i AppShell.
/// - Absoluta tab-routes: prefix "//" följt av tab-name från AppShell.xaml (t.ex. "//TrainPage").
/// - Bakåt-navigation: ".." (en nivå) eller "../.." (två nivåer).
/// - Parametriserade routes: bygg via helper-metoderna nedan, aldrig med rå string-interpolation.
/// </summary>
public static class AppRoutes
{
    // ─────── Bakåt-navigation ───────
    public const string Back = "..";
    public const string BackTwoLevels = "../..";

    // ─────── Tabs (absoluta routes) ───────
    public const string HemTab = "//HemPage";
    public const string TrainTab = "//TrainPage";
    public const string TrainTabLegacy = "//train"; // OBS: används i PostWorkoutViewModel — case-mismatch mot AppShell.xaml (Route="TrainPage")
    public const string HistoryTab = "//HistoryPage";
    public const string LibraryTab = "//LibraryPage";
    public const string KroppTab = "//KroppPage";

    // ─────── Push-sidor (nameof-baserade routes) ───────
    public const string ActiveWorkout = "ActiveWorkoutPage";
    public const string PostWorkout = "PostWorkoutPage";
    public const string SessionDetail = "SessionDetailPage";
    public const string ExercisePicker = "ExercisePickerPage";
    public const string ExerciseProgress = "ExerciseProgressPage";
    public const string ProgramDetail = "ProgramDetailPage";
    public const string BodyWeight = "BodyWeightPage";
    public const string PlateCalculator = "PlateCalculatorPage";
    public const string Settings = "SettingsPage";
    public const string Achievements = "AchievementsPage";
    public const string ProgressPhotos = "ProgressPhotosPage";
    public const string CreateExercise = "CreateExercisePage";
    public const string Cardio = "CardioPage";
    public const string Periodization = "PeriodizationPage";
    public const string MuscleVolume = "MuscleVolumePage";
    public const string WeeklyVolume = "WeeklyVolumePage";
    public const string MesoBuilder = "MesoBuilderPage";
    public const string MesoBuilderLegacy = "mesobuilder"; // OBS: används i PeriodizationViewModel — case-mismatch mot registreringen "MesoBuilderPage"

    // ─────── Lowercase-routes (explicit registrerade så i AppShell.xaml.cs) ───────
    public const string TemplatePlanningBoard = "templateplanningboard";
    public const string TemplatePicker = "templatepicker";
    public const string MesocycleStats = "mesocyclestats";

    // ─────── Parametriserade helpers ───────
    public static string TemplatePlanningBoardEdit(int templateId)
        => $"{TemplatePlanningBoard}?templateId={templateId}";

    public static string MesocycleStatsFor(int mesocycleId)
        => $"{MesocycleStats}?mesocycleId={mesocycleId}";

    public static string ExercisePickerForMuscle(int muscleGroupValue)
        => $"{ExercisePicker}?muscle={muscleGroupValue}";

    public static string ExercisePickerForMuscle(MuscleGroup muscle)
        => $"{ExercisePicker}?muscle={muscle}";
}
