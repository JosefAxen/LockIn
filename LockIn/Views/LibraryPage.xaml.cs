using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

    public LibraryPage(LibraryViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await _vm.LoadAsync();
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(() => WorkoutBanner.IsVisible = _state.IsActive);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
