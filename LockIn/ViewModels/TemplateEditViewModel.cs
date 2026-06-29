using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
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
                Exercises.Add(TemplateExerciseRow.FromTemplateExercise(te, exercise?.Name ?? AppResources.TemplateEdit_UnknownExercise));
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
    private void ClearExercise(TemplateExerciseRow row)
    {
        row.SetsText = "3";
        row.RepsText = "";
        row.WeightText = "";
        row.ProgressionEnabled = false;
        row.TargetRepsMinText = "";
        row.TargetRepsMaxText = "";
        row.WeightIncrementText = "2.5";
    }

    [RelayCommand]
    private async Task ChangeRestAsync(TemplateExerciseRow row)
    {
        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.TemplateEdit_RestTime_Title,
            string.Format(AppResources.TemplateEdit_RestTime_Body_Format, row.ExerciseName),
            initialValue: row.RestSeconds.ToString(),
            keyboard: Keyboard.Numeric);

        if (int.TryParse(result, out var secs) && secs > 0)
            row.RestSeconds = secs;
    }

    [RelayCommand]
    private async Task ToggleSupersetAsync(TemplateExerciseRow row)
    {
        var index = Exercises.IndexOf(row);
        if (index < 0) return;

        if (row.SupersetGroupId.HasValue)
        {
            var gid = row.SupersetGroupId.Value;
            foreach (var r in Exercises)
                if (r.SupersetGroupId == gid) r.SupersetGroupId = null;
        }
        else
        {
            if (index + 1 >= Exercises.Count)
            {
                await Toast.Make(AppResources.TemplateEdit_SupersetToast).Show();
                return;
            }
            var next = Exercises[index + 1];
            var gid = next.SupersetGroupId ?? (Exercises.Max(r => r.SupersetGroupId ?? 0) + 1);
            row.SupersetGroupId = gid;
            next.SupersetGroupId = gid;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            await Toast.Make(AppResources.TemplateEdit_Toast_EnterName).Show();
            return;
        }

        _template.Name = TemplateName.Trim();
        await db.SaveTemplateAsync(_template);

        // Bygg listan av exercises att spara innan transaktionen
        var toSave = Exercises.Select((row, i) =>
        {
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
            te.AutoProgressMode = row.ProgressionEnabled ? 1 : 0;
            te.TargetRepsMin = row.ProgressionEnabled && int.TryParse(row.TargetRepsMinText, out var rMin) ? rMin : 0;
            te.TargetRepsMax = row.ProgressionEnabled && int.TryParse(row.TargetRepsMaxText, out var rMax) ? rMax : 0;
            te.WeightIncrementKg = row.ProgressionEnabled && decimal.TryParse(row.WeightIncrementText.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var inc) ? inc : 2.5m;
            te.SupersetGroupId = row.SupersetGroupId;
            return te;
        }).ToList();

        await db.ReplaceTemplateExercisesAsync(_template.Id, toSave);

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

    // Auto-progression
    [ObservableProperty] private bool _progressionEnabled;
    [ObservableProperty] private string _targetRepsMinText = "";
    [ObservableProperty] private string _targetRepsMaxText = "";
    [ObservableProperty] private string _weightIncrementText = "2.5";

    // Superset
    [ObservableProperty] private int? _supersetGroupId;
    public bool IsInSuperset => SupersetGroupId.HasValue;
    public string SupersetButtonText => SupersetGroupId.HasValue
        ? AppResources.TemplateEdit_SupersetRemove
        : AppResources.TemplateEdit_SupersetAdd;

    partial void OnSupersetGroupIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsInSuperset));
        OnPropertyChanged(nameof(SupersetButtonText));
    }

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
            RestSeconds = te.DefaultRestSeconds,
            ProgressionEnabled = te.AutoProgressMode > 0,
            TargetRepsMinText = te.TargetRepsMin > 0 ? te.TargetRepsMin.ToString() : "",
            TargetRepsMaxText = te.TargetRepsMax > 0 ? te.TargetRepsMax.ToString() : "",
            WeightIncrementText = te.WeightIncrementKg > 0 ? te.WeightIncrementKg.ToString("G") : "2.5",
            SupersetGroupId = te.SupersetGroupId,
        };
}
