using System;
using System.Collections.Generic;
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

        private CosmedBluetoothLEAdvertisementWatcher watcher;
        
        private DevicePairingResult DevicePairingResult;
        private GattDeviceServicesResult gattResult;
        private BluetoothLEDevice bluetoothLeDevice = null;

        #endregion


        #region Properties

        public ulong BluetoothAddress { get; private set; }

        public GattCommunicationStatus GattCommunicationStatus { get { return gattResult.Status; } }
        public GattSession session { get; private set; }

        //removed because duplicate of BluetoothDeviceId.DeviceId
        // public string DeviceId { get; private set; }
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
                return DevicePairingResult == null ? (DevicePairingResult.Status == DevicePairingResultStatus.AlreadyPaired ||
                       DevicePairingResult.Status == DevicePairingResultStatus.Paired) : false;
            }
        }

        public ushort? MaxPduSize { get { return session?.MaxPduSize; } }

        public GattSessionStatus? SessionStatus { get { return session?.SessionStatus; } }

        public bool IsConnectionMaintained { get { return session != null ? session.MaintainConnection : false; } }

        #endregion


        #region Constructors
        public CosmedBleConnectedDevice(BluetoothLEDevice bluetoothLeDevice, CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            this.bluetoothLeDevice = bluetoothLeDevice ?? throw new ArgumentNullException(nameof(bluetoothLeDevice));
            this.watcher = watcher;
            bluetoothLeDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        public CosmedBleConnectedDevice(CosmedBleAdvertisedDevice advertisingDevice, CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            this.watcher = watcher;
            try
            {
                Task t = SetBluetoothLEDeviceAsync(advertisingDevice.DeviceAddress);
                t.Wait();
                bluetoothLeDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }            
        }

        public CosmedBleConnectedDevice(ulong deviceAddress, CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            this.watcher = watcher;
            try
            {
                Task t = SetBluetoothLEDeviceAsync(deviceAddress);
                t.Wait();
                bluetoothLeDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }
        }


        private async Task SetBluetoothLEDeviceAsync(ulong deviceAddress)
        {
            IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
            this.bluetoothLeDevice = await task.AsTask().ConfigureAwait(false);

            if (bluetoothLeDevice == null)
            {
                Console.WriteLine("no device found");
                return;
            }

            BluetoothAddress = bluetoothLeDevice.BluetoothAddress;
            //DeviceId = bluetoothLeDevice.DeviceId;
            Name = bluetoothLeDevice.Name;
            Appearance = bluetoothLeDevice.Appearance;
            BluetoothAddressType = bluetoothLeDevice.BluetoothAddressType;
            DeviceInformation = bluetoothLeDevice.DeviceInformation;
            DeviceAccessInformation = bluetoothLeDevice.DeviceAccessInformation;
            BluetoothDeviceId = bluetoothLeDevice.BluetoothDeviceId;

            session = await GattSession.FromDeviceIdAsync(BluetoothDeviceId).AsTask().ConfigureAwait(false);
            //session = Connect().Result;
        }


        public void MaintainConnection()
        {
            if (session!=null && session.CanMaintainConnection)
            {
                session.MaintainConnection = true;
            }
        }

        #endregion


        #region EventHandlers
        private void OnConnectionStatusChanged(BluetoothLEDevice device, Object o)
        {
            if (device.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                Console.WriteLine("------------------------");
                Console.WriteLine("device is pausing scan");
                Console.WriteLine("------------------------");
                watcher.PauseScanning();
            }
            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Console.WriteLine("------------------------");
                Console.WriteLine("device is resuming scan"); 
                Console.WriteLine("------------------------");
                watcher.ResumeScanning();
            }

        }
        #endregion


        #region Pairing

        public async Task Pair()
        {
            if (DeviceInformation != null)
            {
                DevicePairingResult = await DeviceInformation.Pairing.PairAsync().AsTask().ConfigureAwait(false);
                //DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                // DevicePairingResult dpr = await DeviceInformation.Pairing.PairAsync();
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());
                if(DevicePairingResult.Status != DevicePairingResultStatus.Paired || DevicePairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
                {
                    Console.WriteLine("------------------------");
                    Console.WriteLine("device is resuming scan");
                    Console.WriteLine("------------------------");
                    watcher.ResumeScanning();
                }
                else
                {
                    //in questo caso, se non è ConnectionStatusChanged ad accorgersene,
                    //stoppa lo scanning perchè c´é stato il pairing (da verificare però gli altri DevicePairingResultStatus)
                    watcher.PauseScanning();
                }
            }
        }


        public async Task Pair(DevicePairingProtectionLevel minProtectionLevel)
        {
            if (DeviceInformation != null)
            {
                DevicePairingResult = await DeviceInformation.Pairing.PairAsync(minProtectionLevel).AsTask().ConfigureAwait(false);
                //DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                // DevicePairingResult dpr = await DeviceInformation.Pairing.PairAsync();
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());
                if (DevicePairingResult.Status != DevicePairingResultStatus.Paired || DevicePairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
                {
                    Console.WriteLine("------------------------");
                    Console.WriteLine("device is resuming scan");
                    Console.WriteLine("------------------------");
                    watcher.ResumeScanning();
                }
                else
                {
                    //in questo caso, se non è ConnectionStatusChanged ad accorgersene,
                    //stoppa lo scanning perchè c´é stato il pairing (da verificare però gli altri DevicePairingResultStatus)
                    watcher.PauseScanning();
                }
            }
        }


        public async Task Pair(DevicePairingKinds pairingKindsSupported)
        {
            if (DeviceInformation != null)
            {
       
                DevicePairingResult = await DeviceInformation.Pairing.Custom.PairAsync(pairingKindsSupported).AsTask().ConfigureAwait(false);
                //DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                // DevicePairingResult dpr = await DeviceInformation.Pairing.PairAsync();
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());
                if (DevicePairingResult.Status != DevicePairingResultStatus.Paired || DevicePairingResult.Status != DevicePairingResultStatus.AlreadyPaired)
                {
                    Console.WriteLine("------------------------");
                    Console.WriteLine("device is resuming scan");
                    Console.WriteLine("------------------------");
                    watcher.ResumeScanning();
                    DeviceInformation.Pairing.Custom.PairingRequested += OnPairingRequested;

                    void OnPairingRequested(DeviceInformationCustomPairing a, DevicePairingRequestedEventArgs b)
                    {
                        Console.WriteLine("Pairing requested");
                    }
                }
                else
                {
                    //in questo caso, se non è ConnectionStatusChanged ad accorgersene,
                    //stoppa lo scanning perchè c´é stato il pairing (da verificare però gli altri DevicePairingResultStatus)
                    watcher.PauseScanning();
                }
            }
        }
        #endregion


        #region Connection methods

        public async Task<IReadOnlyDictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>>> DiscoverAllGattServicesAndCharacteristicsAsync()
        {
            IReadOnlyDictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>> dict = new Dictionary<GattDeviceService, IReadOnlyList<GattCharacteristic>>();

            await GetGattServicesAsync();

            if (gattResult.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                dict = (IReadOnlyDictionary < GattDeviceService, IReadOnlyList < GattCharacteristic >> )resultServices.ToDictionary<GattDeviceService, Task<IReadOnlyList<GattCharacteristic>>>(async(s) =>
                {
                    var tempResult =  await s.GetCharacteristicsAsync().AsTask();
                    return tempResult.Characteristics;
                });

                return dict;
            }
            
            return dict;
        }

        private async Task GetGattServicesAsync()
        {
            //vedere cosa fanno questi
            //public IAsyncOperation<DeviceAccessStatus> RequestAccessAsync();
            //public IAsyncOperation<GattDeviceServicesResult> GetGattServicesAsync(BluetoothCacheMode cacheMode);

            try
            {
                gattResult = await bluetoothLeDevice.GetGattServicesAsync().AsTask();
                ;
            }
            catch (Exception e)
            {
                //non so come si comporta in seguito. Restiuisce un´eccezione?
                await Task.FromException<GattDeviceServicesResult>(e);
            }
        }

        public async Task StartConnectionAsync()
        {
            GattDeviceServicesResult result;

            //vedere cosa fanno questi
            //public IAsyncOperation<DeviceAccessStatus> RequestAccessAsync();
            //public IAsyncOperation<GattDeviceServicesResult> GetGattServicesAsync(BluetoothCacheMode cacheMode);
            
            try
            {
                DeviceAccessStatus das = await bluetoothLeDevice.RequestAccessAsync();
                Console.WriteLine("device access status1: " + das);
                result = bluetoothLeDevice.GetGattServicesAsync().AsTask().Result;
                das = await bluetoothLeDevice.RequestAccessAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("exception: " + e.Message);
                return;
            }

            if (result.Status == GattCommunicationStatus.Success)
            {

                IReadOnlyList<GattDeviceService> resultServices = result.Services;
                Console.WriteLine("iterating the services");



                foreach (var service in resultServices)
                {
                    Console.WriteLine("printing a service:");
                    Console.WriteLine("service handle: " + service.AttributeHandle.ToString("X2"));
                    Console.WriteLine("service uuid: " + service.Uuid.ToString());
                    Console.WriteLine("service device access information (current status): " + service.DeviceAccessInformation.CurrentStatus.ToString());
                    Console.WriteLine("service Gatt Session: " + service.Session);
                    /*
                        dalla GattSession posso ottenere dati importanti come MaintainConnection, etc etc
                        public sealed class GattSession : IGattSession, IDisposable
                        {
                            public void Dispose();
                            [RemoteAsync]
                            public static IAsyncOperation<GattSession> FromDeviceIdAsync(BluetoothDeviceId deviceId);

                            public bool MaintainConnection { get; set; }
                            public bool CanMaintainConnection { get; }
                            public BluetoothDeviceId DeviceId { get; }
                            public ushort MaxPduSize { get; }
                            public GattSessionStatus SessionStatus { get; }

                            public event TypedEventHandler<GattSession, object> MaxPduSizeChanged;
                            public event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;
                        }
                     * */

                    //GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync().AsTask();
                    GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync().AsTask().ConfigureAwait(false);

                    if (resultCharacteristics.Status == GattCommunicationStatus.Success)
                    {
                        Console.WriteLine("iterating the characteristics:");
                        IReadOnlyList<GattCharacteristic> characteristics = resultCharacteristics.Characteristics;
                        int i = characteristics.Count;

                        foreach (GattCharacteristic characteristic in characteristics)
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
                                ae.Handle( (x) =>
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

                            //Task t = CharacteristicCommunication(characteristic, result);
                            var t = CharacteristicCommunication(characteristic, result).ConfigureAwait(false);
                            try
                            {
                                await t;
                            }
                            catch (AggregateException ae)
                            {
                                Console.WriteLine("Caught aggregate exception-Task.Wait behavior");
                                ae.Handle((x) =>
                                {
                                    if (x is UnauthorizedAccessException) // This we know how to handle.
                                    {
                                        Console.WriteLine("You do not have permission to access all folders in this path.");
                                        Console.WriteLine("See your network administrator or try another path.");
                                        return true;
                                    }
                                    else
                                    {
                                        Console.WriteLine(ae.InnerException.Message);
                                    }
                                    return false; // Let anything else stop the application.
                                });
                            }
                            catch (SystemException se)
                            {
                                Console.WriteLine(se.Message);
                            }

                        }
                    }
                    else if (resultCharacteristics.Status == GattCommunicationStatus.ProtocolError)
                    {
                        Console.WriteLine("protocol error");
                    }
                    else
                    {
                        Console.WriteLine("protocol status: " + GattCommunicationStatus.AccessDenied.ToString() + " or " + GattCommunicationStatus.Unreachable.ToString());
                    }
                }
            }
            else
            {
                var error = result.ProtocolError;
            }


        }

        private async Task CharacteristicCommunication(GattCharacteristic characteristic, GattDeviceServicesResult result)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            string advType = characteristic.Uuid.ToString();

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                // This characteristic supports reading from it.
                //GattReadResult value = await characteristic.ReadValueAsync().AsTask();
                GattReadResult value = await characteristic.ReadValueAsync().AsTask().ConfigureAwait(false);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    GattReadResultReader grr = new GattReadResultReader(value.Value, value.Status, value.ProtocolError);

                    Console.WriteLine("characteristic buffer hex: " + grr.HexValue);
                    Console.WriteLine(advType + " characteristic buffer UTF8: " + grr.UTF8Value);
                    Console.WriteLine(advType + " characteristic buffer ASCII: " + grr.ASCIIValue);
                    Console.WriteLine(advType + " characteristic buffer UTF16: " + grr.UTF16Value);
                }
            }

            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                // This characteristic supports writing to it.
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                writer.WriteByte(0x01);

                GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask().ConfigureAwait(false);
                //GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer());
                if (value == GattCommunicationStatus.Success)
                {
                    // Successfully wrote to device

                }

            }

            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                // This characteristic supports subscribing to notifications.
                //GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask();
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);

                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                    characteristic.ValueChanged += Characteristic_ValueChanged;
                }

                void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
                {
                    CharacteristicReader cr = new CharacteristicReader(args.CharacteristicValue, args.Timestamp);
                    // An Indicate or Notify reported that the value has changed.

                    Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
                }
            }

            if (properties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                try
                {
                    // This characteristic supports subscribing to notifications.
                    status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask().ConfigureAwait(false);
                    //GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);

                }
                catch(Exception e)
                {
                    Console.WriteLine("error catched with characteristic: " +  characteristic.Uuid.ToString());
                    Console.WriteLine(e.Message);
                }
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                    characteristic.ValueChanged += Characteristic_ValueChanged;
                }

                void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
                {
                    // An Indicate or Notify reported that the value has changed.
                    CharacteristicReader cr = new CharacteristicReader(args.CharacteristicValue, args.Timestamp);
                    Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
                }
            }

            /*
                    public enum GattCharacteristicProperties : uint
                    {
                        None = 0,
                        Broadcast = 1,
                        Read = 2,
                        WriteWithoutResponse = 4,
                        Write = 8,
                        Notify = 16,
                        Indicate = 32,
                        AuthenticatedSignedWrites = 64,
                        ExtendedProperties = 128,
                        ReliableWrites = 256,
                        WritableAuxiliaries = 512
                    }
             */
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

