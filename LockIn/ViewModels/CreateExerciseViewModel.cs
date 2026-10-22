using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class MuscleGroupOption : ObservableObject
{
    private static readonly Color s_dimBorder = Color.FromArgb("#323240");

    public string Label { get; }
    public MuscleGroup Group { get; }
    public Color AccentColor { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Color _backgroundColor = Colors.Transparent;
    [ObservableProperty] private Color _borderColor;

    public MuscleGroupOption(string label, MuscleGroup group, Color accentColor)
    {
        Label = label;
        Group = group;
        AccentColor = accentColor;
        _borderColor = s_dimBorder;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        BackgroundColor = value ? AccentColor.WithAlpha(0.15f) : Colors.Transparent;
        BorderColor = value ? AccentColor : s_dimBorder;
    }
}

public partial class EquipmentOption : ObservableObject
{
    private static readonly Color s_dim    = Color.FromArgb("#323240");

    public string Label { get; }
    public EquipmentType Equipment { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private Color _borderColor = s_dim;
    // Ovald text: TextSecondary (#A2A2A2, 7:1) — tidigare #71717A gav 3,7:1
    [ObservableProperty] private Color _textColor = DesignTokens.TextSecondary;

    public EquipmentOption(string label, EquipmentType equipment)
    {
        Label = label;
        Equipment = equipment;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        BorderColor = value ? DesignTokens.Accent : s_dim;
        TextColor   = value ? DesignTokens.Text : DesignTokens.TextSecondary;
    }
}

[QueryProperty(nameof(CallbackAction), "CallbackAction")]
public partial class CreateExerciseViewModel : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepo;
    private readonly List<MuscleGroupOption> _muscleGroups;
    private readonly List<EquipmentOption> _equipmentOptions;
    private bool _isSaving;

    /// <summary>
    /// Övningsväljarens callback när sidan öppnas därifrån. Den nya övningen går samma väg
    /// som ett val i listan — läggs till i passet, eller ersätter vid "Byt övning".
    /// </summary>
    public Action<Exercise>? CallbackAction { get; set; }

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _restSeconds = 120;
    [ObservableProperty] private string _notes = "";

    public bool IsNameValid => !string.IsNullOrWhiteSpace(Name);
    public IReadOnlyList<MuscleGroupOption> MuscleGroups => _muscleGroups;
    public IReadOnlyList<EquipmentOption> EquipmentOptions => _equipmentOptions;

    public CreateExerciseViewModel(IExerciseRepository exerciseRepo)
    {
        _exerciseRepo = exerciseRepo;

        // Samma färger som i passet och övningsväljaren (DesignTokens.MuscleColor)
        _muscleGroups =
        [
            new(AppResources.Library_Muscle_Chest,     MuscleGroup.Chest,     DesignTokens.MuscleColor(MuscleGroup.Chest)),
            new(AppResources.Library_Muscle_Back,      MuscleGroup.Back,      DesignTokens.MuscleColor(MuscleGroup.Back)),
            new(AppResources.Library_Muscle_Shoulders, MuscleGroup.Shoulders, DesignTokens.MuscleColor(MuscleGroup.Shoulders)),
            new(AppResources.Library_Muscle_Biceps,    MuscleGroup.Biceps,    DesignTokens.MuscleColor(MuscleGroup.Biceps)),
            new(AppResources.Library_Muscle_Triceps,   MuscleGroup.Triceps,   DesignTokens.MuscleColor(MuscleGroup.Triceps)),
            new(AppResources.Library_Muscle_Forearms,  MuscleGroup.Forearms,  DesignTokens.MuscleColor(MuscleGroup.Forearms)),
            new(AppResources.Library_Muscle_Legs,      MuscleGroup.Legs,      DesignTokens.MuscleColor(MuscleGroup.Legs)),
            new(AppResources.Library_Muscle_Core,      MuscleGroup.Core,      DesignTokens.MuscleColor(MuscleGroup.Core)),
            new(AppResources.Library_Muscle_FullBody,  MuscleGroup.FullBody,  DesignTokens.MuscleColor(MuscleGroup.FullBody)),
            new(AppResources.Library_Muscle_Other,     MuscleGroup.Other,     DesignTokens.MuscleColor(MuscleGroup.Other)),
        ];
        _muscleGroups[0].IsSelected = true;

        _equipmentOptions =
        [
            new(AppResources.Library_Equipment_Barbell,    EquipmentType.Barbell),
            new(AppResources.Library_Equipment_Dumbbell,   EquipmentType.Dumbbell),
            new(AppResources.Library_Equipment_Cable,      EquipmentType.Cable),
            new(AppResources.Library_Equipment_Machine,    EquipmentType.Machine),
            new(AppResources.Library_Equipment_Bodyweight, EquipmentType.BodyOnly),
            new(AppResources.Library_Equipment_EZBar,      EquipmentType.EZBar),
            new(AppResources.Library_Equipment_Kettlebell, EquipmentType.Kettlebell),
            new(AppResources.Library_Equipment_Bands,      EquipmentType.Bands),
            new(AppResources.Library_Equipment_Other,      EquipmentType.Other),
        ];
        _equipmentOptions[0].IsSelected = true;
    }

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(IsNameValid));

    [RelayCommand]
    private void SelectMuscle(MuscleGroupOption option)
    {
        foreach (var opt in _muscleGroups) opt.IsSelected = false;
        option.IsSelected = true;
    }

    [RelayCommand]
    private void SelectEquipment(EquipmentOption option)
    {
        foreach (var opt in _equipmentOptions) opt.IsSelected = false;
        option.IsSelected = true;
    }

    [RelayCommand]
    private void IncreaseRest() => RestSeconds = Math.Min(300, RestSeconds + 15);

    [RelayCommand]
    private void DecreaseRest() => RestSeconds = Math.Max(30, RestSeconds - 15);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsNameValid || _isSaving) return;
        _isSaving = true;
        try
        {
            var selectedGroup     = _muscleGroups.FirstOrDefault(m => m.IsSelected)?.Group ?? MuscleGroup.Other;
            var selectedEquipment = _equipmentOptions.FirstOrDefault(e => e.IsSelected)?.Equipment ?? EquipmentType.Other;

            var exercise = new Exercise
            {
                Name              = Name.Trim(),
                IsCustom          = true,
                MuscleGroup       = selectedGroup,
                Equipment         = selectedEquipment,
                DefaultRestSeconds = RestSeconds,
                Notes             = Notes.Trim()
            };
            await _exerciseRepo.SaveAsync(exercise);

            if (CallbackAction is { } callback)
            {
                // Öppnad från övningsväljaren: gör samma sak som ett val där och hoppa förbi väljaren
                callback(exercise);
                await Shell.Current.GoToAsync(AppRoutes.BackTwoLevels);
            }
            else
            {
                await Shell.Current.GoToAsync(AppRoutes.Back);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateExercise] Save: {ex}");
            try { await Shell.Current.DisplayAlertAsync("", AppResources.Common_ErrorRetry, AppResources.Common_OK); }
            catch { /* felvisningen får inte själv fälla sidan */ }
        }
        finally
        {
            _isSaving = false;
        }
    }

    [RelayCommand]
    private static async Task CancelAsync()
    {
        try { await Shell.Current.GoToAsync(AppRoutes.Back); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[CreateExercise] Cancel: {ex}"); }
    }
}
