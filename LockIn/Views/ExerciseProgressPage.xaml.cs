using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ExerciseProgressPage : ContentPage
{
    private readonly ExerciseProgressViewModel _vm;

    public ExerciseProgressPage(ExerciseProgressViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _vm.ChartInvalidated += () => MainThread.BeginInvokeOnMainThread(() => ChartView.Invalidate());
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
