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

            Mapsui.Styles.Color waypointColor = navType?.ToUpper() switch
            {
                "AIRPORT" => Mapsui.Styles.Color.Purple,
                "VOR" => Mapsui.Styles.Color.Cyan,
                "NDB" => Mapsui.Styles.Color.Green,
                _ => Mapsui.Styles.Color.Gray
            };
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = navClean == "AIRPORT" ? SymbolType.Ellipse : SymbolType.Triangle,
                SymbolScale = navClean == "AIRPORT" ? 0.35 : 0.25,
                Fill = new Brush(waypointColor),
                //Outline = new Pen(Mapsui.Styles.Color.White, 2)
            });
            if (!string.IsNullOrEmpty(identifier))
            {
                feature.Styles.Add(new LabelStyle
                {
                    Text = identifier,
                Font = new Font { FontFamily = "Arial", Size = 10, Bold = true },
                ForeColor = Mapsui.Styles.Color.White,
                Halo = new Pen(Mapsui.Styles.Color.Black, 2),
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Top,
                Offset = new Offset(0, 8)
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

    }
}