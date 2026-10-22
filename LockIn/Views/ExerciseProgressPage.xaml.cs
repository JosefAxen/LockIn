using LockIn.Navigation;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ExerciseProgressPage : ContentPage
{
    public ExerciseProgressPage(ExerciseProgressViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimationHelper.PageEntryAsync(this);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ExerciseProgress] Tillbaka misslyckades: {ex}"); }
    }
}
