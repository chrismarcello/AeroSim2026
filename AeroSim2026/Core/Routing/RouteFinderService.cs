using System;
using System.Collections.Generic;
using System.Linq;
using Mapsui.Nts.Providers.Shapefile.Indexing;
using AeroSim2026.Models;

namespace AeroSim2026.Core.Routing
{
    public class RouteFinderService
    {
        private readonly RoutingGraph _graph;

        public RouteFinderService(RoutingGraph graph)
        {
            _graph = graph;
        }

        // Pass cruiseAltitude in here so we respect the aircraft's capabilities
        public List<ProposedRoute> FindAlternativesRoutes(RouteNode startNode, RouteNode endNode, int cruiseAltitude)
        {
            var routes = new List<ProposedRoute>();

            // Route 1: Standard Shortest Path (balanced penalties)
            var standardEdges = FindRoute(startNode, endNode, cruiseAltitude, switchPenalty: 10.0, directMultiplier: 1.25);
            if (standardEdges != null && standardEdges.Count > 0)
            {
                routes.Add(BuildProposedRoute("Standard Route", standardEdges, startNode));
            }

            // Route 2: Airway Preferred (high penalty for direct GPS routing, lower penalty for staying on airways)
            var airwayEdges = FindRoute(startNode, endNode, cruiseAltitude, switchPenalty: 5.0, directMultiplier: 3.0);

            if (airwayEdges != null && airwayEdges.Count > 0 && !AreRoutesIdentical(standardEdges, airwayEdges))
            {
                routes.Add(BuildProposedRoute("Airway Preferred", airwayEdges, startNode));
            }

            // Optional Route 3: Direct GPS (if you wanted to just show a straight line option for comparison)
            // ...

            return routes;
        }

        // cruiseAltitude is back, along with dynamic penalties
        public List<RouteEdge> FindRoute(RouteNode startNode, RouteNode endNode, int cruiseAltitude, double switchPenalty, double directMultiplier)
        {
            if (startNode == null || endNode == null)
                return new List<RouteEdge>();

            var openSet = new PriorityQueue<RouteNode, double>();
            openSet.Enqueue(startNode, 0);

            var cameFrom = new Dictionary<int, (RouteNode Parent, RouteEdge edge)>();

            var gScore = new Dictionary<int, double>();
            gScore[startNode.WaypointId] = 0;

            var fScore = new Dictionary<int, double>();
            fScore[startNode.WaypointId] = Heuristic(startNode, endNode);

            var closedSet = new HashSet<int>();

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current.WaypointId == endNode.WaypointId)
                    return RecontructPath(cameFrom, current);

                closedSet.Add(current.WaypointId);

                foreach (var edge in current.OutgoingEdges)
                {
                    var neighbor = edge.TargetNode;
                    if (closedSet.Contains(neighbor.WaypointId))
                        continue;

                    // RESTORED: Vital for realistic airway routing
                    if (edge.MinimumAltitude.HasValue && cruiseAltitude < edge.MinimumAltitude.Value)
                    {
                        continue;
                    }
                    if (edge.MaximumAltitude.HasValue && cruiseAltitude > edge.MaximumAltitude.Value)
                    {
                        continue;
                    }

                    double edgeCost = edge.AirwayId == null ? edge.Distance * directMultiplier : edge.Distance;

                    double tentativeG = gScore[current.WaypointId] + edgeCost;

                    if (cameFrom.TryGetValue(current.WaypointId, out var previousStep))
                    {
                        if (previousStep.edge.AirwayId != edge.AirwayId && edge.AirwayId != null)
                            tentativeG += switchPenalty;
                    }

                    if (!gScore.ContainsKey(neighbor.WaypointId) || tentativeG < gScore[neighbor.WaypointId])
                    {
                        cameFrom[neighbor.WaypointId] = (current, edge);
                        gScore[neighbor.WaypointId] = tentativeG;
                        fScore[neighbor.WaypointId] = tentativeG + Heuristic(neighbor, endNode);

                        if (!openSet.UnorderedItems.Any(i => i.Element.WaypointId == neighbor.WaypointId))
                            openSet.Enqueue(neighbor, fScore[neighbor.WaypointId]);
                    }
                }
            }
            return null!;
        }

        private List<RouteEdge> RecontructPath(Dictionary<int, (RouteNode Parent, RouteEdge edge)> cameFrom, RouteNode current)
        {
            var totalPath = new List<RouteEdge>();
            while (cameFrom.ContainsKey(current.WaypointId))
            {
                var (parent, edge) = cameFrom[current.WaypointId];
                totalPath.Add(edge);
                current = parent;
            }
            totalPath.Reverse();
            return totalPath;
        }

        private double Heuristic(RouteNode a, RouteNode b)
        {
            return GeoMath.Distance(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
        }

        private ProposedRoute BuildProposedRoute(string name, List<RouteEdge> edges, RouteNode startNode)
        {
            var route = new ProposedRoute { RouteName = name };
            double cumulative = 0;
            int seq = 1;

            route.Legs.Add(new RouteLeg
            {
                SequenceNumber = seq++,
                Waypoint = startNode,
                DistanceFromPrevious = 0,
                CumulativeDistance = 0
            });

            foreach (var edge in edges)
            {
                cumulative += edge.Distance;
                route.Legs.Add(new RouteLeg
                {
                    SequenceNumber = seq++,
                    Waypoint = edge.TargetNode,
                    AirwayName = edge.AirwayName,
                    DistanceFromPrevious = edge.Distance,
                    CumulativeDistance = cumulative
                });
            }
            route.TotalDistance = cumulative;
            return route;
        }

        private bool AreRoutesIdentical(List<RouteEdge> route1, List<RouteEdge> route2)
        {
            if (route1 == null || route2 == null) return false;
            if (route1.Count != route2.Count) return false;

            for (int i = 0; i < route1.Count; i++)
            {
                if (route1[i].TargetNode.WaypointId != route2[i].TargetNode.WaypointId)
                    return false;
            }
            return true;
        }
    }
}