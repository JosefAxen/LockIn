using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public SettingsPage(SettingsViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        await _vm.LoadAsync();
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//TrainPage");
}
