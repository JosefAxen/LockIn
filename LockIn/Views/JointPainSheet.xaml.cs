using CommunityToolkit.Maui.Views;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class JointPainSheet : Popup
{
    private readonly JointPainSheetViewModel _vm;

    public JointPainSheet(JointPainSheetViewModel vm, int sessionExerciseId, string exerciseName)
    {
        InitializeComponent();
        _vm = vm;
        _vm.Reset();
        _vm.SessionExerciseId = sessionExerciseId;
        _vm.ExerciseName = exerciseName;
        _vm.CloseRequested += OnCloseRequested;
        BindingContext = _vm;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _vm.CloseRequested -= OnCloseRequested;
        _ = CloseAsync();
    }
}
