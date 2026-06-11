using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ProgressPhotosPage : ContentPage
{
    public ProgressPhotosPage(ProgressPhotosViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as ProgressPhotosViewModel;
        _ = vm?.LoadAsync();
    }
}
