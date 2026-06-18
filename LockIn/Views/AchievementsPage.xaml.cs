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
        var vm = BindingContext as AchievementsViewModel;
        Content.Opacity = 0;
        Content.TranslationY = 14;
        await Task.WhenAll(
            vm?.LoadAsync() ?? Task.CompletedTask,
            Content.FadeTo(1, 280, Easing.CubicOut),
            Content.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }
}
