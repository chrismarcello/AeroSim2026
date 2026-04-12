using AeroSim2026.EFModels;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AeroSim2026.Core.Converters
{
    public class AirportColorConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // We expect 3 inputs: [CurrentRow, Origin, Destination]
            if (values.Count == 3 && values[0] is Airport currentAirport)
            {
                var origin = values[1] as Airport;
                var dest = values[2] as Airport;

                // Is this row the Origin? -> Green
                if (origin != null && currentAirport.AirportId == origin.AirportId)
                    return Brushes.LightGreen;

                // Is this row the Destination? -> Light Red/Orange
                if (dest != null && currentAirport.AirportId == dest.AirportId)
                    return Brushes.LightSalmon;
            }

            // Default: Transparent (normal row)
            return Brushes.Transparent;
        }
    }
}
