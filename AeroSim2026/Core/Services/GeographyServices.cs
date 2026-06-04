using AeroSim2026.EFModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public class GeographyServices : IGeographyServices
    {
        private readonly Aerosim2026Context _context;

        public GeographyServices(Aerosim2026Context context)
        {
            _context = context;
        }

        public async Task<List<Countryinfo>> GetAllCountriesAsync()
        {
            return await _context.Countryinfos
                .AsNoTracking()
                .OrderBy(c => c.IsoAlpha2)
                .ToListAsync();
        }
    }
}
