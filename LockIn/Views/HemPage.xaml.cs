using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HemPage : ContentPage
{
    private readonly HemViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public HemPage(HemViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        _vm = vm;
        _state = state;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        _state.StateChanged += OnWorkoutStateChanged;
        await _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(() => WorkoutBanner.IsVisible = _state.IsActive);

    private void OnSeAllaHistorik(object sender, TappedEventArgs e)
        => Shell.Current.GoToAsync("//HistoryPage");

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
