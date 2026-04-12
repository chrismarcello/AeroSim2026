using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AeroSim2026.Views;

public partial class SavedFlightsView : ReactiveUserControl<SavedFlightsViewModel>
{
    public SavedFlightsView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {

        });
    }
}