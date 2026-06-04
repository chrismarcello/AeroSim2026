using AeroSim2026.EFModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface IGeographyServices
    {
        Task<List<Countryinfo>> GetAllCountriesAsync();
    }
}
