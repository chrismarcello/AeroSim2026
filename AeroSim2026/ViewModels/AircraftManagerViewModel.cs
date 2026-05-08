using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace AeroSim2026.ViewModels
{
    public class AircraftManagerViewModel : PageViewModelBase
    {
        private readonly IAircraftServices _aircraftServices;
        private readonly IStatusService _statusService;
        public override string Title => $"Aircraft Manager";

        // --- Tab 1: The Fleet ---
        public ObservableCollection<SimAircraft> Fleet { get; } = new();
        private SimAircraft? _selectedSimAircraft;
        public SimAircraft? SelectedSimAircraft
        {
            get => _selectedSimAircraft;
            set => this.RaiseAndSetIfChanged(ref _selectedSimAircraft, value);
        }

        // --- Tab 2: Database (Drill-Down) ---
        public ObservableCollection<AircraftManufacturer> Manufacturers { get; } = new();
        public ObservableCollection<AircraftType> Types { get; } = new();
        public ObservableCollection<AircraftModel> Models { get; } = new();

        private AircraftManufacturer? _selectedManufacturer;
        public AircraftManufacturer? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedManufacturer, value);
                SelectedType = null;
                LoadTypesForManufacturer(value);
            }
        }

        private AircraftType? _selectedType;
        public AircraftType? SelectedType
        {
            get => _selectedType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedType, value);

                LoadModelsForType(value);
            }
        }

        // --- Tab 3: Properties ---
        public ObservableCollection<AircraftProperty> Properties { get; } = new();

        // --- Commands ---

        public ReactiveCommand<Unit, Unit> RefreshDataCommand { get; }


        public AircraftManagerViewModel(IAircraftServices aircraftServices, IStatusService statusService)
        {
            _aircraftServices = aircraftServices;
            _statusService = statusService;
            // Load initial data
            RefreshDataCommand = ReactiveCommand.CreateFromTask(LoadInitialDataAsync);

            RxSchedulers.MainThreadScheduler.Schedule(() => { _ = LoadInitialDataAsync(); });
        }

        private async Task LoadInitialDataAsync()
        {
            _statusService.IsBusy = true;
            _statusService.StatusMessage = "Loading Aircraft Database...";

            try
            {
                var fleet = await _aircraftServices.GetSimAircraftsList();
                Fleet.Clear();

                foreach (var plane in fleet) Fleet.Add(plane);

                var mfrs = await _aircraftServices.GetAircraftManufacturerAsync();
                Manufacturers.Clear();
                foreach (var m in mfrs) Manufacturers.Add(m);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load aircraft data: {ex.Message}");
            }
            finally
            {
                _statusService.IsBusy = false;
                _statusService.StatusMessage = string.Empty;
            }
        }

        private async void LoadTypesForManufacturer(AircraftManufacturer? manufacturer)
        {
            Types.Clear();
            Models.Clear();
            if (manufacturer == null) return;
            
            try
            {
                var types = await _aircraftServices.GetAircraftTypesForManufacturerAsync(manufacturer.ManufacturerId);

                foreach (var t in types) Types.Add(t);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Types: {ex.Message}");
            }
        }

        private async void LoadModelsForType(AircraftType? type)
        {
            Models.Clear();
            if (type == null) return;

            try
            {
                var models = await _aircraftServices.GetAircraftModelsForTypeAsync(type.AircraftTypeId);
                foreach (var m in models) Models.Add(m);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Models: {ex.Message}");
            }
        }
    }
}
