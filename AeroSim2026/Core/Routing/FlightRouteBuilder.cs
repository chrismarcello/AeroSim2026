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

            // Pass the origin and destination to the service
            return _routeFinder.FindAlternativesRoutes(entryNode, exitNode, origin, destination, cruiseAltitude);
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

            var pathEdges = _routeFinder.FindRoute(entryNode, exitNode, origin, destination, cruiseAltitude, 10.0, 1.25);
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
                // Identify geometric "bend" nodes that don't have an official aviation name.
                // In the database these show up as literally "NULL", or fallback to "FIX" in the Graph.
                bool isUnnamedNode = string.IsNullOrWhiteSpace(edge.TargetNode!.Identifier) ||
                                     edge.TargetNode.Identifier.Equals("NULL", StringComparison.OrdinalIgnoreCase) ||
                                     edge.TargetNode.Identifier.Equals("FIX", StringComparison.OrdinalIgnoreCase);

                // If it's just a bend in the airway, skip saving it. 
                // The algorithm will naturally jump to the next named waypoint on this same airway!
                if (isUnnamedNode)
                {
                    continue;
                }

                routeList.Add(new FlightPlanRoute
                {
                    SequenceNumber = sequence++,
                    WaypointId = edge.TargetNode!.WaypointId,
                    AirwayId = edge.AirwayId,
                    Airway = edge.AirwayId.HasValue ? new Airway
                    {
                        AirwayId = edge.AirwayId.Value,
                        MinimumAltitude = edge.MinimumAltitude,
                        MaximumAltitude = edge.MaximumAltitude
                    } : null,
                    Waypoint = new Waypoint
                    {
                        Ident = edge.TargetNode.Identifier,
                        Laty = edge.TargetNode.Latitude,
                        Lonx = edge.TargetNode.Longitude,
                        WaypointType = edge.TargetNode.NavType // <--- The UI needs this for the SVG!
                    }
                });
            }

            ApplyVnavProfiles(origin, destination, routeList, cruiseAltitude);

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

        public static void ApplyVnavProfiles(Airport origin, Airport destination, List<FlightPlanRoute> routeList, int cruiseAltitude)
        {
            if (routeList == null || !routeList.Any()) return;

            double originAlt = origin.Altitude;
            double destAlt = destination.Altitude;

            // Ensure Cruise Altitude is physically possible! 
            // It must be higher than both the origin and destination (let's add a 2,000 ft safety buffer)
            double highestAirportElevation = Math.Max(originAlt, destAlt);
            double minSafeCruise = highestAirportElevation + 2000.0;

            // Override the cruise altitude if it's below the minimum safe level
            double safeCruiseAltitude = Math.Max(cruiseAltitude, minSafeCruise);

            const double climbGradient = 500.0;
            const double descentGradient = 318.0;

            var distancesFromOrigin = new List<double>();
            double accumulatedDist = 0;
            double currentLat = origin.Laty;
            double currentLon = origin.Lonx;

            foreach (var routeItem in routeList)
            {
                if (routeItem.Waypoint != null)
                {
                    double legDist = GeoMath.Distance(currentLat, currentLon, routeItem.Waypoint.Laty, routeItem.Waypoint.Lonx);
                    accumulatedDist += legDist;
                    distancesFromOrigin.Add(accumulatedDist);

                    currentLat = routeItem.Waypoint.Laty;
                    currentLon = routeItem.Waypoint.Lonx;
                }
                else
                {
                    distancesFromOrigin.Add(accumulatedDist);
                }
            }

            double finalLegDist = GeoMath.Distance(currentLat, currentLon, destination.Laty, destination.Lonx);
            double totalRouteDistance = accumulatedDist + finalLegDist;

            for (int i = 0; i < routeList.Count; i++)
            {
                double distFromOrig = distancesFromOrigin[i];
                double distToDest = totalRouteDistance - distFromOrig;

                double maxClimbAlt = originAlt + (distFromOrig * climbGradient);
                double maxDescAlt = destAlt + (distToDest * descentGradient);

                double calculatedAlt = Math.Min(safeCruiseAltitude, Math.Min(maxClimbAlt, maxDescAlt));

                if (routeList[i].Airway != null)
                {
                    if (routeList[i].Airway.MinimumAltitude.HasValue)
                    {
                        calculatedAlt = Math.Max(calculatedAlt, routeList[i].Airway.MinimumAltitude!.Value);
                    }

                    if (routeList[i].Airway.MaximumAltitude.HasValue)
                    {
                        calculatedAlt = Math.Min(calculatedAlt, routeList[i].Airway.MaximumAltitude!.Value);
                    }
                }

                routeList[i].PlannedAltitude = Math.Round(calculatedAlt / 100.0) * 100.0; // Round to nearest 100 ft
            }
        }
    }
}