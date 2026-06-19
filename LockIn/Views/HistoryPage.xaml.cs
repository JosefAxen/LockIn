using LockIn;
using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

    public HistoryPage(HistoryViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        _vm.CalendarChanged += RebuildCalendar;

        StickyHeader.Opacity = 0;

        if (!_hasLoaded)
        {
            Content.Opacity = 0;
            Content.TranslationY = 16;
            await _vm.LoadAsync();
            _hasLoaded = true;
            await Task.WhenAll(
                Content.FadeTo(1, 400, Easing.CubicOut),
                Content.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
        else
        {
            Content.Opacity = 0;
            Content.TranslationY = 12;
            await Task.WhenAll(
                _vm.LoadAsync(),
                Content.FadeTo(1, 320, Easing.CubicOut),
                Content.TranslateTo(0, 0, 320, Easing.CubicOut)
            );
        }
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.CalendarChanged -= RebuildCalendar;
    }

    private void RebuildCalendar() =>
        MainThread.BeginInvokeOnMainThread(BuildCalendar);

    private void BuildCalendar()
    {
        var grid = CalendarGrid;
        grid.RowDefinitions.Clear();
        grid.Children.Clear();

        var firstDay = new DateTime(_vm.CalendarYear, _vm.CalendarMonth, 1);
        var dayOfWeek = (int)firstDay.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

        int daysInMonth = DateTime.DaysInMonth(_vm.CalendarYear, _vm.CalendarMonth);
        int totalCells = offset + daysInMonth;
        int rows = (totalCells + 6) / 7;

        for (int r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(42)));

        int day = 1;
        for (int cell = 0; cell < rows * 7; cell++)
        {
            int r = cell / 7;
            int c = cell % 7;

            if (cell < offset || day > daysInMonth)
            {
                var empty = new BoxView { BackgroundColor = Colors.Transparent };
                Grid.SetRow(empty, r); Grid.SetColumn(empty, c);
                grid.Children.Add(empty);
                continue;
            }

            bool trained = _vm.TrainedDays.Contains(day);
            bool today = day == DateTime.Today.Day
                         && _vm.CalendarYear == DateTime.Today.Year
                         && _vm.CalendarMonth == DateTime.Today.Month;
            bool selected = day == _vm.SelectedCalendarDay;

            var border = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
                BackgroundColor = (trained || selected) ? Color.FromArgb("#006239") : Colors.Transparent,
                Stroke = (today && !trained && !selected) ? DesignTokens.CalTodayStroke : Colors.Transparent,
                StrokeThickness = (today && !trained && !selected) ? 1.5 : 0,
                Margin = new Thickness(3),
                Padding = new Thickness(0),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
            };

            var label = new Label
            {
                Text = day.ToString(),
                FontFamily = "DMSansMedium",
                FontSize = 13,
                TextColor = (trained || selected) ? Colors.White
                    : today ? DesignTokens.CalTodayText
                    : DesignTokens.CalNormalText,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            border.Content = label;

            var capturedDay = day;
            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    if (!trained) return;
                    _vm.SelectCalendarDay(capturedDay);
                })
            });

            Grid.SetRow(border, r); Grid.SetColumn(border, c);
            grid.Children.Add(border);
            day++;
        }
    }

    internal async void OnSessionTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private async void OnCalNavTapped(object sender, TappedEventArgs e)
        => await AnimationHelper.PressAsync(sender);

    private static async void OnButtonPointerPressed(object? sender, PointerEventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(0.93, 65, Easing.CubicOut);
    }

    private static async void OnButtonPointerReleased(object? sender, PointerEventArgs e)
    {
        if (sender is PointerGestureRecognizer pgr && pgr.Parent is VisualElement ve)
            await ve.ScaleTo(1.0, 230, Easing.SpringOut);
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
