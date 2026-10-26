using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;

namespace LockIn.Views;

public partial class ExercisePickerPage : ContentPage
{
    private readonly ExercisePickerViewModel _vm;

    public ExercisePickerPage(ExercisePickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private static void LogError(Exception ex, string context) =>
        System.Diagnostics.Debug.WriteLine($"[ExercisePickerPage] {context}: {ex}");

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            LogError(ex, "OnAppearing");
            try { await DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK); }
            catch { /* felvisningen får inte själv fälla sidan */ }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { LogError(ex, "OnBackClicked"); }
    }

    private async void OnAddCustomExerciseTapped(object sender, TappedEventArgs e)
    {
        try
        {
            await AnimationHelper.PressAsync(sender);
            // Skicka vidare väljarens callback: en ny övning ska göra samma sak som ett val i listan
            // (lägga till i passet, eller ersätta vid "Byt övning").
            if (_vm.CallbackAction is { } callback)
                await Shell.Current.GoToAsync(AppRoutes.CreateExercise, new Dictionary<string, object>
                {
                    { "CallbackAction", callback }
                });
            else
                await Shell.Current.GoToAsync(AppRoutes.CreateExercise);
        }
        catch (Exception ex) { LogError(ex, "OnAddCustomExerciseTapped"); }
    }

    private static async void OnExercisePointerPressed(object? sender, PointerEventArgs e)
    {
        try
        {
            AnimationHelper.Haptic();
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(0.97, 65, Easing.CubicOut);
        }
        catch (Exception ex) { LogError(ex, "OnExercisePointerPressed"); }
    }

    private static async void OnExercisePointerReleased(object? sender, PointerEventArgs e)
    {
        try
        {
            if (AnimationHelper.ReduceMotion) return;
            if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
                await ve.ScaleTo(1.0, 230, Easing.SpringOut);
        }
        catch (Exception ex) { LogError(ex, "OnExercisePointerReleased"); }
    }
}
