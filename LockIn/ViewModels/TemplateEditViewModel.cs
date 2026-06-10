using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

[QueryProperty(nameof(TemplateId), "TemplateId")]
public partial class TemplateEditViewModel(DatabaseService db) : ObservableObject
{
    private WorkoutTemplate? _template;

    [ObservableProperty] private int _templateId;
    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<TemplateExerciseRow> Exercises { get; } = new();

    partial void OnTemplateIdChanged(int value) => _ = LoadAsync(value);

    private async Task LoadAsync(int templateId)
    {
        IsLoading = true;
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
                Exercises.Add(new TemplateExerciseRow
                {
                    TemplateExercise = te,
                    ExerciseName = exercise?.Name ?? "Okänd övning"
                });
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
        Exercises.Add(new TemplateExerciseRow
        {
            TemplateExercise = te,
            ExerciseName = exercise.Name
        });
    }

    [RelayCommand]
    private void RemoveExercise(TemplateExerciseRow row)
    {
        Exercises.Remove(row);
        for (int i = 0; i < Exercises.Count; i++)
            Exercises[i].TemplateExercise.OrderIndex = i;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            await Shell.Current.DisplayAlert("Saknar namn", "Ange ett namn för mallen.", "OK");
            return;
        }

        _template!.Name = TemplateName.Trim();
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
            row.TemplateExercise.TemplateId = _template.Id;
            row.TemplateExercise.OrderIndex = i;
            row.TemplateExercise.Id = 0;
            await db.SaveTemplateExerciseAsync(row.TemplateExercise);
        }

        await Shell.Current.GoToAsync("..");
    }
}

public class TemplateExerciseRow
{
    public TemplateExercise TemplateExercise { get; set; } = new();
    public string ExerciseName { get; set; } = "";

    public string SetsRepsDisplay =>
        $"{TemplateExercise.Sets} × {TemplateExercise.Reps}" +
        (TemplateExercise.TargetWeight > 0 ? $"  @  {TemplateExercise.TargetWeight} kg" : "");

    public string RestDisplay => RestTimerService.Format(TemplateExercise.DefaultRestSeconds);
}
