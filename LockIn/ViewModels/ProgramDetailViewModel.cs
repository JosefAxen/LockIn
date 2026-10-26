using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Data;
using LockIn.Resources.Strings;

namespace LockIn.ViewModels;

// Programmet visas bara som referens. Aktivering (mallar från program) finns inte —
// program-konceptet ersattes av template-flödet (Task 21), så sidan lovar inget sådant.
public partial class ProgramDetailViewModel : ObservableObject, IQueryAttributable
{
    private WorkoutProgram? _program;

    [ObservableProperty] private string _programName = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _daysLabel = "";

    public List<ProgramDay> Days { get; private set; } = [];
    public string LocalizedDescription => _program?.LocalizedDescription ?? "";

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
                OnPropertyChanged(nameof(LocalizedDescription));
            }
        }
    }
}
