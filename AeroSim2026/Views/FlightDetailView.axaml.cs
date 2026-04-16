using AeroSim2026.EFModels;
using AeroSim2026.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Features;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Nts; // Important for Geometry (LineString, Point)
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling; // Important for OpenStreetMap
using Mapsui.UI.Avalonia;
using Mapsui.Widgets;
using NetTopologySuite.Geometries; // For Point, LineString
using ReactiveUI;
using ReactiveUI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace AeroSim2026.Views;

public partial class FlightDetailView : ReactiveUserControl<FlightDetailViewModel>
{
    public FlightDetailView()
    {
        InitializeComponent();
        InitializeMap();
    }

    private void InitializeMap()
    {
        // 1. Create the MapControl programmatically
        var mapControl = new MapControl();

        // 2. Bind its "Map" property to the "FlightMap" property we built in the ViewModel
        mapControl.Bind(MapControl.MapProperty, new Binding("FlightMap"));

    }
}