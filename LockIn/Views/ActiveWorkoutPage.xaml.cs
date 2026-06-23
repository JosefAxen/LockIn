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
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

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
        bool freshlyAdded = ve.BindingContext is LoggedSetRow row && row.IsFreshlyAdded;

        if (freshlyAdded)
        {
            // Veckla ut: layout-höjden växer parallellt med fade/scale så +SET-knappen
            // glider ner mjukt istället för att snappa 46dp direkt.
            const double targetHeight = 46;
            ((LoggedSetRow)ve.BindingContext!).IsFreshlyAdded = false;
            ve.HeightRequest = 0;
            ve.Opacity = 0;
            ve.TranslationY = -6;
            ve.Scale = 0.96;
            await Task.WhenAll(
                AnimateHeightAsync(ve, 0, targetHeight, 220),
                ve.FadeTo(1, 260, Easing.CubicOut),
                ve.TranslateTo(0, 0, 260, Easing.CubicOut),
                ve.ScaleTo(1, 260, Easing.SpringOut)
            );
        }
        else
        {
            // Initial laddning: enklare fade-in utan layout-animation
            ve.Opacity = 0;
            ve.TranslationY = -4;
            await Task.WhenAll(
                ve.FadeTo(1, 180, Easing.CubicOut),
                ve.TranslateTo(0, 0, 180, Easing.CubicOut)
            );
        }
    }

    private async void OnRemoveSetClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.BindingContext is not WorkoutExerciseSection section) return;
        if (section.Sets.Count <= 1) return;

        // Hitta StackLayout som innehåller set-raderna (sibling till +/- knappraden).
        var setsLayout = FindSetsLayout(btn);
        var lastRow = setsLayout?.Children.LastOrDefault() as VisualElement;

        if (lastRow != null)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var startHeight = lastRow.Height > 0 ? lastRow.Height : 46;
            await Task.WhenAll(
                lastRow.FadeTo(0, 220, Easing.CubicIn),
                lastRow.TranslateTo(0, -6, 220, Easing.CubicIn),
                AnimateHeightAsync(lastRow, startHeight, 0, 220)
            );
        }

        // Ta bort från VM efter animationen — resterande rader är redan på plats
        if (BindingContext is ActiveWorkoutViewModel vm)
            vm.RemoveSetCommand.Execute(section);
    }

    private static Task AnimateHeightAsync(VisualElement ve, double from, double to, uint duration)
    {
        var tcs = new TaskCompletionSource<bool>();
        var anim = new Animation(v => ve.HeightRequest = v, from, to, Easing.CubicIn);
        anim.Commit(ve, "RowCollapse", length: duration, rate: 16,
            finished: (_, __) => tcs.TrySetResult(true));
        return tcs.Task;
    }

    private static StackLayout? FindSetsLayout(Element start)
    {
        // Walk upp tills vi når section-templatet, leta efter syskon-StackLayout med BindableLayout.
        var parent = start.Parent;
        while (parent != null)
        {
            if (parent is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is StackLayout sl && BindableLayout.GetItemsSource(sl) != null)
                        return sl;
                }
            }
            parent = parent.Parent;
        }
        return null;
    }

    private async void OnAddExerciseTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

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

    private static async void OnDonePointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.93, 65, Easing.CubicOut);
    }

    private static async void OnDonePointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 230, Easing.SpringOut);
    }
}
