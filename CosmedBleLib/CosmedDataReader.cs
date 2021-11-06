using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace CosmedBleLib
{

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


    public abstract class BufferReader
    {

        #region Properties
        public string HexValue { get; set; }
        public string ASCIIValue { get; set; }
        public string UTF8Value { get; set; }
        public string UTF16Value { get; set; }
        public IBuffer RawData { get; set; }
        #endregion


        #region constructor
        public BufferReader(IBuffer buffer)
        {
            RawData = buffer;
            if (buffer != null)
            {
                HexValue = convertBufferData(buffer, DataConversionType.Hex);
                UTF8Value = convertBufferData(buffer, DataConversionType.Utf8);
                ASCIIValue = convertBufferData(buffer, DataConversionType.ASCII);
                UTF16Value = convertBufferData(buffer, DataConversionType.Utf16);
            }
        }

        #endregion


        #region Methods
        protected string convertBufferData(IBuffer buffer, DataConversionType type)
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
                    result = Encoding.Unicode.GetString(data);
                    break;

                default:
                    result = "";
                    break;
            }
            return result;

        }

        #endregion


        #region Static methods

        public static string ToUTF8String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
            return reader.ReadString(buffer.Length);
        }

        public static string ToUTF16String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

            // UTF16 characters are 2 bytes long and ReadString takes the character count,
            // divide the buffer length by 2.
            return reader.ReadString(buffer.Length / 2);
        }

        public static Int16 ToInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToInt16(data, 0);
        }

        public static int ToInt32(IBuffer buffer)
        {
            if (buffer.Length > sizeof(Int32))
            {
                throw new ArgumentException("Cannot convert to Int32, buffer is too large");
            }

            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public static Int64 ToInt64(IBuffer buffer)
        {
            if (buffer.Length > sizeof(Int64))
            {
                throw new ArgumentException("Cannot convert to Int64, buffer is too large");
            }

            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToInt32(data, 0);
        }

        public static Single ToSingle(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToSingle(data, 0);
        }

        public static Double ToDouble(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToDouble(data, 0);
        }

        public static UInt16 ToUInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public static UInt32 ToUInt32(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public static UInt64 ToUInt64(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToUInt64(data, 0);
        }

        public static byte[] ToByteArray(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            return data;
        }

        public static string ToHexString(IBuffer buffer)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            return BitConverter.ToString(data);
        }


        /// <summary>
        /// Takes an input array of bytes and returns an array with more zeros in the front
        /// </summary>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <returns>A byte array with more zeros in back per little endianness"/></returns>
        private static byte[] PadBytes(byte[] input, int length)
        {
            if (input.Length >= length)
            {
                return input;
            }

            byte[] ret = new byte[length];
            Array.Copy(input, ret, input.Length);
            return ret;
        }


        #endregion
    }


    public class ManufacturerDataReader : BufferReader
    {
        public string CompanyId { get; private set; }
        public string CompanyIdHex { get { return string.Format("X", CompanyId); } }

        public ManufacturerDataReader(IBuffer buffer, ushort CompanyId) : base(buffer)
        {
            this.CompanyId = CompanyId.ToString("X");
        }
    }


    public class GattReadResultReader : BufferReader
    {
        private byte? protocolError;
        public CosmedGattCommunicationStatus Status { get; }


        private string ProtocolError { get { return string.Format("X2", protocolError); } }

        public GattReadResultReader(IBuffer buffer, CosmedGattCommunicationStatus status, byte? protocolError) : base(buffer)
        {
            Status = status;
            this.protocolError = protocolError;
        }

    }


    public class CharacteristicReader : BufferReader
    {
        public DateTimeOffset Timestamp { get; }

        public CharacteristicReader(IBuffer buffer, DateTimeOffset timestamp) : base(buffer)
        {
            timestamp = Timestamp;
        }
    }


    public class DataSectionReader : BufferReader
    {
        public byte RawDataType { get; }
        public string DataType { get; }

        public DataSectionReader(IBuffer buffer,  byte DataType) : base(buffer)
        {
            this.RawDataType = DataType;
            this.DataType = DataType.ToString("X");
        }

    }


    public enum DataConversionType
    {
        Hex,
        ASCII,
        Utf8,
        Utf16
    }


    /*
    public static class GattConvert
    {
        public static IBuffer ToIBufferFromHexString(string data)
        {
            DataWriter writer = new DataWriter();
            data = data.Replace("-", "");

            if (data.Length > 0)
            {
                if (data.Length % 2 != 0)
                {
                    data = "0" + data;
                }

                int NumberChars = data.Length;
                byte[] bytes = new byte[NumberChars / 2];

                for (int i = 0; i < NumberChars; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(data.Substring(i, 2), 16);
                }
                writer.WriteBytes(bytes);
            }
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(bool data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBoolean(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(byte data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(byte[] data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(double data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteDouble(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(Int16 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt16(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(Int32 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt32(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(Int64 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt64(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(Single data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteSingle(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(UInt16 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt16(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(UInt32 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt32(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(UInt64 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt64(data);
            return writer.DetachBuffer();
        }

        public static IBuffer ToIBuffer(string data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            return writer.DetachBuffer();
        }

        public static string ToUTF8String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
            return reader.ReadString(buffer.Length);
        }

        public static string ToUTF16String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

            // UTF16 characters are 2 bytes long and ReadString takes the character count,
            // divide the buffer length by 2.
            return reader.ReadString(buffer.Length / 2);
        }

        public static Int16 ToInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToInt16(data, 0);
        }

        public static int ToInt32(IBuffer buffer)
        {
            if (buffer.Length > sizeof(Int32))
            {
                throw new ArgumentException("Cannot convert to Int32, buffer is too large");
            }

            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public static Int64 ToInt64(IBuffer buffer)
        { 
            if (buffer.Length > sizeof(Int64))
            {
                throw new ArgumentException("Cannot convert to Int64, buffer is too large");
            }

            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToInt32(data, 0);
        }

        public static Single ToSingle(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToSingle(data, 0);
        }

        public static Double ToDouble(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToDouble(data, 0);
        }

        public static UInt16 ToUInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public static UInt32 ToUInt32(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public static UInt64 ToUInt64(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToUInt64(data, 0);
        }

        public static byte[] ToByteArray(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            return data;
        }

        public static string ToHexString(IBuffer buffer)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            return BitConverter.ToString(data);
        }

        /// <summary>
        /// Takes an input array of bytes and returns an array with more zeros in the front
        /// </summary>
        /// <param name="input"></param>
        /// <param name="length"></param>
        /// <returns>A byte array with more zeros in back per little endianness"/></returns>
        private static byte[] PadBytes(byte[] input, int length)
        {
            if (input.Length >= length)
            {
                return input;
            }

            byte[] ret = new byte[length];
            Array.Copy(input, ret, input.Length);
            return ret;
        }
    }
    */
}
