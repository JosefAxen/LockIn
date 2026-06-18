using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ProgramDetailPage : ContentPage
{
    public ProgramDetailPage(ProgramDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimationHelper.PageEntryAsync(this);
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
