using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class KroppPage : ContentPage
{
    private readonly KroppViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

    public KroppPage(KroppViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        _vm.HeatmapReady += BuildHeatmapGrid;
        _vm.PropertyChanged += OnKroppVmPropertyChanged;

        StickyHeader.Opacity = 0;

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
        _vm.HeatmapReady -= BuildHeatmapGrid;
        _vm.PropertyChanged -= OnKroppVmPropertyChanged;
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private void BuildHeatmapGrid() =>
        MainThread.BeginInvokeOnMainThread(BuildHeatmap);

    private void BuildHeatmap() =>
        HeatmapBuilder.Build(HeatmapGrid, _vm.HeatmapTiles);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));

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
        if (animated)
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
