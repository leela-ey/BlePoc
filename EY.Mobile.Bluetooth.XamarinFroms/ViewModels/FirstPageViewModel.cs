using System;
using System.Threading.Tasks;
using System.Windows.Input;
using EY.Mobile.Bluetooth.XamarinFroms.Views;
using EY.Mobile.XamarinForms;
using Xamarin.Forms;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public class FirstPageViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand CreateCentralCommand { get; private set; }

        public ICommand CreatePeripheralCommand { get; private set; }

        public FirstPageViewModel()
        {
            Title = "First Page";
            CreateCentralCommand = new Command(async () => await CreateCentral());
            CreatePeripheralCommand = new Command(async () => await CreatePeripheral());
        }

        //private async Task NavigateToDeveloperPage()
        //{
        //    Console.WriteLine("NavigateToDeveloperPage btn clicked!");
        //    //await ((App)Application.Current).CreateDeveloperRootPage();
        //    await this.PushAsync<FirstPage, FirstPageViewModel>();
        //}

        private async Task CreateCentral()
        {
            Console.WriteLine("CreateCentral btn clicked");
        }

        private async Task CreatePeripheral()
        {
            Console.WriteLine("CreatePeripheral btn clicked");
        }
    }
}