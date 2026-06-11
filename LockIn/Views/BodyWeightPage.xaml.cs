using LockIn.ViewModels;

namespace LockIn.Views;

public partial class BodyWeightPage : ContentPage
{
    private readonly BodyWeightViewModel _vm;

    public BodyWeightPage(BodyWeightViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ChartInvalidated += () => MainThread.BeginInvokeOnMainThread(() => ChartView.Invalidate());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
