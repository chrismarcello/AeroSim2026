using AeroSim2026.EFModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface IAircraftServices
    {
        Task<List<SimAircraft>> GetSimAircraftsList();
        Task<SimAircraft> GetSimAircraftWithPropertiesAsync(int simAircraftId);
        Task<List<AircraftManufacturer>> GetAircraftManufacturerAsync();
        Task<List<AircraftType>> GetAircraftTypesForManufacturerAsync(string manufacturerId);
        Task<List<AircraftModel>> GetAircraftModelsForTypeAsync(string aircraftTypeId);
        Task DeleteSimAircraftAsync(int simPlaneId);
        Task<AircraftManufacturer> AddAircraftManufacturerAsync(string name, string countryIso);
        Task<List<string>> GetDistinctAircraftFamiliesAsync();
        Task<List<string>> GetDistinctEngineFamiliesAsync();
        Task<AircraftType> AddAircraftTypeAsync(string manufacturerId, string name, string typeCode, string aircraftFamily, string engineFamily);
        Task<AircraftModel> AddAircraftModelAsync(string aircraftTypeId, string manufacturerId, string name, string nativeName, int? engineCount, string engineModels);
        Task<SimAircraft> AddSimAircraftAsync(string aircraftModelId);
    }
}
