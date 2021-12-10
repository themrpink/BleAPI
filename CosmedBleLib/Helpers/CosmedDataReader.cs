using System;
using System.Text;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using CosmedBleLib.Extensions;

namespace CosmedBleLib.Helpers
{

    /// <summary>
    /// Types of string data
    /// </summary>
    public  enum DataConversionType
    {
        Hex,
        ASCII,
        Utf8,
        Utf16,
    }

    /// <summary>
    /// The base to build buffer readers specific to a data format
    /// </summary>
    public abstract class BufferReader
    {
        #region Properties

        /// <value>
        /// Gets and sets the data converted in hex format string
        /// </value>
        public string HexValue { get; set; }

        /// <value>
        /// Gets and sets the data converted in utf8 string
        /// </value>
        public string UTF8Value { get; set; }

        /// <value>
        /// Gets and sets the raw data not converted
        /// </value>
        public IBuffer RawData { get; set; }
        #endregion


        #region constructor


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The buffer to be read</param>
        public BufferReader(IBuffer buffer)
        {
            RawData = buffer;
            if (buffer != null)
            {
                HexValue = convertBufferData(buffer, DataConversionType.Hex);
                UTF8Value = convertBufferData(buffer, DataConversionType.Utf8);
            }
        }

        #endregion


        #region Methods

        /// <summary>
        /// Converts the given buffer into the requestes data type.
        /// </summary>
        /// <param name="buffer">The buffer to be read.</param>
        /// <param name="type">The data convertion type.</param>
        /// <returns>the result string.</returns>
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


    }


    /// <summary>
    /// Data Buffer Reader for the ManufacturerData format
    /// </summary>
    public class ManufacturerDataReader : BufferReader
    {
        /// <value>
        /// Gets the company ID value
        /// </value>
        public ushort CompanyId { get; private set; }

        /// <value>
        /// Gets the Company ID in HEX format
        /// </value>
        public string CompanyIdHex { get { return string.Format("{0:X}", CompanyId); } }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer to be read</param>
        /// <param name="CompanyId">Company ID</param>
        public ManufacturerDataReader(IBuffer buffer, ushort CompanyId) : base(buffer)
        {
            this.CompanyId = CompanyId;
        }
    }


    /// <summary>
    /// Data Buffer Reader for the Characteristic format
    /// </summary>
    public class CharacteristicReader : BufferReader
    {
        /// <value>
        /// Gets the involved Characteristic
        /// </value>
        public GattCharacteristic Characteristic { get; }

        /// <summary>
        /// Gets the timestamp
        /// </summary>
        public DateTimeOffset Timestamp { get; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">The buffer to be read</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="sender">The characteristic to whom the buffer belongs</param>
        public CharacteristicReader(IBuffer buffer, DateTimeOffset timestamp, GattCharacteristic sender) : base(buffer)
        {
            Timestamp = timestamp;
            Characteristic = sender;
        }
    }


    /// <summary>
    /// Data Buffer Reader for the Data Section format
    /// </summary>
    public class DataSectionReader : BufferReader
    {
        /// <value>
        /// Gets the raw read data
        /// </value>
        public byte RawDataType { get; }

        /// <value>
        /// Gets the converted data type
        /// </value>
        public string DataType { get; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer to be read.</param>
        /// <param name="dataType">The data type</param>
        public DataSectionReader(IBuffer buffer,  byte dataType) : base(buffer)
        {
            this.RawDataType = dataType;
            this.DataType = string.Format("{0:X}", dataType);
        }

    }



    /// <summary>
    /// Helper methods to write a buffer given various data types.
    /// </summary>
    public static class BufferWriter
    {
        /// <summary>
        /// Converts byte value into Buffer
        /// </summary>
        /// <param name="byteValue">Byte value</param>
        /// <returns>Data writer buffer</returns>
        public static IBuffer ConvertValueToBuffer(byte byteValue)
        {
            DataWriter writer = new DataWriter();
            writer.WriteByte(byteValue);
            return writer.DetachBuffer();
        }

        /// <summary>
        /// Converts two byte values into buffer
        /// </summary>
        /// <param name="byteValue1">Byte value 1</param>
        /// <param name="byteValue2">Byte value 2</param>
        /// <returns>Data writer buffer</returns>
        public static IBuffer ConvertValueToBuffer(byte byteValue1, byte byteValue2)
        {
            DataWriter writer = new DataWriter();
            writer.WriteByte(byteValue1);
            writer.WriteByte(byteValue2);

            return writer.DetachBuffer();
        }

        /// <summary>
        /// Converts date time value to buffer
        /// </summary>
        /// <param name="time">DateTime value</param>
        /// <returns>Data Writer Buffer</returns>
        public static IBuffer ConvertValueToBuffer(DateTime time)
        {
            DataWriter writer = new DataWriter();

            // Date time according to: https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.date_time.xml
            writer.WriteUInt16((ushort)time.Year);
            writer.WriteByte((byte)time.Month);
            writer.WriteByte((byte)time.Day);
            writer.WriteByte((byte)time.Hour);
            writer.WriteByte((byte)time.Minute);
            writer.WriteByte((byte)time.Second);

            // Day of week according to: https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.day_of_week.xml
            // Going to leave this "not known" for now - would have to perform a rotate of DayOfWeek property
            writer.WriteByte(0x0);

            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
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


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(bool data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBoolean(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(byte data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte(data);
            return writer.DetachBuffer();
        }



        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(byte[] data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteBytes(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(double data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteDouble(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(Int16 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt16(data);
            return writer.DetachBuffer();
        }



        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(Int32 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt32(data);
            return writer.DetachBuffer();
        }

        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(Int64 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteInt64(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(Single data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteSingle(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(UInt16 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt16(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(UInt32 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt32(data);
            return writer.DetachBuffer();
        }


        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(UInt64 data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteUInt64(data);
            return writer.DetachBuffer();
        }



        /// <summary>
        /// Writes data into buffer
        /// </summary>
        /// <param name="data">Data to be written</param>
        /// <returns>The result buffer</returns>
        public static IBuffer ToIBuffer(string data)
        {
            DataWriter writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteString(data);
            return writer.DetachBuffer();
        }



    }



    /// <summary>
    /// Help methods to convert buffer data obtained by the client into various data types.
    /// </summary>
    public static class ClientBufferReader
    {
        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>String result.</returns>
        public static string ToUTF8String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
            return reader.ReadString(buffer.Length);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>String result.</returns>
        public static string ToUTF16String(IBuffer buffer)
        {
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

            // UTF16 characters are 2 bytes long and ReadString takes the character count,
            // divide the buffer length by 2.
            return reader.ReadString(buffer.Length / 2);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Int16 result.</returns>
        public static Int16 ToInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToInt16(data, 0);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>int result.</returns>
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


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Int result.</returns>
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


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static Single ToSingle(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToSingle(data, 0);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static Double ToDouble(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToDouble(data, 0);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static UInt16 ToUInt16(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 2);
            return BitConverter.ToUInt16(data, 0);
        }



        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static UInt32 ToUInt32(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 4);
            return BitConverter.ToUInt32(data, 0);
        }


        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static UInt64 ToUInt64(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            data = PadBytes(data, 8);
            return BitConverter.ToUInt64(data, 0);
        }



        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
        public static byte[] ToByteArray(IBuffer buffer)
        {
            byte[] data = new byte[buffer.Length];
            DataReader reader = DataReader.FromBuffer(buffer);
            reader.ByteOrder = ByteOrder.LittleEndian;
            reader.ReadBytes(data);
            return data;
        }



        /// <summary>
        /// Converts buffer data into the specified format.
        /// </summary>
        /// <param name="buffer">The buffer to be converted.</param>
        /// <returns>Conversion result.</returns>
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
}
