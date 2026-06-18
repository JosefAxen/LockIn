using System.Collections.Specialized;
using LockIn.Services;
using LockIn.ViewModels;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace LockIn.Views;

public partial class PostWorkoutPage : ContentPage
{
    private readonly PostWorkoutViewModel _vm;
    private readonly ISoundService _sound;

    public PostWorkoutPage(PostWorkoutViewModel vm, ISoundService sound)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _sound = sound;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.NewAchievements.CollectionChanged += OnAchievementsChanged;
        await AnimationHelper.PageEntryAsync(this);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.NewAchievements.CollectionChanged -= OnAchievementsChanged;
        ConfettiOverlay.Stop();
    }

    private void OnAchievementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems?.Count == 0) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConfettiOverlay.Start();
            _sound.PlayAchievementUnlocked();
            if (Preferences.Default.Get("haptic_enabled", true))
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        });
    }
}
