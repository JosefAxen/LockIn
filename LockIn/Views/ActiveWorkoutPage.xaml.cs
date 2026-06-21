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
        vm.ScrollToSectionRequested += OnScrollToSectionRequested;
    }

    private void OnScrollToSectionRequested(object? sender, int sessionExerciseId)
    {
        var vm = BindingContext as ActiveWorkoutViewModel;
        if (vm is null) return;
        var index = -1;
        for (int i = 0; i < vm.Exercises.Count; i++)
        {
            if (vm.Exercises[i].SessionExerciseId == sessionExerciseId) { index = i; break; }
        }
        if (index < 0 || index >= ExercisesLayout.Children.Count) return;
        var card = ExercisesLayout.Children[index] as VisualElement;
        if (card is null) return;
        MainThread.BeginInvokeOnMainThread(async () =>
            await MainScrollView.ScrollToAsync(card, ScrollToPosition.Start, animated: true));
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
    {
        var opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);
        StickyHeader.Opacity = opacity;
        TopBar.Opacity = 1 - opacity;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Lämna vyn UTAN att avsluta passet. Passet förblir aktivt
        // (state.IsActive == true), så användaren kan återgå via
        // "PASS PÅGÅR"-bannern eller genom att navigera till Träna-fliken.
        // För att avsluta passet på riktigt — använd "AVSLUTA PASS"-knappen.
        await Shell.Current.GoToAsync("..");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StickyHeader.Opacity = 0;
        TopBar.Opacity = 1;
        (BindingContext as ActiveWorkoutViewModel)?.RefreshTimerState();
        if (!_hasAppeared)
        {
            _hasAppeared = true;
            await AnimationHelper.PageEntryAsync(this);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stoppa ConfettiView innan navigation — annars fortsätter dess
        // DispatcherTimer (16ms tick) köra på en disposed page och blockerar
        // UI-tråden vilket renderas som svart skärm vid navigation efter PR.
        Confetti.Stop();
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
