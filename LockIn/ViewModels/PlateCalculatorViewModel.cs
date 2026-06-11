using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace LockIn.ViewModels;

public partial class PlateCalculatorViewModel : ObservableObject
{
    private static readonly decimal[] Plates = [25m, 20m, 15m, 10m, 5m, 2.5m, 1.25m];

    [ObservableProperty] private string _targetWeightText = "";
    [ObservableProperty] private string _barWeightText = "20";
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private bool _hasResult;

    public List<(decimal Plate, int Count)> PlateData { get; private set; } = new();
    public event Action? PlatesChanged;

    [RelayCommand]
    private void Calculate()
    {
        if (!decimal.TryParse(TargetWeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var target) || target <= 0)
        {
            ResultText = "Ange en giltig målvikt.";
            PlateData = new();
            PlatesChanged?.Invoke();
            HasResult = true;
            return;
        }

        decimal barWeight = 20m;
        if (decimal.TryParse(BarWeightText.Replace(',', '.'),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var bar) && bar > 0)
            barWeight = bar;

        if (target < barWeight)
        {
            ResultText = $"Målvikt måste vara ≥ stångvikt ({barWeight} kg).";
            PlateData = new();
            PlatesChanged?.Invoke();
            HasResult = true;
            return;
        }

        var weightPerSide = (target - barWeight) / 2m;
        var remaining = weightPerSide;
        var pairs = new List<(decimal Plate, int Count)>();

        foreach (var plate in Plates)
        {
            var count = (int)Math.Floor(remaining / plate);
            if (count > 0)
            {
                pairs.Add((plate, count));
                remaining -= count * plate;
            }
        }

        if (remaining > 0.01m)
        {
            var achievable = target - remaining * 2;
            ResultText = $"Exakt {target} kg går ej.\nNärmaste: {achievable:G} kg";
            PlateData = new();
            PlatesChanged?.Invoke();
            HasResult = true;
            return;
        }

        if (pairs.Count == 0)
        {
            ResultText = $"Bara stången: {barWeight} kg";
            PlateData = new();
            PlatesChanged?.Invoke();
            HasResult = true;
            return;
        }

        var parts = pairs.Select(p => $"{p.Count}× {p.Plate:G}");
        ResultText = string.Join(" + ", parts) + " kg per sida";
        PlateData = pairs;
        PlatesChanged?.Invoke();
        HasResult = true;
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
