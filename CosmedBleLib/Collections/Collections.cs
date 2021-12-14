
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using CosmedBleLib.Helpers;

namespace CosmedBleLib.Collections
{

    /// <summary>
    /// Collections relative to the Characteristic events
    /// </summary>
    public static class GattCharacteristicEventsCollector
    {
        /// <summary>
        /// Concurrent dictionary of Characteristics and their relative subscribed GattValueChanged events.
        /// </summary>
        public static ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>> CharacteristicsChangedSubscriptions = new ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>>();

    }


    /// <summary>
    /// Iterable collection of ManufacturerData
    /// </summary>
    public class ManufacturerDataCollection : IEnumerable<ManufacturerDataReader>
        {
        /// <summary>
        /// Gets a ReadonlyList of ManufacturerDataReader
        /// </summary>
        /// <see cref="ManufacturerDataReader"/>
        public IReadOnlyList<ManufacturerDataReader> AdvertisedManufacturerData { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="list">List of BluetoothLEManufacturerData</param>
        /// <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothlemanufacturerdata?view=winrt-22000"/>
        public ManufacturerDataCollection(IList<BluetoothLEManufacturerData> list)
        {
            List<ManufacturerDataReader> listManufacturer = new List<ManufacturerDataReader>();
            foreach (var l in list)
            {
                ManufacturerDataReader newData = new ManufacturerDataReader(l.Data, l.CompanyId);
                listManufacturer.Add(newData);
            }
            AdvertisedManufacturerData = listManufacturer.AsReadOnly();
        }

        /// <summary>
        /// Gets enumerator 
        /// </summary>
        /// <returns>Iterable enumerator</returns>
        public IEnumerator<ManufacturerDataReader> GetEnumerator()
            {
                return AdvertisedManufacturerData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }



    /// <summary>
    /// Iterable collection of DataSection
    /// </summary>
    public class DataSectionCollection : IEnumerable<DataSectionReader>
        {
        /// <value>
        /// Gets a ReadonlyList of advertised DataSections
        /// </value>
        public IReadOnlyList<DataSectionReader> AdvertisedDataSection { get; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="list">List of BluetoothLEAdvertisementDataSection
        /// <see href="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementdatasection?view=winrt-22000">BluetoothLEAdvertisementDataSection</see>
        /// </param>
        public DataSectionCollection(IList<BluetoothLEAdvertisementDataSection> list)
        {
            List<DataSectionReader> listData = new List<DataSectionReader>();
            foreach (var l in list)
            {
                DataSectionReader newData = new DataSectionReader(l.Data, l.DataType);
                listData.Add(newData);
            }
            AdvertisedDataSection = listData.AsReadOnly();
        }


        /// <summary>
        /// Gets enumerator 
        /// </summary>
        /// <returns>Iterable enumerator</returns>
        public IEnumerator<DataSectionReader> GetEnumerator()
            {
                return AdvertisedDataSection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

}
