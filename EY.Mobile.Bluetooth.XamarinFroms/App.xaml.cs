using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using EY.Mobile.Bluetooth;
using EY.Mobile.XamarinForms;
using System.Threading.Tasks;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public partial class App : ApplicationBase
    {
        public App()
        {
            InitializeComponent();
            //MainPage = new MainPage();
        }

        protected override void OnStart()
        {

        }

        protected override void OnSleep()
        {

        }

        protected override void OnResume()
        {

        }

        protected override void InitializeContainer()
        {

        }

        protected async override Task<Page> CreateMainPage()
        {
            var page = await CreatePage<DeveloperScreens, DeveloperScreensViewModel>();
            return new NavigationPage(page);
        }
    }
}
