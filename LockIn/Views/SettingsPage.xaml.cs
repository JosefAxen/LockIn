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
        if (_state.IsActive) StartBannerPulse();
        StickyHeader.Opacity = 0;
        Content.Opacity = 0;
        Content.TranslationY = 8;
        await Task.WhenAll(
            _vm.LoadAsync(),
            Content.FadeTo(1, 220, Easing.CubicOut),
            Content.TranslateTo(0, 0, 220, Easing.CubicOut)
        );
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private static async void OnBackPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.88, 65, Easing.CubicOut);
    }

    private static async void OnBackPointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 180, Easing.SpringOut);
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        this.AbortAnimation("BannerPulse");
    }

    private void StartBannerPulse()
    {
        this.AbortAnimation("BannerPulse");
        BannerPulseRing.Scale = 1.0;
        BannerPulseRing.Opacity = 0;
        var pulse = new Animation();
        pulse.Add(0, 1, new Animation(v => BannerPulseRing.Scale = v, 1.0, 2.4, Easing.CubicOut));
        pulse.Add(0, 1, new Animation(v => BannerPulseRing.Opacity = v, 0.65, 0.0, Easing.CubicOut));
        pulse.Commit(this, "BannerPulse", length: 1400, repeat: () => WorkoutBanner.IsVisible);
    }
}
