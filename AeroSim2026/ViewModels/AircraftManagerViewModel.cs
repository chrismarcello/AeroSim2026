using AeroSim2026.Core.Services;
using AeroSim2026.EFModels;
using AeroSim2026.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
        public ObservableCollection<AircraftPropertyValues> SelectedAircraftProperties { get; } = new();

        private SimAircraft? _selectedSimAircraft;
        public SimAircraft? SelectedSimAircraft
        {
            get => _selectedSimAircraft;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSimAircraft, value);
                // Trigger the property load whenever the selection changes
                LoadAircraftPropertiesAsync(value);
            }

        }

        public ReactiveCommand<Unit, Unit> RefreshDataCommand { get; }
        public ReactiveCommand<Unit,Unit> DeleteAircraftCommand { get; }

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

        // The Interaction: It asks for an AddManufacturerViewModel, and expects a string? back.
        public Interaction<AddManufacturerViewModel, string?> ShowAddManufacturerDialog { get; } = new();
        public ReactiveCommand<Unit, Unit> AddManufacturerCommand { get; }

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

        


        public AircraftManagerViewModel(IAircraftServices aircraftServices, IStatusService statusService)
        {
            _aircraftServices = aircraftServices;
            _statusService = statusService;
            // Load initial data
            RefreshDataCommand = ReactiveCommand.CreateFromTask(LoadInitialDataAsync);

            var canDelete = this.WhenAnyValue(x => x.SelectedSimAircraft)
                    .Select(plane => plane != null);
            DeleteAircraftCommand = ReactiveCommand.CreateFromTask(DeleteAircraftAsync, canDelete);

            AddManufacturerCommand = ReactiveCommand.CreateFromTask(AddManufacturerAsync);

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

        private async void LoadAircraftPropertiesAsync(SimAircraft? aircraft)
        {
            // Clear old properties immediately so they don't linger
            SelectedAircraftProperties.Clear();

            if (aircraft == null) return;

            _statusService.IsBusy = true;
            _statusService.StatusMessage = $"Loading properties for {aircraft.Aircraft.DisplayName}...";

            try
            {
                // Fetch the rich object containing the stitched properties
                var detailedPlane = await _aircraftServices.GetSimAircraftWithPropertiesAsync(aircraft.SimPlaneId);

                if (detailedPlane?.Properties != null)
                {
                    foreach (var prop in detailedPlane.Properties)
                    {
                        SelectedAircraftProperties.Add(prop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load aircraft properties: {ex.Message}");
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

        private async Task DeleteAircraftAsync()
        {
            if (SelectedSimAircraft == null) return;

            var planeToDelete = SelectedSimAircraft;

            _statusService.IsBusy = true;
            _statusService.StatusMessage = $"Deleting {planeToDelete.Aircraft.DisplayName} from fleet...";

            try
            {
                // 1. Delete from database
                await _aircraftServices.DeleteSimAircraftAsync(planeToDelete.SimPlaneId);

                // 2. Remove from the local UI collection so it disappears instantly
                Fleet.Remove(planeToDelete);

                // 3. Clear the selection
                SelectedSimAircraft = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete aircraft: {ex.Message}");
            }
            finally
            {
                _statusService.IsBusy = false;
                _statusService.StatusMessage = string.Empty;
            }
        }
        private async Task AddManufacturerAsync()
        {
            var dialogViewModel = new AddManufacturerViewModel();

            // This pauses the method and waits for the user to close the popup!
            var resultName = await ShowAddManufacturerDialog.Handle(dialogViewModel);

            if (!string.IsNullOrWhiteSpace(resultName))
            {
                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Saving Manufacturer...";
                try
                {
                    var newMfr = await _aircraftServices.AddAircraftManufacturerAsync(resultName);
                    // Add it to the UI list so it shows up instantly
                    Manufacturers.Add(newMfr);
                    // Select the newly created item
                    SelectedManufacturer = newMfr;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save manufacturer: {ex.Message}");
                }
                finally
                {
                    _statusService.IsBusy = false;
                    _statusService.StatusMessage = string.Empty;
                }
            }
        }
    }
}
