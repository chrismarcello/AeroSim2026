using AeroSim2026.EFModels;
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
        Task<AircraftManufacturer> AddAircraftManufacturerAsync(string name);
    }
}
