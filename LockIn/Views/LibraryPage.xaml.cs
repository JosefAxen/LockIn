using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;
    private CancellationTokenSource? _loaderCts;

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
        if (_state.IsActive) StartBannerPulse();
        _state.StateChanged += OnWorkoutStateChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;

        StickyHeader.Opacity = 0;

        // Ladda direkt utan att dölja Content — atmosfärisk bakgrund + stålband-loader
        // är synliga under laddning så användaren får omedelbar feedback.
        if (!_hasLoaded)
        {
            StartLoaderAnimation();
            await _vm.LoadAsync();
            StopLoaderAnimation();
            _hasLoaded = true;
        }
        else
        {
            await _vm.LoadAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        StopLoaderAnimation();
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

    private void StartLoaderAnimation()
    {
        _loaderCts?.Cancel();
        _loaderCts = new CancellationTokenSource();
        var token = _loaderCts.Token;

        // Highlight: glid -40 → 140 i loop på 1.4s
        var highlightAnim = new Animation(
            v => LoaderHighlight.TranslationX = v,
            -40, 140, Easing.Linear);
        highlightAnim.Commit(this, "LoaderHighlight",
            length: 1400, repeat: () => !token.IsCancellationRequested);

        // Label: opacity-puls 0.4 → 1.0 → 0.4 på 1.0s
        var pulseAnim = new Animation();
        pulseAnim.Add(0.0, 0.5, new Animation(v => LoaderLabel.Opacity = v, 0.4, 1.0, Easing.SinInOut));
        pulseAnim.Add(0.5, 1.0, new Animation(v => LoaderLabel.Opacity = v, 1.0, 0.4, Easing.SinInOut));
        pulseAnim.Commit(this, "LoaderLabel",
            length: 1000, repeat: () => !token.IsCancellationRequested);
    }

    private void StopLoaderAnimation()
    {
        _loaderCts?.Cancel();
        this.AbortAnimation("LoaderHighlight");
        this.AbortAnimation("LoaderLabel");
    }

    private double _tabColumnWidth;

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.SelectedTab))
        {
            StickyHeader.Opacity = 0;
            UpdateAllTabIndicators(animated: true);
        }
    }

    private void OnTabContainerSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement ve || ve.Width <= 0) return;
        var newWidth = ve.Width / 3.0;
        if (Math.Abs(newWidth - _tabColumnWidth) < 0.5) return;
        _tabColumnWidth = newWidth;
        TabIndicator.WidthRequest  = _tabColumnWidth;
        TabIndicator1.WidthRequest = _tabColumnWidth;
        TabIndicator2.WidthRequest = _tabColumnWidth;
        UpdateAllTabIndicators(animated: false);
    }

    private void UpdateAllTabIndicators(bool animated)
    {
        if (_tabColumnWidth <= 0) return;
        var targetX = _vm.SelectedTab * _tabColumnWidth;
        if (animated)
        {
            TabIndicator.TranslateTo(targetX, 0, 280, Easing.SpringOut);
            TabIndicator1.TranslateTo(targetX, 0, 280, Easing.SpringOut);
            TabIndicator2.TranslateTo(targetX, 0, 280, Easing.SpringOut);
        }
        else
        {
            TabIndicator.TranslationX  = targetX;
            TabIndicator1.TranslationX = targetX;
            TabIndicator2.TranslationX = targetX;
        }
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            WorkoutBanner.IsVisible = _state.IsActive;
            if (_state.IsActive) StartBannerPulse();
            else this.AbortAnimation("BannerPulse");
        });

    private void OnExercisesScrolled(object sender, ItemsViewScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.VerticalOffset - 80.0) / 40.0, 0, 1);

    private void OnTemplatesScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private void OnProgramsScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private async void OnPillTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
