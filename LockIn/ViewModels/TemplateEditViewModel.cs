using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class TemplateEditViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    private WorkoutTemplate _template = new();

    [ObservableProperty] private int _templateId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<TemplateExerciseRow> Exercises { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("TemplateId", out var val) && val is int id)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int templateId)
    {
        IsLoading = true;
        TemplateId = templateId;
        if (templateId == 0)
        {
            _template = new WorkoutTemplate { Name = "" };
            TemplateName = "";
        }
        else
        {
            var templates = await db.GetTemplatesAsync();
            _template = templates.FirstOrDefault(t => t.Id == templateId) ?? new WorkoutTemplate();
            TemplateName = _template.Name;

            var items = await db.GetTemplateExercisesAsync(templateId);
            Exercises.Clear();
            foreach (var te in items)
            {
                var exercise = await db.GetExerciseAsync(te.ExerciseId);
                Exercises.Add(TemplateExerciseRow.FromTemplateExercise(te, exercise?.Name ?? "Okänd övning"));
            }
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        await Shell.Current.GoToAsync(nameof(ExercisePickerPage), new Dictionary<string, object>
        {
            { "CallbackAction", new Action<Exercise>(OnExercisePicked) }
        });
    }

    private void OnExercisePicked(Exercise exercise)
    {
        var te = new TemplateExercise
        {
            ExerciseId = exercise.Id,
            OrderIndex = Exercises.Count,
            Sets = 3,
            Reps = 8,
            TargetWeight = 0,
            DefaultRestSeconds = exercise.DefaultRestSeconds
        };
        Exercises.Add(TemplateExerciseRow.FromTemplateExercise(te, exercise.Name));
    }

    [RelayCommand]
    private void RemoveExercise(TemplateExerciseRow row)
    {
        Exercises.Remove(row);
        for (int i = 0; i < Exercises.Count; i++)
            Exercises[i].TemplateExercise.OrderIndex = i;
    }

    [RelayCommand]
    private async Task ChangeRestAsync(TemplateExerciseRow row)
    {
        var result = await Shell.Current.DisplayPromptAsync(
            "Vilotid",
            $"Vila i sekunder för {row.ExerciseName}:",
            initialValue: row.RestSeconds.ToString(),
            keyboard: Keyboard.Numeric);

        if (int.TryParse(result, out var secs) && secs > 0)
            row.RestSeconds = secs;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            await Toast.Make("Ange ett namn för mallen.").Show();
            return;
        }

        _template.Name = TemplateName.Trim();
        await db.SaveTemplateAsync(_template);

        if (TemplateId != 0)
        {
            var existing = await db.GetTemplateExercisesAsync(_template.Id);
            foreach (var old in existing)
                await db.DeleteTemplateExerciseAsync(old);
        }

        for (int i = 0; i < Exercises.Count; i++)
        {
            var row = Exercises[i];
            var te = row.TemplateExercise;
            te.TemplateId = _template.Id;
            te.OrderIndex = i;
            te.Id = 0;
            te.Sets = int.TryParse(row.SetsText, out var s) && s > 0 ? s : 3;
            te.Reps = int.TryParse(row.RepsText, out var r) && r > 0 ? r : 8;
            te.TargetWeight = decimal.TryParse(row.WeightText.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var w) ? w : 0;
            te.DefaultRestSeconds = row.RestSeconds;
            await db.SaveTemplateExerciseAsync(te);
        }

        await Shell.Current.GoToAsync("..");
    }
}

public partial class TemplateExerciseRow : ObservableObject
{
    public TemplateExercise TemplateExercise { get; set; } = new();
    public int ExerciseId => TemplateExercise.ExerciseId;
    public string ExerciseName { get; set; } = "";

    [ObservableProperty] private string _setsText = "3";
    [ObservableProperty] private string _repsText = "8";
    [ObservableProperty] private string _weightText = "";
    [ObservableProperty] private int _restSeconds = 90;

    public string RestDisplay => RestTimerService.Format(RestSeconds);

    partial void OnRestSecondsChanged(int value) =>
        OnPropertyChanged(nameof(RestDisplay));

    public static TemplateExerciseRow FromTemplateExercise(TemplateExercise te, string name) =>
        new()
        {
            TemplateExercise = te,
            ExerciseName = name,
            SetsText = te.Sets.ToString(),
            RepsText = te.Reps.ToString(),
            WeightText = te.TargetWeight > 0 ? te.TargetWeight.ToString("G") : "",
            RestSeconds = te.DefaultRestSeconds
        };
}
