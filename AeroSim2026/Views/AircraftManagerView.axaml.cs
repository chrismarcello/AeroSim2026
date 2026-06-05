using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.Threading.Tasks;

namespace AeroSim2026.Views;

public partial class AircraftManagerView : ReactiveUserControl<AircraftManagerViewModel>
{
    public AircraftManagerView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            // Catch the interaction from the ViewModel

            if (ViewModel != null)
            {
                d(ViewModel.ShowAddManufacturerDialog.RegisterHandler(interaction => DoShowAddDialogAsync(interaction)));

                d(ViewModel.ShowAddTypeDialog.RegisterHandler(interaction => DoShowAddTypeDialogAsync(interaction)));

                d(ViewModel.ShowAddModelDialog.RegisterHandler(interaction => DoShowAddModelDialogAsync(interaction)));
            }
        
        });
    }
    private async Task DoShowAddDialogAsync(IInteractionContext<AddManufacturerViewModel, (string, string)?> interaction)
    {
        var dialog = new AddManufacturerWindow
        {
            DataContext = interaction.Input // Give the dialog the ViewModel
        };

        // Find the main window so the popup centers over it and blocks it
        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        var result = await dialog.ShowDialog<(string, string)?>(parentWindow!);

        // Pass the string back to the ViewModel
        interaction.SetOutput(result);
    }
    private async Task DoShowAddTypeDialogAsync(IInteractionContext<AddAircraftTypeViewModel, (string Name, string Code, string AirFam, string EngFam)?> interaction)
    {
        // NOTE: Make sure you have created AddAircraftTypeWindow.axaml in your Views folder!
        var dialog = new AddAircraftTypeWindow
        {
            DataContext = interaction.Input
        };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        var result = await dialog.ShowDialog<(string, string, string, string)?>(parentWindow!);

        interaction.SetOutput(result);
    }
    private async Task DoShowAddModelDialogAsync(IInteractionContext<AddAircraftModelViewModel, (string, string, int?, string)?> interaction)
    {
        var dialog = new AddAircraftModelWindow
        {
            DataContext = interaction.Input
        };

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        var result = await dialog.ShowDialog<(string, string, int?, string)?>(parentWindow!);

        interaction.SetOutput(result);
    }
}