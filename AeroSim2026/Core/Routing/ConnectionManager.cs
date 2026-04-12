using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AeroSim2026.EFModels;
using AeroSim2026.Models; // Required for the Airport model

namespace AeroSim2026.Core.Routing
{
    public class ConnectionManager
    {
        private readonly RoutingGraph _graph;

        public ConnectionManager(RoutingGraph graph)
        {
            _graph = graph;
        }

        // Keeping your original method as a fallback just in case
        public RouteNode? FindNearestEntryNode(double lat, double lon, double maxRangeNm = 100)
        {
            RouteNode? bestNode = null;
            double bestDist = double.MaxValue;

            foreach (var node in _graph.GetAllNodes())
            {
                double dist = GeoMath.Distance(lat, lon, node.Latitude, node.Longitude);
                if (dist < bestDist && dist <= maxRangeNm)
                {
                    bestDist = dist;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        public RouteNode? FindOptimalEntryNode(Airport origin, Airport destination, double maxRangeNm = 100)
        {
            double targetBearing = GeoMath.Bearing(origin.Laty, origin.Lonx, destination.Laty, destination.Lonx);

            RouteNode? bestNode = null;
            double bestScore = double.MaxValue;

            foreach (var node in _graph.GetAllNodes())
            {
                double distance = GeoMath.Distance(origin.Laty, origin.Lonx, node.Latitude, node.Longitude);
                if (distance > maxRangeNm) continue;

                double nodeBearing = GeoMath.Bearing(origin.Laty, origin.Lonx, node.Latitude, node.Longitude);

                // 1. Directional Check
                double bearingDiff = Math.Abs(targetBearing - nodeBearing);
                if (bearingDiff > 180) bearingDiff = 360 - bearingDiff;

                // Skip nodes that are behind us (more than 90 degrees off the target bearing)
                if (bearingDiff > 90) continue;

                // 2. Additive Penalties
                // Directional Penalty: Adds up to +30nm artificial distance the further off-course it is
                double directionalPenalty = (bearingDiff / 90.0) * 30.0;

                // Type Penalty: Flat artificial distance added to non-VORs
                double typePenalty = 40.0; // Default worst-case (Intersections/Waypoints)
                if (node.NavType == "V") typePenalty = 0.0;
                else if (node.NavType == "N") typePenalty = 15.0;

                // 3. Final Score Calculation (Additive - Lower is better)
                double score = distance + directionalPenalty + typePenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }

        public RouteNode? FindOptimalExitNode(Airport origin, Airport destination, double maxRangeNm = 100)
        {
            double targetBearing = GeoMath.Bearing(origin.Laty, origin.Lonx, destination.Laty, destination.Lonx);

            RouteNode? bestNode = null;
            double bestScore = double.MaxValue;

            foreach (var node in _graph.GetAllNodes())
            {
                double distance = GeoMath.Distance(destination.Laty, destination.Lonx, node.Latitude, node.Longitude);
                if (distance > maxRangeNm) continue;

                // For the exit node, we want the bearing FROM the node TO the destination 
                // to roughly match our overall flight direction (targetBearing).
                double nodeToDestBearing = GeoMath.Bearing(node.Latitude, node.Longitude, destination.Laty, destination.Lonx);

                // 1. Directional Check
                double bearingDiff = Math.Abs(targetBearing - nodeToDestBearing);
                if (bearingDiff > 180) bearingDiff = 360 - bearingDiff;

                // Skip nodes that would require over-flying and turning around to reach the destination
                if (bearingDiff > 90) continue;

                // 2. Additive Penalties
                double directionalPenalty = (bearingDiff / 90.0) * 30.0;

                double typePenalty = 40.0;
                if (node.NavType == "V") typePenalty = 0.0;
                else if (node.NavType == "N") typePenalty = 15.0;

                // 3. Final Score Calculation (Additive)
                double score = distance + directionalPenalty + typePenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }
    }
}