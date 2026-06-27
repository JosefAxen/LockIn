using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class CardioViewModel(DatabaseService db) : ObservableObject
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedActivity is null) return;

        int.TryParse(DurationText, out var duration);
        double.TryParse(DistanceText, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var distance);
        int.TryParse(HeartRateText, out var hr);
        int.TryParse(CaloriesText, out var cal);

        var session = new CardioSession
        {
            ActivityType       = SelectedActivity.Type,
            CustomActivityName = IsCustom ? CustomName : "",
            StartedAt          = DateTime.Now,
            DurationMinutes    = duration,
            DistanceKm         = distance,
            AvgHeartRate       = hr,
            CaloriesBurned     = cal,
            Notes              = Notes,
        };

        await db.SaveCardioSessionAsync(session);
        await Shell.Current.GoToAsync("..");
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
