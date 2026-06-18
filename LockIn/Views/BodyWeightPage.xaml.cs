using LockIn.ViewModels;

namespace LockIn.Views;

public partial class BodyWeightPage : ContentPage
{
    private readonly BodyWeightViewModel _vm;

    public BodyWeightPage(BodyWeightViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Content.Opacity = 0;
        Content.TranslationY = 14;
        await Task.WhenAll(
            _vm.LoadAsync(),
            Content.FadeTo(1, 280, Easing.CubicOut),
            Content.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }
}
