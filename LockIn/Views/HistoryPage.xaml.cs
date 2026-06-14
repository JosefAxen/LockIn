using LockIn;
using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

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
        await _vm.LoadAsync();
    }

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
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Monday-first

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

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//TrainPage");
}
