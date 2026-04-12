using System;
using System.Collections.Generic;
using System.Text;
using AeroSim2026.EFModels;
using AeroSim2026.Models;

namespace AeroSim2026.Core.Routing
{
    public class FlightRouteBuilder
    {
        private readonly ConnectionManager _connectionManager;
        private readonly RouteFinderService _routeFinder;

        public FlightRouteBuilder(ConnectionManager connectionManager, RouteFinderService routeFinder)
        {
            _connectionManager = connectionManager;
            _routeFinder = routeFinder;
        }
        public List<ProposedRoute> GenerateAlternativeRoutes(Airport origin, Airport destination, int cruiseAltitude)
        {
            var entryNode = _connectionManager.FindOptimalEntryNode(origin, destination);
            var exitNode = _connectionManager.FindOptimalExitNode(origin, destination);

            if (entryNode == null || exitNode == null)
            {
                // Fallback: Return empty or build a direct line proposal here
                return new List<ProposedRoute>();
            }

            return _routeFinder.FindAlternativesRoutes(entryNode, exitNode, cruiseAltitude);
        }
        public List<FlightPlanRoute> GenerateRoute(Airport origin, Airport destination, int cruiseAltitude)
        {
            var routeList = new List<FlightPlanRoute>();
            int sequence = 1;
            var entryNode = _connectionManager.FindOptimalEntryNode(origin, destination);
            var exitNode = _connectionManager.FindOptimalExitNode(origin, destination);
            
            
            if (entryNode == null || exitNode == null)
            {
                // Fallback: Direct GPS
                return CreateDirectRoute(origin, destination);
            }

            var pathEdges = _routeFinder.FindRoute(entryNode, exitNode, cruiseAltitude, 10.0, 1.25);
            if (pathEdges == null || pathEdges.Count == 0)
            {
                return CreateDirectRoute(origin, destination);
            }
            // Add the "Direct" leg from Airport -> First Fix
            routeList.Add(new FlightPlanRoute
            {                
                SequenceNumber = sequence++,
                WaypointId = entryNode.WaypointId,
                AirwayId = null
            });

            // Add the Enroute legs
            foreach (var edge in pathEdges)
            {
                routeList.Add(new FlightPlanRoute
                {                    
                    SequenceNumber = sequence++,
                    WaypointId = edge.TargetNode.WaypointId,
                    AirwayId = edge.AirwayId
                });
            }
            return routeList;
        }
        private List<FlightPlanRoute> CreateDirectRoute(Airport origin, Airport destination)
        {
            return new List<FlightPlanRoute>();
        }
    }
}
