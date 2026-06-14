using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class ExerciseProgressViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
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
    [ObservableProperty] private IReadOnlyList<ChartPoint> _chartPoints = [];

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

            ChartPoints = history
                .TakeLast(12)
                .Select(h => new ChartPoint(h.Date, h.Epley1RM))
                .ToList();
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
