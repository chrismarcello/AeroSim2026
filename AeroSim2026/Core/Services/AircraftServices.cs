using AeroSim2026.EFModels;
using AeroSim2026.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft;
using Newtonsoft.Json;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public class AircraftServices : IAircraftServices
    {
        private readonly ILogger<FlightServices> _logger;
        private readonly Aerosim2026Context context;
        private static Dictionary<string, AircraftProperty>? _propertyCache;

        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        public AircraftServices(ILogger<FlightServices> logger, Aerosim2026Context context)
        {
            _logger = logger;
            this.context = context;
        }
        private async Task<Dictionary<string, AircraftProperty>> GetPropertyDefinitionsAsync()
        {
            // FAST PATH: If cache exists, return it immediately (0 database calls)
            if (_propertyCache != null)
            {
                return _propertyCache;
            }

            // SLOW PATH: Cache is empty, let's load it.
            await _cacheLock.WaitAsync(); // Lock to prevent duplicate loading
            try
            {
                // Double-check: Maybe another thread filled it while we waited?
                if (_propertyCache != null)
                {
                    return _propertyCache;
                }

                // HIT THE DATABASE (Only happens once per app launch!)
                _propertyCache = await context.AircraftProperties
                    .ToDictionaryAsync(k => k.PropertyId);

                return _propertyCache;
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        public async Task<List<SimAircraft>> GetSimAircraftsList()
        {
            try
            {
                return await context.SimAircrafts
                    .Include(sa => sa.Aircraft) // 1. Load the main Aircraft record
                        .ThenInclude(am => am.ManufacturerNavigation) // 2. Load the Manufacturer from that Aircraft
                    .Include(sa => sa.Aircraft) // 3. Target Aircraft again
                        .ThenInclude(am => am.AircraftTypeNavigation) 
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flight plans");
                throw;
            }
        }
        public async Task<SimAircraft> GetSimAircraftWithPropertiesAsync(int simAircraftId)
        {
            var simPlane = await context.SimAircrafts
    .Include(sa => sa.Aircraft) // 1. Load the main Aircraft record
        .ThenInclude(am => am.ManufacturerNavigation) // 2. Load the Manufacturer from that Aircraft
    .Include(sa => sa.Aircraft) // 3. Target Aircraft again
        .ThenInclude(am => am.AircraftTypeNavigation) // 4. FIX: Use 'am' (AircraftModel) here, not 'sa'
    .FirstOrDefaultAsync(sa => sa.SimPlaneId == simAircraftId);

            if (simPlane == null) return null!;

            var rawValues = JsonConvert.DeserializeObject<List<JsonPropertyValue>>(simPlane.Aircraft.PropertyValues);

            // 3. Get the Definitions (The metadata)
            // Optimization: Cache this! Don't hit DB every time if it rarely changes.
            var definitions = await GetPropertyDefinitionsAsync();

            // 4. "Stitch" them together into your Rich Objects
            simPlane.Properties = new List<AircraftPropertyValues>();

            foreach (var item in rawValues!)
            {
                if (string.IsNullOrWhiteSpace(item.PropertyId))
                    continue;
                // Try to find the definition for this ID
                if (definitions.TryGetValue(item.PropertyId, out var def))
                {
                    AircraftPropertyValues propObj = null!;
                    switch (def.PropertyType)
                    {
                        case "integer":                            
                            propObj = AircraftPropertyValues.CreateInteger(
                    def.PropertyId, Convert.ToInt32(item!.Value), def.Unit, def.PropertyType, def.PropertyName, def.Description);
                            break;
                        case "string":
                            propObj = AircraftPropertyValues.CreateString(def.PropertyId, (string)item!.Value, def.Unit!, def.PropertyType!, def.PropertyName, def.Description);
                            break;
                            case "boolean":
                            propObj = AircraftPropertyValues.CreateBool(def.PropertyId, (bool)item!.Value, def.Unit!, def.PropertyType!, def.PropertyName, def.Description);
                            break;
                        case "float":
                            propObj = AircraftPropertyValues.CreateFloat(def.PropertyId, Convert.ToSingle(item!.Value.ToString()), def.Unit!, def.PropertyType!, def.PropertyName, def.Description);
                            break;
                        default: break;
                    }
                    if (propObj != null)
                    {
                        simPlane.Properties.Add(propObj);
                    }
                }
            }
            return simPlane;
        }
        public async Task<List<AircraftManufacturer>> GetAircraftManufacturerAsync()
        {
            try
            {
                return await context.AircraftManufacturers
                    .Where(m => m.AircraftTypes.Any())
                    .OrderBy(m => m.ManufacturerName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aircraft manufacturer");
                throw;
            }
        }
        public async Task<List<AircraftType>> GetAircraftTypesForManufacturerAsync(string manufacturerId)
        {
            try
            {
                return await context.AircraftTypes
                    .Where(t => t.ManufacturerNavigation != null && t.ManufacturerNavigation.ManufacturerId == manufacturerId)
                    .OrderBy(t => t.AircraftTypeName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aircraft types for manufacturer {ManufacturerId}", manufacturerId);
                throw;
            }
        }
        public async Task<List<AircraftModel>> GetAircraftModelsForTypeAsync(string aircraftTypeId)
        {
            try
            {
                return await context.AircraftModels
                    .Where(m => m.AircraftTypeNavigation != null && m.AircraftTypeNavigation.AircraftTypeId == aircraftTypeId)
                    .OrderBy(m => m.AircraftName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving aircraft models for type {AircraftTypeId}", aircraftTypeId);
                throw;
            }
        }

    }
}
