using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HemPage : ContentPage
{
    private readonly HemViewModel _vm;

    public HemPage(HemViewModel vm)
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
}
