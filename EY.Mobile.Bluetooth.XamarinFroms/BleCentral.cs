using System;
using Unity;
using Xamarin.Forms;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public class BleCentral
    {
        public IBluetoothCentralService _bluetoothCentral;
        public BleCentral()
        {
            var app = Application.Current as App;
            _bluetoothCentral = app.Container.Resolve<IBluetoothCentralService>();
        }

        public void CreateCentral()
        {
            _bluetoothCentral.IntializeBluetoothCentralService();
        }

        public void startScanning()
        {
            Guid[] services = new Guid[] { EyC19CTService.EyCtServiceUuid };
            _bluetoothCentral.DeviceDiscovered += BtDeviceDiscovered;

            _bluetoothCentral.StartScanningForDevicesAsync(ScanMode.Balanced, services);

        }

        private void BtDeviceDiscovered(object sender, EventArgs.BTDeviceEventArgs e)
        {
            if (e.Device != null)
            {
                IBTDevice device = e.Device;
                Console.WriteLine($"Discovered a device : {device.Id.ToString()}");
                _bluetoothCentral.DeviceConnected += BtDeviceConnected;

                _bluetoothCentral.ConnectToDeviceAsync(device);
            }
        }

        private async void BtDeviceConnected(object sender, EventArgs.BTDeviceEventArgs e)
        {
            if (e.Device != null)
            {
                Console.WriteLine($"Connected to device : {e.Device.Id.ToString()}");
                IBluetoothService contactTracingService =  await e.Device.GetServiceAsync(EyC19CTService.EyCtServiceUuid);
                if (contactTracingService != null)
                {
                    Console.WriteLine("Discovered the contact tracing service");

                    IBluetoothGattCharacteristic identityCharacteristic = await contactTracingService.GetCharacteristicAsync(EyC19CTService.IdentityCharacteristicUuid);
                    if (identityCharacteristic != null)
                    {
                        byte[] result = await identityCharacteristic.ReadAsync();
                        var str = System.Text.Encoding.Default.GetString(result);
                        Console.WriteLine($"Found Idenity Characterictic : {str}");
                        int rssi = await e.Device.ReadRssiAsync();
                        Console.WriteLine($"RSSI : {rssi}");
                    }
                }
            }
        }
    }
}
