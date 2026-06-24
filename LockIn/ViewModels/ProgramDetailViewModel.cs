using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data;
using LockIn.Resources.Strings;
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
                DaysLabel = string.Format(AppResources.ProgramDetail_DaysLabel_Format, _program.DaysPerWeek, _program.Days.Count);
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
            AppResources.ProgramDetail_Activate_Title,
            string.Format(AppResources.ProgramDetail_Activate_Body_Format, _program.Days.Count, _program.Name),
            AppResources.ProgramDetail_Activate_Confirm,
            AppResources.Common_Cancel);
        if (!confirmed) return;

        IsActivating = true;
        await db.ActivateProgramAsync(_program);
        IsActivating = false;

        await Toast.Make(string.Format(AppResources.ProgramDetail_Toast_Created_Format, _program.Days.Count), ToastDuration.Long).Show();

        await Shell.Current.GoToAsync("..");
    }
}
