using AeroSim2026.EFModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface IAircraftServices
    {
        Task<List<SimAircraft>> GetSimAircraftsList();
        Task<SimAircraft> GetSimAircraftWithPropertiesAsync(int simAircraftId);
    }
}
