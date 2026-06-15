using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class TrainPage : ContentPage
{
    private readonly TrainViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

    public TrainPage(TrainViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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
}
