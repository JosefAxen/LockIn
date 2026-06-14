using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
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

public partial class CreateExerciseViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly List<MuscleGroupOption> _muscleGroups;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _restSeconds = 120;
    [ObservableProperty] private string _notes = "";

    public bool IsNameValid => !string.IsNullOrWhiteSpace(Name);
    public IReadOnlyList<MuscleGroupOption> MuscleGroups => _muscleGroups;

    public CreateExerciseViewModel(DatabaseService db)
    {
        _db = db;
        _muscleGroups =
        [
            new("BRÖST",    MuscleGroup.Chest,     Color.FromArgb("#FB7185")),
            new("RYGG",     MuscleGroup.Back,      Color.FromArgb("#38BDF8")),
            new("AXLAR",    MuscleGroup.Shoulders, Color.FromArgb("#A78BFA")),
            new("BICEPS",   MuscleGroup.Biceps,    Color.FromArgb("#4ADE80")),
            new("TRICEPS",  MuscleGroup.Triceps,   Color.FromArgb("#FBBF24")),
            new("BEN",      MuscleGroup.Legs,      Color.FromArgb("#F97316")),
            new("CORE",     MuscleGroup.Core,      Color.FromArgb("#EC4899")),
            new("HELKROPP", MuscleGroup.FullBody,  Color.FromArgb("#EF4444")),
        ];
        _muscleGroups[0].IsSelected = true;
    }

    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(IsNameValid));

    [RelayCommand]
    private void SelectMuscle(MuscleGroupOption option)
    {
        foreach (var opt in _muscleGroups) opt.IsSelected = false;
        option.IsSelected = true;
    }

    [RelayCommand]
    private void IncreaseRest() => RestSeconds = Math.Min(300, RestSeconds + 15);

    [RelayCommand]
    private void DecreaseRest() => RestSeconds = Math.Max(30, RestSeconds - 15);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!IsNameValid) return;

        var selectedGroup = _muscleGroups.FirstOrDefault(m => m.IsSelected)?.Group ?? MuscleGroup.Other;
        var exercise = new Exercise
        {
            Name = Name.Trim(),
            IsCustom = true,
            MuscleGroup = selectedGroup,
            DefaultRestSeconds = RestSeconds,
            Notes = Notes.Trim()
        };
        await _db.SaveExerciseAsync(exercise);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private static async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
