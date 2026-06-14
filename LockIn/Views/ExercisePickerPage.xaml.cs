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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private async void OnAddCustomExerciseTapped(object sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(CreateExercisePage));

    private static async void OnExercisePointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.97, 65, Easing.CubicOut);
    }

    private static async void OnExercisePointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 230, Easing.SpringOut);
    }
}
