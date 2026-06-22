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
        // Tidigare fanns en if(_state.IsActive) redirect till ActiveWorkoutPage här —
        // den skapade en evig loop när användaren tryckte tillbaka från passet.
        // Borttagen. Användaren kommer tillbaka till passet via "PASS PÅGÅR"-bannern.

        StickyHeader.Opacity = 0;
        Content.Opacity = 0;
        Content.TranslationY = _hasLoaded ? 12 : 16;

        // try/finally säkerhetsnät: om LoadAsync eller animationerna kastar
        // (race condition med PostWorkout DB-save, layout inte mätt än, etc)
        // så förblir Content.Opacity låst på 0 → svart skärm. Återställ alltid.
        try
        {
            if (!_hasLoaded)
            {
                await _vm.LoadAsync();
                _hasLoaded = true;
                await Task.WhenAll(
                    Content.FadeTo(1, 400, Easing.CubicOut),
                    Content.TranslateTo(0, 0, 400, Easing.CubicOut)
                );
            }
            else
            {
                await Task.WhenAll(
                    _vm.LoadAsync(),
                    Content.FadeTo(1, 320, Easing.CubicOut),
                    Content.TranslateTo(0, 0, 320, Easing.CubicOut)
                );
            }
        }
        finally
        {
            Content.Opacity = 1;
            Content.TranslationY = 0;
        }
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    internal async void OnTemplateTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnProgramTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnFabTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

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
