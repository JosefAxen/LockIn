using LockIn.ViewModels;

namespace LockIn.Views;

public partial class WeeklyVolumePage : ContentPage
{
    private readonly WeeklyVolumeViewModel _vm;

    public WeeklyVolumePage(WeeklyVolumeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnBackTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
