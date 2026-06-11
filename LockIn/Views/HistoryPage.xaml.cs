using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public HistoryPage(HistoryViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        await _vm.LoadAsync();
    }

    internal async void OnSessionTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//TrainPage");
}
