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

public partial class MesoBuilderViewModel : ObservableObject
{
    private readonly IMesoBuilderService _builder;
    private readonly DatabaseService _db;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IMesocycleService _mesocycles;

    public MesoBuilderViewModel(IMesoBuilderService builder, DatabaseService db, ISettingsRepository settingsRepo, IMesocycleService mesocycles)
    {
        _builder = builder;
        _db = db;
        _settingsRepo = settingsRepo;
        _mesocycles = mesocycles;
    }

    [ObservableProperty] private int _step = 0;
    [ObservableProperty] private int _daysPerWeek = 4;
    [ObservableProperty] private string _mesocycleName = string.Empty;
    [ObservableProperty] private int _weekCount = 5;
    [ObservableProperty] private UnitSystem _unit = UnitSystem.Kg;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<MusclePriorityRow> Emphasize { get; } = new();
    public ObservableCollection<MusclePriorityRow> Grow { get; } = new();
    public ObservableCollection<MusclePriorityRow> Maintain { get; } = new();

    public bool IsStep0 => Step == 0;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;

    public bool CanContinueStep0 => DaysPerWeek >= 2 && DaysPerWeek <= 6;
    public bool CanContinueStep1 => true;
    public bool CanCreate => !string.IsNullOrWhiteSpace(MesocycleName) && WeekCount >= 3 && !IsBusy;
    // Förklarar varför SKAPA CYKEL är inaktiv
    public bool ShowNameRequiredHint => string.IsNullOrWhiteSpace(MesocycleName);

    public bool IsDays2 => DaysPerWeek == 2;
    public bool IsDays3 => DaysPerWeek == 3;
    public bool IsDays4 => DaysPerWeek == 4;
    public bool IsDays5 => DaysPerWeek == 5;
    public bool IsDays6 => DaysPerWeek == 6;

    public bool IsWeeks4 => WeekCount == 4;
    public bool IsWeeks5 => WeekCount == 5;
    public bool IsWeeks6 => WeekCount == 6;
    public bool IsWeeks7 => WeekCount == 7;
    public bool IsWeeks8 => WeekCount == 8;

    public bool IsUnitKg => Unit == UnitSystem.Kg;
    public bool IsUnitLb => Unit == UnitSystem.Lb;

    public string ProgressLabel => string.Format(AppResources.MesoBuilder_Progress_Format, Step + 1, 3);

    partial void OnStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(ProgressLabel));
    }

    partial void OnDaysPerWeekChanged(int value)
    {
        OnPropertyChanged(nameof(IsDays2));
        OnPropertyChanged(nameof(IsDays3));
        OnPropertyChanged(nameof(IsDays4));
        OnPropertyChanged(nameof(IsDays5));
        OnPropertyChanged(nameof(IsDays6));
        OnPropertyChanged(nameof(CanContinueStep0));
    }

    partial void OnWeekCountChanged(int value)
    {
        OnPropertyChanged(nameof(IsWeeks4));
        OnPropertyChanged(nameof(IsWeeks5));
        OnPropertyChanged(nameof(IsWeeks6));
        OnPropertyChanged(nameof(IsWeeks7));
        OnPropertyChanged(nameof(IsWeeks8));
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnUnitChanged(UnitSystem value)
    {
        OnPropertyChanged(nameof(IsUnitKg));
        OnPropertyChanged(nameof(IsUnitLb));
    }

    partial void OnMesocycleNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(ShowNameRequiredHint));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
    }

    [RelayCommand]
    private void SelectDays(string daysStr)
    {
        if (int.TryParse(daysStr, out var d) && d >= 2 && d <= 6)
            DaysPerWeek = d;
    }

    [RelayCommand]
    private void ContinueToPriorities()
    {
        if (Emphasize.Count == 0 && Grow.Count == 0 && Maintain.Count == 0)
            InitializePriorityLists();
        Step = 1;
    }

    [RelayCommand]
    private void ContinueToFinalize()
    {
        Step = 2;
    }

    [RelayCommand]
    private void BackToDays() => Step = 0;

    [RelayCommand]
    private void BackToPriorities() => Step = 1;

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync(AppRoutes.Back);
    }

    [RelayCommand]
    private void MoveToEmphasize(MusclePriorityRow row)
    {
        if (row is null) return;
        RemoveFromAll(row);
        row.Priority = MusclePriorityLevel.Emphasize;
        Emphasize.Add(row);
    }

    [RelayCommand]
    private void MoveToGrow(MusclePriorityRow row)
    {
        if (row is null) return;
        RemoveFromAll(row);
        row.Priority = MusclePriorityLevel.Grow;
        Grow.Add(row);
    }

    [RelayCommand]
    private void MoveToMaintain(MusclePriorityRow row)
    {
        if (row is null) return;
        RemoveFromAll(row);
        row.Priority = MusclePriorityLevel.Maintain;
        Maintain.Add(row);
    }

    private void RemoveFromAll(MusclePriorityRow row)
    {
        Emphasize.Remove(row);
        Grow.Remove(row);
        Maintain.Remove(row);
    }

    [RelayCommand]
    private void SetWeeks(string weeksStr)
    {
        if (int.TryParse(weeksStr, out var w) && w >= 3 && w <= 12)
            WeekCount = w;
    }

    [RelayCommand]
    private void SetUnitKg() => Unit = UnitSystem.Kg;

    [RelayCommand]
    private void SetUnitLb() => Unit = UnitSystem.Lb;

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!CanCreate) return;

        IsBusy = true;
        try
        {
            // En ny cykel aktiveras och avaktiverar den aktiva — fråga först.
            if (!await ConfirmReplaceActiveAsync(_mesocycles)) return;

            var priorities = new Dictionary<MuscleGroup, MusclePriorityLevel>();
            foreach (var row in Emphasize) priorities[row.Muscle] = MusclePriorityLevel.Emphasize;
            foreach (var row in Grow) priorities[row.Muscle] = MusclePriorityLevel.Grow;
            foreach (var row in Maintain) priorities[row.Muscle] = MusclePriorityLevel.Maintain;

            var settings = await _settingsRepo.GetAsync();
            var equipment = settings.ParsePreferredEquipment();

            var spec = new MesoBuilderSpec(
                MesocycleName: MesocycleName.Trim(),
                WeekCount: WeekCount,
                UnitSystem: Unit,
                DaysPerWeek: DaysPerWeek,
                Priorities: priorities,
                PreferredEquipment: equipment,
                StartDate: DateTime.Today);

            await _builder.BuildAsync(spec);
            await Toast.Make(AppResources.MesoBuilder_Created_Toast).Show();
            await Shell.Current.GoToAsync(AppRoutes.Back);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MesoBuilder] Kunde inte skapa cykel: {ex}");
            await Toast.Make(AppResources.Common_ErrorRetry).Show();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// MesoBuilderServiceV2.StartFromTemplateAsync aktiverar den nya cykeln och avaktiverar
    /// den som är aktiv. Returnerar true om ingen cykel är aktiv eller om användaren godkänner.
    /// Delas med TemplatePickerViewModel.
    /// </summary>
    internal static async Task<bool> ConfirmReplaceActiveAsync(IMesocycleService mesocycles)
    {
        var active = await mesocycles.GetActiveAsync();
        if (active is null) return true;

        var page = Shell.Current?.CurrentPage;
        if (page is null) return false;

        var name = string.IsNullOrWhiteSpace(active.Name) ? "—" : active.Name;
        return await page.DisplayAlert(
            AppResources.MesoBuilder_ReplaceActive_Title,
            string.Format(AppResources.MesoBuilder_ReplaceActive_Body_Format, name),
            AppResources.MesoBuilder_ReplaceActive_Confirm,
            AppResources.Common_Cancel);
    }

    private void InitializePriorityLists()
    {
        Emphasize.Clear();
        Grow.Clear();
        Maintain.Clear();

        var muscles = new[]
        {
            MuscleGroup.Chest, MuscleGroup.Back, MuscleGroup.Shoulders,
            MuscleGroup.Biceps, MuscleGroup.Triceps,
            MuscleGroup.Quads, MuscleGroup.Hamstrings, MuscleGroup.Glutes,
            MuscleGroup.Calves, MuscleGroup.Abs,
        };

        foreach (var m in muscles)
        {
            Grow.Add(new MusclePriorityRow
            {
                Muscle = m,
                Label = m.ToLocalizedLabel(),
                Priority = MusclePriorityLevel.Grow,
            });
        }
    }

}

public partial class MusclePriorityRow : ObservableObject
{
    public MuscleGroup Muscle { get; set; }
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private MusclePriorityLevel _priority;

    public bool IsEmphasize => Priority == MusclePriorityLevel.Emphasize;
    public bool IsGrow => Priority == MusclePriorityLevel.Grow;
    public bool IsMaintain => Priority == MusclePriorityLevel.Maintain;

    partial void OnPriorityChanged(MusclePriorityLevel value)
    {
        OnPropertyChanged(nameof(IsEmphasize));
        OnPropertyChanged(nameof(IsGrow));
        OnPropertyChanged(nameof(IsMaintain));
    }
}
