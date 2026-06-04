using System;
using System.Collections.Generic;
using System.Text;
using AeroSim2026.EFModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AeroSim2026.Models;
using AeroSim2026.Core.Routing;

namespace AeroSim2026.Core.Services
{
    public class FlightServices : IFlightServices
    {
        private readonly ILogger<FlightServices> logger;
        private readonly Aerosim2026Context context;
        private readonly IAirportServices airportServices;
        private readonly IAircraftServices aircraftServices;
        private readonly INavigationServices navigationServices;
        private readonly RoutingGraph _routingGraph;
        private readonly IServiceScopeFactory _scopeFactory;



        private const string CruiseSpeedPropertyId = "b7257438-0d1e-11f1-8f56-00155dcf273e";
        private const string RangePropertyId = "c38c6aa7-fe53-11f0-ae5d-0a0027000002";
        private const string MinTakeoffPropertyId = "adee14ee-fe53-11f0-ae5d-0a0027000002";
        private const string MinLandingPropertyId = "adef229b-fe53-11f0-ae5d-0a0027000002";

        public FlightServices(ILogger<FlightServices> logger, Aerosim2026Context context, IAirportServices airportServices, IAircraftServices aircraftServices, INavigationServices navigationServices, RoutingGraph routingGraph, IServiceScopeFactory scopeFactory)
        {
            this.logger = logger;
            this.context = context;
            this.airportServices = airportServices;
            this.aircraftServices = aircraftServices;
            this.navigationServices = navigationServices;
            _routingGraph = routingGraph;
            _scopeFactory = scopeFactory;
        }

        public async Task BuildCorridorGraphAsync(Airport origin, Airport destination)
        {
            double padding = 5.0; // Add 5 degree padding around the direct line
            double minLat = Math.Min(origin.Laty, destination.Laty) - padding;
            double maxLat = Math.Max(origin.Laty, destination.Laty) + padding;
            double minLon = Math.Min(origin.Lonx, destination.Lonx) - padding;
            double maxLon = Math.Max(origin.Lonx, destination.Lonx) + padding;

            using var scope = _scopeFactory.CreateScope();
            var bgContext = scope.ServiceProvider.GetRequiredService<Aerosim2026Context>();

            var airways = await bgContext.Airways
                .Include(a => a.FromWaypoint)
                .Include(a => a.ToWaypoint)
                .Where(a => a.FromLaty >= minLat && a.FromLaty <= maxLat &&
                    a.FromLonx >= minLon && a.FromLonx <= maxLon)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            var waypointIds = airways.Select(a => a.FromWaypointId)
                .Union(airways.Select(a => a.ToWaypointId))
                .Distinct()
                .ToList();

            var navSearches = await bgContext.NavSearches
            .Where(n => n.WaypointId != null && waypointIds.Contains(n.WaypointId.Value))
             .Select(n => new { n.WaypointId, n.VorId, n.NdbId })
             .AsNoTracking()
             .AsSplitQuery()
             .ToListAsync();

            var navTypeLookup = new Dictionary<int, string>();
            foreach (var nav in navSearches)
            {
                int wpId = nav.WaypointId!.Value;
                if (nav.VorId.HasValue) navTypeLookup[wpId] = "VOR";
                else if (nav.NdbId.HasValue) navTypeLookup[wpId] = "NDB";
                else navTypeLookup[wpId] = "WAYPOINT";
            }

            _routingGraph.BuildGraph(airways, navTypeLookup);
        }

        public async Task<List<FlightPlan>> GetAllFlights()
        {
            try
            {
                return await context.FlightPlans
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)

                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)
                    .Include(fp => fp.AircraftModel)
                    .AsSplitQuery()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving flight plans");
                throw;
            }
        }
        public async Task<List<FlightPlan>> GetflownFlights()
        {
            try
            {
                return await context.FlightPlans.Where(fp => fp.DateFlown != null)
                    .Include(fp => fp.FlightPlanRoutes.OrderBy(r => r.SequenceNumber))
                        .ThenInclude(route => route.Waypoint)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.PrimaryEnd)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.PrimaryEnd)
                    .Include(fp => fp.AircraftModel)
                        .ThenInclude(am => am.ManufacturerNavigation)
                    .Include(fp => fp.AircraftModel)
                        .ThenInclude(am => am.AircraftTypeNavigation)

                    .OrderBy(fp => fp.DateCreated)
                    .AsSplitQuery()
                    .ToListAsync();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving unflown flight plans");
                throw;
            }
        }
        public async Task<List<FlightPlan>> GetUnflownFlights()
        {
            try
            {
                return await context.FlightPlans.Where(fp => fp.DateFlown == null)
                    .Include(fp => fp.FlightPlanRoutes.OrderBy(r => r.SequenceNumber))
                        .ThenInclude(route => route.Waypoint)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.PrimaryEnd)
                    .Include(fp => fp.StartAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.SecondaryEnd)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCity)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoCountry)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin1)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin2)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.AirportsLocation)
                            .ThenInclude(al => al.GeoAdmin3)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.PrimaryEnd)
                    .Include(fp => fp.EndAirport)
                        .ThenInclude(a => a.Runways)
                            .ThenInclude(r => r.SecondaryEnd)
                    .Include(fp => fp.AircraftModel)
                        .ThenInclude(am => am.ManufacturerNavigation)
                    .Include(fp => fp.AircraftModel) 
                        .ThenInclude(am => am.AircraftTypeNavigation)
                    .OrderBy(fp => fp.DateCreated)
                    .AsSplitQuery()
                    .ToListAsync();

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving unflown flight plans");
                throw;
            }
        }
        public async Task<FlightPlan> SaveFlightPlanAsync(FlightPlan flightPlan)
{
    // 0. The Desktop Silver Bullet: Flush the tracker!
    // This wipes EF Core's memory of any waypoints it tracked during previous random flight generations.
    context.ChangeTracker.Clear();

    // 1. Explicitly copy the IDs to satisfy SQLite NOT NULL constraints
    if (flightPlan.StartAirport != null) flightPlan.StartAirportId = flightPlan.StartAirport.AirportId;
    if (flightPlan.EndAirport != null) flightPlan.EndAirportId = flightPlan.EndAirport.AirportId;
    if (flightPlan.AircraftModel != null) flightPlan.AircraftModelId = flightPlan.AircraftModel.AircraftModelId;

    // 2. Setup caches for our complex navigation properties
    var waypointCache = new Dictionary<FlightPlanRoute, Waypoint?>();
    var airwayCache = new Dictionary<FlightPlanRoute, Airway?>();
    
    var startAirportCache = flightPlan.StartAirport;
    var endAirportCache = flightPlan.EndAirport;
    var aircraftCache = flightPlan.AircraftModel;

    // 3. Hide root navigation objects from EF Core
    flightPlan.StartAirport = null;
    flightPlan.EndAirport = null;
    flightPlan.AircraftModel = null;

    // 4. Hide Route navigation objects to prevent Identity Conflicts
    if (flightPlan.FlightPlanRoutes != null)
    {
        foreach (var route in flightPlan.FlightPlanRoutes)
        {
            // Cache the objects
            waypointCache[route] = route.Waypoint;
            airwayCache[route] = route.Airway;

            // Blind EF Core to the objects so it doesn't traverse them
            route.Waypoint = null;
            route.Airway = null;
        }
    }

    try
    {
        // 5. Add and Save! Because the objects are null, EF Core just uses the IDs
        context.FlightPlans.Add(flightPlan);
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error adding flight plan");
        throw;
    }
    finally
    {
        // 6. Restore EVERYTHING so the UI Map and Details still render perfectly
        flightPlan.StartAirport = startAirportCache;
        flightPlan.EndAirport = endAirportCache;
        flightPlan.AircraftModel = aircraftCache;

        if (flightPlan.FlightPlanRoutes != null)
        {
            foreach (var route in flightPlan.FlightPlanRoutes)
            {
                if (waypointCache.TryGetValue(route, out var cachedWaypoint))
                {
                    route.Waypoint = cachedWaypoint;
                }
                if (airwayCache.TryGetValue(route, out var cachedAirway))
                {
                    route.Airway = cachedAirway;
                }
            }
        }
    }

    return flightPlan;
}
        public async Task<FlightPlan> UpdateFlightPlanAsync(FlightPlan flightPlan)
        {
            try
            {
                context.ChangeTracker.Clear();
                flightPlan.LastModified = DateTime.Now;

                // Fetch the existing record directly from the database context
                var existingFlight = await context.FlightPlans.FindAsync(flightPlan.FlightPlanId);

                if (existingFlight != null)
                {
                    // Only update the scalar properties that can actually change in the UI
                    existingFlight.Comments = flightPlan.Comments;
                    existingFlight.DateFlown = flightPlan.DateFlown;
                    existingFlight.CruiseAltitude = flightPlan.CruiseAltitude;
                    existingFlight.AircraftCrashed = flightPlan.AircraftCrashed;
                    existingFlight.LastModified = flightPlan.LastModified;

                    // Because existingFlight is being natively tracked, EF knows exactly what to UPDATE
                    // without ever touching your complex UI object graph!
                    await context.SaveChangesAsync();
                }

                return flightPlan;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error updating flight plan for {flightPlan.FlightPlanId}", flightPlan.FlightPlanId);
                throw;
            }
        }
        public async Task<FlightPlan?> GetFlightPlanWithRoutesAsync(string flightPlanId)
        {
            try
            {
                return await context.FlightPlans
                    .Include(fp => fp.FlightPlanRoutes)
                        .ThenInclude(fpr => fpr.Airway)
                    .Include(fp => fp.FlightPlanRoutes)
                        .ThenInclude(fpr => fpr.Waypoint)
                        .AsSplitQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(fp => fp.FlightPlanId == flightPlanId)
                    ;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error retrieving flight plan for {flightPlanId}", flightPlanId);
                throw;
            }
        }
        public async Task<GeneratedFlight> BuildRandomFlightAsync(RandomFlightParams flightParams)
        {
            GeneratedFlight generatedFlight = new GeneratedFlight();
            Int32 PlannedSpeed = 150;
            Int32 MaxRange = 500;
            Int32 MinTakeoff = 1000;
            Int32 MinLanding = 1000;
            double TotalDistance = 0.00;

            Coordinates departCoords;


            var aircraft = await aircraftServices.GetSimAircraftWithPropertiesAsync(flightParams.SimAircraftId);
            var cruiseSpeed = aircraft.Properties.FirstOrDefault(p => p.PropertyId == CruiseSpeedPropertyId);
            if (cruiseSpeed != null && int.TryParse(cruiseSpeed.PropertyValue?.ToString(), out int speed))
                PlannedSpeed = speed;
            var range = aircraft.Properties.FirstOrDefault(p => p.PropertyId == RangePropertyId);
            if (range != null && int.TryParse(range.PropertyValue?.ToString(), out int rangeValue))
                MaxRange = (Int32)rangeValue - (rangeValue * 32 / 100);
            var minTakeoff = aircraft.Properties.FirstOrDefault(p => p.PropertyId == MinTakeoffPropertyId);
            if (minTakeoff != null && int.TryParse(minTakeoff.PropertyValue?.ToString(), out int minTakeoffValue))
                MinTakeoff = minTakeoffValue;
            var minLanding = aircraft.Properties.FirstOrDefault(p => p.PropertyId == MinLandingPropertyId);
            if (minLanding != null && int.TryParse(minLanding.PropertyValue?.ToString(), out int minLandingValue))
                MinLanding = minLandingValue;
            flightParams.MaxRange = MaxRange;
            flightParams.CruiseSpeed = PlannedSpeed;
            flightParams.MinRotateRunwayLength = (int)(MinTakeoff * 1.2);
            flightParams.MinLandingRunwayLength = (int)(MinLanding * 1.2);
            if (flightParams.MaxDistance == 0.0)
                flightParams.MaxDistance = (double)MaxRange;

            var originAirport = await airportServices.RandomStartAsync(flightParams);
            // Set the departure airport details in the flight parameters
            flightParams.DepartureAirportId = originAirport.AirportId;
            flightParams.DepartureAirportIdent = originAirport.Ident;

            departCoords = new Coordinates(originAirport.Laty, originAirport.Lonx);
            flightParams.Coordinates = departCoords;



            var arrivalAirport = await airportServices.RandomArrivalAirportAsync(flightParams);

            TotalDistance = navigationServices.CalculateDistance(originAirport.Laty, originAirport.Lonx,
                    arrivalAirport.Laty, arrivalAirport.Lonx);
            var ete = navigationServices.CalculateEte(TotalDistance, PlannedSpeed);


            generatedFlight.Aircraft = aircraft;
            generatedFlight.OriginAirport = originAirport;
            generatedFlight.ArrivalAirport = arrivalAirport;
            generatedFlight.PlannedSpeed = PlannedSpeed;
            generatedFlight.CruiseAltitude = flightParams.CruiseAltitude;
            generatedFlight.DistanceNm = TotalDistance;
            generatedFlight.EstFlightTime = ete;

            return generatedFlight;
        }
        public async Task DeleteFlightPlanAsync(string flightPlanId)
        {
            try
            {
                var flightPlan = await context.FlightPlans.FindAsync(flightPlanId);
                if (flightPlan != null)
                {
                   await context.FlightPlanRoutes
                        .Where(r => r.FlightPlanId == flightPlanId)
                        .ExecuteDeleteAsync();

                    await context.FlightPlans
                        .Where(fp => fp.FlightPlanId == flightPlanId)
                        .ExecuteDeleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error deleting flight plan for {flightPlanId}", flightPlanId);
                throw;
            }

        }
    }
}
