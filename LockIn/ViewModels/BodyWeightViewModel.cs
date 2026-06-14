using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LockIn.Models;
using LockIn.Services;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class BodyWeightViewModel(DatabaseService db) : ObservableObject
{
    private static readonly SKColor ChartBlue = new(0x6E, 0xA8, 0xDC);

    [ObservableProperty] private string _latestWeight = "–";
    [ObservableProperty] private string _latestDate = "";
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _xAxes = [];

    public Axis[] YAxes { get; } =
    [
        new Axis { LabelsPaint = null, TicksPaint = null,
                   SeparatorsPaint = new SolidColorPaint(new SKColor(0x2A, 0x2A, 0x2A)) }
    ];

    public ObservableCollection<BodyWeightEntry> RecentEntries { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        var entries = await db.GetBodyWeightEntriesAsync();
        HasData = entries.Count > 0;

        if (HasData)
        {
            var latest = entries[0];
            LatestWeight = $"{latest.WeightKg:F1} kg";
            LatestDate   = latest.LoggedAt.ToString("d MMM yyyy");

            var pts = entries
                .OrderBy(e => e.LoggedAt)
                .Select(e => new DateTimePoint(e.LoggedAt, (double)e.WeightKg))
                .ToArray();

            Series =
            [
                new LineSeries<DateTimePoint>
                {
                    Values          = pts,
                    Stroke          = new SolidColorPaint(ChartBlue, 2),
                    Fill            = new SolidColorPaint(ChartBlue.WithAlpha(30)),
                    GeometryFill    = new SolidColorPaint(ChartBlue),
                    GeometryStroke  = new SolidColorPaint(new SKColor(0x12, 0x12, 0x12), 1.5f),
                    GeometrySize    = 8,
                    LineSmoothness  = 0.3,
                }
            ];
            XAxes =
            [
                new DateTimeAxis(TimeSpan.FromDays(1), d => d.ToString("d/M"))
                {
                    TextSize        = 9,
                    LabelsPaint     = new SolidColorPaint(new SKColor(0x88, 0x88, 0x88)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(0x2A, 0x2A, 0x2A)),
                    TicksPaint      = null,
                }
            ];
        }

        RecentEntries.Clear();
        foreach (var e in entries.Take(10))
            RecentEntries.Add(e);

        IsLoading = false;
    }

    [RelayCommand]
    private async Task LogWeightAsync()
    {
        var result = await Shell.Current.DisplayPromptAsync(
            "Logga vikt", "Ange din kroppsvikt i kg:",
            keyboard: Keyboard.Numeric, placeholder: "t.ex. 80.5");

        if (string.IsNullOrWhiteSpace(result)) return;
        if (!decimal.TryParse(result.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var kg) || kg <= 0) return;

        await db.SaveBodyWeightEntryAsync(new BodyWeightEntry { LoggedAt = DateTime.Now, WeightKg = kg });
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(BodyWeightEntry entry)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Ta bort", $"Ta bort {entry.WeightKg} kg ({entry.LoggedAt:d MMM})?", "Ta bort", "Avbryt");
        if (!confirmed) return;
        await db.DeleteBodyWeightEntryAsync(entry);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
