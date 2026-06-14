using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class TrainPage : ContentPage
{
    private readonly TrainViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public TrainPage(TrainViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_state.IsActive)
        {
            Dispatcher.Dispatch(async () =>
                await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage)));
            return;
        }
        Dispatcher.Dispatch(async () => await _vm.LoadAsync());
    }

    internal async void OnTemplateTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);
}
