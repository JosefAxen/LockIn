using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;

    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    internal async void OnSessionTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);
}
