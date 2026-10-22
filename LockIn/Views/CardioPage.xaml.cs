using LockIn.Navigation;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CardioPage : ContentPage
{
    public CardioPage(CardioViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private static void LogError(Exception ex, string context) =>
        System.Diagnostics.Debug.WriteLine($"[CardioPage] {context}: {ex}");

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Gemensam in-animation som respekterar "Minska rörelse"
        try { await AnimationHelper.PageEntryAsync(this); }
        catch (Exception ex) { LogError(ex, "OnAppearing"); }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { LogError(ex, "OnBackClicked"); }
    }

    private static async void OnBackPointerPressed(object? sender, PointerEventArgs e)
    {
        try
        {
            AnimationHelper.Haptic();
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(0.88, 65, Easing.CubicOut);
        }
        catch (Exception ex) { LogError(ex, "OnBackPointerPressed"); }
    }

    private static async void OnBackPointerReleased(object? sender, PointerEventArgs e)
    {
        try
        {
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(1.0, 180, Easing.SpringOut);
        }
        catch (Exception ex) { LogError(ex, "OnBackPointerReleased"); }
    }

    private async void OnSavePointerPressed(object? sender, PointerEventArgs e)
    {
        try
        {
            AnimationHelper.Haptic();
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(0.97, 65, Easing.CubicOut);
        }
        catch (Exception ex) { LogError(ex, "OnSavePointerPressed"); }
    }

    private async void OnSavePointerReleased(object? sender, PointerEventArgs e)
    {
        try
        {
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(1.0, 180, Easing.SpringOut);
        }
        catch (Exception ex) { LogError(ex, "OnSavePointerReleased"); }
    }
}
