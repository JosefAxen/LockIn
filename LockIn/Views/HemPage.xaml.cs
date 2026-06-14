using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class HemPage : ContentPage
{
    private readonly HemViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

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

        if (!_hasLoaded)
        {
            Content.Opacity = 0;
            Content.TranslationY = 10;
            await _vm.LoadAsync();
            _hasLoaded = true;
            await Task.WhenAll(
                Content.FadeTo(1, 280, Easing.CubicOut),
                Content.TranslateTo(0, 0, 280, Easing.CubicOut)
            );
        }
        else
        {
            Content.Opacity = 0;
            Content.TranslationY = 8;
            await Task.WhenAll(
                _vm.LoadAsync(),
                Content.FadeTo(1, 220, Easing.CubicOut),
                Content.TranslateTo(0, 0, 220, Easing.CubicOut)
            );
        }

        AnimateGauge();
    }

    private void AnimateGauge()
    {
        float target = (float)(_vm.TrainingScore / 100.0);
        var animation = new Animation(v => Gauge.Progress = (float)v, 0, target, Easing.CubicOut);
        animation.Commit(this, "GaugeAnim", length: 1200);
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

    private async void OnAvatarTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//KroppPage");

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
