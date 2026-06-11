using LockIn.ViewModels;

namespace LockIn.Views;

public partial class PlateCalculatorPage : ContentPage
{
    public PlateCalculatorPage(PlateCalculatorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
