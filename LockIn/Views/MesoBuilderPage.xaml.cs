using LockIn.ViewModels;

namespace LockIn.Views;

public partial class MesoBuilderPage : ContentPage
{
    public MesoBuilderPage(MesoBuilderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
