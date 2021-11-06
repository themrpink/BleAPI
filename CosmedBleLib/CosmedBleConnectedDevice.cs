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


        private DevicePairingResult DevicePairingResult;
        private GattDeviceServicesResult gattResult;
        private BluetoothLEDevice bluetoothLeDevice;

        #endregion


        #region Properties

        public BluetoothLEDevice BluetoothLeDevice { get { return bluetoothLeDevice; } }

        public ulong BluetoothAddress { get; private set; }

        //private GattCommunicationStatus GattCommunicationStatus { get { return gattResult.Status; } }

        public GattSession GattSession { get; private set; }

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

        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }

        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }

        public bool IsConnectionMaintained { get { return GattSession != null ? GattSession.MaintainConnection : false; } }

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

        public Action<GattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged { get { return characteristicValueChanged; } }
        
        public Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound { get { return characteristicErrorFound; } }


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


        //private async Task<bool> CheckBleDevice()
        //{
        //    if(bluetoothLeDevice == null)
        //    {
        //        await InitializeAsync(BluetoothAddress);
        //    }
        //    if(bluetoothLeDevice == null)
        //    {
        //        return false;
        //    }
        //    else
        //    {
        //        return true;
        //    }
        //}
        #endregion


        #region EventHandlers

        private void OnConnectionStatusChanged(BluetoothLEDevice device, Object o)
        {               
            Console.WriteLine("------------------------");               
            Console.WriteLine("new device connection status: " + device.ConnectionStatus);              
            Console.WriteLine("------------------------");
        }

        private void characteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            CharacteristicReader cr = new CharacteristicReader(args.CharacteristicValue, args.Timestamp);
            Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
        }

        private void characteristicErrorFound(CosmedGattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        {
            Console.WriteLine("(((((((((((((((( error found ))))))))))))");
        }
        #endregion


        #region Pairing

        public async Task Pair()
        {
            if (DeviceInformation != null)
            {
                try
                {
                    DevicePairingResult = await DeviceInformation.Pairing.PairAsync().AsTask().ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    throw;
                }
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());

            }
        }


        public async Task Pair(DevicePairingProtectionLevel minProtectionLevel)
        {
            if (DeviceInformation != null)
            {
                try
                {
                    DevicePairingResult = await DeviceInformation.Pairing.PairAsync(minProtectionLevel).AsTask().ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    throw;
                }
                //DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());
            }
        }


        public async Task Pair(DevicePairingKinds pairingKindsSupported)
        {
            if (DeviceInformation != null)
            {
                try
                {
                    DevicePairingResult = await DeviceInformation.Pairing.Custom.PairAsync(pairingKindsSupported).AsTask().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw;
                }
                //DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                Console.WriteLine("paring status: " + DevicePairingResult.Status.ToString());
                Console.WriteLine("pairing protection level: " + DevicePairingResult.ProtectionLevelUsed.ToString());
            }
        }
        #endregion


        #region Connection methods


        public async Task<GattDeviceServicesResult> FindGattServiceAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
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


        public async Task<GattCharacteristic> FindGattCharacteristicAsync(Guid requestedUuid)
        {
            try
            {
                await GetGattServicesAsync();
            }
            catch(Exception e)
            {
                return null;
            }   
            
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
                return null;
            }
            
            GattCharacteristic nullGattCharacteristic = null;
            return nullGattCharacteristic;
            //return await Task.FromResult<GattCharacteristic>(nullGattCharacteristic);
        }


        public async Task<ReadOnlyDictionary<GattDeviceService, Task<IReadOnlyList<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics()
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<IReadOnlyList<GattCharacteristic>>>();
            var servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<IReadOnlyList<GattCharacteristic>>>(emptyDictionary);
            
            try
            {
                await GetGattServicesAsync();
            }
            finally
            {
                if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
                {
                    IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                    var servicesDictionaryTemp = resultServices.ToDictionary(s => s, async (s) =>
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        return tempResult.Characteristics;
                    });

                    servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<IReadOnlyList<GattCharacteristic>>>(servicesDictionaryTemp);
                }               
            }
            return servicesDictionary;
        }


        public async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>> DiscoverAllCosmedGattServicesAndCharacteristics()
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>(emptyDictionary);

            try
            {
                await GetGattServicesAsync();
            }
            finally
            {
                if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
                {
                    IReadOnlyList<GattDeviceService> resultServices = gattResult.Services;

                    var servicesDictionaryTemp = resultServices.ToDictionary(s => s, async (s) =>
                    {
                        var tempResult = await s.GetCharacteristicsAsync().AsTask();
                        var temp = tempResult.Characteristics.ToList().AsEnumerable().Select(p => 
                        {
                            var e = new CosmedGattCharacteristic(p, characteristicErrorFound);
                            return e;
                            }
                        ).ToList().AsReadOnly();
                        return temp;
                    });
                    var b = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<CosmedGattCharacteristic>>>(servicesDictionaryTemp);
                    servicesDictionary = b;
                }
            }
            return servicesDictionary;
        }


        private async Task GetGattServicesAsync()
        {
            try
            {
                gattResult = await bluetoothLeDevice.GetGattServicesAsync().AsTask();
            }
            catch (Exception e)
            {
                throw new GattCommunicationFailureException("impossible to retrieve the services", e);
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
                    result = await bluetoothLeDevice.GetGattServicesAsync().AsTask();
                    das = await bluetoothLeDevice.RequestAccessAsync();

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
                                            return false; 
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
                catch (Exception e)
                {
                    Console.WriteLine("exception: " + e.Message);
                    //non so come si comporta in seguito. Restiuisce un´eccezione?
                    await Task.FromException(e);
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
                    CosmedGattCommunicationStatus newStatus = GattCharacteristicExtensions.ConvertStatus(value.Status);
                    GattReadResultReader grr = new GattReadResultReader(value.Value, newStatus, value.ProtocolError);

                    Console.WriteLine("characteristic buffer hex: " + grr.HexValue);
                    Console.WriteLine(advType + " characteristic buffer UTF8: " + grr.UTF8Value);
                    Console.WriteLine(advType + " characteristic buffer ASCII: " + grr.ASCIIValue);
                    Console.WriteLine(advType + " characteristic buffer UTF16: " + grr.UTF16Value);
                }
            }
            GattReadResultReader grr2 = new GattReadResultReader(null, CosmedGattCommunicationStatus.Success, null);
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
                    status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask().ConfigureAwait(false);
                    //GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                    Console.WriteLine("indicate status: " + status.ToString() + ">>>>>>>>>>>>>>>>>>>>>>>>>>");
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

