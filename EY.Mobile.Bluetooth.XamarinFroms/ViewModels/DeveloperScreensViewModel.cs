using System;
using System.Threading.Tasks;
using System.Windows.Input;
using EY.Mobile.Bluetooth.XamarinFroms.Views;
using EY.Mobile.XamarinForms;
using Xamarin.Forms;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public class DeveloperScreensViewModel : ViewModelBase
    {
        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand NavigateToDeveloperPageCommand { get; private set; }

        public DeveloperScreensViewModel()
        {
            Title = "Developer Screens";
            NavigateToDeveloperPageCommand = new Command(async () => await NavigateToDeveloperPage());
        }

        private async Task NavigateToDeveloperPage()
        {
            Console.WriteLine("NavigateToDeveloperPage btn clicked!");
            //await ((App)Application.Current).CreateDeveloperRootPage();
            await this.PushAsync<StartScreen, StartScreenViewModel>();
        }
    }
}