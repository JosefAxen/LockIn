using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Models;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class ProgramDetailViewModel(DatabaseService db) : ObservableObject, IQueryAttributable
{
    private WorkoutProgram? _program;

    [ObservableProperty] private string _programName = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _daysLabel = "";
    [ObservableProperty] private bool _isActivating;

    public List<ProgramDay> Days { get; private set; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ProgramId", out var val) && val is string id)
        {
            _program = WorkoutPrograms.All.FirstOrDefault(p => p.Id == id);
            if (_program is not null)
            {
                ProgramName = _program.Name;
                Description = _program.Description;
                DaysLabel = $"{_program.DaysPerWeek} dagar/vecka · {_program.Days.Count} pass";
                Days = _program.Days;
                OnPropertyChanged(nameof(Days));
            }
        }
    }

    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (_program is null) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Aktivera program",
            $"Skapar {_program.Days.Count} mallar från \"{_program.Name}\". Dessa läggs till i dina mallar.",
            "Aktivera", "Avbryt");
        if (!confirmed) return;

        IsActivating = true;

        var allExercises = await db.GetExercisesAsync();
        var exerciseMap = allExercises.ToDictionary(
            e => e.Name.ToLowerInvariant(), e => e);

        foreach (var day in _program.Days)
        {
            var template = new WorkoutTemplate { Name = day.Label, ProgramId = _program.Id };
            await db.SaveTemplateAsync(template);

            for (int i = 0; i < day.Exercises.Count; i++)
            {
                var pe = day.Exercises[i];
                var key = pe.ExerciseName.ToLowerInvariant();

                if (!exerciseMap.TryGetValue(key, out var exercise))
                {
                    exercise = new Exercise
                    {
                        Name = pe.ExerciseName,
                        IsCustom = true,
                        DefaultRestSeconds = pe.RestSeconds,
                        MuscleGroup = MuscleGroup.Other
                    };
                    await db.SaveExerciseAsync(exercise);
                    exerciseMap[key] = exercise;
                }

                var te = new TemplateExercise
                {
                    TemplateId = template.Id,
                    ExerciseId = exercise.Id,
                    OrderIndex = i,
                    Sets = pe.Sets,
                    Reps = pe.Reps,
                    DefaultRestSeconds = pe.RestSeconds
                };
                await db.SaveTemplateExerciseAsync(te);
            }
        }

        IsActivating = false;

        await Toast.Make($"{_program.Days.Count} mallar skapade! Hitta dem under Mallar i Bibliotek.", ToastDuration.Long).Show();

        await Shell.Current.GoToAsync("..");
    }
}
