using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;

namespace LockIn.ViewModels;

public partial class ExerciseProgressViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private string _exerciseName = "";
    [ObservableProperty] private string _muscleGroupName = "";
    [ObservableProperty] private string _bestSet = "–";
    [ObservableProperty] private string _estimatedOneRm = "–";
    [ObservableProperty] private string _totalSessions = "0 pass";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;

    public ExerciseProgressDrawable ChartDrawable { get; } = new();

    public event Action? ChartInvalidated;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ExerciseId", out var val) && val is int id)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int exerciseId)
    {
        IsLoading = true;

        var exercise = await db.GetExerciseAsync(exerciseId);
        if (exercise is not null)
        {
            ExerciseName = exercise.Name;
            MuscleGroupName = MuscleGroupLabel(exercise.MuscleGroup);
        }

        var history = await db.GetBestSetPerSessionForExerciseAsync(exerciseId);
        HasData = history.Count > 0;

        if (HasData)
        {
            var best = history.OrderByDescending(h => h.Epley1RM).First();
            BestSet = $"{best.WeightKg} kg × {best.Reps} reps";
            EstimatedOneRm = $"{best.Epley1RM:F0} kg";
            TotalSessions = $"{history.Count} pass";

            ChartDrawable.Points = history
                .Select(h => (h.Date, h.Epley1RM, h.IsPR))
                .ToList();
        }

        IsLoading = false;
        ChartInvalidated?.Invoke();
    }

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest => "Bröst",
        MuscleGroup.Back => "Rygg",
        MuscleGroup.Shoulders => "Axlar",
        MuscleGroup.Biceps => "Biceps",
        MuscleGroup.Triceps => "Triceps",
        MuscleGroup.Legs => "Ben",
        MuscleGroup.Core => "Core",
        MuscleGroup.FullBody => "Helkropp",
        _ => "Övrigt"
    };
}
