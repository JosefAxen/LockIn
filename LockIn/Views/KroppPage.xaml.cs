using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class KroppPage : ContentPage
{
    // Bottenpadding för scrollinnehållet: tab-baren, plus "PASS PÅGÅR"-bannern
    // (Margin 96 + ca 59 pt hög) när den visas.
    private const double BottomInset = 120;
    private const double BottomInsetWithBanner = 176;

    private readonly KroppViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;
    private CancellationTokenSource? _heatmapCts;

    public KroppPage(KroppViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    private async void OnWeeklyVolumeTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(AppRoutes.WeeklyVolume);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateBannerState();
        _state.StateChanged += OnWorkoutStateChanged;
        _vm.HeatmapReady += BuildHeatmapGrid;
        _vm.PropertyChanged += OnKroppVmPropertyChanged;

        UpdateStickyHeader(0);

        bool animate = !AnimationHelper.ReduceMotion;
        try
        {
            if (!_hasLoaded)
            {
                if (animate)
                {
                    Content.Opacity = 0;
                    Content.TranslationY = 16;
                }
                await _vm.LoadAsync();
                _hasLoaded = true;
                if (animate)
                    await Task.WhenAll(
                        Content.FadeTo(1, 400, Easing.CubicOut),
                        Content.TranslateTo(0, 0, 400, Easing.CubicOut)
                    );
            }
            else if (animate)
            {
                Content.Opacity = 0;
                Content.TranslationY = 12;
                await Task.WhenAll(
                    _vm.LoadAsync(),
                    Content.FadeTo(1, 320, Easing.CubicOut),
                    Content.TranslateTo(0, 0, 320, Easing.CubicOut)
                );
            }
            else
            {
                await _vm.LoadAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KroppPage] OnAppearing failed: {ex}");
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        finally
        {
            Content.Opacity = 1;
            Content.TranslationY = 0;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
        _vm.HeatmapReady -= BuildHeatmapGrid;
        _vm.PropertyChanged -= OnKroppVmPropertyChanged;
        BannerPulseRing.StopPulse();
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(UpdateBannerState);

    private void UpdateBannerState()
    {
        var active = _state.IsActive;
        WorkoutBanner.IsVisible = active;
        if (active) StartBannerPulse();
        else BannerPulseRing.StopPulse();

        // Sista kortet ska kunna scrollas fram ovanför bannern och tab-baren.
        var p = ScrollContent.Padding;
        ScrollContent.Padding = new Thickness(p.Left, p.Top, p.Right, active ? BottomInsetWithBanner : BottomInset);
    }

    private void StartBannerPulse() => BannerPulseRing.StartPulse();

    private async void OnLogMatsTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => UpdateStickyHeader(e.ScrollY);

    private void UpdateStickyHeader(double scrollY)
    {
        StickyHeader.Opacity = Math.Clamp((scrollY - 80.0) / 40.0, 0, 1);
        // Osynlig header får inte svälja tryck i toppen av sidan.
        StickySettingsButton.InputTransparent = StickyHeader.Opacity < 0.5;
    }

    private void BuildHeatmapGrid() =>
        MainThread.BeginInvokeOnMainThread(BuildHeatmap);

    private void BuildHeatmap()
    {
        _heatmapCts?.Cancel();
        _heatmapCts = new CancellationTokenSource();
        HeatmapBuilder.Build(HeatmapGrid, _vm.HeatmapTiles, _heatmapCts.Token);
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(AppRoutes.ActiveWorkout);

    private double _tabColumnWidth;

    private void OnTabContainerSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement ve || ve.Width <= 0) return;
        _tabColumnWidth = ve.Width / 3.0;
        TabIndicator.WidthRequest = _tabColumnWidth;
        UpdateTabIndicator(animated: false);
    }

    private void UpdateTabIndicator(bool animated)
    {
        if (_tabColumnWidth <= 0) return;
        var targetX = _vm.SelectedTab * _tabColumnWidth;
        if (animated && !AnimationHelper.ReduceMotion)
            TabIndicator.TranslateTo(targetX, 0, 280, Easing.SpringOut);
        else
            TabIndicator.TranslationX = targetX;
    }

    private void OnKroppVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KroppViewModel.SelectedTab))
            UpdateTabIndicator(animated: true);
    }
}
