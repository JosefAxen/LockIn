using LockIn.ViewModels;

namespace LockIn.Views;

public partial class CreateExercisePage : ContentPage
{
    public CreateExercisePage(CreateExerciseViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimationHelper.PageEntryAsync(this);
    }
}
