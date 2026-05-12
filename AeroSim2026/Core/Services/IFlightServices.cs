using AeroSim2026.EFModels;
using AeroSim2026.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AeroSim2026.Core.Services
{
    public interface IFlightServices
    {
        Task BuildCorridorGraphAsync(Airport origin, Airport destination);
        Task<List<FlightPlan>> GetAllFlights();
        Task<List<FlightPlan>> GetflownFlights();
        Task<List<FlightPlan>> GetUnflownFlights();

        Task<FlightPlan> SaveFlightPlanAsync(FlightPlan flightPlan);
        Task<FlightPlan> UpdateFlightPlanAsync(FlightPlan flightPlan);
        Task<FlightPlan?> GetFlightPlanWithRoutesAsync(string flightPlanId);
        Task<GeneratedFlight> BuildRandomFlightAsync(RandomFlightParams flightParams);
        Task DeleteFlightPlanAsync(string flightPlanId);


    }
}
