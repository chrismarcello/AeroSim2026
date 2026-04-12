using System.ComponentModel;

namespace AeroSim2026.Core.Services
{
    public interface IStatusService : INotifyPropertyChanged
    {
        bool IsBusy { get; set; }
        string StatusMessage { get; set; }
    }
}
