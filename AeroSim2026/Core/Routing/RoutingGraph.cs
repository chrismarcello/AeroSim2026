using AeroSim2026.EFModels;
using AeroSim2026.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AeroSim2026.Core.Routing
{
    public class RoutingGraph
    {
        private const double MaxDirectHopNm = 800.0;
        private Dictionary<int, RouteNode> _nodes = new Dictionary<int, RouteNode>();

        /// <summary>
        /// Loads DB data into the in-memory graph structure. This should be called once at startup or when data changes.
        /// </summary>
        public void BuildGraph(List<Airway> allAirways, Dictionary<int, string> navTypeLookup = null!)
        {
            _nodes.Clear();

            string GetTrueNavType(int waypointId, Waypoint defaultWp)
            {
                if (navTypeLookup != null && navTypeLookup.TryGetValue(waypointId, out var mappedType))
                {
                    return mappedType ?? "W";
                }
                return defaultWp?.WaypointType ?? "W";
            }

            foreach (var airway in allAirways)
            {
                // 1. Ensure Start Node exists in the dictionary
                if (!_nodes.ContainsKey(airway.FromWaypointId))
                {
                    _nodes[airway.FromWaypointId] = new RouteNode
                    {
                        WaypointId = airway.FromWaypointId,
                        Identifier = airway.FromWaypoint?.Ident ?? "NULL",
                        Latitude = airway.FromWaypoint?.Laty ?? 0,
                        Longitude = airway.FromWaypoint?.Lonx ?? 0,
                        NavType = GetTrueNavType(airway.FromWaypointId, airway.FromWaypoint!)
                    };
                }

                // 2. Ensure End Node exists in the dictionary
                if (!_nodes.ContainsKey(airway.ToWaypointId))
                {
                    _nodes[airway.ToWaypointId] = new RouteNode
                    {
                        WaypointId = airway.ToWaypointId,
                        Identifier = airway.ToWaypoint?.Ident ?? "NULL",
                        Latitude = airway.ToWaypoint?.Laty ?? 0,
                        Longitude = airway.ToWaypoint?.Lonx ?? 0,
                        NavType = GetTrueNavType(airway.ToWaypointId, airway.ToWaypoint!)
                    };
                }

                double distance = GeoMath.Distance(
                    _nodes[airway.FromWaypointId].Latitude,
                    _nodes[airway.FromWaypointId].Longitude,
                    _nodes[airway.ToWaypointId].Latitude,
                    _nodes[airway.ToWaypointId].Longitude);

                // 3. Add Edges (Directional Logic to respect One-Way Jet Routes)
                var startNode = _nodes[airway.FromWaypointId];
                var endNode = _nodes[airway.ToWaypointId];

                // Safely checks for null or empty without needing the '?.' operator
                string direction = string.IsNullOrEmpty(airway.Direction) ? "N" : airway.Direction.ToUpper();

                // Forward Direction (From -> To)
                // Allowed as long as the direction is NOT explicitly "B" (Backward)
                if (direction != "B")
                {
                    startNode.OutgoingEdges.Add(new RouteEdge
                    {
                        TargetNode = endNode,
                        AirwayName = airway.AirwayName,
                        AirwayId = airway.AirwayId,
                        Distance = distance,
                        MinimumAltitude = airway.MinimumAltitude,
                        MaximumAltitude = airway.MaximumAltitude
                    });
                }

                // Backward Direction (To -> From)
                // Allowed as long as the direction is NOT explicitly "F" (Forward)
                if (direction != "F")
                {
                    endNode.OutgoingEdges.Add(new RouteEdge
                    {
                        TargetNode = startNode,
                        AirwayName = airway.AirwayName,
                        AirwayId = airway.AirwayId,
                        Distance = distance,
                        MinimumAltitude = airway.MinimumAltitude,
                        MaximumAltitude = airway.MaximumAltitude
                    });
                }
            }

            var nodeList = _nodes.Values.ToList();
            for (int i = 0; i < nodeList.Count; i++)
            {
                for (int j = i + 1; j < nodeList.Count; j++)
                {
                    var nodeA = nodeList[i];
                    var nodeB = nodeList[j];

                    // Only check DCT for named VORs/NDBs to prevent massive memory spikes
                    if (nodeA.Identifier != "NULL" && nodeB.Identifier != "NULL")
                    {
                        // THE FIX: Added safe null checking (?.) to permanently clear the CS8602 warning
                        if ((nodeA.NavType?.Contains("V") == true) || (nodeB.NavType?.Contains("V") == true))
                        {
                            CheckAndAddDirectEdge(nodeA, nodeB);
                        }
                    }
                }
            }
        }

        // A quick helper method to keep the loops clean
        private void CheckAndAddDirectEdge(RouteNode nodeA, RouteNode nodeB)
        {
            // Quick Latitude pre-check to skip the expensive square root math if possible
            if (Math.Abs(nodeA.Latitude - nodeB.Latitude) > (MaxDirectHopNm / 60.0)) return;

            double dist = GeoMath.Distance(nodeA.Latitude, nodeA.Longitude, nodeB.Latitude, nodeB.Longitude);

            if (dist <= MaxDirectHopNm)
            {
                nodeA.OutgoingEdges.Add(new RouteEdge
                {
                    TargetNode = nodeB,
                    Distance = dist,
                    AirwayId = null,
                    AirwayName = "DCT"
                });

                nodeB.OutgoingEdges.Add(new RouteEdge
                {
                    TargetNode = nodeA,
                    Distance = dist,
                    AirwayId = null,
                    AirwayName = "DCT"
                });
            }
        }

        public RouteNode GetNode(int waypointId)
        {
            return _nodes.ContainsKey(waypointId) ? _nodes[waypointId] : null!;
        }

        public IEnumerable<RouteNode> GetAllNodes() => _nodes.Values;

    }
}