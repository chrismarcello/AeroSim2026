using System;
using System.Collections.Generic;
using Mapsui.Nts;
using NetTopologySuite.Geometries;

namespace AeroSim2026.Core.Services
{
    public interface IMapFeatureFactory
    {
        GeometryFeature CreateWaypointFeature(double lat, double lon, string identifier, string navType);
        GeometryFeature CreateRouteLine(List<Coordinate> points);
    }
}
