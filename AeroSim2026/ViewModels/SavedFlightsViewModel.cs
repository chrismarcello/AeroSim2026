using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class SavedFlightsViewModel : PageViewModelBase, IActivatableViewModel
    {
        private readonly ILogger<SavedFlightsViewModel> _logger;
        private readonly IFlightServices _flightServices;
        private readonly IMapFeatureFactory _mapFeatureFactory;
        private readonly Action<PageViewModelBase> _navigateAction;
        private readonly IStatusService _statusService;

        private FlightPlan? _selectedUnflownFlight;
        private FlightPlan? _selectedFlownFlight;

        public override string Title => "Saved Flights";
        public ObservableCollection<FlightPlan> FlownFlights { get; } = new();
        public ObservableCollection<FlightPlan> UnflownFlights { get; } = new();


        public ViewModelActivator Activator { get; } = new ViewModelActivator();
        public SavedFlightsViewModel(ILogger<SavedFlightsViewModel> logger, IFlightServices flightServices, IMapFeatureFactory mapFeatureFactory, IStatusService statusService, Action<PageViewModelBase> navigationAction)
        {
            _logger = logger;
            _flightServices = flightServices;
            _mapFeatureFactory = mapFeatureFactory;
            _navigateAction = navigationAction;
            _statusService = statusService;

            
            this.WhenActivated((CompositeDisposable disposables) =>
            {
                // Clear selections upon activation
                SelectedUnflownFlight = null;
                SelectedFlownFlight = null;

                _ = LoadData();

                // 2. Watch BOTH properties for changes
                this.WhenAnyValue(x => x.SelectedUnflownFlight, x => x.SelectedFlownFlight)
                    .Subscribe(tuple =>
                    {
                        // Get whichever flight was just selected
                        var selectedFlight = tuple.Item1 ?? tuple.Item2;

                        if (selectedFlight != null)
                        {
                            NavigateToDetails(selectedFlight);
                        }
                    })
                    .DisposeWith(disposables);
            });

        }

        public async Task LoadData()
        {
            try
            {
                var unflown = await _flightServices.GetUnflownFlights();

                UnflownFlights.Clear();

                foreach (var flight in unflown)
                {
                    UnflownFlights.Add(flight);

                }

                var flown = await _flightServices.GetflownFlights();

                FlownFlights.Clear();

                foreach (var flight in flown)
                {
                    FlownFlights.Add(flight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading flight data");
            }

        }

        public FlightPlan? SelectedUnflownFlight
        {
            get => _selectedUnflownFlight;
            set => this.RaiseAndSetIfChanged(ref _selectedUnflownFlight, value);
        }

        public FlightPlan? SelectedFlownFlight
        {
            get => _selectedFlownFlight;
            set => this.RaiseAndSetIfChanged(ref _selectedFlownFlight, value);
        }
        private void NavigateToDetails(FlightPlan flight)
        {
            Action<PageViewModelBase> returnNavAction = (page) =>
            {
                if (page == this)
                {
                    _=LoadData();

                    SelectedFlownFlight = null;
                    SelectedUnflownFlight = null;

                    _navigateAction(page);
                }
            };
            var detailView = new FlightDetailViewModel(_flightServices, _mapFeatureFactory, _statusService, flight, returnNavAction, this);
            _navigateAction(detailView);
        }
    }
}
