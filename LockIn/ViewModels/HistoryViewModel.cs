using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Services;
using LockIn.Views;
using System.Collections.ObjectModel;
using static LockIn.Services.DatabaseService;

namespace LockIn.ViewModels;

public partial class HistoryViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<SessionSummaryRow> Sessions { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var sessions = await db.GetCompletedSessionsAsync();
        Sessions.Clear();
        foreach (var s in sessions)
            Sessions.Add(s);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task OpenSessionAsync(SessionSummaryRow session)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailPage), new Dictionary<string, object>
        {
            { "Session", session }
        });
    }
}
