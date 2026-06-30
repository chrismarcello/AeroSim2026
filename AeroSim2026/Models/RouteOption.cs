using NetTopologySuite.Geometries;
using System.Collections.Generic;
using AeroSim2026.EFModels;
using System.Collections.ObjectModel;
using AeroSim2026.Core.Routing;
namespace AeroSim2026.Models
{
    public class RouteOption
    {
        public string Title { get; set; } = string.Empty;
        public string RouteString { get; set; } = string.Empty;
        public double Distance { get; set; }

        public List<Coordinate> Waypoints { get; set; } = new();

        public List<Airway> Airways { get; set; } = new();

        public int StartWaypointId { get; set; }

        public List<FlightPlanRoute> GeneratedFlightPlanRoutes { get; set; } = new List<FlightPlanRoute>();

        public ObservableCollection<string> WaypointDetails { get; set; } = new();
    }
    public class ProposedRoute
    {
        public string? RouteName { get; set; }
        public double TotalDistance { get; set; }
        public List<RouteLeg> Legs { get; set; } = new();
    }
    public class RouteLeg
    {
        public int SequenceNumber { get; set; }
        public RouteNode Waypoint { get; set; } = null!;
        public string? AirwayName { get; set; }
        public int? AirwayId { get; set; }
        public int? MinimumAltitude { get; set; }
        public int? MaximumAltitude { get; set; }
        public double DistanceFromPrevious { get; set; }
        public double CumulativeDistance { get; set; }
    }
}
