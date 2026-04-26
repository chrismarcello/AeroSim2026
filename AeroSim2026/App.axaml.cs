using AeroSim2026.Core.Routing;
using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using AeroSim2026.ViewModels;
using AeroSim2026.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace AeroSim2026
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            // Load user settings (like theme preference) before creating the main window
            var userSetting = UserSettingsService.LoadSettings();

            // Apply theme based on user settings
            ApplyTheme(userSetting.Theme);
            

            // Check app folder and copy database
            string connectionString = SetDefaultDatabaseLocation(userSetting.CustomDatabasePath);

            Log.Information($"Using connection string: {connectionString}");

            var services = new ServiceCollection();

            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<MainWindow>();
            services.AddLogging(builder =>
            {
                builder.AddSerilog(Log.Logger);
            });
            services.AddDbContext<Aerosim2026Context>(options =>
            {
                options.UseSqlite(connectionString);
            });
            services.AddScoped<IFlightServices, FlightServices>();
            services.AddScoped<IAircraftServices, AircraftServices>();
            services.AddScoped<IAirportServices, AirportServices>();
            //services.AddScoped<IHelperServices, HelperServices>();
            services.AddTransient<INavigationServices, NavigationServices>();
            services.AddSingleton<IStatusService, StatusService>();
            services.AddSingleton<RoutingGraph>(); // Singleton so it only loads once!
            services.AddSingleton<IMapFeatureFactory, MapFeatureFactory>();
            services.AddTransient<ConnectionManager>();
            services.AddTransient<RouteFinderService>();
            services.AddTransient<FlightRouteBuilder>();
            var serviceProvider = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = serviceProvider.GetRequiredService<MainWindowViewModel>();
                desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow.DataContext = vm;

                //DisableAvaloniaDataAnnotationValidation();

            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        // Helper method to apply theme based on user settings
        private void ApplyTheme(string theme)
        {
            switch (theme)
            {
                case "Light":
                    RequestedThemeVariant = ThemeVariant.Light;
                    break;
                case "Dark":
                    RequestedThemeVariant = ThemeVariant.Dark;
                    break;
                default:
                    RequestedThemeVariant = ThemeVariant.Default; // System preference
                    break;
            }
        }
        private string SetDefaultDatabaseLocation(string customDatabasePath)
        {
            string defaultDbLoc = Path.Combine(AppContext.BaseDirectory, "db", "aerosim2026.db");
            string targetDirectory = "";
            string targetDbFile = "";

            if (!string.IsNullOrEmpty(customDatabasePath) && Directory.Exists(customDatabasePath))
            {
                targetDirectory = customDatabasePath;
            }
            else
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                targetDirectory = Path.Combine(appDataPath, "AeroSim2026", "Data");
            }

            targetDbFile = Path.Combine(targetDirectory, "aerosim2026.db");

            if (!Directory.Exists(targetDirectory))
            {
                try
                {
                    Directory.CreateDirectory(targetDirectory);
                    
                    Log.Information($"Database copied to {targetDirectory}");
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Error creating database directory or copying database");
                }
            }
            if (!File.Exists(targetDbFile))
            {
                try
                {
                    File.Copy(defaultDbLoc, targetDbFile);
                    Log.Information($"Database copied to {targetDbFile}");
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Error copying database file");
                }
            }
            else
            {
                Log.Information($"Database already exists at {targetDbFile}, skipping copy.");
            }

            // 4. Point the app to the working file
            return $"Data Source={targetDbFile}";
        }
    }
}