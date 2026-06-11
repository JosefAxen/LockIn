using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public LibraryPage(LibraryViewModel vm, ActiveWorkoutStateService state)
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

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await _vm.LoadAsync();
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//TrainPage");
}
