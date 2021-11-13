using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace CosmedBleLib
{
    public class CosmedBleConnectedDevice
    {

        #region Private fields

        private GattDeviceServicesResult gattResult;
        private BluetoothLEDevice bluetoothLeDevice;

        #endregion


        #region Device public Properties

       
        //public BluetoothLEDevice BluetoothLeDevice { get { return bluetoothLeDevice; } }

        public ulong BluetoothAddress { get; private set; }

        //private GattCommunicationStatus GattCommunicationStatus { get { return gattResult.Status; } }

        
        public string Name { get; private set; }
        
        public BluetoothLEAppearance Appearance { get; private set; }
        
        public BluetoothAddressType BluetoothAddressType { get; private set; }

        //inforations about device and pairing
        public DeviceInformation DeviceInformation { get; private set; }

        public DeviceAccessInformation DeviceAccessInformation { get; private set; }

        //device ID
        public BluetoothDeviceId BluetoothDeviceId { get; private set; }

        public bool IsConnected { get { return bluetoothLeDevice?.ConnectionStatus == BluetoothConnectionStatus.Connected; } }

        public bool WasSecureConnectionUsedForPairing { get { return bluetoothLeDevice.WasSecureConnectionUsedForPairing; } }

        public bool IsDevicePaired
        {
            get
            {
                return DevicePairingResult != null ? (DevicePairingResult.Status == DevicePairingResultStatus.AlreadyPaired ||
                       DevicePairingResult.Status == DevicePairingResultStatus.Paired) : DeviceInformation.Pairing.IsPaired;
            }
        }

        public DevicePairingResult DevicePairingResult { get; private set; }

        public IReadOnlyDictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>> ServicesDictionary 
        {
            get
            {
                if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
                {
                    IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                    var readOnlyDict = (IReadOnlyDictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>>)resultServices.ToDictionary(async (s) =>
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        return tempResult.Characteristics;
                    });

                    return readOnlyDict;
                }

                return new Dictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>>();
            }             
        }
        #endregion


        #region Session public Properties

        public GattSession GattSession { get; private set; }
        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }
        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }
        public bool IsConnectionMaintained { get { return GattSession != null ? GattSession.MaintainConnection : false; } }

        #endregion


        #region Public Events & Handlers

        //questo va usato per esempio in caso di notifiche, per controlla se la connessione è saltata
        // e in caso verificarla e/o impostarla come maintainedConnection

        //set these 3 functions to receive callbacks
        public event TypedEventHandler<CosmedBleConnectedDevice, object> ConnectionStatusChanged;
        public event TypedEventHandler<CosmedBleConnectedDevice, object> GattServicesChanged;
        public event TypedEventHandler<CosmedBleConnectedDevice, object> NameChanged;
        //called after a pairing request
        public static TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> CustomPairingRequestedHandler { get; set; } = (sender, args) =>
        {
            sender.PairingRequested -= CustomPairingRequestedHandler;
            Console.WriteLine("Test");
            //va gestito, accetta a seconda del tipo di richiesta e in caso deve gestire i dati 
            Console.WriteLine(args.PairingKind.ToString());
            if(string.IsNullOrEmpty(args.Pin))
            {
                args.Accept(args.Pin);
            }
            else
            {
                args.Accept();
            }
            
            //args.Accept(args.Pin);
            //args.GetDeferral();
            //args.AcceptWithPasswordCredential(PasswordCredential passwordCredential);
        };


        #endregion


        #region private EventHandlers

        private Action<CosmedGattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged { get; set; } = (s, a) =>
        {
            CharacteristicReader cr = new CharacteristicReader(a.CharacteristicValue, a.Timestamp, s.characteristic);
            Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
        };

        private Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound { get; set; } = (s, a) =>
        {
            Console.WriteLine("(((((((((((((((( error found, called by the hanlder in CosmedBelConnectedDevices))))))))))))");
        };

        private void OnConnectionStatusChanged(BluetoothLEDevice device, Object o)
        {
            Console.WriteLine("------------------------");
            Console.WriteLine("new device connection status: " + device.ConnectionStatus);
            Console.WriteLine("------------------------");
        }


        //these ones call the public event, to which the user can subscribe
        private void ConnectionStatusChangedHandler(BluetoothLEDevice device, object args)
        {
            ConnectionStatusChanged?.Invoke(this, args);
        }

        private void GattServicesChangedHandler(BluetoothLEDevice device, object args)
        {
            GattServicesChanged?.Invoke(this, args);
        }

        private void NameChangedHandler(BluetoothLEDevice device, object args)
        {
            NameChanged?.Invoke(this, args);
        }

        #endregion


        #region Constructors

        private CosmedBleConnectedDevice()
        {

        }


        private async Task InitializeAsync(ulong deviceAddress)
        {
            BluetoothAddress = deviceAddress;
            try
            {
                IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
                this.bluetoothLeDevice = await task.AsTask().ConfigureAwait(false);
            }
            catch(Exception e)
            {
                throw new BleDeviceNotFoundException("Impossible to connect to device", e);
            }


            if (bluetoothLeDevice == null)
            {
                throw new BleDeviceNotFoundException("Impossible to connect to device");
            }

            
            //DeviceId = bluetoothLeDevice.DeviceId;
            Name = bluetoothLeDevice.Name;
            Appearance = bluetoothLeDevice.Appearance;
            BluetoothAddressType = bluetoothLeDevice.BluetoothAddressType;
            DeviceInformation = bluetoothLeDevice.DeviceInformation;
            DeviceAccessInformation = bluetoothLeDevice.DeviceAccessInformation;
            BluetoothDeviceId = bluetoothLeDevice.BluetoothDeviceId;
            bluetoothLeDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            bluetoothLeDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
            bluetoothLeDevice.GattServicesChanged += GattServicesChangedHandler;
            bluetoothLeDevice.NameChanged += NameChangedHandler;

            try
            {
                GattSession = await GattSession.FromDeviceIdAsync(BluetoothDeviceId).AsTask().ConfigureAwait(false);
            }
            catch(Exception e)
            {
                throw new GattCommunicationFailureException("Impossible to open the Gatt Session", e);
            }
            if(GattSession == null)
            {
                throw new GattCommunicationFailureException("Impossible to open the Gatt Session");
            }
        }

        
        public static async Task<CosmedBleConnectedDevice> CreateAsync(ulong deviceAddress)
        {
            var connectedDevice = new CosmedBleConnectedDevice();
            await connectedDevice.InitializeAsync(deviceAddress);
            return connectedDevice;
        }
        
        
        public static async Task<CosmedBleConnectedDevice> CreateAsync(CosmedBleAdvertisedDevice advertisingDevice)
        {
            if(advertisingDevice == null)
            {
                throw new ArgumentNullException("parameter cannot be null");
            }

            var connectedDevice = new CosmedBleConnectedDevice();
            await connectedDevice.InitializeAsync(advertisingDevice.DeviceAddress);
            return connectedDevice;
        }


        public void MaintainConnection()
        {
            if (GattSession!=null && GattSession.CanMaintainConnection)
            {
                GattSession.MaintainConnection = true;
            }
        }


        #endregion


        #region Pairing methods

        public async Task Pair(DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel)
        {

            var accessStatus = await bluetoothLeDevice.RequestAccessAsync();
            if(accessStatus == DeviceAccessStatus.Allowed)
            {
                
            }
            if (DeviceInformation != null)// && DeviceInformation.Pairing.CanPair)
            {
                try
                { 

                    DeviceInformation.Pairing.Custom.PairingRequested += CustomPairingRequestedHandler;
                    DevicePairingResult = await DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel).AsTask();
                    //DeviceInformation.Pairing.Custom.PairingRequested -= CustomPairingRequestedHandler;

                    Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                    Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());  
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }


        public async Task Unpair()
        {
            try
            {
                DeviceUnpairingResult unpairResult = await DeviceInformation.Pairing.UnpairAsync();
            }
            catch (Exception e)
            {
                throw;
            }

        }


        #endregion


        #region Connection methods

        public void Dispose()
        {
            GattSession.MaintainConnection = false;
            bluetoothLeDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
            bluetoothLeDevice.ConnectionStatusChanged -= ConnectionStatusChangedHandler;
            bluetoothLeDevice.GattServicesChanged -= GattServicesChangedHandler;
            bluetoothLeDevice.NameChanged -= NameChangedHandler;   
            if(gattResult!=null)
                foreach(var s in gattResult.Services)
                {
                    Console.WriteLine(s.Session.SessionStatus.ToString());
                    s.Session.Dispose();
                    s.Dispose();
                    //Console.WriteLine(s.Session.SessionStatus.ToString());

                }
            bluetoothLeDevice.Dispose();
            //bluetoothLeDevice = null;
            GattSession.Dispose();

            GC.Collect();
        }

        public async Task<GattDeviceServicesResult> FindGattServiceByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
        {
            try
            {
                GattDeviceServicesResult services = await bluetoothLeDevice.GetGattServicesForUuidAsync(requestedUuid, cacheMode).AsTask();
                return services;
            }
            catch (Exception e)
            {
                throw new GattCommunicationFailureException("communication with Gatt failed", e);
            }
        }


        public async Task<GattCharacteristic> FindGattCharacteristicByUuidAsync(Guid requestedUuid)
        {
            await GetGattServicesAsync();  
            
            if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in gattResult.Services)
                {
                    try
                    {
                        GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync().AsTask().ConfigureAwait(false);
                        
                        if (resultCharacteristics.Status == GattCommunicationStatus.Success)
                        {
                            foreach (GattCharacteristic characteristic in resultCharacteristics.Characteristics)
                            {
                                if (characteristic.Uuid.Equals(requestedUuid))
                                {
                                    return characteristic;
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        throw new GattCommunicationFailureException("communication with Gatt failed", e);
                    }
                }
            }
            return null;
        }

        public async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics()
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(emptyDictionary);

            await GetGattServicesAsync();

            if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                var servicesDictionaryTemp = resultServices.ToDictionary(s => s, async (s) =>
                {
                    try
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        var temp = tempResult.Characteristics.ToList().AsEnumerable().ToList().AsReadOnly();
                        return temp;
                    }
                    catch (Exception e)
                    {
                        throw new GattCommunicationFailureException("impossible to retrieve the characteristics from Gatt service", e);
                    }

                });
                var b = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(servicesDictionaryTemp);
                servicesDictionary = b;
            }

            return servicesDictionary;
        }
       
        public async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>> DiscoverAllCosmedGattServicesAndCharacteristics()
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>(emptyDictionary);

            await GetGattServicesAsync();

            if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                var servicesDictionaryTemp = resultServices.ToDictionary(s => s, async (s) =>
                {
                    try
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        var temp = tempResult.Characteristics.ToList().AsEnumerable().Select(p =>
                        {
                            var e = new CosmedGattCharacteristic(p, CharacteristicValueChanged, CharacteristicErrorFound) ;
                            return e;
                        }
                        ).ToList().AsReadOnly();
                        return temp;
                    }
                    catch (Exception e)
                    {
                        throw new GattCommunicationFailureException("impossible to retrieve the characteristics from Gatt service", e);
                    }

                });
                var b = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>(servicesDictionaryTemp);
                servicesDictionary = b;
            }

            return servicesDictionary;
        }


        private async Task GetGattServicesAsync()
        {
            try
            {
                gattResult = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Cached).AsTask();
            }
            catch (Exception e)
            {
                throw new GattCommunicationFailureException("impossible to retrieve the services", e);
            }
        }



        #endregion


        #region Print Method
        public async Task PrintCharacteristicValues(GattCharacteristic characteristic)
        {
            Console.WriteLine("Characteristic, user description: " + characteristic.UserDescription);
            Console.WriteLine("UUID: " + characteristic.Uuid.ToString());
            Console.WriteLine("Attribute handle: " + characteristic.AttributeHandle.ToString("X2"));
            Console.WriteLine("Protection level: " + characteristic.ProtectionLevel.ToString());
            Console.WriteLine("Properties: " + characteristic.CharacteristicProperties.ToString());

            foreach (var pf in characteristic.PresentationFormats)
            {
                Console.WriteLine(" - Presentation format - ");
                Console.WriteLine("Description" + pf.Description);
                Console.WriteLine("" + pf.FormatType.ToString("X2"));
                Console.WriteLine("Unit: " + pf.Unit);
                Console.WriteLine("Exponent: " + pf.Exponent);
                Console.WriteLine("Namespace" + pf.Namespace.ToString("X2"));
                Console.WriteLine();
            }

            GattDescriptorsResult descriptors = null;
            try
            {
                //var descriptors = await characteristic.GetDescriptorsAsync().AsTask();
                descriptors = await characteristic.GetDescriptorsAsync().AsTask().ConfigureAwait(false);
            }
            catch (AggregateException ae)
            {
                ae.Handle((x) =>
                {
                    if (x is System.ObjectDisposedException)
                    {
                        //'L'oggetto è stato chiuso. (Eccezione da HRESULT: 0x80000013)'
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(ae.InnerException.Message);
                    }
                    return false; // Let anything else stop the application.
                });

            }

            Console.WriteLine(" - descriptors - ");

            foreach (var descriptor in descriptors?.Descriptors)
            {
                Console.WriteLine("protection level: " + descriptor.ProtectionLevel);
                Console.WriteLine("Uuid: " + descriptor.Uuid.ToString());
                Console.WriteLine("Attribute Handler" + descriptor.AttributeHandle.ToString("X2"));
            }

            Console.WriteLine("Status: " + descriptors?.Status.ToString());

            if (descriptors?.ProtocolError != null)
            {
                Console.WriteLine("Protocol error: " + descriptors?.ProtocolError.Value.ToString("X2"));
            }
        }
        #endregion


        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion
    }





  
}

