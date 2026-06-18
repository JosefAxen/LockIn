using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    private bool _hasAppeared;

    public ActiveWorkoutPage(ActiveWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.PRScored += (_, _) => MainThread.BeginInvokeOnMainThread(() => Confetti.Start());
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Avbryt pass", "Lämna utan att avsluta?", "Ja", "Nej");
        if (confirmed)
        {
            var vm = BindingContext as ActiveWorkoutViewModel;
            vm?.ForceDeactivate();
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as ActiveWorkoutViewModel)?.RefreshTimerState();
        if (!_hasAppeared)
        {
            _hasAppeared = true;
            await AnimationHelper.PageEntryAsync(this);
        }
    }

    // RIR tap — logic only; animation handled by PointerGestureRecognizer
    // sender is the Border the gesture is attached to, not the TapGestureRecognizer itself
    private void OnRirTapped(object sender, TappedEventArgs e)
    {
        if (sender is not VisualElement ve) return;
        if (ve.BindingContext is not LoggedSetRow row) return;
        row.Rir = row.Rir >= 5 || row.Rir < 0 ? 0 : row.Rir + 1;
    }

    private static async void OnSetRowLoaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement ve) return;
        ve.Opacity = 0;
        ve.TranslationY = -6;
        await Task.WhenAll(
            ve.FadeTo(1, 180, Easing.CubicOut),
            ve.TranslateTo(0, 0, 180, Easing.CubicOut)
        );
    }

    // Generic border press animation (scale 0.93)
    private static async void OnElemPointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.93, 65, Easing.CubicOut);
    }

    private static async void OnElemPointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 230, Easing.SpringOut);
    }

    // Done-button press animation (deeper + bounce)
    private static async void OnDonePointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.88, 70, Easing.CubicOut);
    }

    private static async void OnDonePointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
        {
            await ve.ScaleTo(1.12, 180, Easing.SpringOut);
            await ve.ScaleTo(1.0, 100, Easing.CubicIn);
        }
    }
}
