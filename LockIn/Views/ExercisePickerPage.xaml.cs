using LockIn.ViewModels;

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
}
