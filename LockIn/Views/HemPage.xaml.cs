using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

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

    private static async void OnBannerPointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.97, 65, Easing.CubicOut);
    }

    private static async void OnBannerPointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 200, Easing.SpringOut);
    }
}
