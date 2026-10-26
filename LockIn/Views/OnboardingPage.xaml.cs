using LockIn.ViewModels;
using System.ComponentModel;

namespace LockIn.Views;

public partial class OnboardingPage : ContentPage
{
    private int _lastStep = 0;

    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OnboardingViewModel.CurrentStep)) return;
        if (BindingContext is not OnboardingViewModel vm) return;

        var direction = vm.CurrentStep > _lastStep ? 1 : -1; // 1 = framåt, -1 = bak
        _lastStep = vm.CurrentStep;

        var target = vm.CurrentStep switch
        {
            0 => Step0,
            1 => Step1,
            2 => Step2,
            3 => Step3,
            4 => Step4,
            5 => Step5,
            _ => null
        };
        if (target is null) return;

        // "Minska rörelse": byt steg direkt utan inglidning.
        if (AnimationHelper.ReduceMotion)
        {
            target.Opacity = 1;
            target.TranslationX = 0;
            return;
        }

        // Bindningen sätter precis IsVisible på det nya steget. Vi börjar dolt
        // (Opacity 0, lite förskjutet i riktningen) och slidear in.
        try
        {
            target.Opacity = 0;
            target.TranslationX = direction * 24;
            await Task.WhenAll(
                target.FadeTo(1, 300, Easing.CubicOut),
                target.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Onboarding] Steganimation misslyckades: {ex}");
            target.Opacity = 1;
            target.TranslationX = 0;
        }
    }
}
