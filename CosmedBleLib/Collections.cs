using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace CosmedBleLib
{

    //valutare se farlo statico oppure come le altre collection
    public static class GattCharacteristicEventsCollector
    {
        public static ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>> CharacteristicsChangedSubscriptions = new ConcurrentDictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>>();

    }


    public class ManufacturerDataCollection : IEnumerable<ManufacturerDataReader>
        {
            public IReadOnlyList<ManufacturerDataReader> AdvertisedManufacturerData { get; }


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


            public IEnumerator<ManufacturerDataReader> GetEnumerator()
            {
                return AdvertisedManufacturerData.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }


        
    public class DataSectionCollection : IEnumerable<DataSectionReader>
        {
            public IReadOnlyList<DataSectionReader> AdvertisedDataSection { get; }


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


            public IEnumerator<DataSectionReader> GetEnumerator()
            {
                return AdvertisedDataSection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }


    /*
public class AdvertisemendDataCollection<T, R> where T : AdvertisementData
{
    public IReadOnlyList<T> AdvertisedDataSection { get; }

    public AdvertisementDataCollection(IList<R> list)
    {
        List<T> listData = new List<T>();

        foreach (var l in list)
        {

           // Type d1 = typeof(List<>);
            Type typeArg = typeof(T);
            //Type constructed = d1.MakeGenericType(typeArg);
            object o = Activator.CreateInstance(typeArg);
            listData.Add((T)o);
        }

        AdvertisedDataSection = listData.AsReadOnly();
    }
}
*/



}
