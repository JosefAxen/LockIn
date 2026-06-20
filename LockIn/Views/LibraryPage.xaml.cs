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
        _vm.PropertyChanged += OnVmPropertyChanged;

        StickyHeader.Opacity = 0;
        PageTitleRow.Opacity = 1;

        if (!_hasLoaded)
        {
            Content.Opacity = 0;
            Content.TranslationY = 16;
            await _vm.LoadAsync();
            _hasLoaded = true;
            await Task.WhenAll(
                Content.FadeTo(1, 400, Easing.CubicOut),
                Content.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
        else
        {
            Content.Opacity = 0;
            Content.TranslationY = 12;
            await Task.WhenAll(
                _vm.LoadAsync(),
                Content.FadeTo(1, 320, Easing.CubicOut),
                Content.TranslateTo(0, 0, 320, Easing.CubicOut)
            );
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
        _vm.PropertyChanged -= OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.SelectedTab))
        {
            StickyHeader.Opacity = 0;
            PageTitleRow.Opacity = 1;
        }
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await _vm.LoadAsync();
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(() => WorkoutBanner.IsVisible = _state.IsActive);

    private void OnExercisesScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var opacity = Math.Clamp(e.VerticalOffset / 40.0, 0, 1);
        StickyHeader.Opacity = opacity;
        PageTitleRow.Opacity = 1 - opacity;
    }

    private void OnTemplatesScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var opacity = Math.Clamp(e.VerticalOffset / 40.0, 0, 1);
        StickyHeader.Opacity = opacity;
        PageTitleRow.Opacity = 1 - opacity;
    }

    private void OnProgramsScrolled(object sender, ScrolledEventArgs e)
    {
        var opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);
        StickyHeader.Opacity = opacity;
        PageTitleRow.Opacity = 1 - opacity;
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
