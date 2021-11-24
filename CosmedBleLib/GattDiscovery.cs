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

namespace CosmedBleLib
{

    public interface IGattDiscoveryService
    {
        Task<GattDeviceServicesResult> FindGattServicesByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);
        Task<GattCharacteristicsResult> FindGattCharacteristicsByUuidAsync(GattDeviceService service, Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);
        Task<GattDeviceServicesResult> GetAllGattServicesAsync(BluetoothCacheMode bluetoothCacheMode = BluetoothCacheMode.Uncached);
        Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics(BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);

        GattReliableWriteTransaction StartReliableWrite();

        void ClearServices();

        event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;
        event TypedEventHandler<GattSession, object> MaxPduSizeChanged;
        event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;

    }


    public sealed class GattDiscoveryService : IGattDiscoveryService
    {

        #region Private members
        private List<GattDeviceServicesResult> gattResults = new List<GattDeviceServicesResult>();
        #endregion


        #region Constructor


        private GattDiscoveryService()
        {

        }

        public async static Task<GattDiscoveryService> CreateAsync(CosmedBleDevice device)
        {
            GattDiscoveryService service = new GattDiscoveryService();
            await service.InitializeAsync(device);
            return service;
        }


        private async Task InitializeAsync(CosmedBleDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException("given device cannot be null");
            }
            Device = device;
            GattSession = await GattSession.FromDeviceIdAsync(device.BluetoothLeDevice.BluetoothDeviceId);

            Device.BluetoothLeDevice.GattServicesChanged += GattServicesChangedHandler;
            GattSession.SessionStatusChanged += SessionStatusChangedHandler;
            GattSession.MaxPduSizeChanged += MaxPduSizeChangedHandler;
        }
        #endregion


        #region Properties
        public CosmedBleDevice Device { get; private set; }
        public DeviceAccessStatus DeviceAccessStatus { get; private set; }
        public GattSession GattSession { get; private set; }
        public bool MaintainConnection { get { return GattSession.CanMaintainConnection; } set { GattSession.MaintainConnection = value; } }
        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }
        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }

        public event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;
        public event TypedEventHandler<GattSession, object> MaxPduSizeChanged;
        public event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;
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

        private void MaxPduSizeChangedHandler(GattSession gattSession, object obj)
        {
            MaxPduSizeChanged?.Invoke(gattSession, obj);
        }

        private void SessionStatusChangedHandler(GattSession gattSession, GattSessionStatusChangedEventArgs args)
        {
            SessionStatusChanged?.Invoke(gattSession, args);
        }
        #endregion


        #region Operations


        public async Task<GattDeviceServicesResult> FindGattServicesByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
        {
            try
            {
                GattDeviceServicesResult services = await Device.BluetoothLeDevice.GetGattServicesForUuidAsync(requestedUuid, cacheMode).AsTask();
                if (services != null)
                {
                    gattResults.Add(services);
                }
                return services;
            }
            catch (Exception e)
            {
                throw new GattCommunicationException("communication with Gatt failed", e);
            }
        }


        //se voglio invece usare CosmedGattCharacteristic allora devo creare una classe che wrappe il GattCharacteristicsResult
        public async Task<GattCharacteristicsResult> FindGattCharacteristicsByUuidAsync(GattDeviceService service, Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
        {
            // List<GattCharacteristic> tempList = new List<GattCharacteristic>();
            //  var gattResult = await Device.BluetoothLeDevice.GetGattServicesAsync(cacheMode).AsTask(); 

            try
            {
                GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsForUuidAsync(requestedUuid, cacheMode).AsTask().ConfigureAwait(false);
                return resultCharacteristics;
            }
            catch (Exception e)
            {
                throw new GattCommunicationException("communication with Gatt failed", e);
            }
        }


        /*
                public async Task<IReadOnlyList<GattCharacteristic>> FindGattCharacteristicsByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
                {
                    List<GattCharacteristic> tempList = new List<GattCharacteristic>();

                    if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var service in gattResult.Services)
                        {
                            try
                            {
                                GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsForUuidAsync(requestedUuid, cacheMode).AsTask().ConfigureAwait(false);

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
        */

        public async Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics(BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached)
        {
            var emptyDictionary = new Dictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>();
            IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>> servicesDictionary = new ReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>(emptyDictionary);

            var gattResult = await Device.BluetoothLeDevice.GetGattServicesAsync(cacheMode).AsTask();

            if (gattResult != null && gattResult.Status == GattCommunicationStatus.Success)
            {
                gattResults.Add(gattResult);
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


        //uncached here is used for development, for production cached mode should be preferred as default, because it allows lower power consumption
        public async Task<GattDeviceServicesResult> GetAllGattServicesAsync(BluetoothCacheMode bluetoothCacheMode = BluetoothCacheMode.Uncached)
        {
            var accessStatus = await Device.BluetoothLeDevice.RequestAccessAsync();
            if (accessStatus == DeviceAccessStatus.Allowed)
            {
                try
                {
                    //how to set cache mode:
                    //https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothcachemode?view=winrt-22000
                    var gattResult = await Device.BluetoothLeDevice.GetGattServicesAsync(bluetoothCacheMode).AsTask();
                    gattResults.Add(gattResult);
                    return gattResult;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message + " --- device name: " + Device.Name);
                    //throw new GattCommunicationException("impossible to retrieve the services", e);
                }
            }
            ////potrei lanciare un´eccezione se il device non é raggiungibile, invece di restituire null
            //else
            //{
            //    throw expetion ??
            //}
            return null;
        }

        //very useful
        public GattReliableWriteTransaction StartReliableWrite()
        {
            GattReliableWriteTransaction grwt = new GattReliableWriteTransaction();
            return grwt;
        }

        #endregion 


        public void ClearServices()
        {
            //Device.BluetoothLeDevice.GattServicesChanged -= GattServicesChangedHandler;
            //GattSession.SessionStatusChanged -= SessionStatusChangedHandler;
            //GattSession.MaxPduSizeChanged -= MaxPduSizeChangedHandler;

            GattSession.Dispose();
            GattSession = null;

            foreach (var gattResult in gattResults)
            {
                if (gattResult != null)
                {
                    foreach (var service in gattResult.Services)
                    {
                        if (service != null)
                        {
                            service.Dispose();
                        }
                    }
                }
            }

            gattResults = null;

            GC.Collect();
        }

    }


    
}
