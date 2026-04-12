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
        /// 
        // In RoutingGraph.cs
        public void BuildGraph(List<Airway> allAirways)
        {
            _nodes.Clear();

            foreach (var airway in allAirways)
            {
                // 1. Ensure Start Node exists in the dictionary
                if (!_nodes.ContainsKey(airway.FromWaypointId))
                {
                    _nodes[airway.FromWaypointId] = new RouteNode
                    {
                        WaypointId = airway.FromWaypointId,
                        Identifier = airway.FromWaypoint?.Ident ?? "FIX",
                        Latitude = airway.FromLaty,
                        Longitude = airway.FromLonx,
                        NavType = airway.FromWaypoint?.WaypointType ?? "W"
                    };
                }

                // 2. Ensure End Node exists in the dictionary
                if (!_nodes.ContainsKey(airway.ToWaypointId))
                {
                    _nodes[airway.ToWaypointId] = new RouteNode
                    {
                        WaypointId = airway.ToWaypointId,
                        Identifier = airway.ToWaypoint?.Ident ?? "FIX",
                        Latitude = airway.ToLaty,
                        Longitude = airway.ToLonx,
                        NavType = airway.FromWaypoint?.WaypointType ?? "W"
                    };
                }

                // 3. Connect them!
                var startNode = _nodes[airway.FromWaypointId];
                var endNode = _nodes[airway.ToWaypointId];

                double dist = GeoMath.Distance(startNode.Latitude, startNode.Longitude, endNode.Latitude, endNode.Longitude);

                // Forward direction
                startNode.OutgoingEdges.Add(new RouteEdge
                {
                    TargetNode = endNode,
                    Distance = dist,
                    AirwayId = airway.AirwayId,
                    AirwayName = airway.AirwayName,
                    MinimumAltitude = airway.MinimumAltitude,
                    MaximumAltitude = airway.MaximumAltitude
                });

                // Reverse direction (CRITICAL: Airways must be bidirectional so A* can travel south!)
                endNode.OutgoingEdges.Add(new RouteEdge
                {
                    TargetNode = startNode,
                    Distance = dist,
                    AirwayId = airway.AirwayId,
                    AirwayName = airway.AirwayName,
                    MinimumAltitude = airway.MinimumAltitude,
                    MaximumAltitude = airway.MaximumAltitude
                });
            }
            // Convert dictionary values to an array for faster iteration
            var allNodes = _nodes.Values.ToArray();

            double cellSizeDeg = MaxDirectHopNm / 60.0;

            var grid = new Dictionary<(int x, int y), List<RouteNode>>();

            // 1. Drop every node into its appropriate grid cell
            foreach (var node in _nodes.Values)
            {
                int cellX = (int)Math.Floor(node.Longitude / cellSizeDeg);
                int cellY = (int)Math.Floor(node.Latitude / cellSizeDeg);
                var key = (cellX, cellY);

                if (!grid.TryGetValue(key, out var cellList))
                {
                    cellList = new List<RouteNode>();
                    grid[key] = cellList;
                }
                cellList.Add(node);
            }
            // 2. Process each cell
            foreach (var kvp in grid)
            {
                var (cx, cy) = kvp.Key;
                var cellNodes = kvp.Value;

                for (int i = 0; i < cellNodes.Count; i++) 
                {
                    var nodeA = cellNodes[i];
                    for (int j = 0; j < cellNodes.Count; j++)
                    {
                        var nodeB = cellNodes[j];
                        CheckAndAddDirectEdge(nodeA, nodeB);
                    }
                }
                // Step B: Compare nodes against adjacent cells. 
                // We only check 4 directions (East, South-East, South, South-West) 
                // to prevent double-counting connections we already made from the other side!
                (int, int)[] neighborOffsets = { (1, 0), (1, -1), (0, -1), (-1, -1) };

                foreach (var offset in neighborOffsets)
                {
                    int nx = cx + offset.Item1;
                    int ny = cy + offset.Item2;
                    var neighborKey = (nx, ny);

                    if (grid.TryGetValue(neighborKey, out var neighborNodes))
                    {
                        foreach (var nodeA in cellNodes)
                        {
                            foreach (var nodeB in neighborNodes)
                            {
                                CheckAndAddDirectEdge(nodeA, nodeB);
                            }
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
