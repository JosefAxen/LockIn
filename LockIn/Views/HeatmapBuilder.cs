using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public static class HeatmapBuilder
{
    public static void Build(Grid grid, IReadOnlyList<HeatmapTile> tiles, CancellationToken ct = default)
    {
        grid.RowDefinitions.Clear();
        grid.Children.Clear();

        if (tiles.Count == 0) return;

        bool animate = !AnimationHelper.ReduceMotion;

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
                HeightRequest = string.IsNullOrEmpty(tile.FrequencyText) ? 96 : 108,
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 3),
                    Radius = 12,
                    Opacity = 0.22f
                },
                Opacity = animate ? 0 : 1,
                Scale = animate ? 0.9 : 1
            };

            // En sammanfattning per ruta i stället för tre lösa etiketter.
            SemanticProperties.SetDescription(border, tile.Score >= 0.05
                ? string.Format(AppResources.Hem_Heatmap_Tile_A11y, tile.Name, tile.ScoreText)
                : string.Format(AppResources.Hem_Heatmap_Tile_A11y_None, tile.Name));

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
            // Etiketterna har full opacitet: 0,7/0,65 föll under 4,5:1 på de ljusare rutorna.
            stack.Add(new Label
            {
                Text = tile.Name,
                FontSize = 9,
                TextColor = tile.TextColor,
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
            if (!string.IsNullOrEmpty(tile.FrequencyText))
            {
                stack.Add(new Label
                {
                    Text = tile.FrequencyText,
                    FontSize = 8,
                    FontFamily = "DMSansRegular",
                    TextColor = tile.TextColor,
                    HorizontalOptions = LayoutOptions.Center,
                    CharacterSpacing = 0.3,
                });
            }
            Grid.SetRow(stack, 0);
            rootGrid.Children.Add(stack);

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

            // Rutorna är inte tappbara — ingen haptik eller tryck-animation som antyder det.

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);

            if (!animate) continue;

            var delay = i * 30;
            _ = Task.Run(async () =>
            {
                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { return; }
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
}
