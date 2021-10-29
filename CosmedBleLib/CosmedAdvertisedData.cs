using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace CosmedBleLib
{

    public class ManufacturerDataCollection : IEnumerable<AdvertisementManufacturerData>
    {
        public IReadOnlyList<AdvertisementManufacturerData> AdvertisedManufacturerData { get; }

        public ManufacturerDataCollection(IList<BluetoothLEManufacturerData> list)
        {
            List<AdvertisementManufacturerData> listManufacturer = new List<AdvertisementManufacturerData>();

            foreach (var l in list)
            {
                AdvertisementManufacturerData newData = new AdvertisementManufacturerData(l);
                listManufacturer.Add(newData);
            }

            AdvertisedManufacturerData = listManufacturer.AsReadOnly();
        }

        public IEnumerator<AdvertisementManufacturerData> GetEnumerator()
        {
            return AdvertisedManufacturerData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }




    public class DataSectionCollection : IEnumerable<AdvertisementDataSection>
    {
        public IReadOnlyList<AdvertisementDataSection> AdvertisedDataSection { get; }

        public DataSectionCollection(IList<BluetoothLEAdvertisementDataSection> list)
        {
            List<AdvertisementDataSection> listData = new List<AdvertisementDataSection>();

            foreach (var l in list)
            {
                AdvertisementDataSection newData = new AdvertisementDataSection(l);
                listData.Add(newData);
            }

            AdvertisedDataSection = listData.AsReadOnly();
        }

        public IEnumerator<AdvertisementDataSection> GetEnumerator()
        {
            return AdvertisedDataSection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)AdvertisedDataSection).GetEnumerator();
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




    public abstract class AdvertisementData
    {

        public string HexData { get; set; }
        public string ASCIIData { get; set; }
        public string UTF8Data { get; set; }
        public string UTF16Data { get; set; }
        public IBuffer RawData { get; set; }

        internal string convertBufferData(IBuffer buffer, DataConversionType type)
        {
            var data = new byte[buffer.Length];
            using (var reader = DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(data);
            }

            string result;

            switch (type)
            {
                case DataConversionType.Hex:
                    result = BitConverter.ToString(data);
                    break;

                case DataConversionType.ASCII:
                    result = Encoding.ASCII.GetString(data);
                    break;

                case DataConversionType.Utf8:
                    result = Encoding.UTF8.GetString(data);
                    break;

                case DataConversionType.Utf16:
                    result =  new System.Text.UnicodeEncoding().GetString(data);
                    break;

                default:
                    result = "";
                    break;
            }
            return result;
        }

    }


    public class AdvertisementManufacturerData : AdvertisementData
    {
        public string CompanyId { get; private set; }
        public string CompanyIdHex { get { return string.Format("X", CompanyId); } }

        public AdvertisementManufacturerData(BluetoothLEManufacturerData data)
        {
            this.CompanyId = data.CompanyId.ToString("X");
            this.HexData = convertBufferData(data.Data, DataConversionType.Hex);
            this.UTF8Data = convertBufferData(data.Data, DataConversionType.Utf8);
            this.ASCIIData = convertBufferData(data.Data, DataConversionType.ASCII);
            this.UTF16Data = convertBufferData(data.Data, DataConversionType.Utf16);
            this.RawData = data.Data;
        }
    }



    public class AdvertisementDataSection : AdvertisementData
    {
        public byte RawDataType { get; }
        public string DataType { get; }

        public AdvertisementDataSection(BluetoothLEAdvertisementDataSection data)
        {
            RawDataType = data.DataType;
            DataType = data.DataType.ToString("X");
            HexData = convertBufferData(data.Data, DataConversionType.Hex);
            UTF8Data = convertBufferData(data.Data, DataConversionType.Utf8);
            ASCIIData = convertBufferData(data.Data, DataConversionType.ASCII);
            UTF16Data = convertBufferData(data.Data, DataConversionType.Utf16);
            RawData = data.Data;
        }

    }

    public enum DataConversionType
    {
        Hex,
        ASCII,
        Utf8,
        Utf16
    }
}
