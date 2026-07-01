using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Resources.Strings;
using LockIn.Services;

namespace LockIn.ViewModels;

public partial class MesoBuilderViewModel(IMesoBuilderService builder) : ObservableObject
{
    [ObservableProperty] private string _cycleName = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWeek4Selected), nameof(IsWeek5Selected), nameof(IsWeek6Selected), nameof(IsWeek8Selected))]
    private int _weekCount = 5;
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private bool _isBuilding;

    public bool IsWeek4Selected => WeekCount == 4;
    public bool IsWeek5Selected => WeekCount == 5;
    public bool IsWeek6Selected => WeekCount == 6;
    public bool IsWeek8Selected => WeekCount == 8;

    [RelayCommand]
    private void SelectWeekCount(string value)
    {
        if (int.TryParse(value, out var v)) WeekCount = v;
    }

    [RelayCommand]
    private async Task BuildAsync()
    {
        if (string.IsNullOrWhiteSpace(CycleName))
        {
            await Toast.Make(AppResources.MesoBuilder_Error_NameRequired).Show();
            return;
        }
        if (IsBuilding) return;
        IsBuilding = true;
        try
        {
            await builder.BuildAsync(new MesoBuilderSpec(CycleName.Trim(), WeekCount, StartDate));
            await Toast.Make(AppResources.MesoBuilder_Success_Toast).Show();
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBuilding = false;
        }
    }
}
