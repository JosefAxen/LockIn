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
        Content.Opacity = 0;
        Content.TranslationY = 14;
        await Task.WhenAll(
            vm?.LoadAsync() ?? Task.CompletedTask,
            Content.FadeTo(1, 280, Easing.CubicOut),
            Content.TranslateTo(0, 0, 280, Easing.CubicOut)
        );
    }

    private void OnScrolled(object sender, ItemsViewScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.VerticalOffset - 80.0) / 40.0, 0, 1);
}
