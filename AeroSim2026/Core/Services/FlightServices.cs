using System;
using System.Collections.Generic;
using System.Text;
using AeroSim2026.EFModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using AeroSim2026.Models;

namespace AeroSim2026.Core.Services
{
    public class FlightServices : IFlightServices
    {
        private readonly ILogger<FlightServices> logger;
        private readonly Aerosim2026Context context;
        private readonly IAirportServices airportServices;
        private readonly IAircraftServices aircraftServices;
        private readonly INavigationServices navigationServices;

        private const string CruiseSpeedPropertyId = "b7257438-0d1e-11f1-8f56-00155dcf273e";
        private const string RangePropertyId = "c38c6aa7-fe53-11f0-ae5d-0a0027000002";
        private const string MinTakeoffPropertyId = "adee14ee-fe53-11f0-ae5d-0a0027000002";
        private const string MinLandingPropertyId = "adef229b-fe53-11f0-ae5d-0a0027000002";

        public FlightServices(ILogger<FlightServices> logger, Aerosim2026Context context, IAirportServices airportServices, IAircraftServices aircraftServices, INavigationServices navigationServices)
        {
            this.logger = logger;
            this.context = context;
            this.airportServices = airportServices;
            this.aircraftServices = aircraftServices;
            this.navigationServices = navigationServices;
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
            try
            {
                context.FlightPlans.Add(flightPlan);
                await context.SaveChangesAsync();
                return flightPlan;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding flight plan");
                throw;
            }
        }
        public async Task<FlightPlan> UpdateFlightPlanAsync(FlightPlan flightPlan)
        {
            try
            {
                flightPlan.LastModified = DateTime.Now;

                context.FlightPlans.Update(flightPlan);
                await context.SaveChangesAsync();

                return flightPlan;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error updating flight plane for {flightPlan.FlightPlanId}", flightPlan.FlightPlanId);
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
                    .Include(fp => fp.FlightPlanRoutes)     // Add this line
                        .ThenInclude(fpr => fpr.Waypoint)   // Add this line
                    .FirstOrDefaultAsync(fp => fp.FlightPlanId == flightPlanId);
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
    }
}
