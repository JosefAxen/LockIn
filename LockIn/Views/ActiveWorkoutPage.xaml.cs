using LockIn.Navigation;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    // Set-radens höjd: 44 pt tryckytor + 6 pt padding upp/ned (matchar XAML)
    private const double SetRowHeight = 56;
    // Sticky-headern täcker toppen när sidan har scrollats (56 safe area + 44 knapp + luft)
    private const double StickyHeaderInset = 110;
    // Vilotimerns rad längst ned i ett kort (padding 10 + label ~22 + 10)
    private const double TimerBarHeight = 50;

    private readonly ActiveWorkoutViewModel _vm;
    private bool _hasAppeared;
    private bool _isSubscribed;

    public ActiveWorkoutPage(ActiveWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // VM:en är singleton och lever längre än sidan. Lyssnare registreras när sidan visas
    // och avregistreras när den döljs, så att gamla sidinstanser inte läcker eller får
    // dubbla händelser (dubbel konfetti, dubbel scroll).
    private void Subscribe()
    {
        if (_isSubscribed) return;
        _vm.PRScored += OnPRScored;
        _vm.ScrollToSectionRequested += OnScrollToSectionRequested;
        _vm.PropertyChanged += OnVmPropertyChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;
        _vm.PRScored -= OnPRScored;
        _vm.ScrollToSectionRequested -= OnScrollToSectionRequested;
        _vm.PropertyChanged -= OnVmPropertyChanged;
        _isSubscribed = false;
    }

    private static void LogError(Exception ex, string context) =>
        System.Diagnostics.Debug.WriteLine($"[ActiveWorkoutPage] {context}: {ex}");

    private void OnPRScored(object? sender, EventArgs e)
    {
        // Riktigt PR mot tidigare pass — konfetti, men inte med "Minska rörelse"
        if (AnimationHelper.ReduceMotion) return;
        MainThread.BeginInvokeOnMainThread(() => Confetti.Start());
    }

    private async void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(ActiveWorkoutViewModel.HasPR) && _vm.HasPR)
                await SlideInBannerAsync(PrBanner);
            else if (e.PropertyName == nameof(ActiveWorkoutViewModel.HasAutoProgress) && _vm.HasAutoProgress)
                await SlideInBannerAsync(AutoProgressBanner);
        }
        catch (Exception ex) { LogError(ex, "OnVmPropertyChanged"); }
    }

    private static async Task SlideInBannerAsync(VisualElement banner)
    {
        if (AnimationHelper.ReduceMotion)
        {
            banner.Opacity = 1;
            banner.TranslationY = 0;
            return;
        }
        banner.Opacity = 0;
        banner.TranslationY = -12;
        await Task.WhenAll(
            banner.FadeTo(1, 280, Easing.CubicOut),
            banner.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }

    private void OnScrollToSectionRequested(object? sender, int sessionExerciseId)
    {
        var index = -1;
        for (int i = 0; i < _vm.Exercises.Count; i++)
        {
            if (_vm.Exercises[i].SessionExerciseId == sessionExerciseId) { index = i; break; }
        }
        if (index < 0) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Låt layouten hinna med (ny sektion, vilotimer som just blev synlig)
                await Task.Delay(60);
                if (index >= ExercisesLayout.Children.Count) return;
                if (ExercisesLayout.Children[index] is not VisualElement card) return;

                // Kortets överkant precis under sticky-headern
                var y = ExercisesLayout.Y + card.Y - StickyHeaderInset;

                // Vilotimern ligger sist i föregående kort — scrolla inte förbi den,
                // utan visa timern överst och nästa övning under.
                if (index > 0
                    && _vm.Exercises.ElementAtOrDefault(index - 1) is { IsTimerActive: true }
                    && ExercisesLayout.Children[index - 1] is VisualElement prevCard)
                {
                    var prevBottom = ExercisesLayout.Y + prevCard.Y + prevCard.Height;
                    y = prevBottom - TimerBarHeight - StickyHeaderInset;
                }

                await MainScrollView.ScrollToAsync(0, Math.Max(0, y), animated: !AnimationHelper.ReduceMotion);
            }
            catch (Exception ex) { LogError(ex, "ScrollToSection"); }
        });
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    private async void OnBackClicked(object sender, EventArgs e)
    {
        // Lämna vyn UTAN att avsluta passet. Passet förblir aktivt
        // (state.IsActive == true), så användaren kan återgå via
        // "PASS PÅGÅR"-bannern eller genom att navigera till Träna-fliken.
        // För att avsluta passet på riktigt — använd "AVSLUTA PASS"-knappen.
        try
        {
            await Shell.Current.GoToAsync(AppRoutes.Back);
        }
        catch (Exception ex) { LogError(ex, "OnBackClicked"); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            Subscribe();
            StickyHeader.Opacity = 0;
            _vm.RefreshTimerState();

            // Scroll som begärdes medan sidan var dold (t.ex. övning tillagd via väljaren)
            if (_vm.TakePendingScroll() is int pendingId)
                OnScrollToSectionRequested(this, pendingId);

            if (!_hasAppeared)
            {
                _hasAppeared = true;
                await AnimationHelper.PageEntryAsync(this);
            }
        }
        catch (Exception ex) { LogError(ex, "OnAppearing"); }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Unsubscribe();
        // Stoppa ConfettiView innan navigation — annars fortsätter dess
        // DispatcherTimer (16ms tick) köra på en disposed page och blockerar
        // UI-tråden vilket renderas som svart skärm vid navigation efter PR.
        Confetti.Stop();
    }

    // Lämnat vikt-/repsfält på ett redan loggat set → spara ändringen (uppdaterar samma rad i DB)
    private async void OnSetEntryUnfocused(object? sender, FocusEventArgs e)
    {
        try
        {
            if ((sender as BindableObject)?.BindingContext is LoggedSetRow row)
                await _vm.CommitSetEditsAsync(row);
        }
        catch (Exception ex) { LogError(ex, "OnSetEntryUnfocused"); }
    }

    private static async void OnSetRowLoaded(object sender, EventArgs e)
    {
        if (sender is not VisualElement ve) return;
        try
        {
            var row = ve.BindingContext as LoggedSetRow;
            bool freshlyAdded = row?.IsFreshlyAdded == true;
            if (row is not null) row.IsFreshlyAdded = false;

            // Ingen in-animation med "Minska rörelse"
            if (AnimationHelper.ReduceMotion) return;

            if (freshlyAdded)
            {
                // Veckla ut: layout-höjden växer parallellt med fade/scale så +SET-knappen
                // glider ner mjukt istället för att snappa direkt.
                ve.HeightRequest = 0;
                ve.Opacity = 0;
                ve.TranslationY = -6;
                ve.Scale = 0.96;
                await Task.WhenAll(
                    AnimateHeightAsync(ve, 0, SetRowHeight, 220),
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
        catch (Exception ex)
        {
            LogError(ex, "OnSetRowLoaded");
            ve.HeightRequest = SetRowHeight;
            ve.Opacity = 1;
        }
    }

    private async void OnRemoveSetClicked(object sender, EventArgs e)
    {
        VisualElement? lastRow = null;
        try
        {
            if (sender is not Button btn) return;
            if (btn.BindingContext is not WorkoutExerciseSection section) return;

            // Loggade set kräver bekräftelse — de raderas även ur historiken
            if (!await _vm.ConfirmRemoveLastSetAsync(section)) return;

            // Hitta StackLayout som innehåller set-raderna (sibling till +/- knappraden).
            var setsLayout = FindSetsLayout(btn);
            lastRow = setsLayout?.Children.LastOrDefault() as VisualElement;

            AnimationHelper.Haptic();
            if (lastRow != null && !AnimationHelper.ReduceMotion)
            {
                var startHeight = lastRow.Height > 0 ? lastRow.Height : SetRowHeight;
                await Task.WhenAll(
                    lastRow.FadeTo(0, 220, Easing.CubicIn),
                    lastRow.TranslateTo(0, -6, 220, Easing.CubicIn),
                    AnimateHeightAsync(lastRow, startHeight, 0, 220)
                );
            }

            // Ta bort från VM (och DB) efter animationen — resterande rader är redan på plats
            if (!await _vm.RemoveLastSetAsync(section))
                RestoreRow(lastRow);
        }
        catch (Exception ex)
        {
            LogError(ex, "OnRemoveSetClicked");
            RestoreRow(lastRow);
        }
    }

    private static void RestoreRow(VisualElement? row)
    {
        if (row is null) return;
        row.HeightRequest = SetRowHeight;
        row.Opacity = 1;
        row.TranslationY = 0;
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
    {
        try { await AnimationHelper.PressAsync(sender); }
        catch (Exception ex) { LogError(ex, "OnAddExerciseTapped"); }
    }

    // Generic border press animation (scale 0.93)
    private static async void OnElemPointerPressed(object? sender, PointerEventArgs e)
    {
        try
        {
            AnimationHelper.Haptic();
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(0.93, 65, Easing.CubicOut);
        }
        catch (Exception ex) { LogError(ex, "OnElemPointerPressed"); }
    }

    private static async void OnElemPointerReleased(object? sender, PointerEventArgs e)
    {
        try
        {
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(1.0, 230, Easing.SpringOut);
        }
        catch (Exception ex) { LogError(ex, "OnElemPointerReleased"); }
    }

    private static async void OnDonePointerPressed(object? sender, PointerEventArgs e)
    {
        try
        {
            AnimationHelper.Haptic();
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(0.93, 65, Easing.CubicOut);
        }
        catch (Exception ex) { LogError(ex, "OnDonePointerPressed"); }
    }

    private static async void OnDonePointerReleased(object? sender, PointerEventArgs e)
    {
        try
        {
            if (AnimationHelper.ReduceMotion) return;
            if (sender is not PointerGestureRecognizer pgr) return;
            if (pgr.Parent is not VisualElement ve) return;

            await ve.ScaleTo(1.0, 230, Easing.SpringOut);

            // Liten paus så CompleteSetCommand hinner toggla IsCompleted innan vi kollar
            await Task.Delay(40);

            // Kvittera när settet just blev klart (false→true). Hoppa över toggle-off.
            if (ve.BindingContext is LoggedSetRow row && row.IsCompleted)
            {
                AnimationHelper.Haptic();
                await ve.ScaleTo(1.18, 110, Easing.CubicOut);
                await ve.ScaleTo(1.0, 140, Easing.SpringOut);
            }
        }
        catch (Exception ex) { LogError(ex, "OnDonePointerReleased"); }
    }
}
