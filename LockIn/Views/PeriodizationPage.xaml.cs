using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

// RP full-klon Task 19: PeriodizationPage — mesocykel-hub.
public partial class PeriodizationPage : ContentPage
{
    private readonly PeriodizationViewModel _vm;

    public PeriodizationPage(PeriodizationViewModel vm)
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
            System.Diagnostics.Debug.WriteLine($"[Periodization] Laddning misslyckades: {ex}");
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }
}
