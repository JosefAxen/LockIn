using LockIn.ViewModels;

namespace LockIn.Views;

public partial class MesoBuilderPage : ContentPage
{
    public MesoBuilderPage(MesoBuilderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
