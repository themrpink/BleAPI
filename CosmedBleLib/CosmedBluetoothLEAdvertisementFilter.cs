using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CosmedBleLib
{
    public class CosmedBluetoothLEAdvertisementFilter
    {
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; private set; }
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; private set; }


        private void checkAdvertisementFilter()
        {
            if(AdvertisementFilter == null)
            {
                AdvertisementFilter = new BluetoothLEAdvertisementFilter();
            }
        }
        //set Signal Strength Filter
        public CosmedBluetoothLEAdvertisementFilter SetSignalStrengthFilter(short InRangeThresholdInDBm, short OutOfRangeThresholdInDBm, TimeSpan OutOfRangeTimeout)
        {
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

            return this;
        }


        //add advertisement service UUID element: 
        public CosmedBluetoothLEAdvertisementFilter setServiceUUID(Guid ServiceUUID)
        {
            checkAdvertisementFilter();
            AdvertisementFilter.Advertisement.ServiceUuids.Add(ServiceUUID);
            return this;
        }


        /*
            advertisement flags: indicano il tipo di discoverability
            BluetoothLEAdvertisementFlags? flags = AdvertisementFilter.Advertisement.Flags;

            None = 0,
            LimitedDiscoverableMode = 1,
            GeneralDiscoverableMode = 2,
            ClassicNotSupported = 4,
            DualModeControllerCapable = 8,
            DualModeHostCapable = 16

            #define BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE         (0x01)   //< LE Limited Discoverable Mode. 
            #define BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE         (0x02)   //< LE General Discoverable Mode. 
            #define BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED         (0x04)   //< BR/EDR not supported. 
            #define BLE_GAP_ADV_FLAG_LE_BR_EDR_CONTROLLER         (0x08)   //< Simultaneous LE and BR/EDR, Controller. 
            #define BLE_GAP_ADV_FLAG_LE_BR_EDR_HOST               (0x10)   //< Simultaneous LE and BR/EDR, Host.
            #define BLE_GAP_ADV_FLAGS_LE_ONLY_LIMITED_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_LIMITED_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE Limited Discoverable Mode, BR/EDR not supported. (05)
            #define BLE_GAP_ADV_FLAGS_LE_ONLY_GENERAL_DISC_MODE   (BLE_GAP_ADV_FLAG_LE_GENERAL_DISC_MODE | BLE_GAP_ADV_FLAG_BR_EDR_NOT_SUPPORTED)   /**< LE General Discoverable Mode, BR/EDR not supported. (06)
        */
        /*
         * use logical sum to request multiple flags: 
         * setFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported)
         * requested flag value = 6.
         * if only one flag is requested and the device advertises 2 flags, it will be excluded by the filter
         */
        public CosmedBluetoothLEAdvertisementFilter setFlags(BluetoothLEAdvertisementFlags flag)
        {
            checkAdvertisementFilter();
            AdvertisementFilter.Advertisement.Flags = flag;
            return this;
        }
        

        public CosmedBluetoothLEAdvertisementFilter setLocalName(string LocalName)
        {
            if(LocalName == null)
            {
                throw new ArgumentNullException();
                //or LocalName = "";
            }
            checkAdvertisementFilter();
            AdvertisementFilter.Advertisement.LocalName = LocalName;
            return this;
        }



        /*
            //advertisement ManufacturerData list
            IReadOnlyList<BluetoothLEManufacturerData> dataByCompanyID = AdvertisementFilter.Advertisement.GetManufacturerDataByCompanyId(76);
            if (dataByCompanyID.GetEnumerator().MoveNext())
            {
                BluetoothLEManufacturerData manData = dataByCompanyID?[0];
                ushort companyID = manData.CompanyId;
                IBuffer manBuffer = manData.Data;
                uint manBufCapacity = manBuffer.Capacity;
                uint manBufLen = manBuffer.Length;

            }
        */

        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(ushort CompanyId)
        {
            checkAdvertisementFilter();
            var manufacturerData = new BluetoothLEManufacturerData();
            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = CompanyId;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(string CompanyIdHexString)
        {
            checkAdvertisementFilter();
            ushort companyID = Convert.ToUInt16(CompanyIdHexString, 16);
            var manufacturerData = new BluetoothLEManufacturerData();
            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = companyID;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter AddManufacturerData(ushort CompanyId, ushort ManufacturerData)
        {
            checkAdvertisementFilter();
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

            return this;
        }

        /*
        //advertisement data sections elements: una lista di sezioni, ognuna
        IList<BluetoothLEAdvertisementDataSection> dataSections = AdvertisementFilter.Advertisement.DataSections;

        BluetoothLEAdvertisementDataSection dataSec;
            if (dataSections.GetEnumerator().MoveNext())
            {
                dataSec  = dataSections.GetEnumerator().Current;
                byte dataType = dataSec.DataType;
                IBuffer data = dataSec.Data;
                uint capacity = data.Capacity;
                uint length = data.Length;
            }

        //qua ci va un ADType, espresso con un byte
        byte ADType = Convert.ToByte(4);
        IReadOnlyList<BluetoothLEAdvertisementDataSection> sectionByType = AdvertisementFilter.Advertisement.GetSectionsByType(ADType);

        */
        public CosmedBluetoothLEAdvertisementFilter setDataBuffer(byte dataType, ushort data)
        {
            checkAdvertisementFilter();
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            dataSection.DataType = dataType;
            var writer = new Windows.Storage.Streams.DataWriter();
            writer.WriteUInt16(data);
            dataSection.Data = writer.DetachBuffer();
            AdvertisementFilter.Advertisement.DataSections.Add(dataSection);
            return this;
            }

        public CosmedBluetoothLEAdvertisementFilter setDataBuffer(uint dataType, ushort data)
        {
            checkAdvertisementFilter();
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            byte ADType = Convert.ToByte(dataType);
            dataSection.DataType = ADType;
            var writer = new Windows.Storage.Streams.DataWriter();
            writer.WriteUInt16(data);
            dataSection.Data = writer.DetachBuffer();
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter ResetAdvertismentFilter()
        {
            AdvertisementFilter = null;
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter ResetSignalStrengthFilter()
        {
            SignalStrengthFilter = null;
            return this;
        }
    }


    public enum DataType
    {
        UINT8,
        UINT16,
        STRING,
        GUID
    }
}


