using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ExerciseProgressPage : ContentPage
{
    public ExerciseProgressPage(ExerciseProgressViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackClicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
