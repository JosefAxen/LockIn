using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CardioPage : ContentPage
{
    public CardioPage(CardioViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
