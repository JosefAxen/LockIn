using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ProgressPhotosPage : ContentPage
{
    public ProgressPhotosPage(ProgressPhotosViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as ProgressPhotosViewModel;
        Content.Opacity = 0;
        Content.TranslationY = 14;
        await Task.WhenAll(
            vm?.LoadAsync() ?? Task.CompletedTask,
            Content.FadeTo(1, 280, Easing.CubicOut),
            Content.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }
}
