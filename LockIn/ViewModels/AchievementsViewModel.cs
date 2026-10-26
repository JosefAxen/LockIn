using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockIn.Data.Repositories;
using LockIn.Models;
using LockIn.Navigation;
using LockIn.Resources.Strings;
using LockIn.Services;
using System.Collections.ObjectModel;

namespace LockIn.ViewModels;

public partial class AchievementsViewModel(ISettingsRepository settingsRepo) : ObservableObject
{
    public ObservableCollection<AchievementRow> Achievements { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _unlockedCount = "";

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var unlocked = await settingsRepo.GetUnlockedAchievementsAsync();
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync() => await Shell.Current.GoToAsync(AppRoutes.Back);
}

public class AchievementRow
{
    public AchievementId Id { get; set; }
    public string Emoji { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsUnlocked { get; set; }
    public string UnlockedAt { get; set; } = "";

    // Låsta meriter: dämpade färger med full opacitet (≥ 4,5:1) i stället för Opacity 0.45,
    // och status i text ("LÅST" / datum) — inte bara färg. Ingen emoji.
    public string Symbol => Emoji;
    public Color SymbolColor => IsUnlocked ? DesignTokens.AccentAmber : DesignTokens.TextMuted;
    public Color TitleColor => IsUnlocked ? DesignTokens.Text : DesignTokens.TextSecondary;
    public string StatusText => IsUnlocked ? UnlockedAt : AppResources.Achievements_Locked;
    public Color StatusColor => IsUnlocked ? DesignTokens.AccentAmber : DesignTokens.TextMuted;
    public string A11yDescription => $"{Title}. {Description} {StatusText}";

    public Color CardBackground => IsUnlocked
        ? Color.FromArgb("#1FFBBF24")
        : Color.FromArgb("#161618");

    public Color CardStroke => IsUnlocked
        ? Color.FromArgb("#40FBBF24")
        : Colors.Transparent;
}
