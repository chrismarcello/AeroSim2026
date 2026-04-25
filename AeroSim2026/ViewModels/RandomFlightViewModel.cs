using AeroSim2026.Core.Routing;
using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class RandomFlightViewModel : PageViewModelBase
    {
        private readonly IAircraftServices _aircraftServices;
        private readonly IAirportServices _airportServices;
        private readonly INavigationServices _navigationServices;
        private readonly IFlightServices _flightServices;
        private readonly IStatusService _statusService;
        private readonly FlightRouteBuilder _flightRouteBuilder;
        private readonly RoutingGraph _routingGraph;
        private readonly IMapFeatureFactory _mapFeatureFactory;

        // Search Parameters
        private SimAircraft? _selectedAircraft;
        private AirportType? _selectedDepartAirportType;
        private AirportType? _selectedArrivalAirportType;
        private string _departureAirportIdent = string.Empty;
        private Continentcode? _selectedContinent;
        private double _minMiles;
        private double _maxMiles;

        // New Boolean Parameters
        private bool _hasIls;
        private bool _isMilitary;
        private int _cruiseAltitude = 5000;

        public SimAircraft? SelectedAircraft
        {
            get => _selectedAircraft;
            set => this.RaiseAndSetIfChanged(ref _selectedAircraft, value);
        }

        public AirportType? SelectedDepartAirportType
        {
            get => _selectedDepartAirportType;
            set => this.RaiseAndSetIfChanged(ref _selectedDepartAirportType, value);
        }

        public AirportType? SelectedArrivalAirportType
        {
            get => _selectedArrivalAirportType;
            set => this.RaiseAndSetIfChanged(ref _selectedArrivalAirportType, value);
        }

        public string DepartureAirportIdent
        {
            get => _departureAirportIdent;
            set => this.RaiseAndSetIfChanged(ref _departureAirportIdent, value);
        }

        public Continentcode? SelectedContinent
        {
            get => _selectedContinent;
            set => this.RaiseAndSetIfChanged(ref _selectedContinent, value);
        }

        public double MinMiles
        {
            get => _minMiles;
            set => this.RaiseAndSetIfChanged(ref _minMiles, value);
        }

        public double MaxMiles
        {
            get => _maxMiles;
            set => this.RaiseAndSetIfChanged(ref _maxMiles, value);
        }

        public bool HasIls
        {
            get => _hasIls;
            set => this.RaiseAndSetIfChanged(ref _hasIls, value);
        }

        public bool IsMilitary
        {
            get => _isMilitary;
            set => this.RaiseAndSetIfChanged(ref _isMilitary, value);
        }
        public int CruiseAltitude
        {
            get => _cruiseAltitude;
            set => this.RaiseAndSetIfChanged(ref _cruiseAltitude, value);
        }
        private MapViewModel? _mapViewModel;
        public MapViewModel? MapViewModel
        {
            get => _mapViewModel;
            private set => this.RaiseAndSetIfChanged(ref _mapViewModel, value);
        }

        // Collections
        public ObservableCollection<SimAircraft> AircraftList { get; } = new();
        public ObservableCollection<AirportType> AirportTypeList { get; } = new();
        public ObservableCollection<Continentcode> ContinentList { get; } = new();
        public ObservableCollection<RouteOption> FlightPaths { get; } = new();

        // Assuming you have a Flight model in your new architecture
        public ObservableCollection<FlightItemViewModel> FlightList { get; } = new ObservableCollection<FlightItemViewModel>();

        // Commands
        public ReactiveCommand<Unit, Unit> GenerateNewFlightCommand { get; }
        public ReactiveCommand<FlightItemViewModel, Unit> SaveFlightCommand { get; }
        private FlightItemViewModel? _currentFlight;
        public FlightItemViewModel? CurrentFlight
        {
            get => _currentFlight;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentFlight, value);
                this.RaisePropertyChanged(nameof(HasGeneratedFlight)); // Tell UI to show the card
            }
        }

        public bool HasGeneratedFlight => CurrentFlight != null;
        private RouteOption? _selectedFlightPath;
        public RouteOption? SelectedFlightPath
        {
            get => _selectedFlightPath;
            set
            {
                this.RaiseAndSetIfChanged(ref this._selectedFlightPath, value);
            }
        }

        public ReactiveCommand<Unit, Unit> CalculateRoutesCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearFormCommand { get; }
        public override string Title => "Random Flight Generator";
        public RandomFlightViewModel(IAircraftServices aircraftServices, IAirportServices airportServices, INavigationServices navigationServices, IFlightServices flightServices, IStatusService statusService, FlightRouteBuilder flightRouteBuilder, RoutingGraph routingGraph, IMapFeatureFactory mapFeatureFactory)
        {
            _aircraftServices = aircraftServices;
            _airportServices = airportServices;
            _navigationServices = navigationServices;
            _flightServices = flightServices;
            _statusService = statusService;
            _flightRouteBuilder = flightRouteBuilder;
            _routingGraph = routingGraph;
            _mapFeatureFactory = mapFeatureFactory;


            var canGenerateFlight = this.WhenAnyValue(
                vm => vm.SelectedAircraft,
                vm => vm.MinMiles,
                vm => vm.MaxMiles,
                (aircraft, min, max) => aircraft != null && (max == 0 || max > min)
                );
            GenerateNewFlightCommand = ReactiveCommand.CreateFromTask(GenerateNewFlightAsync, canGenerateFlight);

            var canCalculateRoutes = this.WhenAnyValue(x => x.CurrentFlight)
    .Select(flight => flight != null)
    .ObserveOn(RxSchedulers.MainThreadScheduler);

            CalculateRoutesCommand = ReactiveCommand.CreateFromTask(CalculateRoutesAsync, canCalculateRoutes);

            SaveFlightCommand = ReactiveCommand.CreateFromTask<FlightItemViewModel>(SaveFlightAsync);
            ClearFormCommand = ReactiveCommand.Create(ClearForm);

            // Fire and forget data load
            RxSchedulers.MainThreadScheduler.Schedule(LoadDropdownDataAsync);

            this.WhenAnyValue(x => x.SelectedFlightPath)
    .ObserveOn(RxSchedulers.MainThreadScheduler) // Force UI thread to prevent black-screen crashes
    .Subscribe(selectedPath =>
    {
        if (selectedPath != null && MapViewModel != null && CurrentFlight != null)
        {
            CurrentFlight.DistanceNm = selectedPath.Distance;
            int speed = (int)(CurrentFlight.OriginalFlight.PlannedSpeed ?? 150);
            CurrentFlight.EstFlightTimeSpan = _navigationServices.CalculateEte(selectedPath.Distance, speed);

            var origin = CurrentFlight.OriginalFlight.OriginAirport;
            var dest = CurrentFlight.OriginalFlight.ArrivalAirport;

            var markerFeatures = new List<Mapsui.Nts.GeometryFeature>();


            // Add Enroute
            if (selectedPath.GeneratedFlightPlanRoutes != null)
            {
                foreach (var leg in selectedPath.GeneratedFlightPlanRoutes)
                {
                    var node = _routingGraph.GetNode(leg.WaypointId);
                    if (node != null && !double.IsNaN(node.Latitude) && !double.IsNaN(node.Longitude))
                    {
                        if (origin != null && node.Identifier?.Trim() == origin.Ident?.Trim()) continue;
                        if (dest != null && node.Identifier?.Trim() == dest.Ident?.Trim()) continue;

                        markerFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(node.Latitude, node.Longitude, node.Identifier!, node.NavType));
                    }
                }
            }

            // SAFELY Create the Route Line (Requires at least 2 valid points!)
            Mapsui.Nts.GeometryFeature? routeFeature = null;
            var validWaypoints = selectedPath.Waypoints?
                .Where(w => !double.IsNaN(w.X) && !double.IsNaN(w.Y) && !double.IsInfinity(w.X) && !double.IsInfinity(w.Y))
                .ToList();

            if (validWaypoints != null && validWaypoints.Count > 1)
            {
                routeFeature = _mapFeatureFactory.CreateRouteLine(validWaypoints);
            }

            // Pass to MapViewModel
            MapViewModel.UpdateRoute(routeFeature, markerFeatures);
        }});
        }

        private async void LoadDropdownDataAsync()
        {
            // Populate these using your injected services

            var aircrafts = await _aircraftServices.GetSimAircraftsList();
            foreach (var a in aircrafts) AircraftList.Add(a);

            var airportTypes = await _airportServices.GetAirportTypesAsync();
            foreach (var t in airportTypes) AirportTypeList.Add(t);

            var continents = await _airportServices.GetContinentListAsync();
            foreach (var c in continents) ContinentList.Add(c);

        }

        private async Task GenerateNewFlightAsync()
        {
            FlightPaths.Clear();
            SelectedFlightPath = null;

            var flightParams = new RandomFlightParams
            {
                SimAircraftId = SelectedAircraft?.SimPlaneId ?? 0,
                Continent = SelectedContinent?.Code ?? string.Empty,
                DepartureAirportIdent = DepartureAirportIdent,
                DepartAirportTypeId = SelectedDepartAirportType?.TypeId ?? 0,
                ArrivalAirportTypeId = SelectedArrivalAirportType?.TypeId ?? 0,
                MinDistance = this.MinMiles > 0 ? this.MinMiles : 50.0,
                MaxDistance = this.MaxMiles > 0 ? this.MaxMiles : 0.0,
                HasIls = HasIls,
                IsMilitary = IsMilitary,
                CruiseAltitude = this.CruiseAltitude > 0 ? this.CruiseAltitude : 5000,
            };

            var generatedFlight = await _flightServices.BuildRandomFlightAsync(flightParams);

            if (generatedFlight != null)
            {
                CurrentFlight = new FlightItemViewModel(generatedFlight);

                if (generatedFlight.OriginAirport != null && generatedFlight.ArrivalAirport != null)
                {
                    MapViewModel = new MapViewModel(generatedFlight.OriginAirport, generatedFlight.ArrivalAirport, _mapFeatureFactory);
                }
            }
        }
        private async Task CalculateRoutesAsync()
        {
            if (CurrentFlight?.OriginalFlight.OriginAirport == null ||
                CurrentFlight?.OriginalFlight.ArrivalAirport == null) return;

            FlightPaths.Clear();
            _statusService.IsBusy = true;
            _statusService.StatusMessage = "Calculating Airway Routes...";

            try
            {
                var origin = CurrentFlight.OriginalFlight.OriginAirport;
                var dest = CurrentFlight.OriginalFlight.ArrivalAirport;
                var altitude = CruiseAltitude > 0 ? CruiseAltitude : 5000;

                var flightPathResult = await Task.Run(async () =>
                {
                    // 1. BUILD GRAPH ON THE BACKGROUND THREAD
                    await _flightServices.BuildCorridorGraphAsync(origin, dest);

                    var result = new List<RouteOption>();

                    // 2. DIRECT ROUTE (Great Circle)
                    var rawGreatCirclePoints = GeoMath.GenerateGreatCirclePoints(origin.Laty, origin.Lonx, dest.Laty, dest.Lonx, 100);
                    var projectedWaypoints = rawGreatCirclePoints.Select(p =>
                    {
                        var (smX, smY) = Mapsui.Projections.SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                        return new NetTopologySuite.Geometries.Coordinate(smX, smY);
                    }).ToList();

                    result.Add(new RouteOption
                    {
                        Title = "Direct (Great Circle)",
                        RouteString = $"{origin.Ident} DCT {dest.Ident}",
                        Distance = _navigationServices.CalculateDistance(origin.Laty, origin.Lonx, dest.Laty, dest.Lonx),
                        Waypoints = projectedWaypoints,
                        WaypointDetails = new ObservableCollection<string> { $"🛫 {origin.Ident}", "  DCT (Direct)", $"🛬 {dest.Ident}" }
                    });

                    // 3. FETCH SMART ROUTES FROM THE NEW BUILDER
                    var proposedRoutes = await _flightRouteBuilder.GenerateAlternativeRoutesAsync(origin, dest, altitude);

                    // 4. MAP THE PROPOSED ROUTES TO UI ROUTE OPTIONS
                    foreach (var proposed in proposedRoutes)
                    {
                        var details = new ObservableCollection<string> { $"🛫 {origin.Ident} (Departure)" };
                        var routePoints = new List<NetTopologySuite.Geometries.Coordinate>();
                        var routeNames = new List<string>();

                        var fpRoutesToSave = new List<FlightPlanRoute>();

                        var (origX, origY) = Mapsui.Projections.SphericalMercator.FromLonLat(origin.Lonx, origin.Laty);
                        routePoints.Add(new NetTopologySuite.Geometries.Coordinate(origX, origY));

                        foreach (var leg in proposed.Legs)
                        {
                            var (wpX, wpY) = Mapsui.Projections.SphericalMercator.FromLonLat(leg.Waypoint.Longitude, leg.Waypoint.Latitude);
                            routePoints.Add(new NetTopologySuite.Geometries.Coordinate(wpX, wpY));

                            if (leg.SequenceNumber > 1 && leg.SequenceNumber < proposed.Legs.Count)
                            {
                                routeNames.Add(leg.Waypoint.Identifier);
                                string airwayStr = string.IsNullOrEmpty(leg.AirwayName) ? "DCT" : $"via {leg.AirwayName}";
                                details.Add($"  {leg.SequenceNumber - 1:D2}. {leg.Waypoint.Identifier.PadRight(5)} {airwayStr}");
                            }

                            fpRoutesToSave.Add(new FlightPlanRoute
                            {
                                SequenceNumber = leg.SequenceNumber,
                                WaypointId = leg.Waypoint.WaypointId
                            });
                        }

                        var (destX, destY) = Mapsui.Projections.SphericalMercator.FromLonLat(dest.Lonx, dest.Laty);
                        routePoints.Add(new NetTopologySuite.Geometries.Coordinate(destX, destY));

                        details.Add($"🛬 {dest.Ident} (Arrival)");

                        string routeString = $"{origin.Ident} -> " + string.Join(" -> ", routeNames.Take(4)) + (routeNames.Count > 4 ? " ... " : " -> ") + $"{dest.Ident}";

                        result.Add(new RouteOption
                        {
                            Title = proposed.RouteName,
                            RouteString = routeString,
                            Distance = proposed.TotalDistance,
                            Waypoints = routePoints,
                            WaypointDetails = details,
                            GeneratedFlightPlanRoutes = fpRoutesToSave
                        });
                    }

                    return result;
                });

                RxSchedulers.MainThreadScheduler.Schedule(() =>
                {
                    foreach (var option in flightPathResult)
                    {
                        FlightPaths.Add(option);
                    }

                    if (FlightPaths.Any())
                    {
                        SelectedFlightPath = FlightPaths.First();
                    }
                });
            }
            finally
            {
                _statusService.IsBusy = false;
                _statusService.StatusMessage = "Ready";
            }
        }
        private async Task SaveFlightAsync(FlightItemViewModel itemToSave)
        {
            // Ensure we have a valid flight, origin, destination, and selected aircraft before saving
            if (itemToSave?.OriginalFlight.OriginAirport == null ||
                itemToSave?.OriginalFlight.ArrivalAirport == null ||
                SelectedAircraft == null) return;

            _statusService.IsBusy = true;
            _statusService.StatusMessage = "Saving Flight Plan...";

            try
            {
                var origin = itemToSave.OriginalFlight.OriginAirport;
                var dest = itemToSave.OriginalFlight.ArrivalAirport;

                // 1. Build the main FlightPlan entity
                var newPlan = new FlightPlan
                {
                    FlightPlanId = Guid.NewGuid().ToString(),
                    DateCreated = DateTime.UtcNow,
                    AircraftModelId = SelectedAircraft.AircraftId.ToString(),
                    StartAirportId = origin.AirportId,
                    EndAirportId = dest.AirportId,
                    CruiseAltitude = CruiseAltitude > 0 ? CruiseAltitude : 5000,
                    DistanceNm = (int)Math.Round(itemToSave.DistanceNm),
                    EstFlightTime = itemToSave.EstFlightTimeSpan, // Pull from the updated property
                    Comments = SelectedFlightPath != null ? $"Route: {SelectedFlightPath.RouteString}" : "Direct",
                    FlightPlanRoutes = new List<FlightPlanRoute>()
                };

                // 2. If an A* route was selected, map its waypoints to the database entities
                if (SelectedFlightPath != null && SelectedFlightPath.GeneratedFlightPlanRoutes.Any())
                {
                    foreach (var leg in SelectedFlightPath.GeneratedFlightPlanRoutes)
                    {
                        // Generate new IDs so EF Core can insert them cleanly
                        leg.FpStepId = Guid.NewGuid().ToString();
                        leg.FlightPlanId = newPlan.FlightPlanId;
                        newPlan.FlightPlanRoutes.Add(leg);
                    }
                }

                // 3. Save to the database using your injected service
                // (Assuming _flightServices.SaveFlightPlanAsync returns void or Task. Update if it returns a bool)
                await _flightServices.SaveFlightPlanAsync(newPlan);

                _statusService.StatusMessage = "Flight Plan Saved Successfully!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving flight plan: {ex.Message}");
                _statusService.StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                _statusService.IsBusy = false;

                // Clear the success/error message and reset to Ready after 5 seconds
                if (_statusService.StatusMessage.Contains("Saved") || _statusService.StatusMessage.Contains("Error"))
                {
                    // Run the delay without blocking the UI
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        RxSchedulers.MainThreadScheduler.Schedule(() =>
                        {
                            if (!_statusService.IsBusy)
                            {
                                _statusService.StatusMessage = "Ready";
                            }
                        });
                    });
                }
            }
        }
        public void ClearForm()
        {
            // Reset Form Inputs
            SelectedAircraft = null;
            SelectedDepartAirportType = null;
            SelectedArrivalAirportType = null;
            DepartureAirportIdent = string.Empty;
            SelectedContinent = null;
            MinMiles = 0;
            MaxMiles = 0;
            HasIls = false;
            IsMilitary = false;
            CruiseAltitude = 0;

            // Clear Results & Map
            CurrentFlight = null;
            SelectedFlightPath = null;
            FlightPaths.Clear();
            MapViewModel = null;
        }
    }
}

