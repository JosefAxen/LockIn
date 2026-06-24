using CommunityToolkit.Mvvm.ComponentModel;
using LockIn.Models;
using LockIn.Resources.Strings;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class ProgramGroup : ObservableObject
{
    public string ProgramId { get; set; } = "";
    public string ProgramName { get; set; } = "";
    public int TemplateCount => Templates.Count;
    public string TemplateCountText => string.Format(AppResources.Train_SessionCount_Format, Templates.Count);

    [ObservableProperty] private bool _isExpanded;
    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(ChevronIcon));

    public string ChevronIcon => IsExpanded ? "∨" : "›";
    public ObservableCollection<WorkoutTemplate> Templates { get; } = new();
}
