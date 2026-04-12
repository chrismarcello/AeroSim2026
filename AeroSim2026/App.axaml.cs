using AeroSim2026.Core.Routing;
using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.ViewModels;
using AeroSim2026.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
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

            // Check app folder and copy database
            string connectionString = SetDefaultDatabaseLocation();

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

        private string SetDefaultDatabaseLocation()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string databaseDirectory = Path.Combine(appDataPath, "AeroSim2026", "Data");
            string dbFile = Path.Combine(appDataPath, "AeroSim2026", "Data", "aerosim2026.db");
            string defaultDbLoc = Path.Combine(AppContext.BaseDirectory, "DB", "aerosim2026.db");

            if (!Directory.Exists(databaseDirectory))
            {
                try
                {
                    Directory.CreateDirectory(databaseDirectory);
                    File.Copy(defaultDbLoc, dbFile);
                    Log.Information($"Database copied to {databaseDirectory}");
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Error creating database directory or copying database");
                }
            }
            else
            {
                if (!File.Exists(dbFile))
                {
                    try
                    {
                        File.Copy(defaultDbLoc, dbFile);
                        Log.Information($"Database copied to {databaseDirectory}");
                    }
                    catch (IOException ex)
                    {
                        Log.Error(ex, "Error copying database file");
                    }
                }
            }
            return $"Data Source={dbFile}";
        }
    }
}