using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class TrainPage : ContentPage
{
    private readonly TrainViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;
    private int _muscleBarIndex;

    public TrainPage(TrainViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _muscleBarIndex = 0;
        if (_state.IsActive)
        {
            Dispatcher.Dispatch(async () =>
                await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage)));
            return;
        }

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

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    internal async void OnTemplateTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnProgramTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private static async void OnFabPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.93, 70, Easing.CubicOut);
    }

    private static async void OnFabReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 200, Easing.SpringOut);
    }

    private async void OnMuscleBarLoaded(object sender, EventArgs e)
    {
        if (sender is not BoxView bar) return;
        var target = bar.ScaleX;
        if (target <= 0) return;
        bar.ScaleX = 0;
        var delay = ++_muscleBarIndex * 55 + 150;
        await Task.Delay(delay);
        await bar.ScaleXTo(target, 550, Easing.CubicOut);
    }
}
