using LockIn.ViewModels;

namespace LockIn.Views;

public partial class PlateCalculatorPage : ContentPage
{
    private readonly BarbellDrawable _drawable = new();

    public PlateCalculatorPage(PlateCalculatorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        BarbellView.Drawable = _drawable;
        vm.PlatesChanged += () =>
        {
            _drawable.Plates = vm.PlateData;
            MainThread.BeginInvokeOnMainThread(() => BarbellView.Invalidate());
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimationHelper.PageEntryAsync(this);
    }
}
