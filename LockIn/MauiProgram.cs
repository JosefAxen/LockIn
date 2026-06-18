using CommunityToolkit.Maui;
using LockIn.Services;
using LockIn.ViewModels;
using LockIn.Views;
using Microsoft.Extensions.Logging;
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
#endif
            ;

        // Services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<PRService>();
        builder.Services.AddSingleton<RestTimerService>();
        builder.Services.AddSingleton<ActiveWorkoutStateService>();
        builder.Services.AddSingleton<NotificationService>();
#if IOS
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddSingleton<IHealthService, LockIn.Platforms.iOS.HealthKitService>();
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
        builder.Services.AddTransient<TemplateEditPage>();
        builder.Services.AddTransient<TemplateEditViewModel>();
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if IOS
        // Set edge-to-edge layout properties on every ContentPage VC BEFORE the
        // view is laid out. OnAppearing fires after layout — too late for these to
        // take effect. PageHandler.Mapper fires at handler creation time.
        Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping(
            "EdgeToEdge", (handler, _) =>
            {
                // IPageHandler doesn't expose ViewController — cast to the iOS-specific
                // IPlatformViewHandler which does.
                var vc = (handler as Microsoft.Maui.IPlatformViewHandler)?.ViewController;
                if (vc != null)
                {
                    vc.ExtendedLayoutIncludesOpaqueBars = true;
                    vc.EdgesForExtendedLayout = UIKit.UIRectEdge.All;
                }
            });

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
