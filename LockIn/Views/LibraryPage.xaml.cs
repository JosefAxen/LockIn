using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class LibraryPage : ContentPage
{
    // Bottenpadding för listorna: tab-baren, plus "PASS PÅGÅR"-bannern
    // (Margin 96 + ca 59 pt hög) när den visas.
    private const double BottomInset = 120;
    private const double BottomInsetWithBanner = 176;

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
        UpdateBannerState();
        _state.StateChanged += OnWorkoutStateChanged;
        _vm.PropertyChanged += OnVmPropertyChanged;

        UpdateStickyHeader(0);

        // Ladda direkt utan att dölja Content — atmosfärisk bakgrund + stålband-loader
        // är synliga under laddning så användaren får omedelbar feedback.
        try
        {
            if (!_hasLoaded)
            {
                StartLoaderAnimation();
                await _vm.LoadAsync();
                _hasLoaded = true;
            }
            else
            {
                await _vm.LoadAsync();
            }

            UpdateAllTabIndicators(animated: false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryPage] OnAppearing failed: {ex}");
            _vm.IsLoading = false;
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        finally
        {
            StopLoaderAnimation();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        StopLoaderAnimation();
        BannerPulseRing.StopPulse();
    }

    private void StartBannerPulse() => BannerPulseRing.StartPulse();

    private void StartLoaderAnimation()
    {
        _loaderCts?.Cancel();

        // "Minska rörelse": statiskt band och text i stället för svep och puls.
        if (AnimationHelper.ReduceMotion)
        {
            LoaderHighlight.TranslationX = 50;
            LoaderLabel.Opacity = 1;
            return;
        }

        _loaderCts = new CancellationTokenSource();
        var token = _loaderCts.Token;

        // Highlight: glid -40 → 140 i loop på 1.4s
        var highlightAnim = new Animation(
            v => LoaderHighlight.TranslationX = v,
            -40, 140, Easing.Linear);
        highlightAnim.Commit(this, "LoaderHighlight",
            length: 1400, repeat: () => !token.IsCancellationRequested);

        // Label: opacity-puls 0.8 → 1.0 → 0.8 på 1.0s (lägre opacitet ger under 4,5:1 kontrast)
        var pulseAnim = new Animation();
        pulseAnim.Add(0.0, 0.5, new Animation(v => LoaderLabel.Opacity = v, 0.8, 1.0, Easing.SinInOut));
        pulseAnim.Add(0.5, 1.0, new Animation(v => LoaderLabel.Opacity = v, 1.0, 0.8, Easing.SinInOut));
        pulseAnim.Commit(this, "LoaderLabel",
            length: 1000, repeat: () => !token.IsCancellationRequested);
    }

    private void StopLoaderAnimation()
    {
        _loaderCts?.Cancel();
        this.AbortAnimation("LoaderHighlight");
        this.AbortAnimation("LoaderLabel");
        LoaderLabel.Opacity = 1;
    }

    private double _tabColumnWidth;

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryViewModel.SelectedTab))
        {
            UpdateStickyHeader(0);
            UpdateAllTabIndicators(animated: true);
        }
    }

    private void OnTabContainerSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement ve || ve.Width <= 0) return;
        var newWidth = ve.Width / 4.0;
        if (Math.Abs(newWidth - _tabColumnWidth) < 0.5) return;
        _tabColumnWidth = newWidth;
        TabIndicatorExercises.WidthRequest = _tabColumnWidth;
        TabIndicatorTemplates.WidthRequest = _tabColumnWidth;
        TabIndicatorPrograms.WidthRequest  = _tabColumnWidth;
        TabIndicatorCycles.WidthRequest    = _tabColumnWidth;
        UpdateAllTabIndicators(animated: false);
    }

    private void UpdateAllTabIndicators(bool animated)
    {
        if (_tabColumnWidth <= 0) return;
        var targetX = _vm.SelectedTab * _tabColumnWidth;
        if (animated && !AnimationHelper.ReduceMotion)
        {
            TabIndicatorExercises.TranslateTo(targetX, 0, 280, Easing.SpringOut);
            TabIndicatorTemplates.TranslateTo(targetX, 0, 280, Easing.SpringOut);
            TabIndicatorPrograms.TranslateTo(targetX, 0, 280, Easing.SpringOut);
            TabIndicatorCycles.TranslateTo(targetX, 0, 280, Easing.SpringOut);
        }
        else
        {
            TabIndicatorExercises.TranslationX = targetX;
            TabIndicatorTemplates.TranslationX = targetX;
            TabIndicatorPrograms.TranslationX  = targetX;
            TabIndicatorCycles.TranslationX    = targetX;
        }
    }

    private void OnWorkoutStateChanged()
        => MainThread.BeginInvokeOnMainThread(UpdateBannerState);

    private void UpdateBannerState()
    {
        var active = _state.IsActive;
        WorkoutBanner.IsVisible = active;
        if (active) StartBannerPulse();
        else BannerPulseRing.StopPulse();

        // Sista kortet i varje flik ska kunna scrollas fram ovanför bannern och tab-baren.
        var inset = active ? BottomInsetWithBanner : BottomInset;
        ExercisesFooter.HeightRequest = inset;
        TemplatesContent.Padding = new Thickness(0, 0, 0, inset);
        ProgramsContent.Padding  = new Thickness(0, 0, 0, inset);
        CyclesContent.Padding    = new Thickness(0, 0, 0, inset);
    }

    private void UpdateStickyHeader(double scrollY)
    {
        StickyHeader.Opacity = Math.Clamp((scrollY - 80.0) / 40.0, 0, 1);
        // Osynlig header får inte svälja tryck i toppen av sidan.
        StickyActionButton.InputTransparent = StickyHeader.Opacity < 0.5;
    }

    private void OnExercisesScrolled(object sender, ItemsViewScrolledEventArgs e)
        => UpdateStickyHeader(e.VerticalOffset);

    private void OnTemplatesScrolled(object sender, ScrolledEventArgs e)
        => UpdateStickyHeader(e.ScrollY);

    private void OnProgramsScrolled(object sender, ScrolledEventArgs e)
        => UpdateStickyHeader(e.ScrollY);

    private void OnCyclesScrolled(object sender, ScrolledEventArgs e)
        => UpdateStickyHeader(e.ScrollY);

    private async void OnPillTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(AppRoutes.ActiveWorkout);
}
