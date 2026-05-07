using AeroSim2026.Models;
using ReactiveUI;
using System;

namespace AeroSim2026.ViewModels
{
    public class FlightItemViewModel : ReactiveObject
    {
        // Keep a reference to the raw data just in case
        public GeneratedFlight OriginalFlight { get; }

        // --- Header Properties ---
        public string DepartIdent { get; }
        public string DestIdent { get; }
        
        private double _distanceNm;
        public double DistanceNm
        {
            get => _distanceNm;
            set => this.RaiseAndSetIfChanged(ref _distanceNm, value);
        }
        private TimeSpan? _estFlightTimeSpan;
        public TimeSpan? EstFlightTimeSpan
        {
            get => _estFlightTimeSpan;
            set
            {
                this.RaiseAndSetIfChanged(ref _estFlightTimeSpan, value);
                this.RaisePropertyChanged(nameof(EstFlightTimeFormatted)); // Notify that the formatted string has changed
            }
        }
        // --- Departure Properties ---
        public string DepartAirport { get; }
        public string DepartCity { get; }
        public string DepartCountry { get; }
        public string DepartDisplayLocation { get; }
        public int? DepartLongestRunway { get; }
        public int? DepartAltitude { get; }

        // --- Arrival Properties ---
        public string DestAirport { get; }
        public string DestCity { get; }
        public string DestCountry { get; }
        public string DestDisplayLocation { get; }
        public int? DestLongestRunway { get; }
        public int? DestAltitude { get; }

        public string EstFlightTimeFormatted => EstFlightTimeSpan.HasValue ? EstFlightTimeSpan.Value.ToString(@"hh\:mm") : "00:00";
        public FlightItemViewModel(GeneratedFlight flight)
        {
            OriginalFlight = flight;

            // Route & Distance
            DepartIdent = flight.OriginAirport?.Ident ?? "N/A";
            DestIdent = flight.ArrivalAirport?.Ident ?? "N/A";
            DistanceNm = Math.Round(flight.DistanceNm ?? 0, 1);
            EstFlightTimeSpan = flight.EstFlightTime;

            // Departure Details
            DepartAirport = flight.OriginAirport?.AirportName ?? "Unknown Airport";
            DepartCity = flight.OriginAirport?.AirportsLocation?.GeoCity?.Name ?? "Unknown City";
            DepartCountry = flight.OriginAirport?.AirportsLocation?.GeoCountry?.Name ?? "Unknown Country";
            DepartDisplayLocation = flight.OriginAirport?.DisplayLocation ?? "Unknown Location";
            DepartLongestRunway = flight.OriginAirport?.LongestRunwayLength ?? 0;
            DepartAltitude = flight.OriginAirport?.Altitude ?? 0;

            // Arrival Details
            DestAirport = flight.ArrivalAirport?.AirportName ?? "Unknown Airport";
            DestCity = flight.ArrivalAirport?.AirportsLocation?.GeoCity?.Name ?? "Unknown City";
            DestCountry = flight.ArrivalAirport?.AirportsLocation?.GeoCountry?.Name ?? "Unknown Country";
            DestDisplayLocation = flight.ArrivalAirport?.DisplayLocation ?? "Unknown Location";
            DestLongestRunway = flight.ArrivalAirport?.LongestRunwayLength ?? 0;
            DestAltitude = flight.ArrivalAirport?.Altitude ?? 0;
        }
    }
}
