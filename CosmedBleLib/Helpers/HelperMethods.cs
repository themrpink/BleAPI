using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using CosmedBleLib.Values;

/// <summary>
/// Helper methods
/// </summary>
namespace CosmedBleLib.Helpers
{

    /// <summary>
    /// Helper methods to read Gatt Services
    /// </summary>
    public static class GattServiceUuidHelper
    {
        /// <summary>
        /// Helper function to convert a UUID to it's name if registered
        /// </summary>
        /// <param name="uuid">Service UUID. Can be 16 or 128 bit</param>
        /// <returns>Name of the UUID. If not registered return the UUID in string format.</returns>
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public static bool IsReadOnly(Guid uuid)
        {
            if (GattServiceUuids.DeviceInformation == uuid || GattServiceUuids.GenericAttribute == uuid || GattServiceUuids.GenericAccess == uuid || GattServiceUuids.ScanParameters == uuid)
            {
                return true;
            }
            return false;
        }
    }



    /// <summary>
    /// Helper methods to translate registered Characteristic names
    /// </summary>
    public static class GattCharacteristicUuidHelper
    {
        /// <summary>
        /// Helper function to convert a UUID to a name if registered
        /// </summary>
        /// <param name="uuid">UIID of the Characteristic. Can be 16 or 128 bit</param>
        /// <returns>Name of the registered Characteristic. If not registered return the UUID in string format.</returns>
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



    /// <summary>
    /// Helper methods to translate registered Declaration UUID
    /// </summary>
    public static class GattDeclarationHelper
    {
        /// <summary>
        /// Helper function to convert a Declaration UUID to a name
        /// </summary>
        /// <param name="uuid">Declaration UUID. Can be 16 or 128 bit.</param>
        /// <returns>Name of the Declaration UUID. If not registered return the UUID in string format.</returns>
        public static string ConvertUuidToName(Guid uuid)
        {
            var shortId = BluetoothUuidHelper.TryGetShortId(uuid);
            if (shortId.HasValue &&
                Enum.TryParse(shortId.Value.ToString(), out GattDeclarationUuid name) == true)
            {
                return name.ToString();
            }
            return uuid.ToString();
        }
    }



    /// <summary>
    /// Helper methods to translate registered Gatt Descriptors Uuid
    /// </summary>
    public static class GattDescriptorUuidHelper
    {
        /// <summary>
        /// Helper function to convert a Descriptor UUID to a name
        /// </summary>
        /// <param name="uuid">Descriptor UUID</param>
        /// <returns>Name of the Descriptor UUID. If not registered return the UUID in string format.</returns>
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



    /// <summary>
    /// Helper methods for Advertisement Data Types
    /// </summary>
    public static class AdvertisementDataTypeHelper
    {
        /// <summary>
        /// Helper function to convert a registered dataType value to a name
        /// </summary>
        /// <param name="dataType">DataType value..</param>
        /// <returns>Name of the DataType. If not registered return the byte in HEX string format.</returns>
        public static string ConvertAdvertisementDataTypeToString(byte dataType)
        {
            AdvertisementSectionType convertedSectionType;
            if (Enum.TryParse(dataType.ToString(), out convertedSectionType))
            {
                return convertedSectionType.ToString();
            }
            return dataType.ToString("X2");
        }
    }



    /// <summary>
    /// Helper methods for Appearance attribute data type
    /// </summary>
    public static class AppearenceDataTypeHelper
    {
        /// <summary>
        /// Helper function to convert an Appearance data type to a name
        /// </summary>
        /// <param name="dataType">data type value</param>
        /// <returns>Name of the DataType. If not registered return the value in HEX string format.</returns>
        public static string ConvertAppearenceTypeToString(ushort dataType)
        {
            BluetoothAppearanceType appearenceType;
            if (Enum.TryParse(dataType.ToString(), out appearenceType))
            {
                return appearenceType.ToString();
            }
            return dataType.ToString("X2");
        }
    }



    /// <summary>
    /// Helper methods to read Presentation Format Units
    /// </summary>
    public static class PresentationFormatUnitsHelper
    {
        /// <summary>
        /// Helper function to convert a registered Presentation Format Units value to a name
        /// </summary>
        /// <param name="unitType">Presentation Format Unit value</param>
        /// <returns>Name of the Presentation Format Unit. If not registered return the value in HEX string format</returns>
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



    /// <summary>
    /// Helper methods to read Presentation Format Types
    /// </summary>
    public static class PresentationFormatTypeHelper
    {
        /// <summary>
        ///  Helper function to convert a registered Presentation Format type value to a name
        /// </summary>
        /// <param name="formatType">Presentation Format type value</param>
        /// <returns>Name of the Presentation Format type. If not registered return the value in HEX string format</returns>
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



    /// <summary>
    /// Helper methods to read Namespace Types
    /// </summary>
    public static class NamespaceTypeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="namespaceType">Namespace type value</param>
        /// <returns>Name of the Namespace type. If not registered return the value in HEX string format</returns>
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


 
    /// <summary>
    /// Helper class to change the values so they're easily consumable.
    /// Converts GenericGattCharacteristic.Value to a string based on the presentation format.
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
                return ClientBufferReader.ToHexString(value);
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
                return ClientBufferReader.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt8 ||
                     format.FormatType == GattPresentationFormatTypes.UInt12 ||
                     format.FormatType == GattPresentationFormatTypes.UInt16)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientBufferReader.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt24 ||
                     format.FormatType == GattPresentationFormatTypes.UInt32)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientBufferReader.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.UInt48 ||
                     format.FormatType == GattPresentationFormatTypes.UInt64)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientBufferReader.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.SInt8 ||
                     format.FormatType == GattPresentationFormatTypes.SInt12 ||
                     format.FormatType == GattPresentationFormatTypes.SInt16)
            {
                // Previous implementation was incorrect. Need to implement in GattHelper.
                return ClientBufferReader.ToHexString(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.SInt24 ||
                    format.FormatType == GattPresentationFormatTypes.SInt32)
            {
                return ClientBufferReader.ToInt32(value).ToString();
            }
            else if (format.FormatType == GattPresentationFormatTypes.Utf8)
            {
                return ClientBufferReader.ToUTF8String(value);
            }
            else if (format.FormatType == GattPresentationFormatTypes.Utf16)
            {
                return ClientBufferReader.ToUTF16String(value);
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
                return ClientBufferReader.ToHexString(value);
            }
        }
    }
}
