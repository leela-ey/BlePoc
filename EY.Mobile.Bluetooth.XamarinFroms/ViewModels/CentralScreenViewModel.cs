using System;
using System.Threading.Tasks;
using System.Windows.Input;
using EY.Mobile.XamarinForms;
using Xamarin.Forms;

namespace EY.Mobile.Bluetooth.XamarinFroms.ViewModels
{
    public class CentralScreenViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _centralLogs;
        public string CentralLogs
        {
            get => _centralLogs;
            set => SetProperty(ref _centralLogs, value);
        }

        private string _scanningStatus;
        public string ScanningStatus
        {
            get => _scanningStatus;
            set => SetProperty(ref _scanningStatus, value);
        }

        public ICommand StartScanningCommand { get; private set; }

        public CentralScreenViewModel()
        {
            StartScanningCommand = new Command(async () => await StartScanning());
            ScanningStatus = "scanning status...";

            CentralLogs = "hsbrfhrbfiu";
        }

        private async Task StartScanning()
        {
            Console.WriteLine("CreateCentral btn clicked");
            BleService._bleCentral?.startScanning();

            ScanningStatus = $"Scanning :: {BleService._bleCentral._bluetoothCentral.IsScanning()}";

        }
    }
}