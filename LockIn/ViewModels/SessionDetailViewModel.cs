using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Services;
using System.Collections.ObjectModel;
using static LockIn.Services.DatabaseService;

namespace LockIn.ViewModels;

public partial class SessionDetailViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    public ObservableCollection<SessionExerciseGroup> ExerciseGroups { get; } = new();

    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private string _dateDisplay = "";
    [ObservableProperty] private string _durationDisplay = "";
    [ObservableProperty] private string _volumeDisplay = "";
    [ObservableProperty] private int _prCountValue;
    [ObservableProperty] private string _sessionNotes = "";
    [ObservableProperty] private bool _hasNotes;
    [ObservableProperty] private bool _isLoading;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Session", out var val) && val is SessionSummaryRow session)
            _ = LoadAsync(session);
    }

    private async Task LoadAsync(SessionSummaryRow session)
    {
        IsLoading = true;
        TemplateName = session.TemplateName;
        DateDisplay = session.StartedAt.ToString("d MMM yyyy");
        PrCountValue = session.PRCount;
        VolumeDisplay = $"{session.TotalVolume:F0} kg";
        SessionNotes = session.Notes ?? "";
        HasNotes = !string.IsNullOrWhiteSpace(session.Notes);

        if (session.CompletedAt.HasValue)
        {
            var dur = session.CompletedAt.Value - session.StartedAt;
            DurationDisplay = dur.TotalHours >= 1
                ? $"{(int)dur.TotalHours}t {dur.Minutes}m"
                : $"{(int)dur.TotalMinutes}m";
        }

        var rows = await db.GetSessionExerciseDetailsAsync(session.Id);
        ExerciseGroups.Clear();
        foreach (var group in rows.GroupBy(r => r.ExerciseName))
        {
            var g = new SessionExerciseGroup(group.Key);
            foreach (var set in group)
                g.Add(set);
            ExerciseGroups.Add(g);
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}

public class SessionExerciseGroup(string exerciseName) : ObservableCollection<SessionExerciseDetailRow>
{
    public string ExerciseName { get; } = exerciseName;
}
