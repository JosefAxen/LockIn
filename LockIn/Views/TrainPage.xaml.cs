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

        if (!_hasLoaded)
        {
            Content.Opacity = 0;
            Content.TranslationY = 10;
        }

        await _vm.LoadAsync();

        if (!_hasLoaded)
        {
            _hasLoaded = true;
            await Task.WhenAll(
                Content.FadeTo(1, 280, Easing.CubicOut),
                Content.TranslateTo(0, 0, 280, Easing.CubicOut)
            );
        }
    }

    internal async void OnTemplateTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);
}
