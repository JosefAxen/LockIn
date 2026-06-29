using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class ExerciseProgressViewModel(DatabaseService db, ProgressReportService report) : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private string _exerciseName = "";
    [ObservableProperty] private string _muscleGroupName = "";
    [ObservableProperty] private string _bestSet = "–";
    [ObservableProperty] private string _estimatedOneRmFormatted = "–";
    [ObservableProperty] private string _totalSessions = "";
    [ObservableProperty] private string _exerciseNotes = "";
    [ObservableProperty] private string _exerciseDescription = "";
    [ObservableProperty] private bool _hasDescription;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private IReadOnlyList<ChartPoint> _chartPoints = [];
    [ObservableProperty] private string _equipmentName = "";
    [ObservableProperty] private string _secondaryMusclesText = "";
    [ObservableProperty] private string _levelName = "";
    [ObservableProperty] private string _mechanicName = "";
    [ObservableProperty] private string _forceName = "";
    [ObservableProperty] private bool _hasMetadata;
    [ObservableProperty] private bool _isSharing;

    private int _sessionCount;
    private Exercise? _exercise;

    partial void OnIsSharingChanged(bool value)     => ShareProgressCommand.NotifyCanExecuteChanged();
    partial void OnChartPointsChanged(IReadOnlyList<ChartPoint> value) => ShareProgressCommand.NotifyCanExecuteChanged();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ExerciseId", out var val) && val is int id)
            _ = LoadAsync(id);
    }

    private async Task LoadAsync(int exerciseId)
    {
        IsLoading = true;

        _exercise = await db.GetExerciseAsync(exerciseId);
        if (_exercise is not null)
        {
            ExerciseName        = _exercise.Name;
            MuscleGroupName     = MuscleGroupLabel(_exercise.MuscleGroup);
            ExerciseNotes       = _exercise.Notes ?? "";
            ExerciseDescription = _exercise.Description ?? "";
            HasDescription      = !string.IsNullOrWhiteSpace(ExerciseDescription);
            EquipmentName       = EquipmentLabel(_exercise.Equipment);
            SecondaryMusclesText = _exercise.SecondaryMuscles ?? "";
            LevelName           = LevelLabel(_exercise.Level);
            MechanicName        = MechanicLabel(_exercise.Mechanic);
            ForceName           = ForceLabel(_exercise.Force);
            HasMetadata         = _exercise.Equipment != EquipmentType.Other
                                  || (_exercise.SecondaryMuscles ?? "").Length > 0
                                  || _exercise.Level != ExerciseLevel.Beginner
                                  || _exercise.Mechanic != ExerciseMechanic.Other
                                  || _exercise.Force != ExerciseForce.Other;
        }

        var history = await db.GetBestSetPerSessionForExerciseAsync(exerciseId);
        HasData = history.Count > 0;
        _sessionCount = history.Count;

        if (HasData)
        {
            var best = history.OrderByDescending(h => h.Epley1RM).First();
            BestSet                 = $"{best.WeightKg} kg × {best.Reps} reps";
            EstimatedOneRmFormatted = $"{AppResources.ExerciseProgress_Est1RM_Prefix}{best.Epley1RM:F0} kg";
            TotalSessions           = string.Format(AppResources.ExerciseProgress_Sessions_Format, history.Count);

            ChartPoints = history
                .TakeLast(12)
                .Select(h => new ChartPoint(h.Date, h.Epley1RM))
                .ToList();
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveNotesAsync()
    {
        if (_exercise is null) return;
        _exercise.Notes = ExerciseNotes;
        await db.SaveExerciseAsync(_exercise);
    }

    private bool CanShareProgress() => !IsSharing && ChartPoints.Count > 0;

    [RelayCommand(CanExecute = nameof(CanShareProgress))]
    private async Task ShareProgressAsync()
    {
        if (IsSharing) return;
        IsSharing = true;
        try
        {
            var data = new ProgressReportData(
                ExerciseName,
                MuscleGroupName,
                ChartPoints,
                _sessionCount);

            var path = await report.CreateReportImageAsync(data);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.ExerciseProgress_Share_ImageTitle,
                File  = new ShareFile(path, "image/png")
            });
        }
        catch
        {
            await Shell.Current.DisplayAlert(
                null,
                AppResources.ExerciseProgress_Share_Error,
                AppResources.Common_OK);
        }
        finally
        {
            IsSharing = false;
        }
    }

    private static string EquipmentLabel(EquipmentType e) => e switch
    {
        EquipmentType.Barbell      => AppResources.Library_Equipment_Barbell,
        EquipmentType.Dumbbell     => AppResources.Library_Equipment_Dumbbell,
        EquipmentType.Cable        => AppResources.Library_Equipment_Cable,
        EquipmentType.Machine      => AppResources.Library_Equipment_Machine,
        EquipmentType.BodyOnly     => AppResources.Library_Equipment_Bodyweight,
        EquipmentType.EZBar        => AppResources.Library_Equipment_EZBar,
        EquipmentType.Kettlebell   => AppResources.Library_Equipment_Kettlebell,
        EquipmentType.Bands        => AppResources.Library_Equipment_Bands,
        EquipmentType.FoamRoll     => AppResources.Library_Equipment_FoamRoll,
        EquipmentType.MedicineBall => AppResources.Library_Equipment_MedicineBall,
        _                          => ""
    };

    private static string LevelLabel(ExerciseLevel l) => l switch
    {
        ExerciseLevel.Beginner     => AppResources.ExerciseProgress_Level_Beginner,
        ExerciseLevel.Intermediate => AppResources.ExerciseProgress_Level_Intermediate,
        ExerciseLevel.Expert       => AppResources.ExerciseProgress_Level_Expert,
        _                          => ""
    };

    private static string MechanicLabel(ExerciseMechanic m) => m switch
    {
        ExerciseMechanic.Compound  => AppResources.ExerciseProgress_Mechanic_Compound,
        ExerciseMechanic.Isolation => AppResources.ExerciseProgress_Mechanic_Isolation,
        _                          => ""
    };

    private static string ForceLabel(ExerciseForce f) => f switch
    {
        ExerciseForce.Push   => AppResources.ExerciseProgress_ForceType_Push,
        ExerciseForce.Pull   => AppResources.ExerciseProgress_ForceType_Pull,
        ExerciseForce.Static => AppResources.ExerciseProgress_ForceType_Static,
        _                    => ""
    };

    private static string MuscleGroupLabel(MuscleGroup mg) => mg switch
    {
        MuscleGroup.Chest     => AppResources.Library_Muscle_Chest,
        MuscleGroup.Back      => AppResources.Library_Muscle_Back,
        MuscleGroup.Shoulders => AppResources.Library_Muscle_Shoulders,
        MuscleGroup.Biceps    => AppResources.Library_Muscle_Biceps,
        MuscleGroup.Triceps   => AppResources.Library_Muscle_Triceps,
        MuscleGroup.Legs      => AppResources.Library_Muscle_Legs,
        MuscleGroup.Core      => AppResources.Library_Muscle_Core,
        MuscleGroup.FullBody  => AppResources.Library_Muscle_FullBody,
        _                     => AppResources.Library_Muscle_Other
    };
}
