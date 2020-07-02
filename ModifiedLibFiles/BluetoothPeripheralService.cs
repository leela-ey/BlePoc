using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreBluetooth;
using CoreFoundation;
using EY.Mobile.Bluetooth.Enums;
using EY.Mobile.Bluetooth.EventArgs;
using Foundation;
using System.Linq;

namespace EY.Mobile.Bluetooth.iOS
{
    public class BluetoothPeripheralService : CBPeripheralManagerDelegate,IBluetoothPeripheralService
    {
        private static string _restorationIdentifier;

        public static void UseRestorationIdentifier(string restorationIdentifier)
        {
            _restorationIdentifier = restorationIdentifier;
        }

        CBPeripheralManager peripheralManager;
        IList<CBMutableService> servicesList;
        IList<CBMutableCharacteristic> characteristicsList;

        public BluetoothPeripheralService()
        {
            Console.WriteLine("iOS :: Concstructor of BluetoothPeripheralService invoked");
        }

        #region 1. Creation of Ble Peripheral Object

        public Task CreatePeripheral()
        {
            peripheralManager = new CBPeripheralManager(this, DispatchQueue.CurrentQueue, InitOptions);
            servicesList?.Clear();
            characteristicsList?.Clear();
            return Task.CompletedTask;
        }

        private NSDictionary InitOptions
        {
            get
            {
                var keys = new[] { new NSString(CBPeripheralManager.OptionRestoreIdentifierKey) };
                var values = new[] { new NSString(_restorationIdentifier) };
                var options = new NSDictionary<NSString, NSObject>(keys, values);
                return options;
            }
        }

        #endregion

        #region Implementation of CBPeripheralManagerDelegate methods

        // 1. Peripheral state is updated
        private event EventHandler<BluetoothState> _bluetoothStateUpdated;
        public event EventHandler<BluetoothState> BluetoothStateUpdated
        {
            add => _bluetoothStateUpdated += value;
            remove => _bluetoothStateUpdated -= value;
        }

        public override void StateUpdated(CBPeripheralManager peripheral)
        {
            _bluetoothStateUpdated?.Invoke(this, peripheral.State.ToBluetoothState());
        }

        // 2. Peripheral started advertising
        private event EventHandler<bool> _deviceAdvertised;
        public event EventHandler<bool> DeviceAdvertised
        {
            add => _deviceAdvertised += value;
            remove => _deviceAdvertised -= value;
        }

        private event EventHandler<BTAdvertiseFailureEventArgs> _deviceAdvertiseError;
        public event EventHandler<BTAdvertiseFailureEventArgs> DeviceAdvertiseError
        {
            add => _deviceAdvertiseError += value;
            remove => _deviceAdvertiseError -= value;
        }

        public override void AdvertisingStarted(CBPeripheralManager peripheral, NSError error)
        {
            if (error == null)
            {
                _deviceAdvertised?.Invoke(this, (peripheralManager != null) ? peripheralManager.Advertising : false);
            }
            else
            {
                _deviceAdvertiseError?.Invoke(this, new BTAdvertiseFailureEventArgs() { ErrorMessage = error.LocalizedDescription});
            }
        }

        // 3. Peripheral state is restored by iOS syste, when app is already terminated.
        private event EventHandler<BTPeripheralStateRestoredEventArgs> _peripheralStateRestored;
        public event EventHandler<BTPeripheralStateRestoredEventArgs> PeripheralStateRestored
        {
            add => _peripheralStateRestored += value;
            remove => _peripheralStateRestored -= value;
        }

        public override void WillRestoreState(CBPeripheralManager peripheral, NSDictionary dict)
        {
            var args = new BTPeripheralStateRestoredEventArgs();
            args.IsAdvertising = peripheral.Advertising;

            if (dict.ContainsKey(CBPeripheralManager.RestoredStateServicesKey))
            {
                var servicesRestored = (NSArray)dict[CBPeripheralManager.RestoredStateServicesKey];
                if (servicesRestored != null && servicesRestored.Count > 0)
                {
                    var services = new LocalPeripheralService[servicesRestored.Count];
                    for (nuint i = 0; i < servicesRestored.Count; i++)
                    {
                        var nativeService = servicesRestored.GetItem<CBMutableService>(i);
                        services[i] = new LocalPeripheralService(nativeService);
                    }
                    args.RestoredServices = services;
                }
            }
            _peripheralStateRestored?.Invoke(this, args);
        }

        // 4. Received Read Request
        private event EventHandler<BTGattCharacteristicReadRequestEventArgs> _receivedReadRequest;
        public event EventHandler<BTGattCharacteristicReadRequestEventArgs> ReceivedReadRequest
        {
            add => _receivedReadRequest += value;
            remove => _receivedReadRequest -= value;
        }
        public override void ReadRequestReceived(CBPeripheralManager peripheral, CBATTRequest request)
        {
            var readRequest = new BTGattCharacteristicReadRequest();
            readRequest.Central = request.Central;
            readRequest.Characteristic = new LocalPeripheralCharacteristic((CBMutableCharacteristic)request.Characteristic);
            readRequest.Offset = (int)request.Offset;
            readRequest.Value = request.Value.ToArray();
            readRequest.NativeRequest = request;
            _receivedReadRequest?.Invoke(this, new BTGattCharacteristicReadRequestEventArgs() { ReadRequest = readRequest });
        }

        // 5. Received Write Request
        private event EventHandler<BTGattCharacteristicWriteRequestEventArgs> _receivedWriteRequest;
        public event EventHandler<BTGattCharacteristicWriteRequestEventArgs> ReceivedWriteRequest
        {
            add => _receivedWriteRequest += value;
            remove => _receivedWriteRequest -= value;
        }
        public override void WriteRequestsReceived(CBPeripheralManager peripheral, CBATTRequest[] requests)
        {
            var writeRequest = new BTGattCharacteristicWriteRequest();
            writeRequest.Central = requests[0].Central;
            writeRequest.Characteristic = new LocalPeripheralCharacteristic((CBMutableCharacteristic)requests[0].Characteristic);
            writeRequest.Offset = (int)requests[0].Offset;
            writeRequest.Value = requests[0].Value.ToArray();
            writeRequest.NativeRequest = requests[0];
            _receivedWriteRequest?.Invoke(this, new BTGattCharacteristicWriteRequestEventArgs() { WriteRequest = writeRequest });
        }

        #endregion

        #region Create/Add/Remove Services

        public ILocalPeripheralService CreateLocalPeripheralService(Guid guid, bool isPrimary, ILocalPeripheralCharacteristic[] characteristics)
        {
            return new LocalPeripheralService(guid, isPrimary, characteristics);
        }

        public ILocalPeripheralCharacteristic CreateLocalPeripheralCharacteristic(Guid guid, CharacteristicPropertyType properties, CharacteristicPermissionType permissions, byte[] value)
        {
            return new LocalPeripheralCharacteristic(guid, properties, permissions, value);
        }

        public bool AddService(ILocalPeripheralService service)
        {
            if(servicesList == null)
            {
                servicesList = new List<CBMutableService>();
            }

            if (characteristicsList == null)
            {
                characteristicsList = new List<CBMutableCharacteristic>();
            }

            foreach (var characteristic in service.Characteristics)
            {
                characteristicsList.Add((CBMutableCharacteristic)characteristic.NativeCharacteristic);
            }

            servicesList.Add((CBMutableService)service.NativeService);

            peripheralManager.AddService((CBMutableService)service.NativeService);
            return true;
        }

        public void RemoveService(ILocalPeripheralService service)
        {
            peripheralManager?.RemoveService((CBMutableService)service.NativeService);
        }

        public void RemoveAllServices()
        {
            peripheralManager?.RemoveAllServices();
        }

        #endregion

        #region Current state of the Peripheral

        public BluetoothState GetPeripheralState()
        {
            return peripheralManager.State.ToBluetoothState();
        }

        #endregion


        #region Send notification to subscribed centrals

        public bool SendNotification(ILocalPeripheralCharacteristic characteristic, byte[] value, bool confirm)
        {
            var characteristicsToBeUpdated = characteristicsList.FirstOrDefault(x => x.UUID == characteristic.Guid.cbuuidFromGuid());
            bool result = false;
            if (characteristicsToBeUpdated != null)
            {
                result = peripheralManager.UpdateValue(NSData.FromArray(value), characteristicsToBeUpdated, null);
            }
            return result; // TODO: Define proper response which includes meaning information about failure.
        }

        #endregion

        #region Responding to Read/Write request
        
        public void RespondToReadRequest(BTGattCharacteristicReadResponse response)
        {
            peripheralManager.RespondToRequest((CBATTRequest)response.ReadRequest.NativeRequest, response.Result.ToNative());
        }

        public void RespondToWriteRequest(BTGattCharacteristicWriteResponse response)
        {
            peripheralManager.RespondToRequest((CBATTRequest)response.WriteRequest.NativeRequest, response.Result.ToNative());
        }

        #endregion

        #region Advertising

        public Task StartAdvertising(BTAdvertiseData data)
        {
            CBUUID[] servicesUuid = Array.ConvertAll(data.ServiceGuids,
            new Converter<Guid, CBUUID>(GuidToCbuuid));
            //var serviceUuids = new CBUUID[data.ServiceGuids.Length];
            //for (int i = 0; i < data.ServiceGuids.Length; i++)
            //{
            //    serviceUuids[i] = data.ServiceGuids[i].cbuuidFromGuid();
            //}
            var options = new StartAdvertisingOptions() { LocalName = data.DeviceName, ServicesUUID = servicesUuid };
            peripheralManager?.StartAdvertising(options);
            return Task.CompletedTask;
        }

        private CBUUID GuidToCbuuid(Guid guid)
        {
            return guid.cbuuidFromGuid();
        }

        public Task StopAdvertising()
        {
            peripheralManager?.StopAdvertising();
            return Task.CompletedTask;
        }

        public bool IsAdvertising()
        {
            return (peripheralManager != null)? peripheralManager.Advertising : false;
        }

        #endregion

    }
}
