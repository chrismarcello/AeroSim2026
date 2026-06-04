using AeroSim2026.EFModels;
using AeroSim2026.Core.Services;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace AeroSim2026.ViewModels
{
    public class AddManufacturerViewModel : ViewModelBase
    {
        private readonly IGeographyServices _geoServices;

        private string _manufacturerName = string.Empty;
        public string ManufacturerName
        {
            get => _manufacturerName;
            set => this.RaiseAndSetIfChanged(ref _manufacturerName, value);
        }

        public ObservableCollection<Countryinfo> AvailableCountries { get; } = new();

        private Countryinfo? _selectedCountry;
        public Countryinfo? SelectedCountry
        {
            get => _selectedCountry;
            set => this.RaiseAndSetIfChanged(ref _selectedCountry, value);
        }

        public ReactiveCommand<Unit, (string Name, string CountryIso)?> SaveCommand { get; }
        public ReactiveCommand<Unit, (string Name, string CountryIso)?> CancelCommand { get; }

        public AddManufacturerViewModel(IGeographyServices geographyServices)
        {            
            _geoServices = geographyServices;

            var canSave = this.WhenAnyValue(
                x => x.ManufacturerName,
                x => x.SelectedCountry,
                (name, country) => !string.IsNullOrWhiteSpace(name) && country != null);

            SaveCommand = ReactiveCommand.Create(() =>
            {
                return ((string, string)?)(ManufacturerName, SelectedCountry?.IsoAlpha2 ?? "None");
            },
            canSave
            );

            CancelCommand = ReactiveCommand.Create(() => ((string, string)?)null);

            LoadCountriesAsync();
        }

        private async void LoadCountriesAsync()
        {
            var countries = await _geoServices.GetAllCountriesAsync();

            AvailableCountries.Add(new Countryinfo { Name = "None", IsoAlpha2 = "None" });

            foreach (var country in countries)
            {
                AvailableCountries.Add(country);
            }

            SelectedCountry = AvailableCountries.First();
        }
    }
}
