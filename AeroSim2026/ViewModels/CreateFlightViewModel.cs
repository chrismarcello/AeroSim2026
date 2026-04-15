using AeroSim2026.Core.Routing;
using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Widgets;
using Mapsui.Widgets.ButtonWidgets;
using Mapsui.Widgets.InfoWidgets;
using NetTopologySuite.Geometries;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class CreateFlightViewModel : PageViewModelBase
    {
        private readonly IAircraftServices _aircraftServices;
        private readonly IAirportServices _airportServices;
        private readonly INavigationServices _navigationServices;
        private readonly IFlightServices _flightServices;
        private readonly IStatusService _statusService;
        private readonly FlightRouteBuilder _flightRouteBuilder;
        private readonly RoutingGraph _routingGraph;
        private readonly IMapFeatureFactory _mapFeatureFactory; // NEW FACTORY INJECTED
        private ILayer? _majorAirportsLayer;
        private ILayer? _regionalAirportsLayer;
        private ILayer? _smallAirportsLayer;
        private ILayer? _milAirportsLayer;

        private List<Airport> _airports = new();

        public ObservableCollection<SimAircraft> AircraftList { get; } = new();
        public ObservableCollection<Airport> FilteredAirports { get; } = new();
        public ObservableCollection<RouteOption> RouteOptions { get; } = new();

        private SimAircraft? _selectedAircraft;
        private SimAircraft? _fullSelectedAircraft;
        private int _selectedCruiseAltitude = 5000;
        private string _searchText = "";
        private int _plannedSpeed = 150;
        private double _totalDistance;
        private TimeSpan _estimatedTime;
        private MemoryLayer _routeLayer;
        private RouteOption? _selectedRouteOption;

        public SimAircraft? SelectedAircraft
        {
            get => _selectedAircraft;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAircraft, value);
                if (value != null)
                {
                    LoadAircraftProperties(value.SimPlaneId);
                }
                else
                {
                    FullSelectedAircraft = null;
                }
            }
        }

        public SimAircraft? FullSelectedAircraft
        {
            get => _fullSelectedAircraft;
            set => this.RaiseAndSetIfChanged(ref _fullSelectedAircraft, value);
        }

        public int SelectedCruiseAltitude
        {
            get => _selectedCruiseAltitude;
            set => this.RaiseAndSetIfChanged(ref _selectedCruiseAltitude, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        private Airport? _originAirport;
        public Airport? OriginAirport
        {
            get => _originAirport;
            set => this.RaiseAndSetIfChanged(ref _originAirport, value);
        }

        private Airport? _destAirport;
        public Airport? DestAirport
        {
            get => _destAirport;
            set => this.RaiseAndSetIfChanged(ref _destAirport, value);
        }

        public RouteOption? SelectedRouteOption
        {
            get => _selectedRouteOption;
            set => this.RaiseAndSetIfChanged(ref _selectedRouteOption, value);
        }

        public ReactiveCommand<Airport, Unit> SetOriginCommand { get; }
        public ReactiveCommand<Airport, Unit> SetDestCommand { get; }
        public ReactiveCommand<Airport, Unit> ViewOnMapCommand { get; }
        public ReactiveCommand<Unit, Unit> BuildFlightPathCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveFlightPlanCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearFormCommand { get; }

        private const string CruiseSpeedPropertyId = "b7257438-0d1e-11f1-8f56-00155dcf273e";

        // ADD FACTORY TO CONSTRUCTOR
        public CreateFlightViewModel(IAircraftServices aircraftServices, IAirportServices airportServices, INavigationServices navigationServices, IFlightServices flightServices, IStatusService statusService, FlightRouteBuilder flightRouteBuilder, RoutingGraph routingGraph, IMapFeatureFactory mapFeatureFactory)
        {
            _aircraftServices = aircraftServices;
            _airportServices = airportServices;
            _navigationServices = navigationServices;
            _flightServices = flightServices;
            _statusService = statusService;
            _flightRouteBuilder = flightRouteBuilder;
            _routingGraph = routingGraph;
            _mapFeatureFactory = mapFeatureFactory;

            SetOriginCommand = ReactiveCommand.Create<Airport>(airport => { OriginAirport = airport; });
            SetDestCommand = ReactiveCommand.Create<Airport>(airport => { DestAirport = airport; });

            this.WhenAnyValue(x => x.OriginAirport)
                .Where(airport => airport != null)
                .Subscribe(airport => FlyToAirport(airport!));

            this.WhenAnyValue(x => x.DestAirport)
                .Where(airport => airport != null)
                .Subscribe(airport => FlyToAirport(airport!));

            var canExecuteFlightActions = this.WhenAnyValue(
                x => x.SelectedAircraft,
                x => x.OriginAirport,
                x => x.DestAirport,
                (aircraft, origin, dest) => aircraft != null && origin != null && dest != null
            ).ObserveOn(RxSchedulers.MainThreadScheduler);

            ViewOnMapCommand = ReactiveCommand.Create<Airport>(FlyToAirport);
            ClearFormCommand = ReactiveCommand.Create(ClearForm);

            BuildFlightPathCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                RouteOptions.Clear();

                if (OriginAirport == null || DestAirport == null) return;

                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Calculating Flight Plan...";

                int cruiseAltitude = SelectedCruiseAltitude;
                try
                {
                    await _flightServices.BuildCorridorGraphAsync(OriginAirport, DestAirport);

                    var flightPathResult = await Task.Run(() =>
                    {
                        var result = new List<RouteOption>();

                        // 1. Direct Route (Great Circle)
                        var rawGreatCirclePoints = GeoMath.GenerateGreatCirclePoints(OriginAirport.Laty, OriginAirport.Lonx, DestAirport.Laty, DestAirport.Lonx, 100);
                        var projectedWaypoints = rawGreatCirclePoints.Select(p =>
                        {
                            var (smX, smY) = Mapsui.Projections.SphericalMercator.FromLonLat(p.Longitude, p.Latitude);
                            return new Coordinate(smX, smY);
                        }).ToList();

                        result.Add(new RouteOption
                        {
                            Title = "Direct (Great Circle)",
                            RouteString = $"{OriginAirport.Ident} DCT {DestAirport.Ident}",
                            Distance = _navigationServices.CalculateDistance(OriginAirport.Laty, OriginAirport.Lonx, DestAirport.Laty, DestAirport.Lonx),
                            Waypoints = projectedWaypoints,
                            WaypointDetails = new ObservableCollection<string> { $"🛫 {OriginAirport.Ident}", "  DCT (Direct)", $"🛬 {DestAirport.Ident}" },
                            GeneratedFlightPlanRoutes = new List<FlightPlanRoute>()
                        });

                        // 2. FETCH SMART ROUTES FROM THE NEW BUILDER
                        var proposedRoutes = _flightRouteBuilder.GenerateAlternativeRoutes(OriginAirport, DestAirport, cruiseAltitude);

                        foreach (var proposed in proposedRoutes)
                        {
                            var details = new ObservableCollection<string> { $"🛫 {OriginAirport.Ident} (Departure)" };
                            var routePoints = new List<Coordinate>();
                            var routeNames = new List<string>();
                            var fpRoutesToSave = new List<FlightPlanRoute>();

                            var (origX, origY) = Mapsui.Projections.SphericalMercator.FromLonLat(OriginAirport.Lonx, OriginAirport.Laty);
                            routePoints.Add(new Coordinate(origX, origY));

                            foreach (var leg in proposed.Legs)
                            {
                                var (wpX, wpY) = Mapsui.Projections.SphericalMercator.FromLonLat(leg.Waypoint.Longitude, leg.Waypoint.Latitude);
                                routePoints.Add(new Coordinate(wpX, wpY));

                                if (leg.SequenceNumber > 1 && leg.SequenceNumber < proposed.Legs.Count)
                                {
                                    routeNames.Add(leg.Waypoint.Identifier);
                                    string airwayStr = string.IsNullOrEmpty(leg.AirwayName) ? "DCT" : $"via {leg.AirwayName}";
                                    details.Add($"  {leg.SequenceNumber - 1:D2}. {leg.Waypoint.Identifier.PadRight(5)} {airwayStr}");
                                }

                                // We map these into EF Entities here so the Save command can easily save them to DB later
                                fpRoutesToSave.Add(new FlightPlanRoute
                                {
                                    SequenceNumber = leg.SequenceNumber,
                                    WaypointId = leg.Waypoint.WaypointId
                                });
                            }
                            var (destX, destY) = Mapsui.Projections.SphericalMercator.FromLonLat(DestAirport.Lonx, DestAirport.Laty);
                            routePoints.Add(new Coordinate(destX, destY));
                            details.Add($"🛬 {DestAirport.Ident} (Arrival)");

                            string routeString = $"{OriginAirport.Ident} -> " + string.Join(" -> ", routeNames.Take(4)) + (routeNames.Count > 4 ? " ... " : " -> ") + $"{DestAirport.Ident}";

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

                    foreach (var option in flightPathResult)
                    {
                        RouteOptions.Add(option);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error calculating flight path: {ex.Message}");
                    _statusService.StatusMessage = $"Error: {ex.Message}";
                }
                finally
                {
                    _statusService.IsBusy = false;
                    if (!_statusService.StatusMessage.StartsWith("Error"))
                    {
                        _statusService.StatusMessage = "Ready";
                    }
                }
            }, canExecuteFlightActions);

            SaveFlightPlanCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (OriginAirport == null || DestAirport == null || SelectedAircraft == null) return;

                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Saving Flight Plan...";

                try
                {
                    var newPlan = new FlightPlan
                    {
                        FlightPlanId = Guid.NewGuid().ToString(),
                        DateCreated = DateTime.UtcNow,
                        AircraftModelId = SelectedAircraft.AircraftId.ToString(),
                        StartAirportId = OriginAirport.AirportId,
                        EndAirportId = DestAirport.AirportId,
                        CruiseAltitude = SelectedCruiseAltitude,
                        DistanceNm = (int)Math.Round(TotalDistance),
                        EstFlightTime = EstimatedTime,
                        Comments = SelectedRouteOption != null ? $"Route: {SelectedRouteOption.RouteString}" : "Direct",
                        FlightPlanRoutes = new List<FlightPlanRoute>()
                    };

                    if (SelectedRouteOption != null && SelectedRouteOption.GeneratedFlightPlanRoutes.Any())
                    {
                        foreach (var leg in SelectedRouteOption.GeneratedFlightPlanRoutes)
                        {
                            leg.FpStepId = Guid.NewGuid().ToString();
                            leg.FlightPlanId = newPlan.FlightPlanId;
                            newPlan.FlightPlanRoutes.Add(leg);
                        }
                    }

                    await _flightServices.SaveFlightPlanAsync(newPlan);
                    _statusService.StatusMessage = "Flight plan saved successfully!";
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving flight plan: {ex.Message}");
                    _statusService.StatusMessage = "Error saving flight plan.";
                }
                finally
                {
                    _statusService.IsBusy = false;
                    if (_statusService.StatusMessage == "Error saving flight plan." || _statusService.StatusMessage == "Flight plan saved successfully!")
                    {
                        await Task.Delay(5000);
                        _statusService.StatusMessage = "Ready";
                    }
                }
            }, canExecuteFlightActions);

            this.WhenAnyValue(x => x.SelectedRouteOption)
                .ObserveOn(RxSchedulers.MainThreadScheduler) // <--- CRITICAL: Forces UI Thread
                .Subscribe(route =>
    {
        if (route != null)
        {
            TotalDistance = route.Distance;
            EstimatedTime = _navigationServices.CalculateEte(route.Distance, PlannedSpeed);
        }
        UpdateFlightPath();
    });

            InitializeDataAsync();

            this.WhenAnyValue(x => x.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(DoFilter);

            this.WhenAnyValue(x => x.FullSelectedAircraft)
                .Where(plane => plane != null)
                .Subscribe(plane =>
                {
                    var speedProp = plane!.Properties?.FirstOrDefault(p => p.PropertyId == CruiseSpeedPropertyId);

                    if (speedProp != null && int.TryParse(speedProp.PropertyValue?.ToString(), out int speed))
                    {
                        PlannedSpeed = speed;
                    }
                    else
                    {
                        PlannedSpeed = 150;
                    }
                });

            this.WhenAnyValue(
                x => x.OriginAirport,
                x => x.DestAirport,
                x => x.PlannedSpeed)
            .Subscribe(inputs =>
            {
                var (origin, dest, speed) = inputs;

                RouteOptions.Clear();
                SelectedRouteOption = null;
                UpdateFlightPath();

                if (origin == null || dest == null)
                {
                    TotalDistance = 0;
                    EstimatedTime = TimeSpan.Zero;
                    return;
                }

                TotalDistance = _navigationServices.CalculateDistance(
                    origin.Laty, origin.Lonx,
                    dest.Laty, dest.Lonx
                );

                EstimatedTime = _navigationServices.CalculateEte(TotalDistance, speed);
            });
        }

        public double TotalDistance
        {
            get => _totalDistance;
            set => this.RaiseAndSetIfChanged(ref _totalDistance, value);
        }

        public TimeSpan EstimatedTime
        {
            get => _estimatedTime;
            set => this.RaiseAndSetIfChanged(ref _estimatedTime, value);
        }

        public int PlannedSpeed
        {
            get => _plannedSpeed;
            set => this.RaiseAndSetIfChanged(ref _plannedSpeed, value);
        }

        private Map? _flightMap;
        public Map? FlightMap
        {
            get => _flightMap;
            set => this.RaiseAndSetIfChanged(ref _flightMap, value);
        }


        public override string Title => "Create Flight";

        private async void InitializeDataAsync()
        {
            try
            {
                await LoadAircraftAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading aircraft: {ex.Message}");
            }

            try
            {
                await LoadAirportsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading airports/map: {ex.Message}");
            }
        }

        private async Task LoadAircraftAsync()
        {
            var aircraft = await _aircraftServices.GetSimAircraftsList();
            AircraftList.Clear();
            foreach (var plane in aircraft)
            {
                AircraftList.Add(plane);
            }
        }

        private async Task LoadAirportsAsync()
        {
            var fetchedAirports = await _airportServices.GetAirportsList();
            _airports.Clear();
            _airports.AddRange(fetchedAirports);

            SetupMap();
            DoFilter(SearchText);
        }

        private void DoFilter(string filter)
        {
            FilteredAirports.Clear();
            var query = filter?.ToLower().Trim() ?? "";

            foreach (var apt in _airports)
            {
                if (string.IsNullOrWhiteSpace(query) ||
                    (apt.AirportName?.ToLower().Contains(query) ?? false) ||
                    (apt.Ident?.ToLower().Contains(query) ?? false) ||
                    (apt.AirportsLocation?.GeoCity?.Name?.ToLower().Contains(query) ?? false))
                {
                    FilteredAirports.Add(apt);
                }
            }
        }

        private async void LoadAircraftProperties(int aircraftId)
        {
            var hydratedPlane = await _aircraftServices.GetSimAircraftWithPropertiesAsync(aircraftId);
            FullSelectedAircraft = hydratedPlane;
        }

        private void SetupMap()
        {
            LoggingWidget.ShowLoggingInMap = ActiveMode.No;
            var map = new Map();

            var perfWidget = map.Widgets.FirstOrDefault(w => w.GetType().Name == "PerformanceWidget");
            if (perfWidget != null) perfWidget.Enabled = false;

            map.BackColor = Color.Transparent;

            var (homeX, homeY) = SphericalMercator.FromLonLat(-71.5667, 42.3918);

            map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer("AeroSim2026/1.0"));

            _majorAirportsLayer = CreateAirportLayer(_airports.Where(a => a.AirportType == 2), Color.Red, 0, double.MaxValue);
            _regionalAirportsLayer = CreateAirportLayer(_airports.Where(a => a.AirportType == 3), Color.Orange, 0, 4000);
            _smallAirportsLayer = CreateAirportLayer(_airports.Where(a => a.AirportType == 1), Color.OrangeRed, 0, 1000);
            _milAirportsLayer = CreateAirportLayer(_airports.Where(a => a.AirportType == 8), Color.OrangeRed, 0, 1000);

            map.Layers.Add(_majorAirportsLayer);
            map.Layers.Add(_regionalAirportsLayer);
            map.Layers.Add(_smallAirportsLayer);
            map.Layers.Add(_milAirportsLayer);

            _routeLayer = new MemoryLayer
            {
                Name = "Route",
                Style = new VectorStyle
                {
                    Line = new Pen(Color.Magenta, 4),
                    Fill = null
                }
            };
            map.Layers.Add(_routeLayer);
            map.Widgets.Add(CreateZoomInOutWidget(Orientation.Horizontal, VerticalAlignment.Top, HorizontalAlignment.Right));

            FlightMap = map;
        }

        private ILayer CreateAirportLayer(IEnumerable<Airport> airports, Color color, double minVisible, double maxVisible)
        {
            var features = new List<GeometryFeature>();
            foreach (var airport in airports)
            {
                var mPoint = SphericalMercator.FromLonLat(airport.Lonx, airport.Laty);
                var ntsPoint = new Point(new Coordinate(mPoint.x, mPoint.y));

                var feature = new GeometryFeature(ntsPoint);
                feature["AirportData"] = airport;

                feature.Styles.Add(new LabelStyle
                {
                    Text = airport.Ident,
                    Font = new Font { FontFamily = "Arial", Size = 10, Bold = true },
                    ForeColor = Color.White,
                    Halo = new Pen(Color.Black, 2),
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom,
                    Offset = new Offset(0, -12)
                });

                features.Add(feature);
            }

            return new MemoryLayer
            {
                Name = "Airports",
                Features = features,
                Style = new SymbolStyle { SymbolScale = 0.5, Fill = new Brush(color) },
                MinVisible = minVisible,
                MaxVisible = maxVisible
            };
        }

        private void UpdateFlightPath()
        {

            bool hideClutter = OriginAirport != null && DestAirport != null;
            if (_majorAirportsLayer != null) _majorAirportsLayer.Enabled = !hideClutter;
            if (_regionalAirportsLayer != null) _regionalAirportsLayer.Enabled = !hideClutter;
            if (_smallAirportsLayer != null) _smallAirportsLayer.Enabled = !hideClutter;
            if (_milAirportsLayer != null) _milAirportsLayer.Enabled = !hideClutter;

            if (_routeLayer == null) return;
            var newFeatures = new List<GeometryFeature>();

            try
            {
                if (SelectedRouteOption != null && SelectedRouteOption.Waypoints != null && SelectedRouteOption.Waypoints.Any())
                {
                    var validWaypoints = SelectedRouteOption.Waypoints
                        .Where(w => !double.IsNaN(w.X) && !double.IsNaN(w.Y) && !double.IsInfinity(w.X) && !double.IsInfinity(w.Y))
                        .ToList();

                    if (validWaypoints.Count > 1)
                    {
                        newFeatures.Add(_mapFeatureFactory.CreateRouteLine(validWaypoints));
                    }

                    // ALWAYS add Origin marker
                    if (OriginAirport != null)
                    {
                        newFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(OriginAirport.Laty, OriginAirport.Lonx, OriginAirport.Ident, "AIRPORT"));
                    }

                    // Add intermediate Enroute markers
                    if (SelectedRouteOption.GeneratedFlightPlanRoutes != null && SelectedRouteOption.GeneratedFlightPlanRoutes.Any())
                    {
                        foreach (var leg in SelectedRouteOption.GeneratedFlightPlanRoutes)
                        {
                            var node = _routingGraph.GetNode(leg.WaypointId);
                            if (node != null && !double.IsNaN(node.Latitude) && !double.IsNaN(node.Longitude))
                            {
                                // Don't draw duplicates if the routing engine included the origin/dest in the airway list
                                if (OriginAirport != null && node.Identifier == OriginAirport.Ident) continue;
                                if (DestAirport != null && node.Identifier == DestAirport.Ident) continue;

                                newFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(node.Latitude, node.Longitude, node.Identifier, node.NavType));
                            }
                        }
                    }

                    // ALWAYS add Destination marker
                    if (DestAirport != null)
                    {
                        newFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(DestAirport.Laty, DestAirport.Lonx, DestAirport.Ident, "AIRPORT"));
                    }

                    // Auto-Center logic
                    if (FlightMap?.Navigator != null && validWaypoints.Count > 0)
                    {
                        MRect mRect;
                        if (validWaypoints.Count == 1)
                        {
                            mRect = new MRect(validWaypoints[0].X, validWaypoints[0].Y, validWaypoints[0].X, validWaypoints[0].Y);
                        }
                        else
                        {
                            var lineString = new LineString(validWaypoints.ToArray());
                            var env = lineString.EnvelopeInternal;
                            mRect = new MRect(env.MinX, env.MinY, env.MaxX, env.MaxY);
                        }

                        if (mRect != null && !double.IsNaN(mRect.Width) && !double.IsNaN(mRect.Height))
                        {
                            double padX = Math.Max(mRect.Width * 0.2, 50000);
                            double padY = Math.Max(mRect.Height * 0.2, 50000);

                            mRect = mRect.Grow(padX, padY);

                            if (FlightMap.Navigator.Viewport.Width > 0 && FlightMap.Navigator.Viewport.Height > 0)
                            {
                                FlightMap.Navigator.ZoomToBox(mRect);
                            }
                        }
                    }
                }

                _routeLayer.Features = newFeatures;
                _routeLayer.DataHasChanged();
                FlightMap?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL MAP ERROR: {ex.Message}");
            }
        }

        private void FlyToAirport(Airport airport)
        {
            var (x, y) = SphericalMercator.FromLonLat(airport.Lonx, airport.Laty);

            if (FlightMap?.Navigator != null)
            {
                double level14Resolution = 9.554628535647032;
                FlightMap.Navigator.CenterOnAndZoomTo(new MPoint(x, y), level14Resolution);
            }
        }
        public void ClearForm()
        {
            // Reset inputs
            OriginAirport = null;
            DestAirport = null;
            SelectedAircraft = null;
            SearchText = string.Empty;

            // Clear outputs and properties
            RouteOptions.Clear();
            SelectedRouteOption = null;
            TotalDistance = 0;
            EstimatedTime = TimeSpan.Zero;

            // Wipe the map route
            if (_routeLayer != null)
            {
                _routeLayer.Features = new List<GeometryFeature>();
                _routeLayer.DataHasChanged();
                FlightMap?.Refresh();
            }

            // Optional: Zoom the map back out to the whole US/World
            if (FlightMap?.Navigator != null)
            {
                var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(-98.5795, 39.8283); // Center of US
                FlightMap.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 4891); // Zoomed out resolution
            }
        }
        private static ZoomInOutWidget CreateZoomInOutWidget(Orientation orientation,
        VerticalAlignment verticalAlignment, HorizontalAlignment horizontalAlignment)
        {
            return new ZoomInOutWidget
            {
                Orientation = orientation,
                VerticalAlignment = verticalAlignment,
                HorizontalAlignment = horizontalAlignment,
                Margin = new MRect(20),
            };
        }
    }
}
