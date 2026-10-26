using LockIn.Navigation;
using LockIn.Resources.Strings;
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
        try
        {
            await _vm.RefreshAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TemplatePicker] Laddning misslyckades: {ex}");
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TemplatePicker] Tillbaka misslyckades: {ex}"); }
    }
}
