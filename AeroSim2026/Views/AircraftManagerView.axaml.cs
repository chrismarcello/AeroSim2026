using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AeroSim2026.Views;

public partial class AircraftManagerView : ReactiveUserControl<AircraftManagerViewModel>
{
    public AircraftManagerView()
    {
        InitializeComponent();
    }
}