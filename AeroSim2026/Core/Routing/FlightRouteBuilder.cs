using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Added for async Tasks

namespace AeroSim2026.Core.Routing
{
    public class FlightRouteBuilder
    {
        private readonly ConnectionManager _connectionManager;
        private readonly RouteFinderService _routeFinder;
        private readonly INavigationServices _navigationServices;
        private readonly RoutingGraph _graph;

        // 1. Inject the graph and nav services so the Builder can initialize itself!
        public FlightRouteBuilder(ConnectionManager connectionManager, RouteFinderService routeFinder, INavigationServices navigationServices, RoutingGraph graph)
        {
            _connectionManager = connectionManager;
            _routeFinder = routeFinder;
            _navigationServices = navigationServices;
            _graph = graph;
        }

        public async Task<List<ProposedRoute>> GenerateAlternativeRoutesAsync(Airport origin, Airport destination, int cruiseAltitude)
        {
            await EnsureGraphIsLoadedAsync();

            var entryNode = _connectionManager.FindOptimalEntryNode(origin, destination);
            var exitNode = _connectionManager.FindOptimalExitNode(origin, destination);

            if (entryNode == null || exitNode == null) return new List<ProposedRoute>();

            return _routeFinder.FindAlternativesRoutes(entryNode, exitNode, cruiseAltitude);
        }

        // 2. Change to async Task
        public async Task<List<FlightPlanRoute>> GenerateRouteAsync(Airport origin, Airport destination, int cruiseAltitude)
        {
            await EnsureGraphIsLoadedAsync();

            var routeList = new List<FlightPlanRoute>();
            int sequence = 1;
            var entryNode = _connectionManager.FindOptimalEntryNode(origin, destination);
            var exitNode = _connectionManager.FindOptimalExitNode(origin, destination);

            if (entryNode == null || exitNode == null)
            {
                return CreateDirectRoute(origin, destination);
            }

            var pathEdges = _routeFinder.FindRoute(entryNode, exitNode, cruiseAltitude, 10.0, 1.25);
            if (pathEdges == null || pathEdges.Count == 0)
            {
                return CreateDirectRoute(origin, destination);
            }

            // 3. Add the "Direct" leg from Airport -> First Fix, and pass the NavType!
            routeList.Add(new FlightPlanRoute
            {
                SequenceNumber = sequence++,
                WaypointId = entryNode.WaypointId,
                AirwayId = null,
                Waypoint = new Waypoint
                {
                    Ident = entryNode.Identifier,
                    Laty = entryNode.Latitude,
                    Lonx = entryNode.Longitude,
                    WaypointType = entryNode.NavType // <--- The UI needs this for the SVG!
                }
            });

            // 4. Add the Enroute legs, passing the NavType!
            foreach (var edge in pathEdges)
            {
                routeList.Add(new FlightPlanRoute
                {
                    SequenceNumber = sequence++,
                    WaypointId = edge.TargetNode.WaypointId,
                    AirwayId = edge.AirwayId,
                    Waypoint = new Waypoint
                    {
                        Ident = edge.TargetNode.Identifier,
                        Laty = edge.TargetNode.Latitude,
                        Lonx = edge.TargetNode.Longitude,
                        WaypointType = edge.TargetNode.NavType // <--- The UI needs this for the SVG!
                    }
                });
            }
            return routeList;
        }

        private List<FlightPlanRoute> CreateDirectRoute(Airport origin, Airport destination)
        {
            return new List<FlightPlanRoute>();
        }

        // 5. The Just-In-Time Loader
        private async Task EnsureGraphIsLoadedAsync()
        {
            // If the graph already has nodes, skip loading! (Instant route generation)
            if (!_graph.GetAllNodes().Any())
            {
                // Task.Run pushes the heavy EF Core query entirely to a background thread,
                // guaranteeing that your UI will NEVER freeze while it builds!
                await Task.Run(async () =>
                {
                    await _navigationServices.InitializeRoutingGraphAsync(_graph);
                });
            }
        }
    }
}