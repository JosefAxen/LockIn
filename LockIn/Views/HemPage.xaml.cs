using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HemPage : ContentPage
{
    public HemPage(HemViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
