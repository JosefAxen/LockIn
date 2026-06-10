using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
public partial class PostWorkoutViewModel(DatabaseService db) : ObservableObject
{
    [ObservableProperty] private int _sessionId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _duration = "";
    [ObservableProperty] private string _totalVolume = "";
    [ObservableProperty] private string _totalSets = "";
    [ObservableProperty] private string _prCount = "";
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<MuscleGroupRow> MuscleGroups { get; } = new();
    public ObservableCollection<PRRow> PRs { get; } = new();

    partial void OnSessionIdChanged(int value) => _ = LoadAsync(value);

    private async Task LoadAsync(int sessionId)
    {
        IsLoading = true;

        var sessions = await db.GetSessionsAsync();
        var session = sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session is null) { IsLoading = false; return; }

        var templates = await db.GetTemplatesAsync();
        TemplateName = templates.FirstOrDefault(t => t.Id == session.TemplateId)?.Name ?? "";

        if (session.CompletedAt.HasValue)
        {
            var elapsed = session.CompletedAt.Value - session.StartedAt;
            Duration = $"{(int)elapsed.TotalMinutes}m";
        }

        var muscleVolume = await db.GetSessionVolumeByMuscleGroupAsync(sessionId);
        var allSets = await GetAllSessionSetsAsync(sessionId);

        var totalVol = allSets.Sum(s => s.WeightKg * s.Reps);
        TotalVolume = totalVol >= 1000
            ? $"{totalVol / 1000:F1}k"
            : totalVol.ToString("F0");
        TotalSets = allSets.Count.ToString();

        MuscleGroups.Clear();
        if (muscleVolume.Any())
        {
            var maxVol = muscleVolume.Values.Max(v => v.Volume);
            foreach (var kv in muscleVolume.OrderByDescending(kv => kv.Value.Volume))
            {
                MuscleGroups.Add(new MuscleGroupRow
                {
                    Name = MuscleGroupName(kv.Key),
                    Volume = kv.Value.Volume,
                    Sets = kv.Value.Sets,
                    ProgressFraction = maxVol > 0 ? (double)(kv.Value.Volume / maxVol) : 0
                });
            }
        }

        var prSets = await db.GetPRsForSessionAsync(sessionId);
        PrCount = prSets.Count.ToString();
        PRs.Clear();
        foreach (var ps in prSets)
        {
            var se = await db.GetSessionExercisesAsync(sessionId);
            var seRow = se.FirstOrDefault(x => x.Id == ps.SessionExerciseId);
            var exercise = seRow != null ? await db.GetExerciseAsync(seRow.ExerciseId) : null;
            PRs.Add(new PRRow
            {
                ExerciseName = exercise?.Name ?? "",
                Display = $"{ps.WeightKg} kg × {ps.Reps}",
                Epley1RM = $"Est. 1RM {PRService.CalculateEpley1RM(ps.WeightKg, ps.Reps):F0} kg"
            });
        }

        IsLoading = false;
    }

    private async Task<List<LoggedSet>> GetAllSessionSetsAsync(int sessionId)
    {
        var ses = await db.GetSessionExercisesAsync(sessionId);
        var result = new List<LoggedSet>();
        foreach (var se in ses)
        {
            var sets = await db.GetSetsForSessionExerciseAsync(se.Id);
            result.AddRange(sets);
        }
        return result;
    }

    [RelayCommand]
    private async Task DoneAsync() =>
        await Shell.Current.GoToAsync("//TrainPage");

    private static string MuscleGroupName(MuscleGroup mg) => mg switch
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

public class MuscleGroupRow
{
    public string Name { get; set; } = "";
    public decimal Volume { get; set; }
    public int Sets { get; set; }
    public double ProgressFraction { get; set; }
    public string VolumeDisplay => $"{Volume:F0} kg";
    public string SetsDisplay => $"{Sets} set";
}

public class PRRow
{
    public string ExerciseName { get; set; } = "";
    public string Display { get; set; } = "";
    public string Epley1RM { get; set; } = "";
}
