using LockIn.ViewModels;

namespace LockIn.Views;

public partial class PeriodizationPage : ContentPage
{
    public PeriodizationPage(PeriodizationViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Opacity = 0;
        this.TranslationY = 20;
        if (BindingContext is PeriodizationViewModel vm)
            await vm.LoadAsync();
        await Task.WhenAll(
            this.FadeTo(1, 200),
            this.TranslateTo(0, 0, 200, Easing.CubicOut));
    }
}
