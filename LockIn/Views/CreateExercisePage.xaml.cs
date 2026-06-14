using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CreateExercisePage : ContentPage
{
    public CreateExercisePage(CreateExerciseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
