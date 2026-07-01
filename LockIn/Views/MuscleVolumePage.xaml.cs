using LockIn.ViewModels;

namespace LockIn.Views;

public partial class MuscleVolumePage : ContentPage
{
    private readonly MuscleVolumeViewModel _vm;

    public MuscleVolumePage(MuscleVolumeViewModel vm)
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
