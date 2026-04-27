using AeroSim2026.Core.Routing;
using AeroSim2026.Core.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AeroSim2026.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger<SavedFlightsViewModel> _logger;
        private readonly IFlightServices _flightServices;
        private readonly IAircraftServices _aircraftServices;
        private readonly IAirportServices _airportServices;
        private readonly INavigationServices _navigationServices;
        private readonly FlightRouteBuilder _flightRouteBuilder;
        private readonly RoutingGraph _routingGraph;
        private readonly IMapFeatureFactory _mapFeatureFactory;
        public IStatusService StatusService { get; }
        public MainWindowViewModel(ILogger<SavedFlightsViewModel> logger, IFlightServices flightservices, IAircraftServices aircraftServices, IAirportServices airportServices, INavigationServices navigationServices, IStatusService statusService, FlightRouteBuilder flightRouteBuilder, RoutingGraph routingGraph, IMapFeatureFactory mapFeatureFactory)
        {
            _logger = logger;
            StatusService = statusService;
            _flightServices = flightservices;
            _aircraftServices = aircraftServices;
            _airportServices = airportServices;
            _navigationServices = navigationServices;
            _flightRouteBuilder = flightRouteBuilder;
            _routingGraph = routingGraph;
            _mapFeatureFactory = mapFeatureFactory;
#pragma warning disable CS8604 // Possible null reference argument.
            Pages = new ObservableCollection<PageViewModelBase>
            {
                new DashboardViewModel(),
                new SavedFlightsViewModel(_logger,_flightServices, _mapFeatureFactory, StatusService, Navigate),
                new CreateFlightViewModel(_aircraftServices, _airportServices, _navigationServices, _flightServices, StatusService, _flightRouteBuilder, _routingGraph, _mapFeatureFactory),
                new RandomFlightViewModel(_aircraftServices, _airportServices, _navigationServices, _flightServices, StatusService, _flightRouteBuilder, _routingGraph, mapFeatureFactory),
                new SettingsViewModel()
                
            };


#pragma warning disable CS8601 // Possible null reference assignment.
            _CurrentPage = Pages.FirstOrDefault();
#pragma warning restore CS8601 // Possible null reference assignment.

            NavigateCommand = ReactiveCommand.Create<PageViewModelBase>(Navigate);
            InitializeApplicationData();

        }

        public ObservableCollection<PageViewModelBase> Pages { get; }

        private PageViewModelBase _CurrentPage;
        public PageViewModelBase CurrentPage
        {
            get => _CurrentPage;
            set => this.RaiseAndSetIfChanged(ref _CurrentPage, value);
        }

        public ICommand NavigateCommand { get; }

        private void Navigate(PageViewModelBase page)
        {
            CurrentPage = page;
        }
        private async void InitializeApplicationData()
        {
            StatusService.IsBusy = true;
            StatusService.StatusMessage = "Warming up...";

            // Load the graph in the background so the UI doesn't freeze on startup
            //await Task.Run(async () =>
            //{
            //    await _navigationServices.InitializeRoutingGraphAsync(_routingGraph);
            //});
            await Task.Delay(500); // Simulate some startup delay
            StatusService.IsBusy = false;
            StatusService.StatusMessage = "Ready";
        }
    }
}
