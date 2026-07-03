using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

// RP full-klon Task 21: TemplatePickerPage — listar seedade templates
// (IsCustom=false) och låter användaren konfigurera + skapa en mesocykel.
public partial class TemplatePickerViewModel : ObservableObject
{
    private readonly ITemplateService _templates;
    private readonly IMesoBuilderService _builder;

    public ObservableCollection<TemplateCard> Templates { get; } = new();

    [ObservableProperty] private bool _isBusy;

    // Config-panel state
    [ObservableProperty] private TemplateCard? _selectedTemplate;
    [ObservableProperty] private string _mesocycleName = "";
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private int _weekCount = 5;
    [ObservableProperty] private UnitSystem _unit = UnitSystem.Kg;

    public bool IsConfigOpen => SelectedTemplate is not null;
    public bool CanCreate => !IsBusy && !string.IsNullOrWhiteSpace(MesocycleName);

    // Toggle-hjälpar för XAML-binding (samma pattern som MesoBuilderViewModel)
    public bool IsWeeks4 => WeekCount == 4;
    public bool IsWeeks5 => WeekCount == 5;
    public bool IsWeeks6 => WeekCount == 6;
    public bool IsWeeks7 => WeekCount == 7;
    public bool IsWeeks8 => WeekCount == 8;

    public bool IsUnitKg => Unit == UnitSystem.Kg;
    public bool IsUnitLb => Unit == UnitSystem.Lb;

    public TemplatePickerViewModel(ITemplateService templates, IMesoBuilderService builder)
    {
        _templates = templates;
        _builder = builder;
    }

    partial void OnSelectedTemplateChanged(TemplateCard? value)
    {
        OnPropertyChanged(nameof(IsConfigOpen));
    }

    partial void OnMesocycleNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
    }

    partial void OnWeekCountChanged(int value)
    {
        OnPropertyChanged(nameof(IsWeeks4));
        OnPropertyChanged(nameof(IsWeeks5));
        OnPropertyChanged(nameof(IsWeeks6));
        OnPropertyChanged(nameof(IsWeeks7));
        OnPropertyChanged(nameof(IsWeeks8));
    }

    partial void OnUnitChanged(UnitSystem value)
    {
        OnPropertyChanged(nameof(IsUnitKg));
        OnPropertyChanged(nameof(IsUnitLb));
    }

    public async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _templates.ListAsync();

            Templates.Clear();
            foreach (var t in list.Where(t => !t.IsCustom).OrderBy(t => t.Name))
            {
                Templates.Add(new TemplateCard
                {
                    TemplateId = t.Id,
                    Name = t.Name,
                    DaysPerWeek = t.DefaultDaysPerWeek,
                    Description = DescriptionFor(t),
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectTemplate(TemplateCard? card)
    {
        if (card is null) return;
        MesocycleName = $"{card.Name} – {DateTime.Today:yyyy-MM-dd}";
        StartDate = DateTime.Today;
        WeekCount = 5;
        Unit = UnitSystem.Kg;
        SelectedTemplate = card;
    }

    [RelayCommand]
    private void CloseConfig() => SelectedTemplate = null;

    [RelayCommand]
    private void SetWeeks(string weeks)
    {
        if (int.TryParse(weeks, out var w)) WeekCount = w;
    }

    [RelayCommand]
    private void SetUnitKg() => Unit = UnitSystem.Kg;

    [RelayCommand]
    private void SetUnitLb() => Unit = UnitSystem.Lb;

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (SelectedTemplate is null) return;
        if (string.IsNullOrWhiteSpace(MesocycleName)) return;

        IsBusy = true;
        try
        {
            var detail = await _templates.GetDetailAsync(SelectedTemplate.TemplateId);
            var priorities = new Dictionary<MuscleGroup, MusclePriorityLevel>();
            if (detail is not null)
            {
                foreach (var slotList in detail.SlotsByDayId.Values)
                {
                    foreach (var slot in slotList)
                    {
                        priorities.TryAdd(slot.MuscleGroup, MusclePriorityLevel.Grow);
                    }
                }
            }

            await _builder.StartFromTemplateAsync(
                SelectedTemplate.TemplateId,
                MesocycleName.Trim(),
                StartDate,
                WeekCount,
                Unit,
                priorities);

            await Toast.Make(AppResources.TemplatePicker_Created_Toast).Show();
            SelectedTemplate = null;
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Toast.Make($"Fel: {ex.Message}").Show();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string DescriptionFor(WorkoutTemplate t)
    {
        var name = t.Name ?? "";
        if (name.Contains("PPL", StringComparison.OrdinalIgnoreCase))
            return AppResources.TemplatePicker_Desc_PPL;
        if (name.Contains("Upper/Lower", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Upper Lower", StringComparison.OrdinalIgnoreCase)
            || name.Contains("U/L", StringComparison.OrdinalIgnoreCase))
            return AppResources.TemplatePicker_Desc_UpperLower;
        if (name.Contains("Full Body", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Fullbody", StringComparison.OrdinalIgnoreCase))
            return AppResources.TemplatePicker_Desc_FullBody;
        if (name.Contains("Bro", StringComparison.OrdinalIgnoreCase))
            return AppResources.TemplatePicker_Desc_Bro;
        return AppResources.TemplatePicker_Desc_Default;
    }
}

public partial class TemplateCard : ObservableObject
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = "";
    public int DaysPerWeek { get; set; }
    public string Description { get; set; } = "";

    public string DaysDisplay =>
        string.Format(AppResources.TemplatePicker_DaysFormat, DaysPerWeek);
}
