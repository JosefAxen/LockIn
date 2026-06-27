using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CardioPage : ContentPage
{
    public CardioPage(CardioViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Content.Opacity = 0;
        Content.TranslationY = 8;
        await Task.WhenAll(
            Content.FadeTo(1, 220, Easing.CubicOut),
            Content.TranslateTo(0, 0, 220, Easing.CubicOut)
        );
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private static async void OnBackPointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.88, 65, Easing.CubicOut);
    }

    private static async void OnBackPointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 180, Easing.SpringOut);
    }

    private async void OnSavePointerPressed(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.97, 65, Easing.CubicOut);
    }

    private async void OnSavePointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 180, Easing.SpringOut);
    }
}
