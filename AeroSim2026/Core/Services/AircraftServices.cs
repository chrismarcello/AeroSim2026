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
                    //.Where(m => m.AircraftTypes.Any())
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
        public async Task DeleteSimAircraftAsync(int simPlaneId)
        {
            try
            {
                var plane = await context.SimAircrafts.FindAsync(simPlaneId);
                if (plane != null)
                {
                    context.SimAircrafts.Remove(plane);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SimAircraft with ID {SimPlaneId}", simPlaneId);
                throw;
            }
        }

        public async Task<AircraftManufacturer> AddAircraftManufacturerAsync(string name, string countryIso)
        {
            try
            {
                var newManufacturer = new AircraftManufacturer
                {
                    ManufacturerId = Guid.NewGuid().ToString(), // Generate a unique string ID
                    ManufacturerName = name,
                    ManufacturerCountry = countryIso
                };

                context.AircraftManufacturers.Add(newManufacturer);
                await context.SaveChangesAsync();

                return newManufacturer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding aircraft manufacturer");
                throw;
            }
        }
        public async Task<List<string>> GetDistinctAircraftFamiliesAsync()
        {
            try
            {
                // Scans the table, grabs just the Family column, removes duplicates, and alphabetizes it!
                return await context.AircraftTypes
                    .Where(t => !string.IsNullOrEmpty(t.AircraftFamily))
                    .Select(t => t.AircraftFamily!)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distinct aircraft families");
                throw;
            }
        }
        public async Task<List<string>> GetDistinctEngineFamiliesAsync()
        {
            try
            {
                return await context.AircraftTypes
        .Where(t => !string.IsNullOrEmpty(t.EngineFamily))
        .Select(t => t.EngineFamily!)
        .Distinct()
        .OrderBy(f => f)
        .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distinct engine families");
                throw;
            }
        }
        public async Task<AircraftType> AddAircraftTypeAsync(string manufacturerId, string name, string typeCode, string aircraftFamily, string engineFamily)
        {
            try
            {
                var manufacturer = await context.AircraftManufacturers.FindAsync(manufacturerId.ToString());
                if (manufacturer == null)
                {
                    throw new Exception("Manufacturer not found");
                }
                var newType = new AircraftType
                {
                    AircraftTypeId = Guid.NewGuid().ToString(),
                    AircraftTypeName = name,
                    IcaoCode = typeCode,
                    Manufacturer = manufacturerId.ToString(),
                    AircraftFamily = aircraftFamily,
                    EngineFamily = engineFamily
                };
                context.AircraftTypes.Add(newType);
                // Tell SQLite to ignore the broken database blueprint
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

                await context.SaveChangesAsync();

                // Turn it back on to protect the rest of your app
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                return newType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding aircraft type");
                throw;
            }
        }
        public async Task<AircraftModel> AddAircraftModelAsync(string aircraftTypeId, string manufacturerId, string name, string nativeName, int? engineCount, string engineModels)
        {
            try
            {
                var newModel = new AircraftModel
                {
                    AircraftModelId = Guid.NewGuid().ToString(),
                    AircraftType = aircraftTypeId.ToString(),
                    Manufacturer = manufacturerId.ToString(),
                    AircraftName = name,
                    NativeName = nativeName,
                    EngineCount = engineCount,
                    EngineModels = engineModels
                };
                context.AircraftModels.Add(newModel);
                // Tell SQLite to ignore the broken database blueprint
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
                await context.SaveChangesAsync();
                // Turn it back on to protect the rest of your app
                await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
                return newModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding aircraft model");
                throw;
            }
        }
        public async Task<SimAircraft> AddSimAircraftAsync(string aircraftModelId)
        {
            try
            {
                var newSimAircraft = new SimAircraft
                {
                    AircraftId = aircraftModelId.ToString(),

                };
                context.SimAircrafts.Add(newSimAircraft);
                await context.SaveChangesAsync();

                // Include the related data so the UI can display its name immediately
                return await GetSimAircraftWithPropertiesAsync(newSimAircraft.SimPlaneId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding SimAircraft");
                throw;
            }
        }
    }
}
