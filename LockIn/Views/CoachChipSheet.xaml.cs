// LockIn/Views/CoachChipSheet.xaml.cs
using CommunityToolkit.Maui.Views;
using LockIn.Models;

namespace LockIn.Views;

public partial class CoachChipSheet : Popup
{
    public CoachChipSheet(CoachChip chip)
    {
        InitializeComponent();
        HeaderLabel.Text = chip.DetailHeader;
        BodyLabel.Text   = chip.DetailBody;
    }

    private void OnCloseTapped(object sender, TappedEventArgs e) => CloseAsync();
}
