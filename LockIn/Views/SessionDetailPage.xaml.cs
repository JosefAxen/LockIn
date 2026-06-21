using LockIn.ViewModels;

namespace LockIn.Views;

public partial class SessionDetailPage : ContentPage
{
    public SessionDetailPage(SessionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StickyHeader.Opacity = 0;
        await AnimationHelper.PageEntryAsync(this);
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);
}
