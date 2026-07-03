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
        await _vm.RefreshAsync();
    }
}
