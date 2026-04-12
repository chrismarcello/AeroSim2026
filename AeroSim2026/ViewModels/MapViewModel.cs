using AeroSim2026.EFModels;
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
        private static string? _cachedDepartIconBase64;
        private static string? _cachedDestIconBase64;

        public Map Map
        {
            get => _map;
            private set => this.RaiseAndSetIfChanged(ref _map, value);
        }

        public MapViewModel(Airport departure, Airport arrival)
        {
            var tempMap = new Map();

            // 1. Hide the performance widget
            var perfWidget = tempMap.Widgets.FirstOrDefault(w => w.GetType().Name == "PerformanceWidget");
            if (perfWidget != null) perfWidget.Enabled = false;

            tempMap.Layers.Add(OpenStreetMap.CreateTileLayer());

            // 2. Set up the dynamic route layer with a thick magenta line
            _routeLayer = new MemoryLayer
            {
                Name = "Flight Route",
                Style = new VectorStyle
                {
                    Line = new Pen { Color = Mapsui.Styles.Color.Magenta, Width = 4 }
                }
            };
            tempMap.Layers.Add(_routeLayer);

            // 3. Project coordinates
            var (startX, startY) = SphericalMercator.FromLonLat((double)departure.Lonx!, (double)departure.Laty!);
            var (endX, endY) = SphericalMercator.FromLonLat((double)arrival.Lonx!, (double)arrival.Laty!);

            // 4. Add the custom airport icons & labels
            tempMap.Layers.Add(CreateAirportMarkersLayer(departure, arrival, startX, startY, endX, endY));

            // 5. Setup initial route line
            var initialWaypoints = new List<Coordinate>
            {
                new Coordinate(startX, startY),
                new Coordinate(endX, endY)
            };
            UpdateRoute(null, null);

            // 6. Calculate bounding box and apply padding manually 
            var minX = Math.Min(startX, endX);
            var minY = Math.Min(startY, endY);
            var maxX = Math.Max(startX, endX);
            var maxY = Math.Max(startY, endY);

            double paddingX = Math.Max((maxX - minX) * 0.2, 50000);
            double paddingY = Math.Max((maxY - minY) * 0.2, 50000);
            var paddedExtent = new MRect(minX - paddingX, minY - paddingY, maxX + paddingX, maxY + paddingY);

            tempMap.Navigator.ZoomToBox(paddedExtent);

            // 7. Add Zoom In/Out Widget
            tempMap.Widgets.Add(CreateZoomInOutWidget(Orientation.Horizontal, VerticalAlignment.Top, HorizontalAlignment.Right));

            Map = tempMap;
        }

        private ILayer CreateAirportMarkersLayer(Airport departure, Airport arrival, double startX, double startY, double endX, double endY)
        {
            // Load Images safely (Using Orange for Origin, Red for Dest based on your assets folder!)
            if (_cachedDepartIconBase64 == null) _cachedDepartIconBase64 = LoadIconBase64("avares://AeroSim2026/Assets/p-o-i-solid_Orange.png");
            if (_cachedDestIconBase64 == null) _cachedDestIconBase64 = LoadIconBase64("avares://AeroSim2026/Assets/p-o-i-solid_Red.png");

            // Departure Marker
            var departMarker = new GeometryFeature(new Point(startX, startY));
            ApplyMarkerStyles(departMarker, departure.Ident, _cachedDepartIconBase64, Mapsui.Styles.Color.Orange);

            // Arrival Marker
            var destMarker = new GeometryFeature(new Point(endX, endY));
            ApplyMarkerStyles(destMarker, arrival.Ident, _cachedDestIconBase64, Mapsui.Styles.Color.Red);

            return new MemoryLayer
            {
                Name = "Airports",
                Features = new[] { departMarker, destMarker }
            };
        }

        private void ApplyMarkerStyles(GeometryFeature feature, string ident, string? base64Icon, Mapsui.Styles.Color fallbackColor)
        {
            // 1. Image or Fallback Circle
            if (!string.IsNullOrEmpty(base64Icon))
            {
                feature.Styles.Add(new ImageStyle
                {
                    Image = new Mapsui.Styles.Image { Source = $"base64-content://{base64Icon}" },
                    SymbolScale = 0.05, // Adjust as needed for your icon size
                    Offset = new Offset(0, 20)
                });
            }
            else
            {
                feature.Styles.Add(new SymbolStyle
                {
                    Fill = new Brush(fallbackColor),
                    SymbolScale = 0.8,
                    Outline = new Pen(Mapsui.Styles.Color.Black, 2),
                    SymbolType = SymbolType.Ellipse
                });
            }

            // 2. Text Label (The Airport Ident) stacked on top
            feature.Styles.Add(new LabelStyle
            {
                Text = ident,
                Font = new Font { FontFamily = "Arial", Size = 14, Bold = true },
                ForeColor = Mapsui.Styles.Color.White,
                Halo = new Pen(Mapsui.Styles.Color.Black, 2), // Keeps it readable over roads/rivers
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom,
                Offset = new Offset(0, -15) // Pushes the text slightly above the icon
            });
        }

        private string? LoadIconBase64(string uri)
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(uri));
                using var memoryStream = new System.IO.MemoryStream();
                stream.CopyTo(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading marker icon {uri}: {ex.Message}");
                return null;
            }
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
            // 1. If the Map object itself doesn't exist yet, abort safely!
            if (Map == null || Map.Layers == null) return;

            // 2. Find the Route layer, or auto-create it if it's missing
            var routeLayer = Map.Layers.FirstOrDefault(l => l.Name == "Route") as MemoryLayer;
            if (routeLayer == null)
            {
                routeLayer = new MemoryLayer { Name = "Route" };
                Map.Layers.Add(routeLayer);
            }

            // 3. Find the Markers layer, or auto-create it if it's missing
            var markerLayer = Map.Layers.FirstOrDefault(l => l.Name == "Markers") as MemoryLayer;
            if (markerLayer == null)
            {
                markerLayer = new MemoryLayer { Name = "Markers" };
                Map.Layers.Add(markerLayer);
            }

            // 4. Safely apply the route line (handling nulls)
            var routeFeatures = new List<GeometryFeature>();
            if (routeLine != null)
            {
                routeFeatures.Add(routeLine);
            }
            routeLayer.Features = routeFeatures;
            routeLayer.DataHasChanged();

            // 5. Safely apply the markers (handling nulls)
            markerLayer.Features = markers ?? new List<GeometryFeature>();
            markerLayer.DataHasChanged();

            // 6. Refresh the map UI
            Map.Refresh();
        }
    }
}
