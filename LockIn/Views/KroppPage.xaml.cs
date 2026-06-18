using LockIn.Services;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class KroppPage : ContentPage
{
    private readonly KroppViewModel _vm;
    private readonly ActiveWorkoutStateService _state;
    private bool _hasLoaded;

    public KroppPage(KroppViewModel vm, ActiveWorkoutStateService state)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _state = state;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        WorkoutBanner.IsVisible = _state.IsActive;
        _vm.HeatmapReady += BuildHeatmapGrid;

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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.HeatmapReady -= BuildHeatmapGrid;
    }

    private void OnScrolled(object sender, ScrolledEventArgs e)
        => StickyHeader.Opacity = Math.Clamp((e.ScrollY - 80.0) / 40.0, 0, 1);

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
