using LockIn.ViewModels;

namespace LockIn.Views;

public partial class TemplateEditPage : ContentPage
{
    private bool _hasAppeared;

    public TemplateEditPage(TemplateEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_hasAppeared)
        {
            _hasAppeared = true;
            await AnimationHelper.PageEntryAsync(this);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
