using CommunityToolkit.Maui;
using LockIn.Services;
using LockIn.ViewModels;
using LockIn.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace LockIn;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
.UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("BebasNeue-Regular.ttf", "BebasNeue");
                fonts.AddFont("DMSans-Regular.ttf", "DMSansRegular");
                fonts.AddFont("DMSans-Medium.ttf", "DMSansMedium");
            })
#if IOS
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<LockIn.Views.AppIcon, LockIn.Platforms.iOS.AppIconHandler>();
            })
            // Home Screen quick actions. With the UIScene life cycle MauiUISceneDelegate raises
            // this for cold starts (connectionOptions.ShortcutItem) and while running.
            .ConfigureLifecycleEvents(events => events.AddiOS(ios => ios
                .PerformActionForShortcutItem((_, shortcutItem, completionHandler) =>
                {
                    AppDelegate.HandleShortcut(shortcutItem.Type);
                    completionHandler(true);
                })))
#endif
            ;

        // Services
        builder.Services.AddSingleton<DatabaseService>();
        // ADR-001: repositories per aggregat
        builder.Services.AddSingleton<Data.Repositories.ISettingsRepository, Data.Repositories.SettingsRepository>();
        builder.Services.AddSingleton<Data.Repositories.IBodyRepository, Data.Repositories.BodyRepository>();
        builder.Services.AddSingleton<Data.Repositories.IExerciseRepository, Data.Repositories.ExerciseRepository>();
        builder.Services.AddSingleton<Data.Repositories.ITemplateRepository, Data.Repositories.TemplateRepository>();
        builder.Services.AddSingleton<Data.Repositories.IMesocycleRepository, Data.Repositories.MesocycleRepository>();
        builder.Services.AddSingleton<Data.Repositories.ISessionRepository, Data.Repositories.SessionRepository>();
        builder.Services.AddTransient<PhotoService>();
        builder.Services.AddTransient<WorkoutShareService>();
        builder.Services.AddTransient<ProgressReportService>();
        builder.Services.AddSingleton<ExportService>();
        builder.Services.AddSingleton<PRService>();
        builder.Services.AddSingleton<IWeightProgressionService, WeightProgressionService>();
        builder.Services.AddSingleton<ISetProgressionEngine, SetProgressionEngine>();
        builder.Services.AddSingleton<IWeeklyVolumeService, WeeklyVolumeService>();
        builder.Services.AddSingleton<ICycleContextService, CycleContextService>();
        builder.Services.AddSingleton<IExerciseRecommendationService, ExerciseRecommendationService>();
        builder.Services.AddSingleton<IMesoBuilderService, MesoBuilderServiceV2>();
        builder.Services.AddSingleton<IVolumeProgressionService, VolumeProgressionService>();
        builder.Services.AddSingleton<ITemplateService, TemplateService>();
        builder.Services.AddSingleton<IMesocycleService, MesocycleService>();
        builder.Services.AddSingleton<RestTimerService>();
        builder.Services.AddSingleton<ActiveWorkoutStateService>();
        builder.Services.AddSingleton<NotificationService>();
#if IOS
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddSingleton<IHealthService, LockIn.Platforms.iOS.HealthKitService>();
#else
        builder.Services.AddSingleton<IHealthService, NullHealthService>();
#endif

        // Tab pages
        builder.Services.AddTransient<HemPage>();
        builder.Services.AddTransient<HemViewModel>();
        builder.Services.AddTransient<TrainPage>();
        builder.Services.AddTransient<TrainViewModel>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<LibraryPage>();
        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<KroppPage>();
        builder.Services.AddTransient<KroppViewModel>();

        // Navigation pages
        builder.Services.AddTransient<TemplatePlanningBoardPage>();
        builder.Services.AddTransient<TemplatePlanningBoardViewModel>();
        builder.Services.AddTransient<ExercisePickerPage>();
        builder.Services.AddTransient<ExercisePickerViewModel>();
        builder.Services.AddTransient<ActiveWorkoutPage>();
        builder.Services.AddSingleton<ActiveWorkoutViewModel>();
        builder.Services.AddTransient<PostWorkoutPage>();
        builder.Services.AddTransient<PostWorkoutViewModel>();
        builder.Services.AddTransient<SessionDetailPage>();
        builder.Services.AddTransient<SessionDetailViewModel>();
        builder.Services.AddTransient<ExerciseProgressPage>();
        builder.Services.AddTransient<ExerciseProgressViewModel>();
        builder.Services.AddTransient<ProgramDetailPage>();
        builder.Services.AddTransient<ProgramDetailViewModel>();
        builder.Services.AddTransient<BodyWeightPage>();
        builder.Services.AddTransient<BodyWeightViewModel>();
        builder.Services.AddTransient<PlateCalculatorPage>();
        builder.Services.AddTransient<PlateCalculatorViewModel>();
        builder.Services.AddTransient<AchievementsPage>();
        builder.Services.AddTransient<AchievementsViewModel>();
        builder.Services.AddTransient<ProgressPhotosPage>();
        builder.Services.AddTransient<ProgressPhotosViewModel>();
        builder.Services.AddTransient<CreateExercisePage>();
        builder.Services.AddTransient<CreateExerciseViewModel>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<CardioPage>();
        builder.Services.AddTransient<CardioViewModel>();
        builder.Services.AddTransient<MuscleVolumePage>();
        builder.Services.AddTransient<MuscleVolumeViewModel>();
        builder.Services.AddTransient<WeeklyVolumePage>();
        builder.Services.AddTransient<WeeklyVolumeViewModel>();
        builder.Services.AddTransient<MesoBuilderPage>();
        builder.Services.AddTransient<MesoBuilderViewModel>();
        builder.Services.AddTransient<PeriodizationPage>();
        builder.Services.AddTransient<PeriodizationViewModel>();
        builder.Services.AddTransient<TemplatePickerPage>();
        builder.Services.AddTransient<TemplatePickerViewModel>();
        builder.Services.AddTransient<MesocycleStatsPage>();
        builder.Services.AddTransient<MesocycleStatsViewModel>();
        builder.Services.AddTransient<JointPainSheetViewModel>();
        builder.Services.AddTransient<MuscleFeedbackSheetViewModel>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if IOS
        // Prevent iOS from auto-adding safe-area contentInset to scroll views.
        // We handle all top/bottom padding manually in XAML.
        Microsoft.Maui.Handlers.ScrollViewHandler.Mapper.AppendToMapping(
            "NoInsetAdjustment", (handler, _) =>
            {
                handler.PlatformView.ContentInsetAdjustmentBehavior =
                    UIKit.UIScrollViewContentInsetAdjustmentBehavior.Never;
            });

        // Tint button icon images white. Deferred to next run-loop so the image
        // is guaranteed to be loaded when we call ImageForState.
        Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping(
            "ImageSource", (handler, _) =>
        {
            var platformView = handler.PlatformView;
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                var img = platformView.ImageForState(UIKit.UIControlState.Normal);
                if (img is null || img.RenderingMode == UIKit.UIImageRenderingMode.AlwaysTemplate) return;
                platformView.SetImage(
                    img.ImageWithRenderingMode(UIKit.UIImageRenderingMode.AlwaysTemplate),
                    UIKit.UIControlState.Normal);
                platformView.TintColor = UIKit.UIColor.White;
            });
        });
#endif

        return builder.Build();
    }
}
