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
        private readonly IGeographyServices _geographyServices;
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

        // The Interaction: It asks for an AddManufacturerViewModel, and expects a (string Name, string CountryIso)? back.
        public Interaction<AddManufacturerViewModel, (string Name, string CountryIso)?> ShowAddManufacturerDialog { get; } = new();
        public ReactiveCommand<Unit, Unit> AddManufacturerCommand { get; }

        public Interaction<AddAircraftTypeViewModel, (string Name, string Code, string AirFam, string EngFam)?> ShowAddTypeDialog { get; } = new();
        public ReactiveCommand<Unit, Unit> AddTypeCommand { get; }

        private AircraftModel? _selectedModel;
        public AircraftModel? SelectedModel
        {
            get => _selectedModel;
            set => this.RaiseAndSetIfChanged(ref _selectedModel, value);
        }

        // Interaction for the Model Dialog
        public Interaction<AddAircraftModelViewModel, (string Name, string NativeName, int? EngineCount, string EngineModels)?> ShowAddModelDialog { get; } = new();

        // Commands
        public ReactiveCommand<Unit, Unit> AddModelCommand { get; }
        public ReactiveCommand<Unit, Unit> AddToFleetCommand { get; }


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

        


        public AircraftManagerViewModel(IAircraftServices aircraftServices, IStatusService statusService, IGeographyServices geographyServices)
        {
            _aircraftServices = aircraftServices;
            _statusService = statusService;
            _geographyServices = geographyServices;
            // Load initial data
            RefreshDataCommand = ReactiveCommand.CreateFromTask(LoadInitialDataAsync);

            var canDelete = this.WhenAnyValue(x => x.SelectedSimAircraft)
                    .Select(plane => plane != null);
            DeleteAircraftCommand = ReactiveCommand.CreateFromTask(DeleteAircraftAsync, canDelete);

            AddManufacturerCommand = ReactiveCommand.CreateFromTask(AddManufacturerAsync);

            var canAddType = this.WhenAnyValue(x => x.SelectedManufacturer)
                     .Select(mfr => mfr != null);

            AddTypeCommand = ReactiveCommand.CreateFromTask(AddTypeAsync, canAddType);

            var canAddModel = this.WhenAnyValue(x => x.SelectedType)
                      .Select(type => type != null);
            AddModelCommand = ReactiveCommand.CreateFromTask(AddModelAsync, canAddModel);

            var canAddToFleet = this.WhenAnyValue(x => x.SelectedModel)
                                    .Select(model => model != null);
            AddToFleetCommand = ReactiveCommand.CreateFromTask(AddToFleetAsync, canAddToFleet);

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
            var dialogViewModel = new AddManufacturerViewModel(_geographyServices);

            // This pauses the method and waits for the user to close the popup!
            var result = await ShowAddManufacturerDialog.Handle(dialogViewModel);
            
            if (result.HasValue)
            {
                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Saving Manufacturer...";
                try
                {
                    var newMfr = await _aircraftServices.AddAircraftManufacturerAsync(result.Value.Name, result.Value.CountryIso);
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
        private async Task AddTypeAsync()
        {
            if (SelectedManufacturer == null) return;
            var dialogViewModel = new AddAircraftTypeViewModel(_aircraftServices,SelectedManufacturer.ManufacturerName);
            // This pauses the method and waits for the user to close the popup!
            var result = await ShowAddTypeDialog.Handle(dialogViewModel);
            if (result.HasValue)
            {
                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Saving Aircraft Type...";
                try
                {
                    var newType = await _aircraftServices.AddAircraftTypeAsync(
                SelectedManufacturer.ManufacturerId,
                result.Value.Name,
                result.Value.Code,
                result.Value.AirFam,
                result.Value.EngFam);

                    Types.Add(newType);
                    SelectedType = newType; // Auto-select the newly added type
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save aircraft type: {ex.Message}");
                }
                finally
                {
                    _statusService.IsBusy = false;
                    _statusService.StatusMessage = string.Empty;
                }
            }
        }
        private async Task AddModelAsync()
        {
            if (SelectedType == null || SelectedManufacturer == null) return;

            // NOTE: You will need to create AddAircraftModelViewModel next!
            var dialogViewModel = new AddAircraftModelViewModel(SelectedType.AircraftTypeName);

            var result = await ShowAddModelDialog.Handle(dialogViewModel);

            if (result.HasValue)
            {
                _statusService.IsBusy = true;
                _statusService.StatusMessage = "Saving Aircraft Model...";
                try
                {
                    var newModel = await _aircraftServices.AddAircraftModelAsync(
                        SelectedType.AircraftTypeId,
                        SelectedManufacturer.ManufacturerId,
                        result.Value.Name,
                        result.Value.NativeName,
                        result.Value.EngineCount,
                        result.Value.EngineModels);

                    Models.Add(newModel);
                    SelectedModel = newModel;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save aircraft model: {ex.Message}");
                }
                finally
                {
                    _statusService.IsBusy = false;
                    _statusService.StatusMessage = string.Empty;
                }
            }
        }

        private async Task AddToFleetAsync()
        {
            if (SelectedModel == null) return;

            _statusService.IsBusy = true;
            _statusService.StatusMessage = $"Adding {SelectedModel.AircraftName} to Fleet...";

            try
            {
                var newSimPlane = await _aircraftServices.AddSimAircraftAsync(SelectedModel.AircraftModelId);

                if (newSimPlane != null)
                {
                    Fleet.Add(newSimPlane); // Adds it to Tab 1 instantly
                    SelectedSimAircraft = newSimPlane;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add to fleet: {ex.Message}");
            }
            finally
            {
                _statusService.IsBusy = false;
                _statusService.StatusMessage = string.Empty;
            }
        }
    }
}
