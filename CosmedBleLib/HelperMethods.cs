using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace CosmedBleLib
{

    public static class GattServiceUuidHelper
    {
        /// <summary>
        /// Helper function to convert a UUID to a name
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>Name of the UUID</returns>
        public static string ConvertUuidToName(Guid uuid)
        {
            var shortId = BluetoothUuidHelper.TryGetShortId(uuid);
            if (shortId.HasValue &&
                Enum.TryParse(shortId.Value.ToString(), out GattServiceUuid name) == true)
            {
                return name.ToString();
            }
            return uuid.ToString();
        }

        public static bool IsReadOnly(Guid uuid)
        {
            if (GattServiceUuids.DeviceInformation == uuid || GattServiceUuids.GenericAttribute == uuid || GattServiceUuids.GenericAccess == uuid || GattServiceUuids.ScanParameters == uuid)
            {
                return true;
            }

            return false;
        }


        public static bool IsReserved(Guid uuid)
        {
            if (GattServiceUuids.HumanInterfaceDevice == uuid)
            {
                return true;
            }

            return false;
        }
    }


    public static class GattCharacteristicUuidHelper
    {
        /// <summary>
        /// Helper function to convert a UUID to a name
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>Name of the UUID</returns>
        public static string ConvertUuidToName(Guid uuid)
        {
            var shortId = BluetoothUuidHelper.TryGetShortId(uuid);
            if (shortId.HasValue &&
                Enum.TryParse(shortId.Value.ToString(), out GattCharacteristicUuid name) == true)
            {
                return name.ToString();
            }
            return uuid.ToString();
        }
    }


    public static class GattDescriptorUuidHelper
    {
        /// <summary>
        /// Helper function to convert a UUID to a name
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns>Name of the UUID</returns>
        public static string ConvertUuidToName(Guid uuid)
        {
            var shortId = BluetoothUuidHelper.TryGetShortId(uuid);
            if (shortId.HasValue &&
                Enum.TryParse(shortId.Value.ToString(), out GattDescriptorUuid name) == true)
            {
                return name.ToString();
            }
            return uuid.ToString();
        }
    }


    public static class AdvertisementDataTypeHelper
    {
        public static string ConvertAdvertisementDataTypeToString(byte dataType)
        {
            AdvertisementSectionTypeUuid convertedSectionType;
            if (Enum.TryParse(dataType.ToString(), out convertedSectionType))
            {
                return convertedSectionType.ToString();
            }
            return dataType.ToString("X2");
        }
    }


    public static class AppearenceDataTypeHelper
    {
        public static string ConvertAppearenceTypeToString(byte dataType)
        {
            BluetoothAppearanceTypeUuid appearenceType;
            if (Enum.TryParse(dataType.ToString(), out appearenceType))
            {
                return appearenceType.ToString();
            }
            return dataType.ToString("X2");
        }
    }


    public static class PresentationFormatUnitsHelper
    {
        public static string ConvertUnitTypeToString(ushort unitType)
        {
            PresentationFormats.Units unit;
            if (Enum.TryParse(unitType.ToString(), out unit))
            {
                return unit.ToString();
            }
            return unitType.ToString("X2");
        }
    }


    public static class PresentationFormatTypeHelper
    {
        public static string ConvertFormatTypeToString(byte formatType)
        {
            PresentationFormats.FormatTypes format;
            if (Enum.TryParse(formatType.ToString(), out format))
            {
                return format.ToString();
            }
            return formatType.ToString("X2");
        }
    }

    public static class NamespaceTypeHelper
    {
        public static string ConvertNamespaceTypeToString(byte namespaceType)
        {
            PresentationFormats.FormatTypes format;
            if (Enum.TryParse(namespaceType.ToString(), out format))
            {
                return format.ToString();
            }
            return namespaceType.ToString("X2");
        }
    }


    // Converts GenericGattCharacteristic.Value to a string based on the presentation format
    /// <summary>
    /// Helper class to change the values so they're easily consumable
    /// </summary>
    public class ValueConverter
    {
        /// <summary>
        /// Converts GenericGattCharacteristic.Value to a string based on the presentation format
        /// </summary>
        /// <param name="characteristic"></param>
        /// <returns>value as a string</returns>
        public static string ConvertGattCharacteristicValueToString(GattCharacteristic characteristic, IBuffer value)
        {
            if (value == null)
            {
                return String.Empty;
            }

            GattPresentationFormat format = null;

            if (characteristic.PresentationFormats.Count > 0)
            {
                format = characteristic.PresentationFormats[0];
            }

            return ConvertValueBufferToString(value, format);
        }

        /// <summary>
        /// Converts GenericGattCharacteristic.Value to a string based on the presentation format
        /// </summary>
        /// <param name="value">value to convert</param>
        /// <param name="format">presentation format to use</param>
        /// <returns>value as string</returns>
        public static string ConvertValueBufferToString(IBuffer value, GattPresentationFormat format = null)
        {
            // no format, return bytes
            if (format == null)
            {
                return ClientGattBufferReaderWriter.ToHexString(value);
            }

            // Bool
            if (format.FormatType == GattPresentationFormatTypes.Boolean)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                throw new NotImplementedException();
            }
            else if (format.FormatType == GattPresentationFormatTypes.Bit2 ||
                     format.FormatType == GattPresentationFormatTypes.Nibble)
            {
                // 2bit or nibble - no exponent
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt8 ||
                     format.FormatType == GattPresentationFormatTypes.UInt12 ||
                     format.FormatType == GattPresentationFormatTypes.UInt16)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt24 ||
                     format.FormatType == GattPresentationFormatTypes.UInt32)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt48 ||
                     format.FormatType == GattPresentationFormatTypes.UInt64)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.SInt8 ||
                     format.FormatType == GattPresentationFormatTypes.SInt12 ||
                     format.FormatType == GattPresentationFormatTypes.SInt16)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.SInt24 ||
                    format.FormatType == GattPresentationFormatTypes.SInt32)
            {
                return ClientGattBufferReaderWriter.ToInt32(value).ToString();
            }
            else if (format.FormatType == GattPresentationFormatTypes.Utf8)
            {
                return ClientGattBufferReaderWriter.ToUTF8String(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.Utf16)
            {
                return ClientGattBufferReaderWriter.ToUTF16String(value);
            }
            else
            {
                // format.FormatType == GattPresentationFormatTypes.UInt128 ||
                // format.FormatType == GattPresentationFormatTypes.SInt128 ||
                // format.FormatType == GattPresentationFormatTypes.DUInt16 ||
                // format.FormatType == GattPresentationFormatTypes.SInt64 ||
                // format.FormatType == GattPresentationFormatTypes.Struct ||
                // format.FormatType == GattPresentationFormatTypes.Float ||
                // format.FormatType == GattPresentationFormatTypes.Float32 ||
                // format.FormatType == GattPresentationFormatTypes.Float64
                return ClientGattBufferReaderWriter.ToHexString(value);
            }
        }
    }
}
