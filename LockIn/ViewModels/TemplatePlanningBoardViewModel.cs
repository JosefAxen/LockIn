using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

/// <summary>
/// TemplatePlanningBoardViewModel — RP-clone Task 18.
/// Redigerar en WorkoutTemplate hierarkiskt: Days -> MuscleSlots -> Exercises.
/// Ersätter gamla TemplateEditViewModel.
/// </summary>
public partial class TemplatePlanningBoardViewModel : ObservableObject, IQueryAttributable
{
    private readonly ITemplateService _templates;
    private readonly IExerciseRecommendationService _recommendation;
    private readonly IExerciseRepository _exerciseRepo;

    private int? _templateId;

    public TemplatePlanningBoardViewModel(
        ITemplateService templates,
        IExerciseRecommendationService recommendation,
        IExerciseRepository exerciseRepo)
    {
        _templates = templates;
        _recommendation = recommendation;
        _exerciseRepo = exerciseRepo;
    }

    [ObservableProperty] private string _templateName = "";
    [ObservableProperty] private int _defaultDaysPerWeek = 4;
    [ObservableProperty] private TemplateDayViewModel? _selectedDay;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<TemplateDayViewModel> Days { get; } = new();

    // ── IQueryAttributable ────────────────────────────────────────────
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("templateId", out var v) && int.TryParse(v?.ToString(), out var id) && id > 0)
            _templateId = id;
        else
            _templateId = null;
        _ = LoadAsync();
    }

    // ── Load ──────────────────────────────────────────────────────────
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Days.Clear();

            if (_templateId is null)
            {
                TemplateName = "";
                for (int i = 0; i < 3; i++)
                {
                    Days.Add(new TemplateDayViewModel
                    {
                        DayIndex = i,
                        Label = $"Day {i + 1}",
                        DefaultDayOfWeek = null,
                    });
                }
                SelectedDay = Days.FirstOrDefault();
                DefaultDaysPerWeek = Days.Count;
                return;
            }

            var detail = await _templates.GetDetailAsync(_templateId.Value);
            if (detail is null) return;

            TemplateName = detail.Template.Name;

            var exerciseNameById = (await _exerciseRepo.GetAllAsync())
                .ToDictionary(e => e.Id, e => e.Name);

            foreach (var d in detail.Days.OrderBy(x => x.DayIndex))
            {
                var dayVm = new TemplateDayViewModel
                {
                    DayIndex = d.DayIndex,
                    Label = d.Label,
                    DefaultDayOfWeek = d.DefaultDayOfWeek,
                };

                if (detail.SlotsByDayId.TryGetValue(d.Id, out var slots))
                {
                    foreach (var s in slots.OrderBy(x => x.SortOrder))
                    {
                        var slotVm = new MuscleSlotViewModel
                        {
                            Muscle = s.MuscleGroup,
                            SortOrder = s.SortOrder,
                        };

                        if (detail.ExercisesBySlotId.TryGetValue(s.Id, out var exs))
                        {
                            foreach (var e in exs.OrderBy(x => x.SortOrder))
                            {
                                slotVm.Exercises.Add(new ExerciseRowViewModel
                                {
                                    ExerciseId = e.ExerciseId,
                                    ExerciseName = exerciseNameById.GetValueOrDefault(e.ExerciseId, "?"),
                                    SortOrder = e.SortOrder,
                                    TargetRepsMin = e.TargetRepsMin,
                                    TargetRepsMax = e.TargetRepsMax,
                                });
                            }
                        }

                        dayVm.Slots.Add(slotVm);
                    }
                }

                Days.Add(dayVm);
            }

            SelectedDay = Days.FirstOrDefault();
            DefaultDaysPerWeek = Days.Count;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectDay(TemplateDayViewModel? day)
    {
        if (day is null) return;
        SelectedDay = day;
    }

    [RelayCommand]
    private async Task AddDayAsync()
    {
        var idx = Days.Count;
        var d = new TemplateDayViewModel
        {
            DayIndex = idx,
            Label = $"Day {idx + 1}",
            DefaultDayOfWeek = null,
        };
        Days.Add(d);
        SelectedDay = d;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RemoveDayAsync(TemplateDayViewModel? day)
    {
        if (day is null) return;
        var body = string.Format(AppResources.TemplateBoard_RemoveDay_Confirm, day.Label);
        var ok = await Shell.Current.DisplayAlert(
            AppResources.Common_Confirm,
            body,
            AppResources.Common_Delete,
            AppResources.Common_Cancel);
        if (!ok) return;

        var wasSelected = SelectedDay == day;
        Days.Remove(day);

        // Re-index DayIndex så SaveAsync-nyckeln stämmer
        for (int i = 0; i < Days.Count; i++)
            Days[i].DayIndex = i;

        if (wasSelected) SelectedDay = Days.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddMuscleSlotAsync(TemplateDayViewModel? day)
    {
        day ??= SelectedDay;
        if (day is null) return;

        var options = _allMuscles.Select(m => m.ToLocalizedLabel()).ToArray();
        var choice = await Shell.Current.CurrentPage.DisplayActionSheet(
            AppResources.TemplateBoard_AddMuscle_Title,
            AppResources.Common_Cancel,
            null,
            options);
        if (string.IsNullOrEmpty(choice) || choice == AppResources.Common_Cancel) return;

        var idxInLabels = Array.IndexOf(options, choice);
        if (idxInLabels < 0) return;
        var muscle = _allMuscles[idxInLabels];

        var nextSort = day.Slots.Count == 0 ? 0 : day.Slots.Max(s => s.SortOrder) + 1;
        day.Slots.Add(new MuscleSlotViewModel
        {
            Muscle = muscle,
            SortOrder = nextSort,
        });
    }

    [RelayCommand]
    private async Task RemoveSlotAsync(MuscleSlotViewModel? slot)
    {
        if (slot is null || SelectedDay is null) return;
        var body = string.Format(
            AppResources.TemplateBoard_RemoveSlot_Confirm,
            slot.MuscleLabel,
            SelectedDay.Label);
        var ok = await Shell.Current.DisplayAlert(
            AppResources.Common_Confirm,
            body,
            AppResources.Common_Delete,
            AppResources.Common_Cancel);
        if (!ok) return;

        SelectedDay.Slots.Remove(slot);
    }

    [RelayCommand]
    private async Task AutoFillSlotAsync(MuscleSlotViewModel? slot)
    {
        if (slot is null) return;
        var existingIds = slot.Exercises.Select(e => e.ExerciseId).ToList();
        var recs = await _recommendation.RecommendForMuscleAsync(
            slot.Muscle,
            MusclePriorityLevel.Grow,
            existingIds,
            0, 0);

        foreach (var ex in recs.Take(2))
        {
            slot.Exercises.Add(new ExerciseRowViewModel
            {
                ExerciseId = ex.Id,
                ExerciseName = ex.Name,
                SortOrder = slot.Exercises.Count,
                TargetRepsMin = 8,
                TargetRepsMax = 12,
            });
        }
    }

    [RelayCommand]
    private async Task AddExerciseAsync(MuscleSlotViewModel? slot)
    {
        if (slot is null) return;
        // Task 22 implementerar exercisepicker med muscle-filter — stubb tills vidare.
        try
        {
            await Shell.Current.GoToAsync(AppRoutes.ExercisePickerForMuscle((int)slot.Muscle));
        }
        catch
        {
            // Route saknar ännu parameter-support — fallback: kör auto-fyll.
            await AutoFillSlotAsync(slot);
        }
    }

    [RelayCommand]
    private void RemoveExercise(ExerciseRowViewModel? row)
    {
        if (row is null || SelectedDay is null) return;
        foreach (var s in SelectedDay.Slots)
        {
            if (s.Exercises.Contains(row))
            {
                s.Exercises.Remove(row);
                Reindex(s);
                return;
            }
        }
    }

    [RelayCommand]
    private void MoveExerciseUp(ExerciseRowViewModel? row)
    {
        if (row is null || SelectedDay is null) return;
        foreach (var s in SelectedDay.Slots)
        {
            var idx = s.Exercises.IndexOf(row);
            if (idx <= 0) continue;
            s.Exercises.Move(idx, idx - 1);
            Reindex(s);
            return;
        }
    }

    [RelayCommand]
    private void MoveExerciseDown(ExerciseRowViewModel? row)
    {
        if (row is null || SelectedDay is null) return;
        foreach (var s in SelectedDay.Slots)
        {
            var idx = s.Exercises.IndexOf(row);
            if (idx < 0 || idx >= s.Exercises.Count - 1) continue;
            s.Exercises.Move(idx, idx + 1);
            Reindex(s);
            return;
        }
    }

    private static void Reindex(MuscleSlotViewModel slot)
    {
        for (int i = 0; i < slot.Exercises.Count; i++)
            slot.Exercises[i].SortOrder = i;
    }

    [RelayCommand]
    private async Task AutoFillAllEmptyAsync()
    {
        foreach (var d in Days)
        {
            foreach (var s in d.Slots)
            {
                if (s.Exercises.Count == 0)
                    await AutoFillSlotAsync(s);
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // Re-indexa dagar innan seralisering så DayIndex är 0..n-1
            for (int i = 0; i < Days.Count; i++)
                Days[i].DayIndex = i;

            var template = new WorkoutTemplate
            {
                Id = _templateId ?? 0,
                Name = string.IsNullOrWhiteSpace(TemplateName) ? "Namnlös template" : TemplateName.Trim(),
                IsCustom = true,
                DefaultDaysPerWeek = Days.Count,
                CreatedAt = DateTime.Now,
            };

            var days = Days.Select(d => new TemplateDay
            {
                DayIndex = d.DayIndex,
                Label = d.Label,
                DefaultDayOfWeek = d.DefaultDayOfWeek,
            }).ToList();

            var slotsByDayIndex = new Dictionary<int, List<TemplateMuscleSlot>>();
            var exercisesBySlot = new Dictionary<(int, int), List<TemplateExercise>>();

            foreach (var d in Days)
            {
                // Normalisera SortOrder på slots så nyckeln är förutsägbar
                var slotsList = d.Slots.Select((s, i) => new { Vm = s, Order = i }).ToList();
                foreach (var pair in slotsList) pair.Vm.SortOrder = pair.Order;

                slotsByDayIndex[d.DayIndex] = slotsList.Select(pair => new TemplateMuscleSlot
                {
                    MuscleGroupValue = (int)pair.Vm.Muscle,
                    SortOrder = pair.Order,
                }).ToList();

                foreach (var pair in slotsList)
                {
                    var s = pair.Vm;
                    exercisesBySlot[(d.DayIndex, pair.Order)] = s.Exercises.Select((e, i) => new TemplateExercise
                    {
                        ExerciseId = e.ExerciseId,
                        SortOrder = i,
                        TargetRepsMin = e.TargetRepsMin,
                        TargetRepsMax = e.TargetRepsMax,
                        DefaultRestSeconds = 120,
                    }).ToList();
                }
            }

            await _templates.SaveAsync(template, days, slotsByDayIndex, exercisesBySlot);
            await Toast.Make(AppResources.Template_Saved_Toast).Show();
            await Shell.Current.GoToAsync(AppRoutes.Back);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync(AppRoutes.Back);

    // ── Muscle label ──────────────────────────────────────────────────
    private static readonly MuscleGroup[] _allMuscles = new[]
    {
        MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
        MuscleGroup.Biceps, MuscleGroup.Triceps, MuscleGroup.Forearms,
        MuscleGroup.Legs, MuscleGroup.Quads, MuscleGroup.Hamstrings,
        MuscleGroup.Glutes, MuscleGroup.Calves, MuscleGroup.Core,
        MuscleGroup.Abs, MuscleGroup.Traps, MuscleGroup.FullBody,
        MuscleGroup.Other,
    };

}

// ── Nested VMs (deklareras i samma fil för att hålla ihop scope) ─────
public partial class TemplateDayViewModel : ObservableObject
{
    public int DayIndex { get; set; }
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private int? _defaultDayOfWeek;
    public ObservableCollection<MuscleSlotViewModel> Slots { get; } = new();
}

public partial class MuscleSlotViewModel : ObservableObject
{
    public MuscleGroup Muscle { get; set; }
    public int SortOrder { get; set; }
    public string MuscleLabel => Muscle.ToLocalizedLabel();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriorityTag))]
    private MusclePriorityLevel _priority = MusclePriorityLevel.Grow;

    public string PriorityTag => Priority.ToRomanNumeral();

    public ObservableCollection<ExerciseRowViewModel> Exercises { get; } = new();
}

public partial class ExerciseRowViewModel : ObservableObject
{
    public int ExerciseId { get; set; }
    [ObservableProperty] private string _exerciseName = "";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private int _targetRepsMin = 8;
    [ObservableProperty] private int _targetRepsMax = 12;

    public string RepsDisplay => $"{TargetRepsMin}-{TargetRepsMax}";

    partial void OnTargetRepsMinChanged(int value) => OnPropertyChanged(nameof(RepsDisplay));
    partial void OnTargetRepsMaxChanged(int value) => OnPropertyChanged(nameof(RepsDisplay));
}
