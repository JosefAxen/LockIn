using LockIn.Services;
using LockIn.ViewModels;
using LockIn.Views;
using Microsoft.Extensions.Logging;

namespace LockIn;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("BebasNeue-Regular.ttf", "BebasNeue");
                fonts.AddFont("DMSans-Regular.ttf", "DMSansRegular");
                fonts.AddFont("DMSans-Medium.ttf", "DMSansMedium");
            });

        // Services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<PRService>();
        builder.Services.AddSingleton<RestTimerService>();

        // Tab pages
        builder.Services.AddTransient<TrainPage>();
        builder.Services.AddTransient<TrainViewModel>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<LibraryPage>();
        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Navigation pages
        builder.Services.AddTransient<TemplateEditPage>();
        builder.Services.AddTransient<TemplateEditViewModel>();
        builder.Services.AddTransient<ExercisePickerPage>();
        builder.Services.AddTransient<ExercisePickerViewModel>();
        builder.Services.AddTransient<ActiveWorkoutPage>();
        builder.Services.AddTransient<ActiveWorkoutViewModel>();
        builder.Services.AddTransient<PostWorkoutPage>();
        builder.Services.AddTransient<PostWorkoutViewModel>();
        builder.Services.AddTransient<SessionDetailPage>();
        builder.Services.AddTransient<SessionDetailViewModel>();
        builder.Services.AddTransient<ExerciseProgressPage>();
        builder.Services.AddTransient<ExerciseProgressViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
