using LockIn.ViewModels;

namespace LockIn.Views;

public partial class TemplateEditPage : ContentPage
{
    public TemplateEditPage(TemplateEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
