using LockIn.ViewModels;

namespace LockIn.Views;

public partial class PostWorkoutPage : ContentPage
{
    private readonly PostWorkoutViewModel _vm;

    public PostWorkoutPage(PostWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            StickyHeader.Opacity = 0;
            await AnimationHelper.PageEntryAsync(this);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PostWorkoutPage] OnAppearing: {ex}"); }
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        // En sida som pushas ovanpå (t.ex. mesocykel-statistik) ska INTE avgöra passet —
        // användaren kommer tillbaka hit och kan fortfarande skriva anteckningar, trycka
        // NÄSTA PASS eller Ångra. Bara när sidan faktiskt lämnas avgörs passet.
        if (args.NavigationType == NavigationType.Push) return;
        try
        {
            await _vm.HandleLeftPageAsync(args.DestinationPage is ActiveWorkoutPage);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PostWorkoutPage] OnNavigatedFrom: {ex}"); }
    }

    private async void OnKlarTapped(object sender, TappedEventArgs e)
    {
        try { await AnimationHelper.PressAsync(sender); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[PostWorkoutPage] OnKlarTapped: {ex}"); }
    }
}
