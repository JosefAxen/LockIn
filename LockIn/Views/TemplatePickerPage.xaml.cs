using LockIn.ViewModels;

namespace LockIn.Views;

// RP full-klon Task 21: TemplatePickerPage — välj färdig template
// och konfigurera en mesocykel via inline-panel.
public partial class TemplatePickerPage : ContentPage
{
    private readonly TemplatePickerViewModel _vm;

    public TemplatePickerPage(TemplatePickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.RefreshAsync();
    }

    private async void OnBackTapped(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
