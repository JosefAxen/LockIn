using LockIn.ViewModels;

namespace LockIn.Views;

public partial class PostWorkoutPage : ContentPage
{
    public PostWorkoutPage(PostWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
