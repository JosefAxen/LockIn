using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class WeeklyVolumePage : ContentPage
{
    private readonly WeeklyVolumeViewModel _vm;

    public WeeklyVolumePage(WeeklyVolumeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WeeklyVolume] Laddning misslyckades: {ex}");
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WeeklyVolume] Tillbaka misslyckades: {ex}"); }
    }
}
