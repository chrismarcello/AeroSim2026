using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace AeroSim2026.Core.Routing
{
    public class  RouteNode
    {
        public int WaypointId { get; set; }
        public string? Identifier { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? NavType { get; set; }
        public ConcurrentBag<RouteEdge> OutgoingEdges { get; set; } = new ConcurrentBag<RouteEdge>();
    }

    public class RouteEdge
    {
        public RouteNode? TargetNode { get; set; }
        public double Distance { get; set; }
        public int? AirwayId { get; set; }
        public string? AirwayName { get; set; }

        public int? MinimumAltitude { get; set; }
        public int? MaximumAltitude { get; set; }
    }
}
