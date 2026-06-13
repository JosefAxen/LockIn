using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    public ActiveWorkoutPage(ActiveWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Avbryt pass", "Lämna utan att avsluta?", "Ja", "Nej");
        if (confirmed)
        {
            var vm = BindingContext as ActiveWorkoutViewModel;
            vm?.ForceDeactivate();
            await Shell.Current.GoToAsync("..");
        }
    }

    private void OnRirTapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bo) return;
        if (bo.BindingContext is not LoggedSetRow row) return;
        row.Rir = row.Rir >= 5 || row.Rir < 0 ? 0 : row.Rir + 1;
        if (bo is Border border)
        {
            var label = border.Content as Label;
            if (label is not null)
            {
                label.Text = row.RirDisplay;
                label.TextColor = row.Rir >= 0
                    ? Color.FromArgb("#4ADE80")
                    : Color.FromArgb("#505055");
            }
        }
    }
}
