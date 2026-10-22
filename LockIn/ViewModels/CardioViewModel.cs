using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class CardioViewModel(ISessionRepository sessionRepo, IHealthService health) : ObservableObject
{
    public record ActivityOption(CardioActivityType Type, string Name);

    public List<ActivityOption> ActivityOptions { get; } = Enum.GetValues<CardioActivityType>()
        .Select(t => new ActivityOption(t, ActivityTypeLabel(t)))
        .ToList();

    [ObservableProperty] private ActivityOption _selectedActivity =
        new(CardioActivityType.Running, AppResources.Cardio_Activity_Running);

    [ObservableProperty] private string _durationText = "";
    [ObservableProperty] private string _distanceText = "";
    [ObservableProperty] private string _heartRateText = "";
    [ObservableProperty] private string _caloriesText = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private string _customName = "";
    [ObservableProperty] private bool _isCustom;

    partial void OnSelectedActivityChanged(ActivityOption value)
        => IsCustom = value.Type == CardioActivityType.Custom;

    [RelayCommand]
    private void SelectActivity(ActivityOption opt) => SelectedActivity = opt;

    // "5,2" och "5.2" ska båda bli 5,2 km. Aldrig NumberStyles.Any med InvariantCulture —
    // då tolkas kommat som tusentalsavgränsare och "5,2" blir 52.
    private static decimal ParseNumber(string? text) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 0m;

    private bool _isSaving;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedActivity is null || _isSaving) return;
        _isSaving = true;

        try
        {
            var session = new CardioSession
            {
                ActivityType       = SelectedActivity.Type,
                CustomActivityName = IsCustom ? CustomName : "",
                StartedAt          = DateTime.Now,
                DurationMinutes    = (int)Math.Round(ParseNumber(DurationText)),
                DistanceKm         = (double)ParseNumber(DistanceText),
                AvgHeartRate       = (int)Math.Round(ParseNumber(HeartRateText)),
                CaloriesBurned     = (int)Math.Round(ParseNumber(CaloriesText)),
                Notes              = Notes,
            };

            await sessionRepo.SaveCardioSessionAsync(session);

            // Apple Hälsa är en bonus — ett fel där ska inte stoppa att passet sparats lokalt
            try
            {
                var hkStart = session.StartedAt;
                var hkEnd   = hkStart.AddMinutes(session.DurationMinutes > 0 ? session.DurationMinutes : 1);
                await health.SaveCardioWorkoutAsync(
                    session.ActivityType,
                    hkStart,
                    hkEnd,
                    session.CaloriesBurned,
                    session.DistanceKm * 1000.0);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Cardio] HealthKit: {ex}"); }

            await Shell.Current.GoToAsync(AppRoutes.Back);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cardio] Save: {ex}");
            try { await Shell.Current.DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK); }
            catch { /* felvisningen får inte själv fälla sidan */ }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private static string ActivityTypeLabel(CardioActivityType t) => t switch
    {
        CardioActivityType.Running            => AppResources.Cardio_Activity_Running,
        CardioActivityType.OutdoorCycling     => AppResources.Cardio_Activity_OutdoorCycling,
        CardioActivityType.IndoorCycling      => AppResources.Cardio_Activity_IndoorCycling,
        CardioActivityType.Rowing             => AppResources.Cardio_Activity_Rowing,
        CardioActivityType.Stairmaster        => AppResources.Cardio_Activity_Stairmaster,
        CardioActivityType.Elliptical         => AppResources.Cardio_Activity_Elliptical,
        CardioActivityType.Walking            => AppResources.Cardio_Activity_Walking,
        CardioActivityType.Swimming           => AppResources.Cardio_Activity_Swimming,
        CardioActivityType.JumpRope           => AppResources.Cardio_Activity_JumpRope,
        CardioActivityType.Hiit               => AppResources.Cardio_Activity_Hiit,
        CardioActivityType.Boxing             => AppResources.Cardio_Activity_Boxing,
        CardioActivityType.Padel              => AppResources.Cardio_Activity_Padel,
        CardioActivityType.Dancing            => AppResources.Cardio_Activity_Dancing,
        CardioActivityType.Yoga               => AppResources.Cardio_Activity_Yoga,
        CardioActivityType.CrossCountrySkiing => AppResources.Cardio_Activity_CrossCountrySkiing,
        CardioActivityType.Other              => AppResources.Cardio_Activity_Other,
        CardioActivityType.Custom             => AppResources.Cardio_Activity_Custom,
        _                                     => t.ToString(),
    };
}
