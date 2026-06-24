using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Models;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class AchievementsViewModel(DatabaseService db) : ObservableObject
{
    public ObservableCollection<AchievementRow> Achievements { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _unlockedCount = "";

    public async Task LoadAsync()
    {
        IsLoading = true;
        var unlocked = await db.GetUnlockedAchievementsAsync();
        var unlockedIds = unlocked.ToDictionary(u => (AchievementId)u.Id, u => u.UnlockedAt);

        Achievements.Clear();
        foreach (var def in AchievementService.All)
        {
            var isUnlocked = unlockedIds.TryGetValue(def.Id, out var unlockedAt);
            Achievements.Add(new AchievementRow
            {
                Id = def.Id,
                Emoji = def.Emoji,
                Title = def.Title,
                Description = def.Description,
                IsUnlocked = isUnlocked,
                UnlockedAt = isUnlocked ? unlockedAt.ToString("d MMM yyyy") : "",
            });
        }

        UnlockedCount = $"{unlockedIds.Count}/{AchievementService.All.Count} {AppResources.Achievements_Unlocked}";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}

public class AchievementRow
{
    public AchievementId Id { get; set; }
    public string Emoji { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsUnlocked { get; set; }
    public string UnlockedAt { get; set; } = "";

    public string DisplayEmoji => IsUnlocked ? Emoji : "🔒";
    public double CardOpacity => IsUnlocked ? 1.0 : 0.45;

    public Color CardBackground => IsUnlocked
        ? Color.FromArgb("#1FFBBF24")
        : Color.FromArgb("#161618");

    public Color CardStroke => IsUnlocked
        ? Color.FromArgb("#40FBBF24")
        : Colors.Transparent;
}
