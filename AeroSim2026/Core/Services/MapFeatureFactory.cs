using Avalonia.Platform;
using Mapsui.Nts;
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

        public GeometryFeature CreateWaypointFeature(double lat, double lon, string identifier, string navType)
        {
            var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(lon, lat);
            var feature = new GeometryFeature(new Point(new Coordinate(x, y)));

            string navClean = navType?.Trim().ToUpper() ?? "W";

            // Select asset based on NavType
            string? assetUri = navClean switch
            {
                "AIRPORT" => "avares://AeroSim2026/Assets/Icons/p-o-i-solid_Red.png",
                "VOR" or "V" => "avares://AeroSim2026/Assets/svg/vor.svg",
                "NDB" or "N" => "avares://AeroSim2026/Assets/svg/ndb.svg",
                _ => "avares://AeroSim2026/Assets/svg/waypoint.svg" // Catch-all for W, I, R, U, or unknown codes
            };

            bool useFallback = true;

            // Only attempt to apply the image if we found a match AND it loaded successfully
            if (assetUri != null)
            {
                string base64Image = GetOrLoadImageBase64(assetUri);

                if (!string.IsNullOrEmpty(base64Image))
                {
                    useFallback = false;
                    feature.Styles.Add(new ImageStyle
                    {
                        Image = new Mapsui.Styles.Image { Source = $"base64-content://{base64Image}" },

                        // REDUCED: Changed 0.3 to 0.08 so the pin isn't giant
                        SymbolScale = navType?.ToUpper() == "AIRPORT" ? 0.08 : 0.15,

                        // Slightly adjusted the offset so the tip of the smaller pin points exactly at the coordinate
                        //Offset = navType?.ToUpper() == "AIRPORT" ? new Offset(0, -18) : new Offset(0, 0)
                        Offset = new Offset(0, 0)
                    });
                }
            }

            // If it's a generic waypoint OR the image failed to load, draw a safe circle
            if (useFallback)
            {
                feature.Styles.Add(new SymbolStyle
                {
                    Fill = new Brush(Color.Cyan),
                    SymbolScale = 0.2,
                    SymbolType = SymbolType.Ellipse,
                    Outline = new Pen(Color.Magenta, 2)
                });
            }

            // Add Label
            if (!string.IsNullOrEmpty(identifier))
            {
                feature.Styles.Add(new LabelStyle
                {
                    Text = identifier,
                    Font = new Font { Size = 12, Bold = true, FontFamily = "Arial" },
                    ForeColor = Color.White,
                    Halo = new Pen(Color.Black, 2),
                    Offset = new Offset(0, -12)
                });
            }

            return feature;
        }

        public GeometryFeature CreateRouteLine(List<Coordinate> points)
        {
            var feature = new GeometryFeature(new LineString(points.ToArray()));
            feature.Styles.Add(new VectorStyle { Line = new Pen(Color.Magenta, 4), Fill = null });
            return feature;
        }

        private string GetOrLoadImageBase64(string uri)
        {
            if (_iconCache.TryGetValue(uri, out var cachedBase64))
            {
                return cachedBase64;
            }

            try
            {
                using var stream = AssetLoader.Open(new Uri(uri));
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                _iconCache[uri] = base64;
                return base64;
            }
            catch (Exception ex)
            {
                // Silently swallow the error and return null so the factory knows to use the fallback
                System.Diagnostics.Debug.WriteLine($"Failed to load map icon: {uri} - {ex.Message}");
                return null!;
            }
        }
    }
}