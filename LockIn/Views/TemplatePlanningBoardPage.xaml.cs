using LockIn.Resources.Strings;
using LockIn.ViewModels;

namespace LockIn.Views;

public partial class TemplatePlanningBoardPage : ContentPage
{
    private readonly TemplatePlanningBoardViewModel _vm;

    public TemplatePlanningBoardPage(TemplatePlanningBoardViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    private async void OnExerciseMenuTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as VisualElement)?.BindingContext is not ExerciseRowViewModel row) return;

        var choice = await Shell.Current.CurrentPage.DisplayActionSheet(
            AppResources.TemplateBoard_ExerciseMenu_Title,
            AppResources.Common_Cancel,
            null,
            AppResources.TemplateBoard_ExerciseMenu_MoveUp,
            AppResources.TemplateBoard_ExerciseMenu_MoveDown,
            AppResources.TemplateBoard_ExerciseMenu_Remove);

        if (string.IsNullOrEmpty(choice) || choice == AppResources.Common_Cancel) return;

        if (choice == AppResources.TemplateBoard_ExerciseMenu_MoveUp)
            _vm.MoveExerciseUpCommand.Execute(row);
        else if (choice == AppResources.TemplateBoard_ExerciseMenu_MoveDown)
            _vm.MoveExerciseDownCommand.Execute(row);
        else if (choice == AppResources.TemplateBoard_ExerciseMenu_Remove)
            _vm.RemoveExerciseCommand.Execute(row);
    }
}
