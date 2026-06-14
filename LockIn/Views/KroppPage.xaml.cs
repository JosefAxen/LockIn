using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class KroppPage : ContentPage
{
    private readonly KroppViewModel _vm;
    private readonly ActiveWorkoutStateService _state;

    public KroppPage(KroppViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;

        WeightChartView.Drawable = _vm.WeightChartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        _vm.ChartInvalidated += OnChartInvalidated;
        _vm.HeatmapReady += BuildHeatmapGrid;
        await _vm.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.ChartInvalidated -= OnChartInvalidated;
        _vm.HeatmapReady -= BuildHeatmapGrid;
    }

    private void OnChartInvalidated() =>
        MainThread.BeginInvokeOnMainThread(() => WeightChartView?.Invalidate());

    private void BuildHeatmapGrid() =>
        MainThread.BeginInvokeOnMainThread(BuildHeatmap);

    private void BuildHeatmap()
    {
        var grid = HeatmapGrid;
        grid.RowDefinitions.Clear();
        grid.Children.Clear();

        var tiles = _vm.HeatmapTiles;
        if (tiles.Count == 0) return;

        int rows = (tiles.Count + 3) / 4;
        for (int r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            int row = i / 4;
            int col = i % 4;

            var border = new Border
            {
                BackgroundColor = tile.TileColor,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                StrokeThickness = 0,
                Padding = new Thickness(6, 16),
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 5,
                HorizontalOptions = LayoutOptions.Center
            };
            stack.Add(new Label
            {
                Text = tile.Name,
                FontSize = 9,
                TextColor = tile.TextColor,
                Opacity = 0.85,
                FontFamily = "BebasNeue",
                CharacterSpacing = 0.5,
                HorizontalOptions = LayoutOptions.Center,
            });
            stack.Add(new Label
            {
                Text = tile.ScoreText,
                FontSize = 20,
                FontFamily = "BebasNeue",
                TextColor = tile.TextColor,
                HorizontalOptions = LayoutOptions.Center,
            });
            border.Content = stack;

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);
        }
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
