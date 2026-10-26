using CommunityToolkit.Maui.Extensions;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class HemPage : ContentPage
{
    // Bottenpadding för scrollinnehållet: tab-baren, plus "PASS PÅGÅR"-bannern
    // (Margin 96 + ca 59 pt hög) när den visas.
    private const double BottomInset = 120;
    private const double BottomInsetWithBanner = 176;

    private readonly HemViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;
    private bool _healthLoading;
    private bool _healthErrorShown;
    private CancellationTokenSource? _heatmapCts;

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
        UpdateBannerState();
        _state.StateChanged += OnWorkoutStateChanged;

        StickyHeader.Opacity = 0;

        bool animate = !AnimationHelper.ReduceMotion;
        try
        {
            // Steg 1: lokal data — sidan visas så fort den finns, inte efter Hälsa-dialogen.
            if (animate)
            {
                Content.Opacity = 0;
                Content.TranslationY = _hasLoaded ? 12 : 16;
            }
            if (!_hasLoaded || !animate)
            {
                await _vm.LoadLocalAsync();
                if (animate)
                    await Task.WhenAll(
                        Content.FadeTo(1, 400, Easing.CubicOut),
                        Content.TranslateTo(0, 0, 400, Easing.CubicOut)
                    );
            }
            else
            {
                await Task.WhenAll(
                    _vm.LoadLocalAsync(),
                    Content.FadeTo(1, 320, Easing.CubicOut),
                    Content.TranslateTo(0, 0, 320, Easing.CubicOut)
                );
            }
            _hasLoaded = true;

            ScrollStreakToToday();
            AnimateGauge();
            _heatmapCts?.Cancel();
            _heatmapCts = new CancellationTokenSource();
            HeatmapBuilder.Build(HemHeatmapGrid, _vm.HeatmapItems, _heatmapCts.Token);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HemPage] Local load failed: {ex}");
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
        finally
        {
            Content.Opacity = 1;
            Content.TranslationY = 0;
        }

        // Steg 2: Hälsa (behörighetsdialog första gången + HealthKit-frågor) fyller på efteråt.
        if (_healthLoading) return;
        _healthLoading = true;
        try
        {
            await _vm.LoadHealthAsync();
        }
        catch (Exception ex)
        {
            // Sidan visar redan "–" och inget råd. Meddela en gång, inte vid varje flikbyte.
            System.Diagnostics.Debug.WriteLine($"[HemPage] Health load failed: {ex}");
            if (!_healthErrorShown)
            {
                _healthErrorShown = true;
                await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
            }
        }
        finally
        {
            _healthLoading = false;
        }
    }

    private void ScrollStreakToToday()
    {
        if (_vm.TodayIndex < _vm.Days.Count)
            StreakCollectionView.ScrollTo(_vm.Days[_vm.TodayIndex], position: ScrollToPosition.Center, animate: false);
    }

    private void AnimateGauge()
    {
        float target = (float)(_vm.TrainingScore / 100.0);
        if (AnimationHelper.ReduceMotion)
        {
            Gauge.Progress = target;
            return;
        }
        var animation = new Animation(v => Gauge.Progress = (float)v, 0, target, Easing.CubicOut);
        animation.Commit(this, "GaugeAnim", length: 1200);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _state.StateChanged -= OnWorkoutStateChanged;
        _heatmapCts?.Cancel();
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

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private void OnSeAllaHistorik(object sender, TappedEventArgs e)
        => Shell.Current.GoToAsync(AppRoutes.HistoryTab);

    private async void OnAvatarTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(AppRoutes.KroppTab);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(AppRoutes.ActiveWorkout);

    private async void OnChipTapped(object sender, TappedEventArgs e)
    {
        if (sender is not VisualElement view) return;
        if (view.BindingContext is not LockIn.Models.CoachChip chip) return;

        LockIn.Services.CoachPromptEngine.MarkShown(chip.PromptId);
        await this.ShowPopupAsync(new CoachChipSheet(chip), new CommunityToolkit.Maui.PopupOptions());
    }

    private static async void OnBannerPointerPressed(object? sender, PointerEventArgs e)
    {
        AnimationHelper.Haptic(HapticFeedbackType.Click);
        if (AnimationHelper.ReduceMotion) return;
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.97, 65, Easing.CubicOut);
    }

    private static async void OnBannerPointerReleased(object? sender, PointerEventArgs e)
    {
        if (AnimationHelper.ReduceMotion) return;
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 200, Easing.SpringOut);
    }
}
