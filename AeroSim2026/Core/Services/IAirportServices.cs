using AeroSim2026.EFModels;
using AeroSim2026.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface IAirportServices
    {
        Task<List<Airport>> GetAirportsList();
        Task<Airport> GetAirportAsync(int airportId);
        Task<List<Continentcode>> GetContinentListAsync();
        Task<List<AirportType>> GetAirportTypesAsync();
        Task<Airport> RandomStartAsync(RandomFlightParams randomParams);
        Task<Airport> RandomArrivalAirportAsync(RandomFlightParams randomParams);
    }
}
