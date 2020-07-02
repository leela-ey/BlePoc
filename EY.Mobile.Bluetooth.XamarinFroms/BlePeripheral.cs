using System;
using System.Text;
using Unity;
using Xamarin.Forms;
//using EY.Mobile.Bluetooth.iOS;

namespace EY.Mobile.Bluetooth.XamarinFroms
{
    public class BlePeripheral
    {
        public IBluetoothPeripheralService _bluetoothPeripheral;

        public BlePeripheral()
        {
            var app = Application.Current as App;
            _bluetoothPeripheral = app.Container.Resolve<IBluetoothPeripheralService>();
            _bluetoothPeripheral.BluetoothStateUpdated += BluetoothStateUpdated;
        }

        private void BluetoothStateUpdated(object sender, Enums.BluetoothState e)
        {
            Console.WriteLine($"BluetoothStateUpdated : {e.ToString()}");
        }

        public void CreatePeripheral()
        {
            _bluetoothPeripheral.CreatePeripheral();
        }

        public void StartAdvertising()
        {
            BTAdvertiseData advertisementData = new BTAdvertiseData();
            advertisementData.DeviceName = "EYBleDev";
            advertisementData.ServiceGuids = new Guid[] { EyC19CTService.EyCtServiceUuid };
            _bluetoothPeripheral.DeviceAdvertised += _bluetoothPeripheral_DeviceAdvertised;
            _bluetoothPeripheral.StartAdvertising(advertisementData);
        }

        private void _bluetoothPeripheral_DeviceAdvertised(object sender, bool e)
        {
            Console.WriteLine($"Peripheral advertising : {e}");
            if (e)
            {
                _bluetoothPeripheral.ReceivedReadRequest += ReceivedReadRequest;
                _bluetoothPeripheral.ReceivedWriteRequest += ReceivedWriteRequest;
            }
        }

        private void ReceivedWriteRequest(object sender, EventArgs.BTGattCharacteristicWriteRequestEventArgs e)
        {
            
        }

        private void ReceivedReadRequest(object sender, EventArgs.BTGattCharacteristicReadRequestEventArgs e)
        {
            if (e.ReadRequest.Characteristic.Guid == EyC19CTService.IdentityCharacteristicUuid)
            {
                byte[] data = Encoding.ASCII.GetBytes("identity");
                e.ReadRequest.Value = data;
                var response = new BTGattCharacteristicReadResponse();
                response.ReadRequest = e.ReadRequest;
                response.Result = Enums.CharacteristicReadWriteResult.Success;
                _bluetoothPeripheral.RespondToReadRequest(response);
            }
        }

        public void StopAdvertising()
        {
            _bluetoothPeripheral.StopAdvertising();
        }
        
        public void AddServices()
        {
            ILocalPeripheralCharacteristic identityCharacteristic =
                _bluetoothPeripheral.CreateLocalPeripheralCharacteristic(
                    EyC19CTService.IdentityCharacteristicUuid,
                    CharacteristicPropertyType.Read,
                    CharacteristicPermissionType.PERMISSION_READ,
                    null);

            ILocalPeripheralCharacteristic[] characteristics = { identityCharacteristic };
            ILocalPeripheralService service =
                _bluetoothPeripheral.CreateLocalPeripheralService(
                    EyC19CTService.EyCtServiceUuid, true, characteristics);
            _bluetoothPeripheral.AddService(service);
        }
    }
}
