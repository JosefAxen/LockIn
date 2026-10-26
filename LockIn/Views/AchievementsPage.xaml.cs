using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class AchievementsPage : ContentPage
{
    public AchievementsPage(AchievementsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StickyHeader.Opacity = 0;
        var vm = BindingContext as AchievementsViewModel;
        try
        {
            // PageEntryAsync hoppar över inglidningen vid "Minska rörelse"
            await Task.WhenAll(
                vm?.LoadAsync() ?? Task.CompletedTask,
                AnimationHelper.PageEntryAsync(this)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Achievements] Laddning misslyckades: {ex}");
            Content.Opacity = 1;
            Content.TranslationY = 0;
            await DisplayAlert(null, AppResources.Common_ErrorRetry, AppResources.Common_OK);
        }
    }

    private void OnScrolled(object sender, ItemsViewScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.VerticalOffset - 80.0) / 40.0, 0, 1);
}
