using LockIn.Navigation;
using LockIn.Resources.Strings;
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
        try
        {
            // PageEntryAsync hoppar över inglidningen vid "Minska rörelse"
            await Task.WhenAll(
                _vm.LoadAsync(),
                AnimationHelper.PageEntryAsync(this)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Laddning misslyckades: {ex}");
            Content.Opacity = 1;
            Content.TranslationY = 0;
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private async void OnBackClicked(object sender, EventArgs e)
        => await NavigateAsync(AppRoutes.Back);

    private async void OnMuscleVolumeTapped(object sender, TappedEventArgs e)
        => await NavigateAsync(AppRoutes.MuscleVolume);

    private static async void OnBackPointerPressed(object? sender, PointerEventArgs e)
    {
        if (AnimationHelper.ReduceMotion) return;
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.88, 65, Easing.CubicOut);
    }

    private static async void OnBackPointerReleased(object? sender, PointerEventArgs e)
    {
        if (AnimationHelper.ReduceMotion) return;
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 180, Easing.SpringOut);
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await NavigateAsync(AppRoutes.ActiveWorkout);

    private static async Task NavigateAsync(string route)
    {
        try { await Shell.Current.GoToAsync(route); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Settings] Navigering till {route} misslyckades: {ex}"); }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BannerPulseRing.StopPulse();
    }

    private void StartBannerPulse() => BannerPulseRing.StartPulse();
}
