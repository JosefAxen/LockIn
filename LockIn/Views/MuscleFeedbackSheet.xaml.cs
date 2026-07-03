using CommunityToolkit.Maui.Views;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class MuscleFeedbackSheet : Popup
{
    private readonly MuscleFeedbackSheetViewModel _vm;

    public MuscleFeedbackSheet(MuscleFeedbackSheetViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        _vm.CloseRequested += OnCloseRequested;
        BindingContext = _vm;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _vm.CloseRequested -= OnCloseRequested;
        _ = CloseAsync();
    }
}
