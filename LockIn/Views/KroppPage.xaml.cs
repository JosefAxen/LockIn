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
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 0,
                Padding = new Thickness(8, 14, 8, 10),
                HeightRequest = 96,
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 3),
                    Radius = 12,
                    Opacity = 0.22f
                },
                // initial state for stagger fade-in
                Opacity = 0,
                Scale = 0.9
            };

            var rootGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(new GridLength(3))
                },
                RowSpacing = 6
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            stack.Add(new Label
            {
                Text = tile.Name,
                FontSize = 9,
                TextColor = tile.TextColor,
                Opacity = 0.7,
                FontFamily = "BebasNeue",
                CharacterSpacing = 1,
                HorizontalOptions = LayoutOptions.Center,
            });
            stack.Add(new Label
            {
                Text = tile.ScoreText,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                TextColor = tile.TextColor,
                LineHeight = 1,
                CharacterSpacing = -1,
                HorizontalOptions = LayoutOptions.Center,
            });
            Grid.SetRow(stack, 0);
            rootGrid.Children.Add(stack);

            // Tunn progress-indikator i botten: track + fill baserat på score (0–10)
            var trackGrid = new Grid();
            trackGrid.Children.Add(new BoxView
            {
                BackgroundColor = Colors.Black,
                Opacity = 0.2,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                CornerRadius = 2
            });
            var fillFraction = (float)Math.Clamp(tile.Score / 10.0, 0, 1);
            trackGrid.Children.Add(new BoxView
            {
                BackgroundColor = tile.TextColor,
                Opacity = 0.85,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                AnchorX = 0,
                ScaleX = fillFraction,
                CornerRadius = 2
            });
            Grid.SetRow(trackGrid, 1);
            rootGrid.Children.Add(trackGrid);

            border.Content = rootGrid;

            // Tap-feedback: scale-pop med haptic
            border.GestureRecognizers.Add(new PointerGestureRecognizer());
            var pointer = (PointerGestureRecognizer)border.GestureRecognizers[^1];
            pointer.PointerPressed += async (_, __) =>
            {
                Microsoft.Maui.Devices.HapticFeedback.Default.Perform(
                    Microsoft.Maui.Devices.HapticFeedbackType.Click);
                await border.ScaleTo(0.94, 60, Easing.CubicOut);
            };
            pointer.PointerReleased += async (_, __) =>
            {
                await border.ScaleTo(1.0, 180, Easing.SpringOut);
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);

            // Stagger fade-in animation
            var delay = i * 30;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.WhenAll(
                        border.FadeTo(1, 280, Easing.CubicOut),
                        border.ScaleTo(1, 280, Easing.SpringOut)
                    );
                });
            });
        }
    }

    private async void OnWorkoutBannerTapped(object sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
}
