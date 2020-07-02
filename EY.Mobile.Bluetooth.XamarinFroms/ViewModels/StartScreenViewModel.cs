using System;
using System.Threading.Tasks;
using System.Windows.Input;
using EY.Mobile.Bluetooth.XamarinFroms.ViewModels;
using EY.Mobile.Bluetooth.XamarinFroms.Views;
using EY.Mobile.XamarinForms;
using Xamarin.Forms;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public class StartScreenViewModel : ViewModelBase
    {
        //BleService _bleService;
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand CreateCentralCommand { get; private set; }

        public ICommand CreatePeripheralCommand { get; private set; }

        public StartScreenViewModel()
        {
            //_bleService = new BleService();
            CreateCentralCommand = new Command(async () => await CreateCentral());
            CreatePeripheralCommand = new Command(async () => await CreatePeripheral());
        }

        private async Task CreateCentral()
        {
            Console.WriteLine("CreateCentral btn clicked");
            BleService._bleCentral?.CreateCentral();
            await this.PushAsync<CentralScreen, CentralScreenViewModel>();
        }

        private async Task CreatePeripheral()
        {
            Console.WriteLine("CreatePeripheral btn clicked");
        }
    }
}
