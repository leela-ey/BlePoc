using System;
using CoreBluetooth;
using CoreFoundation;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Foundation;
using EY.Mobile.Bluetooth.EventArgs;
using System.Collections.Generic;

namespace EY.Mobile.Bluetooth.iOS
{
    /// <summary>
    ///     BluetoothCentralService class contains methods to create BLE central, scan/stopScan for peripherals, handle discovered/connected/disconnected peripherals
    /// </summary>
    public class BluetoothCentralService : CBCentralManagerDelegate, IBluetoothCentralService
    {
        private static string _restorationIdentifier;
        private static bool _showPowerAlert = true;

        CBCentralManager centralManager;

        public static void UseRestorationIdentifier(string restorationIdentifier)
        {
            _restorationIdentifier = restorationIdentifier;
        }

        public static void ShowPowerAlert(bool showPowerAlert)
        {
            _showPowerAlert = showPowerAlert;
        }

        public BluetoothCentralService()
        {
            Console.WriteLine("iOS :: Concstructor of BluetoothCentralService invoked");
        }

        #region 1. Creation of Ble Central object

        // Create Bluetooth Low Energy central object
        public void IntializeBluetoothCentralService()
        {
            // prepare init options
            CBCentralInitOptions initOptions = CreateInitOptions();

            //centralManagerDelegate = new BleCentralManagerDelegate();

            // create ble central object
            centralManager = new CBCentralManager(this, DispatchQueue.CurrentQueue, initOptions);
        }

        private CBCentralInitOptions CreateInitOptions()
        {
            return new CBCentralInitOptions
            {
                RestoreIdentifier = _restorationIdentifier,
                ShowPowerAlert = _showPowerAlert
            };
        }

        public bool IsScanning()
        {
            return (centralManager != null)? centralManager.IsScanning : false;
        }

        public Task StartScanningForDevicesAsync(ScanMode mode, Guid[] serviceUuids = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default)
        {
            if (centralManager.State == CBCentralManagerState.PoweredOn)
            {
                Console.WriteLine("BluetoothCentralService: Starting a scan for devices.");

                CBUUID[] serviceCbuuids = null;
                if (serviceUuids != null && serviceUuids.Any())
                {
                    serviceCbuuids = serviceUuids.Select(uuid => uuid.cbuuidFromGuid()).ToArray();
                }

                centralManager.ScanForPeripherals(serviceCbuuids, new PeripheralScanningOptions { AllowDuplicatesKey = allowDuplicatesKey });
                Console.WriteLine("BluetoothCentralService: Scanning for " + serviceCbuuids.First());
                return Task.CompletedTask;
            }
            else
            {
                throw new TaskCanceledException("Scanning cancelled, Bluetooth is not ON");
            }
        }

        public Task StopScanningForDevicesAsync()
        {
            centralManager.StopScan();
            return Task.CompletedTask;
        }

        public Task ConnectToDeviceAsync(IBTDevice device, CancellationToken cancellationToken = default)
        {
            if (device != null)
            {
                centralManager.ConnectPeripheral(device.NativeDevice as CBPeripheral, new PeripheralConnectionOptions());

                cancellationToken.Register(() =>
                {
                    Console.WriteLine("Canceling the connect attempt");
                    centralManager.CancelPeripheralConnection(device.NativeDevice as CBPeripheral);
                });
            }
            return Task.FromResult(true);
        }

        public Task DisconnectDeviceAsync(IBTDevice device)
        {
            centralManager.CancelPeripheralConnection(device.NativeDevice as CBPeripheral);
            return Task.CompletedTask;
        }

        #endregion


        #region 2. IBluetoothCentralService events implementation

        private event EventHandler<BTDeviceEventArgs> _discoveredPeripheral;
        public event EventHandler<BTDeviceEventArgs> DeviceDiscovered
        {
            add => _discoveredPeripheral += value;
            remove => _discoveredPeripheral -= value;
        }

        private event EventHandler<BTDeviceEventArgs> _deviceConnected;
        public event EventHandler<BTDeviceEventArgs> DeviceConnected
        {
            add => _deviceConnected += value;
            remove => _deviceConnected -= value;
        }

        private event EventHandler<BTDeviceEventArgs> _deviceDisconnected;
        public event EventHandler<BTDeviceEventArgs> DeviceDisconnected
        {
            add => _deviceDisconnected += value;
            remove => _deviceDisconnected -= value;
        }

        private event EventHandler<BTDeviceErrorEventArgs> _deviceConnectionLost;
        public event EventHandler<BTDeviceErrorEventArgs> DeviceConnectionLost
        {
            add => _deviceConnectionLost += value;
            remove => _deviceConnectionLost -= value;
        }


        private event EventHandler<BTCentralStateRestoredEventArgs> _centralStateRestorediOS;
        public event EventHandler<BTCentralStateRestoredEventArgs> CentralStateRestorediOS
        {
            add => _centralStateRestorediOS += value;
            remove => _centralStateRestorediOS -= value;
        }


        #endregion

        #region CBCentralManagerDelegate methods implementation

        public override void UpdatedState(CBCentralManager central)
        {
            
        }

        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            BTCentralStateRestoredEventArgs args = new BTCentralStateRestoredEventArgs();

            if (dict.ContainsKey(CBCentralManager.RestoredStatePeripheralsKey))
            {
                var value = dict[CBCentralManager.RestoredStatePeripheralsKey];
                IEnumerable<CBPeripheral> restorePeripherals = value as IEnumerable<CBPeripheral>;
                if (restorePeripherals != null && restorePeripherals.Count() > 0)
                {
                    IList<IBTDevice> restoredBtDevices = new List<IBTDevice>();
                    foreach (var peripheral in restorePeripherals)
                    {
                        restoredBtDevices.Append(new BTDevice(peripheral, this));
                    }
                    args.AreBtDevicesToBeRestored = true;
                    args.RestoredBtDevices = restoredBtDevices;
                }
            }

            if (dict.ContainsKey(CBCentralManager.RestoredStateScanServicesKey))
            {
                var value = dict[CBCentralManager.RestoredStateScanServicesKey];
                IEnumerable<CBUUID> restoredScanCBUuids = value as IEnumerable<CBUUID>;
                if (restoredScanCBUuids != null && restoredScanCBUuids.Count() > 0)
                {
                    IList<Guid> restoredScanGuids = new List<Guid>();
                    foreach (var cbuuid in restoredScanCBUuids)
                    {
                        restoredScanGuids.Append(cbuuid.GuidFromUuid());
                    }
                    args.IsScanningToBeRestored = true;
                    args.RestoredScanServices = restoredScanGuids;
                }
            }

            _centralStateRestorediOS?.Invoke(this, args);
        }

        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
            Console.WriteLine($"DiscoveredPeripheral: {peripheral.Name}, Id: {peripheral.Identifier}");

            // create a new BTDevice object from discovered peripheral
            var advRecord = ParseAdvertismentData(advertisementData);

            var name = peripheral.Name;
            if (advertisementData.ContainsKey(CBAdvertisement.DataLocalNameKey))
            {
                name = ((NSString)advertisementData.ValueForKey(CBAdvertisement.DataLocalNameKey)).ToString();
            }
            IBTDevice btDevice = new BTDevice(name, RSSI.Int32Value, peripheral, advRecord, this);

            _discoveredPeripheral?.Invoke(this, new BTDeviceEventArgs() { Device = btDevice });
        }

        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            Console.WriteLine($"ConnectedPeripherial: {peripheral.Name}");

            // when a peripheral gets connected, add that peripheral to our running list of connected peripherals
            var guid = ParseDeviceGuid(peripheral).ToString();

            IBTDevice btDevice = new BTDevice(peripheral, this);

            _deviceConnected?.Invoke(this, new BTDeviceEventArgs { Device = btDevice });
        }

        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
            if (error != null)
            {
                Console.WriteLine($"Disconnect error {error.Code} {error.Description} {error.Domain}");
            }

            IBTDevice disconnectedDevice = new BTDevice(peripheral, this);

            // check if it is a peripheral disconnection, which would be treated as normal
            if (error != null && error.Code == 7 && error.Domain == "CBErrorDomain")
            {
                Console.WriteLine($"DisconnectedPeripheral by user: {disconnectedDevice.Name}");
                _deviceDisconnected?.Invoke(this, new BTDeviceEventArgs { Device = disconnectedDevice });
            }
            else
            {
                Console.WriteLine($"DisconnectedPeripheral by lost signal: {disconnectedDevice.Name}");
                _deviceConnectionLost?.Invoke(this, new BTDeviceErrorEventArgs { Device = disconnectedDevice, ErrorMessage = error.LocalizedDescription });
            }
        }

        #endregion

        #region Parsers
        // 1. Parse advertisement data to BTAdvertiseData
        private AdvertisementRecord ParseAdvertismentData(NSDictionary advertisementData)
        {
            var advertisementRecord = new AdvertisementRecord();

            foreach (var o in advertisementData.Keys)
            {
                var key = (NSString)o;
                if (key == CBAdvertisement.DataLocalNameKey)
                {
                    advertisementRecord.CompleteLocalName =
                        NSData.FromString(advertisementData.ObjectForKey(key) as NSString).ToArray();
                }
                else if (key == CBAdvertisement.DataManufacturerDataKey)
                {
                    var arr = ((NSData)advertisementData.ObjectForKey(key)).ToArray();
                    advertisementRecord.ManufacturerSpecificData = arr;
                }
                else if (key == CBAdvertisement.DataServiceUUIDsKey)
                {
                    var array = (NSArray)advertisementData.ObjectForKey(key);

                    var dataList = new List<NSData>();
                    for (nuint i = 0; i < array.Count; i++)
                    {
                        var cbuuid = array.GetItem<CBUUID>(i);
                        dataList.Add(cbuuid.Data);
                    }
                    advertisementRecord.UuidsComplete128Bit = dataList.SelectMany(d => d.ToArray()).ToArray();
                }
                else if (key == CBAdvertisement.DataTxPowerLevelKey)
                {
                    sbyte byteValue = Convert.ToSByte(((NSNumber)advertisementData.ObjectForKey(key)).Int32Value);
                    byte[] arr = { (byte)byteValue };
                    advertisementRecord.TxPowerLevel = arr;
                }
                else if (key == CBAdvertisement.DataServiceDataKey)
                {
                    NSDictionary serviceDict = (NSDictionary)advertisementData.ObjectForKey(key);
                    foreach (CBUUID dKey in serviceDict.Keys)
                    {
                        byte[] keyAsData = dKey.Data.ToArray();

                        Array.Reverse(keyAsData);

                        var data = (NSData)serviceDict.ObjectForKey(dKey);
                        byte[] valueAsData = data.Length > 0 ? data.ToArray() : new byte[0];

                        byte[] arr = new byte[keyAsData.Length + valueAsData.Length];
                        Buffer.BlockCopy(keyAsData, 0, arr, 0, keyAsData.Length);
                        Buffer.BlockCopy(valueAsData, 0, arr, keyAsData.Length, valueAsData.Length);

                        advertisementRecord.ServiceData = arr;
                    }
                }
                else if (key == CBAdvertisement.IsConnectable)
                {
                    advertisementRecord.IsConnectable = new byte[] { ((NSNumber)advertisementData.ObjectForKey(key)).ByteValue };
                }
                else
                {
                    Console.WriteLine($"Parsing Advertisement: Not parsed {key.ToString()}");
                }
            }

            return advertisementRecord;
        }

        // 2. Parse CBUUID to Guid
        private Guid ParseDeviceGuid(CBPeripheral peripherial)
        {
            return Guid.ParseExact(peripherial.Identifier.AsString(), "d");
        }
        #endregion

    }
}
