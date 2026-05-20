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
            }
        });
    }
    private async Task DoShowAddDialogAsync(IInteractionContext<AddManufacturerViewModel, string?> interaction)
    {
        var dialog = new AddManufacturerWindow
        {
            DataContext = interaction.Input // Give the dialog the ViewModel
        };

        // Find the main window so the popup centers over it and blocks it
        var parentWindow = TopLevel.GetTopLevel(this) as Window;

        var result = await dialog.ShowDialog<string?>(parentWindow!);

        // Pass the string back to the ViewModel
        interaction.SetOutput(result);
    }
}