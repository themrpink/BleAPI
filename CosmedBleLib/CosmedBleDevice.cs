using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Credentials;

namespace CosmedBleLib
{

    //wrap a BluetoothLEDevice, which is obtained with a unpaired connection (therefore the device is reachable, otherwise the object is null)
    public class CosmedBleDevice
    {
        internal BluetoothLEDevice bluetoothLeDevice;


        #region Public Properties
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


        #endregion


        #region Public events
        public event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> AccessChanged;
        public event TypedEventHandler<CosmedBleDevice, object> ConnectionStatusChanged;
        public event TypedEventHandler<CosmedBleDevice, object> NameChanged;
        #endregion


        #region Private Event handlers

        private void OnConnectionStatusChanged(CosmedBleDevice device, Object o)
        {
            Console.WriteLine("------------------------");
            Console.WriteLine("new device connection status: " + bluetoothLeDevice?.ConnectionStatus.ToString());
            Console.WriteLine("------------------------");
        }

        //by disposal these 3 handler shall be unsubscribed
        private void AccessChangedHanlder(DeviceAccessInformation accessInformation, DeviceAccessChangedEventArgs args)
        {
            AccessChanged?.Invoke(accessInformation, args);
        }

        //these ones call the public event, to which the user can subscribe
        private void ConnectionStatusChangedHandler(BluetoothLEDevice device, object args)
        {
            ConnectionStatusChanged?.Invoke(this, args);
        }


        private void NameChangedHandler(BluetoothLEDevice device, object args)
        {
            NameChanged?.Invoke(this, args);
        }

        #endregion


        #region Constructors

        protected CosmedBleDevice()
        {

        }


        protected async Task InitializeAsync(ulong deviceAddress)
        {
            BluetoothAddress = deviceAddress;
            try
            {
                // Verificare: BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
                this.bluetoothLeDevice = await task.AsTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device", e);
            }


            if (bluetoothLeDevice == null)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device");
            }

            //DeviceId = bluetoothLeDevice.DeviceId;
            Name = bluetoothLeDevice.Name;
            Appearance = bluetoothLeDevice.Appearance;
            BluetoothAddressType = bluetoothLeDevice.BluetoothAddressType;
            DeviceInformation = bluetoothLeDevice.DeviceInformation;
            DeviceAccessInformation = bluetoothLeDevice.DeviceAccessInformation;
            BluetoothDeviceId = bluetoothLeDevice.BluetoothDeviceId;

            bluetoothLeDevice.DeviceAccessInformation.AccessChanged += AccessChangedHanlder;
            bluetoothLeDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged += NameChangedHandler;

            //this is for test purpose, the user can implement his own method
            this.ConnectionStatusChanged += OnConnectionStatusChanged;

        }


        public static async Task<CosmedBleDevice> CreateAsync(ulong deviceAddress)
        {
            var device = new CosmedBleDevice();
            await device.InitializeAsync(deviceAddress);
            return device;
        }


        public static async Task<CosmedBleDevice> CreateAsync(CosmedBleAdvertisedDevice advertisingDevice)
        {
            if (advertisingDevice == null)
            {
                throw new ArgumentNullException("parameter cannot be null");
            }

            var device = new CosmedBleDevice();
            await device.InitializeAsync(advertisingDevice.DeviceAddress);
            return device;
        }


        #endregion


        #region Dispose device

        public void Disconnect()
        {
            ClearBluetoothLEDevice();
        }

        private void ClearBluetoothLEDevice()
        {

            // Need to clear the CCCD from the remote device so we stop receiving notifications
            List<Task<GattCharacteristic>> removals = GattDiscoveryService.CharacteristicsChangedSubscriptions.Where(c => c.Key.Service.Session.DeviceId.Equals(bluetoothLeDevice.DeviceId)).Select(async d =>
            {
                var result = await d.Key.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result == GattCommunicationStatus.Success)
                {
                    d.Key.ValueChanged -= d.Value;
                }
                return d.Key;

            }).ToList();

            TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> eventHandler;
            foreach (var r in removals)
            {
                var result = GattDiscoveryService.CharacteristicsChangedSubscriptions.TryRemove(r.Result, out eventHandler);
            }

            


            bluetoothLeDevice.ConnectionStatusChanged -= ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged -= NameChangedHandler;
            bluetoothLeDevice.DeviceAccessInformation.AccessChanged -= AccessChangedHanlder;

            bluetoothLeDevice?.Dispose();
            bluetoothLeDevice = null;

        }

        #endregion

    }


    //a paired CosmedBleDevice
    public class PairedDevice: CosmedBleDevice
    {
        public bool WasSecureConnectionUsedForPairing { get { return bluetoothLeDevice.WasSecureConnectionUsedForPairing; } }

        public bool IsDevicePaired { get { return DeviceInformation.Pairing.IsPaired; } }

        public DevicePairingProtectionLevel ProtectionLevelUsed { get; }

        public DevicePairingResultStatus PairingResultStatus { get; }

        private PairedDevice(DevicePairingResult devicePairingResult)
        {
            ProtectionLevelUsed = devicePairingResult.ProtectionLevelUsed;
            PairingResultStatus = devicePairingResult.Status;
        }
       
        public static async Task<PairedDevice>  CreateAsync(ulong deviceAddress, DevicePairingResult devicePairingResult)
        {
            var device = new PairedDevice(devicePairingResult);
            await device.InitializeAsync(deviceAddress);
            return device;
        }
    
    }

  
    //take a CosmedBleDevice and try to pair it. If pairing succeeds the return a PairedDevice
    public static class PairingService
    {
        private static void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' where there is no Windows Consent UX, you may want to provide your own confirmation.
                    Console.WriteLine("ok presse enter to accept pairing");
                    Console.ReadLine();
                    args.Accept();
                    break;

                case DevicePairingKinds.DisplayPin:
                    // We just show the PIN on this side. The ceremony is actually completed when the user enters the PIN
                    // on the target device. We automatically accept here since we can't really "cancel" the operation
                    // from this side.
                    Console.WriteLine("ok presse enter to accept pin " + args.Pin);
                    Console.ReadLine();
                    args.Accept();

                    // No need for a deferral since we don't need any decision from the user
                    Console.WriteLine("Please enter this PIN on the device you are pairing with: " + args.Pin, args.PairingKind);
                    Console.ReadLine();
                    break;

                case DevicePairingKinds.ProvidePin:
                    // A PIN may be shown on the target device and the user needs to enter the matching PIN on
                    // this Windows device. Get a deferral so we can perform the async request to the user.
                    var collectPinDeferral = args.GetDeferral();

                    Console.WriteLine("Please enter the PIN shown on the device you're pairing with");
                    string pin = Console.ReadLine();
                    if (!string.IsNullOrEmpty(pin))
                    {
                        args.Accept(pin);
                    }

                    collectPinDeferral.Complete();

                    break;

                case DevicePairingKinds.ProvidePasswordCredential:

                    var collectCredentialDeferral = args.GetDeferral();
                    Console.WriteLine("insert username");
                    string username = Console.ReadLine();
                    Console.WriteLine("insert password");
                    string password = Console.ReadLine();
                    var credential = new PasswordCredential() { UserName = username, Password = password };
                    if (credential != null)
                    {
                        args.AcceptWithPasswordCredential(credential);
                    }
                    collectCredentialDeferral.Complete();

                    break;

                case DevicePairingKinds.ConfirmPinMatch:
                    // We show the PIN here and the user responds with whether the PIN matches what they see
                    // on the target device. Response comes back and we set it on the PinComparePairingRequestedData
                    // then complete the deferral.
                    var displayMessageDeferral = args.GetDeferral();

                    Console.WriteLine("does the pin matches? Y/N");
                    string answer = Console.ReadLine();
                    while (!answer.ToLower().Equals("y") || !answer.ToLower().Equals("n"))
                    {
                        Console.WriteLine("please answer y or n");
                        answer = Console.ReadLine();
                    }
                    if (answer.ToLower().Equals("Y"))
                    {
                        args.Accept();
                    }

                    displayMessageDeferral.Complete();
                    break;
            }
            
        }

        public static async Task<PairedDevice> GetPairedDevice(CosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel)
        {
            DeviceInformation deviceInformation = device.DeviceInformation;
            try
            {
                deviceInformation.Pairing.Custom.PairingRequested += PairingRequestedHandler;
                DevicePairingResult devicePairingResult = await deviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                deviceInformation.Pairing.Custom.PairingRequested -= PairingRequestedHandler;
                
                if (devicePairingResult.Status == DevicePairingResultStatus.AlreadyPaired || devicePairingResult.Status == DevicePairingResultStatus.Paired)
                    return await PairedDevice.CreateAsync(device.BluetoothAddress, devicePairingResult);
                else
                    return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static async Task<PairedDevice> GetPairedDevice(CosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel, TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> eventHandler)
        {
            DeviceInformation deviceInformation = device.DeviceInformation;
            try
            {
                deviceInformation.Pairing.Custom.PairingRequested += eventHandler;
                DevicePairingResult devicePairingResult = await deviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                deviceInformation.Pairing.Custom.PairingRequested -= eventHandler;

                if (devicePairingResult.Status == DevicePairingResultStatus.AlreadyPaired || devicePairingResult.Status == DevicePairingResultStatus.Paired)
                    return await PairedDevice.CreateAsync(device.BluetoothAddress, devicePairingResult);
                else
                    return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async static Task<DeviceUnpairingResult> Unpair(PairedDevice device)
        {
            try
            {
                DeviceUnpairingResult unpairResult = await device.DeviceInformation.Pairing.UnpairAsync();
                return unpairResult;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }


    public class GattDiscoveryService
    {

        #region Private members
        private GattDeviceServicesResult gattResult;

        // Only one registered characteristic at a time.
        private GattCharacteristic registeredCharacteristic;
        private GattPresentationFormat presentationFormat;

        #endregion


        #region Static Member

        public static ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>> CharacteristicsChangedSubscriptions = new ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>>();

        #endregion


        #region Properties
        public CosmedBleDevice device { get; private set; }
        public DeviceAccessStatus DeviceAccessStatus { get; private set; }
        public GattSession GattSession { get; private set; }
        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }
        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }

        public event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;
        #endregion


        #region Constructors / Initialiser
        private GattDiscoveryService()
        {

        }


        public async static Task<GattDiscoveryService> CreateAsync(CosmedBleDevice device)
        {
            GattDiscoveryService dt = new GattDiscoveryService();
            DeviceAccessStatus DeviceAccessStatus = await dt.InitAsync(device);
            return dt;
        }


        private async Task<DeviceAccessStatus> InitAsync(CosmedBleDevice device)
        {
            this.device = device;
            //BluetoothLEDevice bluetoothLeDevice;

            var accessStatus = await device.bluetoothLeDevice.RequestAccessAsync();
            if (accessStatus == DeviceAccessStatus.Allowed)
            {
                gattResult = await GetGattServicesAsync();
                //try
                //{
                //    bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(device.BluetoothAddress).AsTask();
                //}
                //catch (Exception e)
                //{
                //    throw new BleDeviceConnectionException("impossible to reach the device", e);
                //}
                try
                {
                    GattSession = await GattSession.FromDeviceIdAsync(device.BluetoothDeviceId).AsTask().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new GattCommunicationException("Impossible to open the Gatt Session", e);
                }

                device.bluetoothLeDevice.GattServicesChanged += GattServicesChangedHandler;
    }

            return accessStatus;
        }


        public async Task<DeviceAccessStatus> RetryInit()
        {
            if(DeviceAccessStatus != DeviceAccessStatus.Allowed)
            {
                DeviceAccessStatus = await InitAsync(device);
            }
            return DeviceAccessStatus;
        }

        #endregion


        #region EventHandlers
        private Action<CosmedGattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged { get; set; } = (s, a) =>
        {
            CharacteristicReader cr = new CharacteristicReader(a.CharacteristicValue, a.Timestamp, s.characteristic);
            Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
        };

        private Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound { get; set; } = (s, a) =>
        {
            Console.WriteLine("(((((((((((((((( error found, called by the hanlder in CosmedBelConnectedDevices))))))))))))");
        };


        //by disposal this must be unsubscribed
        private void GattServicesChangedHandler(BluetoothLEDevice BleDevice, object arg)
        {
            GattServicesChanged?.Invoke(BleDevice, arg);
        }
        #endregion


        #region Operations


        public async Task<GattDeviceServicesResult> FindGattServicesByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
        {
            try
            {
                GattDeviceServicesResult services = await device.bluetoothLeDevice.GetGattServicesForUuidAsync(requestedUuid, cacheMode).AsTask();
                return services;
            }
            catch (Exception e)
            {
                throw new GattCommunicationException("communication with Gatt failed", e);
            }
        }


        public async Task<IReadOnlyList<GattCharacteristic>> FindGattCharacteristicsByUuidAsync(Guid requestedUuid)
        {
            List<GattCharacteristic> tempList = new List<GattCharacteristic>();

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
                                    tempList.Add(characteristic);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new GattCommunicationException("communication with Gatt failed", e);
                    }
                }
            }
            return tempList.AsReadOnly();
        }


        public async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics()
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(emptyDictionary);

            await GetGattServicesAsync(BluetoothCacheMode.Cached);

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
                        throw new GattCommunicationException("impossible to retrieve the characteristics from Gatt service", e);
                    }

                });
                var b = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(servicesDictionaryTemp);
                servicesDictionary = b;
            }

            return servicesDictionary;
        }


        public async Task<GattDeviceServicesResult> GetGattServicesAsync(BluetoothCacheMode bluetoothCacheMode = BluetoothCacheMode.Uncached)
        {
            var accessStatus = await device.bluetoothLeDevice.RequestAccessAsync();
            if (accessStatus == DeviceAccessStatus.Allowed)
            {
                try
                {
                    return await device.bluetoothLeDevice.GetGattServicesAsync(bluetoothCacheMode).AsTask();
                    //return gattResult;
                }
                catch (Exception e)
                {
                    throw new GattCommunicationException("impossible to retrieve the services", e);
                }
            }
            return null;
        }

        #endregion 
    }


}
