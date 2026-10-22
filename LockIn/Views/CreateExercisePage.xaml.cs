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
        try { await AnimationHelper.PageEntryAsync(this); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CreateExercisePage] OnAppearing: {ex}"); }
    }
}
