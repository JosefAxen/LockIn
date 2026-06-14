using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LockIn.Models;
using LockIn.Services;
using SkiaSharp;

namespace LockIn.ViewModels;

public partial class ExerciseProgressViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    private static readonly SKColor AccentGreen = new(0x4A, 0xDE, 0x80);

    [ObservableProperty] private string _exerciseName = "";
    [ObservableProperty] private string _muscleGroupName = "";
    [ObservableProperty] private string _bestSet = "–";
    [ObservableProperty] private string _estimatedOneRm = "–";
    [ObservableProperty] private string _totalSessions = "0 pass";
    [ObservableProperty] private string _exerciseNotes = "";
    [ObservableProperty] private string _exerciseDescription = "";
    [ObservableProperty] private bool _hasDescription;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxes =
    [
        new Axis
        {
            TextSize        = 9,
            LabelsPaint     = new SolidColorPaint(new SKColor(0x88, 0x88, 0x88)),
            SeparatorsPaint = new SolidColorPaint(new SKColor(0x2A, 0x2A, 0x2A)),
            TicksPaint      = null,
            Labeler         = v => $"{(int)v}",
        }
    ];

    private Exercise? _exercise;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ExerciseId", out var val) && val is int id)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int exerciseId)
    {
        IsLoading = true;

        _exercise = await db.GetExerciseAsync(exerciseId);
        if (_exercise is not null)
        {
            ExerciseName        = _exercise.Name;
            MuscleGroupName     = MuscleGroupLabel(_exercise.MuscleGroup);
            ExerciseNotes       = _exercise.Notes ?? "";
            ExerciseDescription = _exercise.Description ?? "";
            HasDescription      = !string.IsNullOrWhiteSpace(ExerciseDescription);
        }

        var history = await db.GetBestSetPerSessionForExerciseAsync(exerciseId);
        HasData = history.Count > 0;

        if (HasData)
        {
            var best = history.OrderByDescending(h => h.Epley1RM).First();
            BestSet        = $"{best.WeightKg} kg × {best.Reps} reps";
            EstimatedOneRm = $"{best.Epley1RM:F0} kg";
            TotalSessions  = $"{history.Count} pass";

            var pts = history
                .TakeLast(12)
                .Select(h => new DateTimePoint(h.Date, h.Epley1RM))
                .ToArray();

            Series =
            [
                new LineSeries<DateTimePoint>
                {
                    Values         = pts,
                    Stroke         = new SolidColorPaint(AccentGreen, 2),
                    Fill           = new SolidColorPaint(AccentGreen.WithAlpha(26)),
                    GeometryFill   = new SolidColorPaint(AccentGreen),
                    GeometryStroke = new SolidColorPaint(new SKColor(0x12, 0x12, 0x12), 1.5f),
                    GeometrySize   = 9,
                    LineSmoothness = 0.3,
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

        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveNotesAsync()
    {
        if (_exercise is null) return;
        _exercise.Notes = ExerciseNotes;
        await db.SaveExerciseAsync(_exercise);
    }

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest    => "Bröst",
        MuscleGroup.Back     => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps   => "Biceps",
        MuscleGroup.Triceps  => "Triceps",
        MuscleGroup.Legs     => "Ben",
        MuscleGroup.Core     => "Core",
        MuscleGroup.FullBody => "Helkropp",
        _                    => "Övrigt"
    };
}
