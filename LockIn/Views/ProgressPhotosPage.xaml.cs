using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class ProgressPhotosPage : ContentPage
{
    public ProgressPhotosPage(ProgressPhotosViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var vm = BindingContext as ProgressPhotosViewModel;
            // Gemensam in-animation som respekterar "Minska rörelse"
            await Task.WhenAll(
                vm?.LoadAsync() ?? Task.CompletedTask,
                AnimationHelper.PageEntryAsync(this)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProgressPhotosPage] OnAppearing: {ex}");
            try { await DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK); }
            catch { /* felvisningen får inte själv fälla sidan */ }
        }
    }
}
