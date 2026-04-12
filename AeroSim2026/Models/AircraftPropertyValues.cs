using Newtonsoft;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace AeroSim2026.Models
{
    public class JsonPropertyValue
    {
        // WAS: [JsonPropertyName("property")]  <-- This is for System.Text.Json
        // USE THIS INSTEAD:
        [JsonProperty("property")]
        public string PropertyId { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }

    public class AircraftPropertyValues
    {

        public required string PropertyId { get; set; }

        public string? Unit { get; set; }

        public string? PropertyType { get; set; }

        public string? PropertyName { get; set; }

        public string? Description { get; set; }

        public object? PropertyValue { get; set; }

        public object? PropertyValueConverted { get; set; } = null;

        public string? PropertyValueConvertedUnit { get; set; } = null;


        public static AircraftPropertyValues CreateInteger(string propertyid, int propertyvalue, string unittype, string propertytype, string propertyname, string description)
        {

            return new AircraftPropertyValues
            {
                PropertyId = propertyid,
                PropertyValue = propertyvalue,
                Unit = unittype,
                PropertyType = propertytype,
                PropertyName = propertyname,
                Description = description,
                PropertyValueConverted = IntMetricToStandard(propertyvalue, unittype),
                PropertyValueConvertedUnit = ConvertedUnitType(unittype)
            };
        }
        public static AircraftPropertyValues CreateString(string propertyid, string? propertyvalue, string unittype, string propertytype, string propertyname, string description)
        {
            return new AircraftPropertyValues
            {
                PropertyId = propertyid,
                PropertyValue = propertyvalue,
                Unit = unittype,
                PropertyType = propertytype,
                PropertyName = propertyname,
                Description = description
            };
        }
        public static AircraftPropertyValues CreateBool(string propertyid, bool propertyvalue, string unittype, string propertytype, string propertyname, string description)
        {
            return new AircraftPropertyValues
            {
                PropertyId = propertyid,
                PropertyValue = propertyvalue,
                Unit = unittype,
                PropertyType = propertytype,
                PropertyName = propertyname,
                Description = description
            };
        }
        public static AircraftPropertyValues CreateFloat(string propertyid, float propertyvalue, string unittype, string propertytype, string propertyname, string description)
        {
            return new AircraftPropertyValues
            {
                PropertyId = propertyid,
                PropertyValue = propertyvalue,
                Unit = unittype,
                PropertyType = propertytype,
                PropertyName = propertyname,
                Description = description,
                PropertyValueConverted = FloatMetricToStandard(propertyvalue, unittype),
                PropertyValueConvertedUnit = ConvertedUnitType(unittype)
            };
        }
        public static string ConvertedUnitType(string unit)
        {
            string result = null!;
            switch (unit)
            {
                case "kilogram":
                    result = "pound";
                    break;
                case "kilometre":
                    result = "mile";
                    break;
                case "metre":
                    result = "foot";
                    break;
                case "square-metre":
                    result = "square-foot";
                    break;
                case "litre":
                    result = "gallon";
                    break;
                case "m/s":
                    result = "mph";
                    break;
                case "N":
                    result = "lbf";
                    break;
                case "kN":
                    result = "lbf";
                    break;
                case "hPa":
                    result = "inHg";
                    break;
                default:
                    break;
            }
            return result;
        }
        public static Int32 IntMetricToStandard(int value, string unit)
        {
            int result = 0;

            switch (unit)
            {
                case "kilogram":
                    result = Convert.ToInt32(value * 2.20462);
                    break;
                case "kilometre":
                    result = Convert.ToInt32(value * 0.621371);
                    break;
                case "metre":
                    result = Convert.ToInt32(value * 3.28084);
                    break;
                case "square-metre":
                    result = Convert.ToInt32(value * 10.7639);
                    break;
                case "litre":
                    result = Convert.ToInt32(value * 0.264172);
                    break;
                case "m/s":
                    result = Convert.ToInt32(value * 3.6 * 0.621371);
                    break;
                case "N":
                    result = Convert.ToInt32(value * 0.224809);
                    break;
                case "kN":
                    result = Convert.ToInt32(value * 224.809);
                    break;
                case "hPa":
                    result = Convert.ToInt32(value * 0.02953);
                    break;
                default:
                    break;
            }

            return result;
        }
        public static float FloatMetricToStandard(float value, string unit)
        {
            float result = 0f;
            switch (unit)
            {
                case "kilogram":
                    result = Convert.ToSingle(value * 2.20462);
                    break;
                case "kilometre":
                    result = Convert.ToSingle(value * 0.621371);
                    break;
                case "metre":
                    result = Convert.ToSingle(value * 3.28084);
                    break;
                case "square-metre":
                    result = Convert.ToSingle(value * 10.7639);
                    break;
                case "litre":
                    result = Convert.ToSingle(value * 0.264172);
                    break;
                case "m/s":
                    result = Convert.ToSingle(value * 3.6 * 0.621371);
                    break;
                case "N":
                    result = Convert.ToSingle(value * 0.224809);
                    break;
                case "kN":
                    result = Convert.ToSingle(value * 224.809);
                    break;
                case "hPa":
                    result = Convert.ToSingle(value * 0.02953);
                    break;
                default:

                    break;
            }
            return result;
        }
    }
}



