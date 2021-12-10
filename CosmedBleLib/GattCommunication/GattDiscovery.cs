using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using CosmedBleLib.CustomExceptions;
using CosmedBleLib.Helpers;
using CosmedBleLib.ConnectionServices;

namespace CosmedBleLib.GattCommunication
{

    /// <summary>
    /// Offers the basic services to communicate with the Gatt
    /// </summary>
    public interface IGattDiscoveryService
    {
        /// <summary>
        /// Finds the Services by the desired Uuid.
        /// </summary>
        /// <param name="requestedUuid">The requested Uuid.</param>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The search result</returns>
        Task<GattDeviceServicesResult> FindGattServicesByUuidAsync(Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);

        /// <summary>
        /// Finds from a specific Service the Characteristics with the desired Uuid.
        /// </summary>
        /// <param name="service">The service from which extract the characteristics</param>
        /// <param name="requestedUuid">The requested Uuid.</param>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested characteristic. If not available in the cache 
        /// it tries to retrieve the characteristics from the remote device.</param>
        /// <returns>The search result</returns>
        Task<GattCharacteristicsResult> FindGattCharacteristicsByUuidAsync(GattDeviceService service, Guid requestedUuid, BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);

        /// <summary>
        /// Gets all the Characteristics from the remote device.
        /// </summary>
        /// <param name="bluetoothCacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The search result</returns>
        Task<GattDeviceServicesResult> GetAllGattServicesAsync(BluetoothCacheMode bluetoothCacheMode = BluetoothCacheMode.Uncached);

        /// <summary>
        /// Finds all the Services and Characteristics on the remote device and save them to offline management.
        /// </summary>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The dictionary containing all the found characteristics for every found service</returns>
        Task<IReadOnlyDictionary<GattDeviceService, Task<ReadOnlyCollection<GattCharacteristic>>>> DiscoverAllGattServicesAndCharacteristics(BluetoothCacheMode cacheMode = BluetoothCacheMode.Uncached);


        /// <summary>
        /// Starts a reliable write transaction. The result is a GattReliableWriteTransaction where writes can be queued
        /// and later processed all together.
        /// </summary>
        /// <returns>The GattReliableWriteTransaction</returns>
        GattReliableWriteTransaction StartReliableWriteTransaction();

        /// <summary>
        /// Clear the found services
        /// </summary>
        void ClearServices();

        /// <summary>
        /// Fired when gatt services are changed
        /// </summary>
        event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;

        /// <summary>
        /// Fired when Max PDU size changes
        /// </summary>
        event TypedEventHandler<GattSession, object> MaxPduSizeChanged;

        /// <summary>
        /// Fired when the session status changes
        /// </summary>
        event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;


        /// <value>
        /// Gets the remote Ble Device object of Gatt communication
        /// </value>
        ICosmedBleDevice Device { get; }

        /// <value>
        /// Gets the device access status
        /// </value>
        DeviceAccessStatus DeviceAccessStatus { get; }

        /// <value>
        /// Gets the Gatt session
        /// </value>
        GattSession GattSession { get; }

        /// <value>
        /// Gets a boolean indicating if the Gatt session can maintain connection
        /// </value>
        bool CanMaintainConnection { get; }

        /// <value>
        /// Sets and gets the option to maintain connection
        /// </value>
        bool MaintainConnection { get; set; }

        /// <value>
        /// Gets the value of the Max Pdu supported size
        /// </value>
        ushort MaxPduSize { get; }

        /// <value>
        /// Gets the session status
        /// </value>
        GattSessionStatus SessionStatus { get; }

    }

    /// <summary>
    /// This class presents the methods to communicate with the Gatt
    /// </summary>
    public sealed class GattDiscoveryService : IGattDiscoveryService
    {

        #region Private members
        private List<GattDeviceServicesResult> gattResults = new List<GattDeviceServicesResult>();
        #endregion


        #region Constructor


        private GattDiscoveryService()
        {

        }


        /// <summary>
        /// Create an instance of the class.
        /// </summary>
        /// <param name="device">The destination device for the gatt communication </param>
        /// <returns>An instance of the class</returns>
        public async static Task<GattDiscoveryService> CreateAsync(ICosmedBleDevice device)
        {
            GattDiscoveryService service = new GattDiscoveryService();
            await service.InitializeAsync(device);
            return service;
        }


        private async Task InitializeAsync(ICosmedBleDevice device)
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

        /// <value>
        /// Gets the remote Ble Device object of Gatt communication
        /// </value>
        public ICosmedBleDevice Device { get; private set; }

        /// <value>
        /// Gets the device access status
        /// </value>
        public DeviceAccessStatus DeviceAccessStatus { get; private set; }

        /// <value>
        /// Gets the Gatt session
        /// </value>
        public GattSession GattSession { get; private set; }

        /// <value>
        /// Gets a boolean indicating if the Gatt session can maintain connection
        /// </value>
        public bool CanMaintainConnection { get { return GattSession.CanMaintainConnection; } set { GattSession.MaintainConnection = value; } }

        /// <value>
        /// Sets and gets the option to maintain connection
        /// </value>
        public bool MaintainConnection { get { return GattSession.MaintainConnection; } set { GattSession.MaintainConnection = value; } }

        /// <value>
        /// Gets the value of the Max Pdu supported size
        /// </value>
        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }

        /// <value>
        /// Gets the session status
        /// </value>
        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }

        /// <summary>
        /// Fired if the Gatt services change
        /// </summary>
        public event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;

        /// <summary>
        /// Fired if the Max Pdu size changes
        /// </summary>
        public event TypedEventHandler<GattSession, object> MaxPduSizeChanged;

        /// <summary>
        /// Fired if the session status changes
        /// </summary>
        public event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;
        #endregion


        #region EventHandlers

        //private Action<CosmedGattCharacteristic, GattValueChangedEventArgs> CharacteristicValueChanged { get; set; } = (s, a) =>
        //{
        //    CharacteristicReader cr = new CharacteristicReader(a.CharacteristicValue, a.Timestamp, s.characteristic);
        //    Console.WriteLine("characteristic buffer hex: " + cr.HexValue);
        //};

        //private Action<CosmedGattCharacteristic, CosmedGattErrorFoundEventArgs> CharacteristicErrorFound { get; set; } = (s, a) =>
        //{
        //    Console.WriteLine("(((((((((((((((( error found, called by the hanlder in CosmedBelConnectedDevices))))))))))))");
        //};


        //by disposal these must be unsubscribed
        //they invoke the underlying events not accessible to the user
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

        /// <summary>
        /// Finds the Services by the desired Uuid.
        /// </summary>
        /// <param name="requestedUuid">The requested Uuid.</param>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The search result</returns>
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


        /// <summary>
        /// Finds from a specific Service the Characteristics with the desired Uuid.
        /// </summary>
        /// <param name="service">The service from which extract the characteristics</param>
        /// <param name="requestedUuid">The requested Uuid.</param>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested characteristic. If not available in the cache 
        /// it tries to retrieve the characteristics from the remote device.</param>
        /// <returns>The search result</returns>
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

        /// <summary>
        /// Finds all the Services and Characteristics on the remote device and save them to offline management.
        /// </summary>
        /// <param name="cacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The dictionary containing all the found characteristics for every found service</returns>
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


        /// <summary>
        /// Gets all the Characteristics from the remote device.
        /// </summary>
        /// <param name="bluetoothCacheMode">The cache mode. Default value is "uncached". If "cached mode" is requested,
        /// the system attemps first to find the cached values for the requested service. If not available in the cache 
        /// it tries to retrieve the services from the remote device.</param>
        /// <returns>The search result</returns>        
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
                    Console.WriteLine("ERROR: " + e.Message + " --- device name: " + Device.BluetoothLeDevice.Name);
                    //throw new GattCommunicationException("impossible to retrieve the services", e);
                }
            }
            return null;
        }


        /// <summary>
        /// Starts a reliable write transaction. The result is a GattReliableWriteTransaction where writes can be queued
        /// and later processed all together.
        /// <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.genericattributeprofile.gattreliablewritetransaction?view=winrt-22000"/>
        /// </summary>
        /// <returns>The GattReliableWriteTransaction</returns>
        public GattReliableWriteTransaction StartReliableWriteTransaction()
        {
            GattReliableWriteTransaction grwt = new GattReliableWriteTransaction();
            return grwt;
        }

        #endregion


        /// <summary>
        /// Clear the found services
        /// </summary>
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
