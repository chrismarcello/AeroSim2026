using AeroSim2026.EFModels;
using Avalonia.Platform;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;

namespace AeroSim2026.Core.Services
{
    public class MapFeatureFactory : IMapFeatureFactory
    {
        private readonly Dictionary<string, string> _iconCache = new();

        public GeometryFeature CreateWaypointFeature(FlightPlanRoute routePoint)
        {
            return CreateWaypointFeature((double)routePoint.Waypoint.Laty!, (double)routePoint.Waypoint.Lonx!, routePoint.Waypoint.Ident, routePoint.Waypoint.WaypointType);
        }

        public GeometryFeature CreateWaypointFeature(double lat, double lon, string ident, string navType)
        {
            var (x, y) = SphericalMercator.FromLonLat(lon, lat);
            var feature = new GeometryFeature(new Point(x, y));

            // CRITICAL: Kills the ghost circle on the feature itself!
            feature.Styles.Clear();
            string typeCode = (navType ?? "W").Trim().ToUpper();
            // Pick the right SVG based on the waypoint type
            string svgFileName = typeCode switch
            {
                "ORIGIN" => "userpoint_Airport_Origin.svg",
                "DESTINATION" => "userpoint_Airport_Dest.svg",
                "AIRPORT" or "A" => "userpoint_Airport.svg",
                "VORDME" or "VD" => "userpoint_VORDME.svg",
                "VORTAC" or "VT" => "userpoint_VORTAC.svg",
                "VOR" or "V" => "userpoint_VOR.svg",
                "NDB" or "N" => "userpoint_NDB.svg",
                "TACAN" or "T" => "userpoint_TACAN.svg",
                "DME" or "D" => "userpoint_DME.svg",
                "HELIPAD" or "H" => "userpoint_Helipad.svg",
                "MARKER" or "M" => "userpoint_Marker.svg",
                _ => "userpoint_Waypoint.svg" // Fallback to generic waypoint
            };

            string? imageSource = GetOrLoadSvgSource($"avares://AeroSim2026/Assets/svg/{svgFileName}");

            if (imageSource != null)
            {
                feature.Styles.Add(new ImageStyle
                {
                    Image = new Mapsui.Styles.Image { Source = imageSource },
                    // 1. Bumped the scale back up to a safe, visible size to test
                    SymbolScale = (typeCode == "AIRPORT" || typeCode == "A" || typeCode == "ORIGIN" || typeCode == "DESTINATION") ? 0.20 : 0.14,
                    Offset = new Offset(0, 0)
                });
            }
            else
            {
                // 2. FALLBACK: If the SVG file is misspelled or missing, draw a highly visible RED DOT.
                // This guarantees you are never left with an empty map!
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.5,
                    Fill = new Brush(Mapsui.Styles.Color.Red),
                    Outline = new Pen(Mapsui.Styles.Color.White, 2)
                });
            }

            // Add the text label underneath the SVG
            feature.Styles.Add(new LabelStyle
            {
                Text = ident,
                Font = new Font { FontFamily = "Arial", Size = 10, Bold = true },
                ForeColor = Mapsui.Styles.Color.White,
                Halo = new Pen(Mapsui.Styles.Color.Black, 2),
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Top,
                Offset = new Offset(0, 8)
            });

            return feature;
        }

        public GeometryFeature CreateRouteLine(List<Coordinate> points)
        {
            var feature = new GeometryFeature(new LineString(points.ToArray()));
            feature.Styles.Add(new VectorStyle { Line = new Pen(Color.Magenta, 4), Fill = null });
            return feature;
        }
        private string? GetOrLoadSvgSource(string uri)
        {
            if (_iconCache.TryGetValue(uri, out var cachedSource)) return cachedSource;

            try
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri(uri));
                using var reader = new System.IO.StreamReader(stream);
                var svgText = reader.ReadToEnd();

                // Safe practice: Strip the XML header if it exists so Mapsui parses it cleanly
                int svgIndex = svgText.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
                if (svgIndex > 0) svgText = svgText.Substring(svgIndex);

                // This is the exact format required by the Mapsui v5 ImageSource docs!
                var sourceString = $"svg-content://{svgText}";

                _iconCache[uri] = sourceString;
                return sourceString;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load SVG icon: {uri} - {ex.Message}");
                return null;
            }
        }
    }
}