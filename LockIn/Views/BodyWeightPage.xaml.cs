using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class BodyWeightPage : ContentPage
{
    private readonly BodyWeightViewModel _vm;

    public BodyWeightPage(BodyWeightViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async void OnLoggaViktTapped(object sender, TappedEventArgs e)
    {
        try { await AnimationHelper.PressAsync(sender); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[BodyWeight] Tryckanimation misslyckades: {ex}"); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            // PageEntryAsync hoppar över inglidningen vid "Minska rörelse"
            await Task.WhenAll(
                _vm.LoadAsync(),
                AnimationHelper.PageEntryAsync(this)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BodyWeight] Laddning misslyckades: {ex}");
            Content.Opacity = 1;
            Content.TranslationY = 0;
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }
}
