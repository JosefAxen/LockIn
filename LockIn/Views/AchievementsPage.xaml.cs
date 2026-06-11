using LockIn.ViewModels;

namespace LockIn.Views;

public partial class AchievementsPage : ContentPage
{
    public AchievementsPage(AchievementsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as AchievementsViewModel;
        _ = vm?.LoadAsync();
    }
}
