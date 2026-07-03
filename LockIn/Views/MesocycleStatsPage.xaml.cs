using LockIn.ViewModels;

namespace LockIn.Views;

// RP full-klon Task 28: MesocycleStatsPage code-behind.
public partial class MesocycleStatsPage : ContentPage
{
    public MesocycleStatsPage(MesocycleStatsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
