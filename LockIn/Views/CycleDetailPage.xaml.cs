using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CycleDetailPage : ContentPage
{
    public CycleDetailPage(CycleDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
