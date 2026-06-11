using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ProgramDetailPage : ContentPage
{
    public ProgramDetailPage(ProgramDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
