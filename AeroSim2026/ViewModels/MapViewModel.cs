using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DynamicData;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.Widgets;
using Mapsui.Widgets.BoxWidgets;
using Mapsui.Widgets.ButtonWidgets;
using NetTopologySuite.Geometries;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AeroSim2026.ViewModels
{
    public class MapViewModel : ReactiveObject
    {
        private Map _map;
        private MemoryLayer _routeLayer;

        // Cache the images so we only load them once per app session
        private static string? _cachedDepartIconSource;
        private static string? _cachedDestIconSource;

        public Map Map
        {
            get => _map;
            private set => this.RaiseAndSetIfChanged(ref _map, value);
        }

        public MapViewModel(Airport departure, Airport arrival, IMapFeatureFactory mapFeatureFactory)
        {
            var tempMap = new Map();

            var perfWidget = tempMap.Widgets.FirstOrDefault(w => w.GetType().Name == "PerformanceWidget");
            if (perfWidget != null) perfWidget.Enabled = false;

            tempMap.BackColor = Mapsui.Styles.Color.Transparent;
            tempMap.Layers.Add(OpenStreetMap.CreateTileLayer());

            _routeLayer = new MemoryLayer
            {
                Name = "Flight Route",
                Style = new VectorStyle { Fill = new Brush { Color = Color.Transparent, FillStyle = FillStyle.Hollow } }
            };
            tempMap.Layers.Add(_routeLayer);

            var (startX, startY) = SphericalMercator.FromLonLat((double)departure.Lonx!, (double)departure.Laty!);
            var (endX, endY) = SphericalMercator.FromLonLat((double)arrival.Lonx!, (double)arrival.Laty!);

            // 2. Pass the factory down to the marker generator
            tempMap.Layers.Add(CreateAirportMarkersLayer(departure, arrival, mapFeatureFactory));

            var initialWaypoints = new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(endX, endY)
            };
            UpdateRoute(null, null);

            var minX = Math.Min(startX, endX);
            var minY = Math.Min(startY, endY);
            var maxX = Math.Max(startX, endX);
            var maxY = Math.Max(startY, endY);

            double paddingX = Math.Max((maxX - minX) * 0.2, 50000);
            double paddingY = Math.Max((maxY - minY) * 0.2, 50000);
            var paddedExtent = new MRect(minX - paddingX, minY - paddingY, maxX + paddingX, maxY + paddingY);

            tempMap.Navigator.ZoomToBox(paddedExtent);
            tempMap.Widgets.Add(CreateZoomInOutWidget(Orientation.Horizontal, VerticalAlignment.Top, HorizontalAlignment.Right));

            Map = tempMap;
        }

        private ILayer CreateAirportMarkersLayer(Airport departure, Airport arrival, IMapFeatureFactory factory)
        {
            // Pass "ORIGIN" and "DESTINATION" instead of "AIRPORT"
            var departMarker = factory.CreateWaypointFeature((double)departure.Laty!, (double)departure.Lonx!, departure.Ident, "ORIGIN");
            var destMarker = factory.CreateWaypointFeature((double)arrival.Laty!, (double)arrival.Lonx!, arrival.Ident, "DESTINATION");

            return new MemoryLayer
            {
                Name = "Airports",
                Features = new[] { departMarker, destMarker },
                Style = null
            };
        }

        private static ZoomInOutWidget CreateZoomInOutWidget(Orientation orientation, VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment)
        {
            return new ZoomInOutWidget
            {
                Orientation = orientation,
                VerticalAlignment = verticalAlignment,
                HorizontalAlignment = horizontalAlignment,
                Margin = new MRect(20)
            };
        }

        public void UpdateRoute(GeometryFeature? routeLine, List<GeometryFeature>? markers)
        {
            if (Map == null || Map.Layers == null) return;

            var routeLayer = Map.Layers.FirstOrDefault(l => l.Name == "Route") as MemoryLayer;
            if (routeLayer == null)
            {
                routeLayer = new MemoryLayer { Name = "Route", Style = new VectorStyle { Fill = new Brush { Color = Color.Transparent, FillStyle = FillStyle.Hollow } } };
                Map.Layers.Insert(1, routeLayer);
            }

            var markerLayer = Map.Layers.FirstOrDefault(l => l.Name == "Markers") as MemoryLayer;
            if (markerLayer == null)
            {
                markerLayer = new MemoryLayer { Name = "Markers", Style = null };
                Map.Layers.Add(markerLayer);
            }

            var routeFeatures = new List<GeometryFeature>();
            if (routeLine != null) routeFeatures.Add(routeLine);

            routeLayer.Features = routeFeatures;
            routeLayer.DataHasChanged();

            markerLayer.Features = markers ?? new List<GeometryFeature>();
            markerLayer.DataHasChanged();

            Map.Refresh();
        }
    }
}
