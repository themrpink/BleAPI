using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;

namespace CosmedBleLib
{
    public class CosmedBluetoothLEAdvertisementFilter
    {
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; }
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; }

        public CosmedBluetoothLEAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();


            //advertisement data sections elements: una lista di sezioni, ognuna
            IList<BluetoothLEAdvertisementDataSection> dataSections = AdvertisementFilter.Advertisement.DataSections;
            BluetoothLEAdvertisementDataSection dataSec = dataSections.GetEnumerator().Current;
            byte dataType = dataSec.DataType;
            IBuffer data = dataSec.Data;
            uint capacity = data.Capacity;
            uint length = data.Length;

            //advertisement service UUID elements: 
            IList<Guid> serviceUUID = AdvertisementFilter.Advertisement.ServiceUuids;
            Guid g = serviceUUID[0];
            string uuid = g.ToString();


            //advertisement flags: indicano il tipo di discoverability
            BluetoothLEAdvertisementFlags? flags = AdvertisementFilter.Advertisement.Flags;
            /*
             None = 0,
             LimitedDiscoverableMode = 1,
             GeneralDiscoverableMode = 2,
             ClassicNotSupported = 4,
             DualModeControllerCapable = 8,
             DualModeHostCapable = 16
            */
            /*
             #define BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE         (0x01)   //< LE Limited Discoverable Mode. 
             #define BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE         (0x02)   //< LE General Discoverable Mode. 
             #define BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED         (0x04)   //< BR/EDR not supported. 
             #define BLE_GAP_ADV_FLAG_LE_BR_EDR_CONTROLLER         (0x08)   //< Simultaneous LE and BR/EDR, Controller. 
             #define BLE_GAP_ADV_FLAG_LE_BR_EDR_HOST               (0x10)   //< Simultaneous LE and BR/EDR, Host.
             #define BLE_GAP_ADV_FLAGS_LE_ONLY_LIMITED_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE Limited Discoverable Mode, BR/EDR not supported. (05)
             #define BLE_GAP_ADV_FLAGS_LE_ONLY_GENERAL_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE General Discoverable Mode, BR/EDR not supported. (06)
             */

            //local name, già offerto come stringa 
            string localName = AdvertisementFilter.Advertisement.LocalName;

            //advertisement ManufacturerData list
            IReadOnlyList<BluetoothLEManufacturerData> dataByCompanyID = AdvertisementFilter.Advertisement.GetManufacturerDataByCompanyId(76);
            BluetoothLEManufacturerData manData = dataByCompanyID?[0];
            ushort companyID = manData.CompanyId;
            IBuffer manBuffer = manData.Data;
            uint manBufCapacity = manBuffer.Capacity;
            uint manBufLen = manBuffer.Length;

            //per il data section, vedi sopra
            //qua ci va un ADType, espresso con un byte
            byte ADType = Convert.ToByte(4);
            IReadOnlyList<BluetoothLEAdvertisementDataSection> sectionByType = AdvertisementFilter.Advertisement.GetSectionsByType(ADType);
            
            
            /*
             * esempio per inserire i manufacturer data
             */
                
            // First, let create a manufacturer data section we wanted to match for. These are the same as the one 
            // created in Scenario 2 and 4.
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = 0xFFFE;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new Windows.Storage.Streams.DataWriter();
            writer.WriteUInt16(0x1234);

            // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);


            SignalStrengthFilter = new BluetoothSignalStrengthFilter
            {
                //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //will start to be considered "in-range"
                InRangeThresholdInDBm = -70,

                // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeThresholdInDBm = -75,

                // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000)
            };

        }

        public CosmedBluetoothLEAdvertisementFilter(ushort CompanyId, ushort ManufacturerData, short InRangeThresholdInDBm, short OutOfRangeThresholdInDBm, TimeSpan OutOfRangeTimeout)
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter
            {
                Advertisement = new BluetoothLEAdvertisement
                {
                    LocalName = "",
                    Flags = BluetoothLEAdvertisementFlags.GeneralDiscoverableMode
                }
            };

            
            // First, let create a manufacturer data section we wanted to match for. These are the same as the one 
            // created in Scenario 2 and 4.
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = CompanyId;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new Windows.Storage.Streams.DataWriter();
            writer.WriteUInt16(ManufacturerData);

            // Make sure that the buffer length can fit within an advertisement payload. Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);


            SignalStrengthFilter = new BluetoothSignalStrengthFilter
            {
                //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //will start to be considered "in-range"
                InRangeThresholdInDBm = InRangeThresholdInDBm,

                // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeThresholdInDBm = OutOfRangeThresholdInDBm,

                // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
                // to determine when an advertisement is no longer considered "in-range"
                OutOfRangeTimeout = OutOfRangeTimeout
            };

        }

    }
}


