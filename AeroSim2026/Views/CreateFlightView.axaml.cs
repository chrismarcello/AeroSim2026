using Avalonia.Controls;
using Mapsui.UI.Avalonia;
using Mapsui;
using AeroSim2026.EFModels;
using AeroSim2026.ViewModels;
using System.Linq;

namespace AeroSim2026.Views;

public partial class CreateFlightView : UserControl
{
    private MapControl? _flightMapControl;
    private bool _hasInitialZoomFired = false;

    public CreateFlightView()
    {
        InitializeComponent();

        // 1. Grab the exact MapControl we already built in the XAML
        _flightMapControl = this.FindControl<MapControl>("MapControl");

        if (_flightMapControl != null)
        {
            // 2. Wire up your custom map events directly to the UI map
            _flightMapControl.Info += OnMapInfo;
            _flightMapControl.SizeChanged += OnMapSizeChanged;
        }
    }

    private void OnMapSizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
    {
        if (!_hasInitialZoomFired && _flightMapControl?.Map != null && e.NewSize.Width > 0)
        {
            _hasInitialZoomFired = true;

            var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(-71.5667, 42.3918);

            if (_flightMapControl.Map.Navigator.Resolutions.Count > 8)
            {
                var resolution = _flightMapControl.Map.Navigator.Resolutions[8];
                _flightMapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), resolution);
            }
        }
    }

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        var layers = _flightMapControl?.Map?.Layers.Where(l => l.Name == "Airports");
        if (layers == null) return;

        var mapInfo = e.GetMapInfo(layers);

        if (mapInfo?.Feature != null)
        {
            if (mapInfo.Feature["AirportData"] is Airport clickedAirport)
            {
                if (DataContext is CreateFlightViewModel viewModel)
                {
                    ShowAirportMenu(clickedAirport, viewModel);
                }
            }
            e.Handled = true;
        }
    }

    private void ShowAirportMenu(Airport airport, CreateFlightViewModel viewModel)
    {
        var flyout = new MenuFlyout();

        var originItem = new MenuItem
        {
            Header = $"Set {airport.DisplayName} as Origin",
            Command = viewModel.SetOriginCommand,
            CommandParameter = airport
        };

        var destItem = new MenuItem
        {
            Header = $"Set {airport.DisplayName} as Destination",
            Command = viewModel.SetDestCommand,
            CommandParameter = airport
        };

        flyout.Items.Add(originItem);
        flyout.Items.Add(destItem);

        if (_flightMapControl != null)
        {
            flyout.ShowAt(_flightMapControl, true);
        }
    }
}