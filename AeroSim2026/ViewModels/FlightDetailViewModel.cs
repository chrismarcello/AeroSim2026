using AeroSim2026.EFModels;
using AeroSim2026.Core.Services;
using Avalonia.Platform;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Logging;
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
using System.Reactive.Disposables;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AeroSim2026.ViewModels
{
    public class FlightDetailViewModel : PageViewModelBase
    {
        private readonly IFlightServices _flightServices;
        private readonly IMapFeatureFactory _mapFeatureFactory;

        public FlightPlan Flight { get; set; }
        private readonly Action<PageViewModelBase> _navigate;
        private readonly PageViewModelBase _previousPage;
        private static string? _cachedTargetIconBase64;
        public string CrashedDisplayText => IsCrashed ? "Yes" : "No";
        private string? _originalComments;
        private DateTime? _originalDateFlown;
        private byte? _originalAircraftCrashed;
        private int? _originalCruiseAltitude;
        public ObservableCollection<FlightPlanRoute> FlightRoutes { get; } = new();

        private Map? _flightMap;
        public Map? FlightMap
        {
            get => _flightMap; 
            set => this.RaiseAndSetIfChanged(ref _flightMap, value);

        }
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                this.RaiseAndSetIfChanged(ref _isEditing, value);

                // Manually tell the UI that the opposite state has also changed
                this.RaisePropertyChanged(nameof(IsNotEditing));
            }
        }
        public decimal? EditCruiseAltitude
        {
            get => Flight.CruiseAltitude.HasValue ? (decimal)Flight.CruiseAltitude.Value : null;
            set
            {
                // Cast the incoming decimal to an int
                int? newInt = value.HasValue ? (int)value.Value : null;

                // Only trigger an update if the value actually changed
                if (Flight.CruiseAltitude != newInt)
                {
                    Flight.CruiseAltitude = newInt;
                    this.RaisePropertyChanged();
                }
            }
        }
        public DateTime? EditDateFlown
        {
            get => Flight.DateFlown;
            set { Flight.DateFlown = value; this.RaisePropertyChanged(); }
        }

        public string? EditComments
        {
            get => Flight.Comments;
            set { Flight.Comments = value; this.RaisePropertyChanged(); }
        }

        public bool IsCrashed
        {
            get => Flight.AircraftCrashed == 1;
            set
            {
                Flight.AircraftCrashed = value ? (byte)1 : (byte)0;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(CrashedDisplayText));
            }
        }

        public bool IsNotEditing => !IsEditing;
        public ICommand BeginEditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public override string Title => $" Flight Plan: {Flight.StartAirport.DisplayName} - {Flight.EndAirport.DisplayName}";
        
        public FlightDetailViewModel(IFlightServices flightServices, IMapFeatureFactory mapFeatureFactory, FlightPlan flight, Action<PageViewModelBase> navigate, PageViewModelBase previousPage) 
        { 
            _flightServices = flightServices;
            _mapFeatureFactory = mapFeatureFactory;
            Flight = flight;
            _navigate = navigate;
            _previousPage = previousPage;

            GoBackCommand = ReactiveCommand.Create(GoBack);                      
            BeginEditCommand = ReactiveCommand.Create(BeginEdit);
            SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
            CancelCommand = ReactiveCommand.Create(Cancel);

            _ = SetupMapAsync();
        }

        public ICommand GoBackCommand { get; }
        private void GoBack()
        {
            _navigate(_previousPage);
        }
        private void BeginEdit()
        {
            _originalComments = Flight.Comments; // Store original comments in case of cancel
            _originalDateFlown = Flight.DateFlown;
            _originalAircraftCrashed = Flight.AircraftCrashed;
            _originalCruiseAltitude = Flight.CruiseAltitude;
            IsEditing = true;
        }
        private void Cancel()
        {
            // Revert all values
            Flight.Comments = _originalComments;
            Flight.DateFlown = _originalDateFlown;
            Flight.AircraftCrashed = _originalAircraftCrashed;
            Flight.CruiseAltitude = _originalCruiseAltitude;

            // Manually tell the UI to refresh ALL the proxy properties
            this.RaisePropertyChanged(nameof(EditComments));
            this.RaisePropertyChanged(nameof(EditDateFlown));
            this.RaisePropertyChanged(nameof(EditCruiseAltitude));
            this.RaisePropertyChanged(nameof(IsCrashed));
            this.RaisePropertyChanged(nameof(CrashedDisplayText));

            IsEditing = false;
        }

        private async Task SaveAsync()
        {
            try
            {
                await _flightServices.UpdateFlightPlanAsync(Flight);

                // Tell the UI to refresh all fields to ensure read-mode shows the latest data
                this.RaisePropertyChanged(nameof(EditComments));
                this.RaisePropertyChanged(nameof(EditDateFlown));
                this.RaisePropertyChanged(nameof(EditCruiseAltitude));
                this.RaisePropertyChanged(nameof(IsCrashed));
                this.RaisePropertyChanged(nameof(CrashedDisplayText));

                IsEditing = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database save failed: {ex.Message}");
            }
        }
        private async Task SetupMapAsync()
        {
            LoggingWidget.ShowLoggingInMap = ActiveMode.No;
            var map = new Map();
            var perfWidget = map.Widgets.FirstOrDefault(w => w.GetType().Name == "PerformanceWidget");
            if (perfWidget != null) perfWidget.Enabled = false;

            map.BackColor = Mapsui.Styles.Color.Transparent;

            map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer("AeroSim2026/1.0"));

            var markerFeatures = new List<GeometryFeature>();
            var routePoints = new List<Coordinate>();

            var detailedFlight = await _flightServices.GetFlightPlanWithRoutesAsync(Flight.FlightPlanId);

            // 1. Origin Airport
            markerFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(Flight.StartAirport.Laty, Flight.StartAirport.Lonx, Flight.StartAirport.Ident, "AIRPORT"));

            var (oX, oY) = SphericalMercator.FromLonLat(Flight.StartAirport.Lonx, Flight.StartAirport.Laty);
            routePoints.Add(new Coordinate(oX, oY));

            // 2. Enroute Waypoints
            if (detailedFlight != null && detailedFlight.FlightPlanRoutes.Any())
            {
                var orderedRoutes = detailedFlight.FlightPlanRoutes.OrderBy(r => r.SequenceNumber).ToList();
                foreach (var step in orderedRoutes)
                {
                    if (step.Waypoint != null)
                    {
                        markerFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(step.Waypoint.Laty, step.Waypoint.Lonx, step.Waypoint.Ident, step.Waypoint.WaypointType));

                        var (wpX, wpY) = SphericalMercator.FromLonLat(step.Waypoint.Lonx, step.Waypoint.Laty);
                        routePoints.Add(new Coordinate(wpX, wpY));
                    }
                }
            }

            // 3. Destination Airport
            markerFeatures.Add(_mapFeatureFactory.CreateWaypointFeature(Flight.EndAirport.Laty, Flight.EndAirport.Lonx, Flight.EndAirport.Ident, "AIRPORT"));

            var (dX, dY) = SphericalMercator.FromLonLat(Flight.EndAirport.Lonx, Flight.EndAirport.Laty);
            routePoints.Add(new Coordinate(dX, dY));

            // Generate Great Circle if no intermediate waypoints exist
            if (routePoints.Count <= 2)
            {
                routePoints = GenerateGreatCirclePoints(Flight.StartAirport, Flight.EndAirport);
            }

            // 4. Assemble Layers
            map.Layers.Add(new MemoryLayer { Name = "Route", Features = new List<GeometryFeature> { _mapFeatureFactory.CreateRouteLine(routePoints) } });
            map.Layers.Add(new MemoryLayer { Name = "Markers", Features = markerFeatures });

            // 5. Zoom & Pan
            if (routePoints.Any())
            {
                var boundingBox = new MRect(
                    routePoints.Min(p => p.X), routePoints.Min(p => p.Y),
                    routePoints.Max(p => p.X), routePoints.Max(p => p.Y)
                );
                map.Navigator.ZoomToBox(boundingBox.Grow(boundingBox.Width * 0.2));
                map.Widgets.Add(CreateZoomInOutWidget(Orientation.Horizontal, VerticalAlignment.Top, HorizontalAlignment.Right));
            }

            FlightMap = map;
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
        private List<Coordinate> GenerateGreatCirclePoints(Airport origin, Airport dest, int numPoints = 100)
        {
            var points = new List<Coordinate>();

            double lat1 = origin.Laty * (Math.PI / 180.0);
            double lon1 = origin.Lonx * (Math.PI / 180.0);
            double lat2 = dest.Laty * (Math.PI / 180.0);
            double lon2 = dest.Lonx * (Math.PI / 180.0);

            double d = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin((lat2 - lat1) / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon2 - lon1) / 2), 2)));

            for (int i = 0; i <= numPoints; i++)
            {
                double f = (double)i / numPoints;
                double A = Math.Sin((1 - f) * d) / Math.Sin(d);
                double B = Math.Sin(f * d) / Math.Sin(d);

                double x = A * Math.Cos(lat1) * Math.Cos(lon1) + B * Math.Cos(lat2) * Math.Cos(lon2);
                double y = A * Math.Cos(lat1) * Math.Sin(lon1) + B * Math.Cos(lat2) * Math.Sin(lon2);
                double z = A * Math.Sin(lat1) + B * Math.Sin(lat2);

                double lat = Math.Atan2(z, Math.Sqrt(x * x + y * y));
                double lon = Math.Atan2(y, x);


                var (smX, smY) = SphericalMercator.FromLonLat(lon * (180.0 / Math.PI), lat * (180.0 / Math.PI));

                points.Add(new Coordinate(smX, smY));

            }
            return points;
        }
    }
}
