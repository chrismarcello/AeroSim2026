using System;
using System.IO;
using System.Text.Json;
using AeroSim2026.Models;

namespace AeroSim2026.Core.Services
{
    public static class UserSettingsService
    {
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AeroSim2026");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "user_settings.json");

        public static UserSettings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
                return new UserSettings(); // Return default settings if file doesn't exist

            try
            {
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (Exception ex)
            {
                // Log the error if you have a logging mechanism
                Console.WriteLine($"Error loading user settings: {ex.Message}");
                return new UserSettings(); // Return default settings on error
            }
        }

        public static void SaveSettings(UserSettings settings)
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }
}
