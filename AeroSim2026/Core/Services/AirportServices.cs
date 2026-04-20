using AeroSim2026.Core.Converters;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public class AirportServices : IAirportServices
    {
        private readonly ILogger<AircraftServices> _logger;
        private readonly Aerosim2026Context _context;

        public AirportServices(ILogger<AircraftServices> logger, Aerosim2026Context context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<List<Airport>> GetAirportsList()
        {
            try
            {
                return await _context.Airports
                    // Branch 1: City
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCity)

                    // Branch 2: Country
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCountry)

                    // Branch 3: Admin 1 (State/Province)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin1)

                    // Branch 4: Admin 2 (County/Region)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin2)

                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured getting the Airport List.");
                throw;
            }
        }
        public async Task<Airport> GetAirportAsync(int airportId)
        {
            try
            {
#pragma warning disable CS8603 // Possible null reference return.
                return await _context.Airports
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCity)

                    // Branch 2: Country
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCountry)

                    // Branch 3: Admin 1 (State/Province)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin1)

                    // Branch 4: Admin 2 (County/Region)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin2)
                    .Include(a => a.AirportsComs)
                    .Include(a => a.Runways)

                    .FirstOrDefaultAsync(a => a.AirportId == airportId) ?? null;
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured getting the Airport List.");
                throw;
            }
        }
        public async Task<Airport> GetAirportByIdentAsync(string airportIdent)
        {
            try
            {
                string searchIdent = airportIdent.Trim().ToLower();

#pragma warning disable CS8603 // Possible null reference return.
                return await _context.Airports
                    .AsSplitQuery()
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCity)

                    // Branch 2: Country
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoCountry)

                    // Branch 3: Admin 1 (State/Province)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin1)

                    // Branch 4: Admin 2 (County/Region)
                    .Include(a => a.AirportsLocation)
                        .ThenInclude(al => al.GeoAdmin2)
                    .Include(a => a.AirportsComs)
                    .Include(a => a.Runways)

                    .FirstOrDefaultAsync(a => a.Ident != null && a.Ident.ToLower() == searchIdent);
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured getting the Airport List.");
                throw;
            }
        }
        public async Task<List<AirportType>> GetAirportTypesAsync()
        {
            try
            {
                return await _context.AirportTypes.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured getting the Airport Types List.");
                throw;
            }
        }
        public async Task<List<Continentcode>> GetContinentListAsync()
        {
            try
            {
                return await _context.Continentcodes.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured getting the Continent List.");
                throw;
            }
        }
        public async Task<Airport> RandomStartAsync(RandomFlightParams randomParams)
        {
            
            Airport? ap = new Airport();
            try
            {
                if (String.IsNullOrEmpty(randomParams.DepartureAirportIdent)) // If no specified departure airport, we'll pick a random one in the arrival method. This is just for when we want to start at a specific airport.
                {
                    if (!String.IsNullOrEmpty(randomParams.Continent))
                    {
                        if (randomParams.DepartAirportTypeId != 0)
                        {
                            ap = await _context.Airports
                            .Where(a => a.AirportType == randomParams.DepartAirportTypeId && a.LongestRunwayLength >= randomParams.MinRotateRunwayLength && a.AirportsLocation.ContinentCode.Equals(randomParams.Continent))
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCity)
                            // Branch 2: Country
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCountry)
                            // Branch 3: Admin 1 (State/Province)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin1)
                            // Branch 4: Admin 2 (County/Region)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin2)
                            .Include(a => a.AirportsComs)
                            .Include(a => a.Runways)
                            .OrderBy(a => EF.Functions.Random())
                                .FirstOrDefaultAsync();
                        }
                        else
                        {
                            ap = await _context.Airports
                            .Where(a =>  a.LongestRunwayLength >= randomParams.MinRotateRunwayLength && a.AirportsLocation.ContinentCode.Equals(randomParams.Continent))
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCity)
                            // Branch 2: Country
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCountry)
                            // Branch 3: Admin 1 (State/Province)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin1)
                            // Branch 4: Admin 2 (County/Region)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin2)
                            .Include(a => a.AirportsComs)
                            .Include(a => a.Runways)
                            .OrderBy(a => EF.Functions.Random())
                                .FirstOrDefaultAsync();
                        }
                    }
                    else
                    {
                        if (randomParams.DepartAirportTypeId != 0)
                        {
                            ap = await _context.Airports
                            .Where(a => a.AirportType == randomParams.DepartAirportTypeId && a.LongestRunwayLength >= randomParams.MinRotateRunwayLength)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCity)
                            // Branch 2: Country
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCountry)
                            // Branch 3: Admin 1 (State/Province)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin1)
                            // Branch 4: Admin 2 (County/Region)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin2)
                            .Include(a => a.AirportsComs)
                            .Include(a => a.Runways)
                            .OrderBy(a => EF.Functions.Random())
                                .FirstOrDefaultAsync();
                        }
                        else
                        {
                            ap = await _context.Airports
                            .Where(a => a.LongestRunwayLength >= randomParams.MinRotateRunwayLength)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCity)
                            // Branch 2: Country
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoCountry)
                            // Branch 3: Admin 1 (State/Province)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin1)
                            // Branch 4: Admin 2 (County/Region)
                            .Include(a => a.AirportsLocation)
                                .ThenInclude(al => al.GeoAdmin2)
                            .Include(a => a.AirportsComs)
                            .Include(a => a.Runways)
                            .OrderBy(a => EF.Functions.Random())
                                .FirstOrDefaultAsync();
                        }
                    }
                }
                else
                {
                   
                    ap = await GetAirportByIdentAsync(randomParams.DepartureAirportIdent!);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RandomStartAsync");
            }
            return ap!;
        }
        public async Task<Airport> RandomArrivalAirportAsync(RandomFlightParams randomParams)
        {
            Airport? ap = new Airport();
            try
            {
                // 1. Get the starting coordinates
                double startLat = (double)randomParams.Coordinates!.Latitude;
                double startLon = (double)randomParams.Coordinates!.Longitude;
                double maxDistNm = randomParams.MaxDistance;

                // 2. Calculate the Bounding Box (Rough Square)
                // 1 Nautical Mile is exactly 1 minute of latitude (1/60th of a degree)
                double latDelta = maxDistNm / 60.0;

                // Longitude distance shrinks as you move away from the equator, so we adjust with Cosine
                double lonDelta = maxDistNm / (60.0 * Math.Cos(startLat * Math.PI / 180.0));

                double minLat = startLat - latDelta;
                double maxLat = startLat + latDelta;
                double minLon = startLon - lonDelta;
                double maxLon = startLon + lonDelta;

                // 3. Find the longest runways (Same as your old code)
                var longestRunway = _context.Runways
                    .GroupBy(runway => runway.AirportId)
                    .Select(group => new
                    {
                        AirportId = group.Key,
                        MaxRunwayLength = group.Max(rw => rw.Length),
                    })
                    .Where(r => r.MaxRunwayLength > randomParams.MinRotateRunwayLength);

                // 4. THE MAGIC: Pre-filter in SQLite using the Bounding Box!
                // This drops the memory load from 30,000+ airports down to just a few dozen.
                var preFilteredAirports = await _context.Airports
    .Where(a => a.AirportId != randomParams.DepartureAirportId)
    .Where(a => a.Laty >= minLat && a.Laty <= maxLat)
    .Where(a => a.Lonx >= minLon && a.Lonx <= maxLon)
    // Note: Using MinLandingRunwayLength since this is the destination
    .Where(a => a.LongestRunwayLength >= randomParams.MinLandingRunwayLength)
    .ToListAsync();

                // 5. Apply the precise circle distance and type filters in memory on the tiny dataset
                List<RandomArrivalAirport> validArrivals = new List<RandomArrivalAirport>();
                Coordinates departCoords = new Coordinates(startLat, startLon);

                foreach (var airport in preFilteredAirports)
                {
                    Coordinates arrivalCoords = new Coordinates((double)airport.Laty!, (double)airport.Lonx!);
                    double preciseDistance = CoordinatesDistance.DistanceTo(departCoords, arrivalCoords, randomParams.UnitOfLength);

                    if (preciseDistance >= randomParams.MinDistance && preciseDistance <= randomParams.MaxDistance)
                    {
                        validArrivals.Add(new RandomArrivalAirport
                        {
                            AirportId = airport.AirportId,
                            Distance = preciseDistance,
                            Coordinates = arrivalCoords,
                            TypeId = (int)(airport.AirportType ?? 0),
                            UnitOfLength = randomParams.UnitOfLength
                        });
                    }
                }

                // 6. Filter by Airport Type
                if (randomParams.ArrivalAirportTypeId != 0)
                {
                    validArrivals = validArrivals.Where(a => a.TypeId == randomParams.ArrivalAirportTypeId).ToList();
                }
                else
                {
                    validArrivals = validArrivals.Where(a => a.TypeId == 1 || a.TypeId == 2 || a.TypeId == 3).ToList();
                }

                // 7. Pick a random valid airport
                var finalSelection = validArrivals.OrderBy(g => Guid.NewGuid()).FirstOrDefault();

                // 8. Fetch the full details for the winner
                if (finalSelection != null)
                {
                    ap = await GetAirportAsync(finalSelection.AirportId);
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RandomArrivalAirportAsync");
            }

            return ap!;
        }
        public static int GetRandomTypeAsync()
        {
            var numbers = new List<int> { 1, 2, 3, 8 };

            int randomInt = numbers[Random.Shared.Next(numbers.Count)];

            return randomInt;
        }

    }
}
