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

    public sealed class GattDiscoveryService
    {

        #region Private members

        private GattDeviceServicesResult gattResult;

        #endregion


        #region Properties
        public CosmedBleDevice device { get; private set; }
        public DeviceAccessStatus DeviceAccessStatus { get; private set; }
        public GattSession GattSession { get; private set; }
        public ushort MaxPduSize { get { return GattSession.MaxPduSize; } }
        public GattSessionStatus SessionStatus { get { return GattSession.SessionStatus; } }

        public event TypedEventHandler<BluetoothLEDevice, object> GattServicesChanged;
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


        public void ClearServices()
        {
            if (gattResult != null)
            {
                foreach (var service in gattResult.Services)
                {
                    service.Dispose();
                }
            }
        }

    }

}
