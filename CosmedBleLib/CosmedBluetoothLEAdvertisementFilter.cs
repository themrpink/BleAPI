using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CosmedBleLib
{


    public interface IFilter
    {
        BluetoothLEAdvertisementFilter AdvertisementFilter { get; set; }
        BluetoothSignalStrengthFilter SignalStrengthFilter { get; set; }
        bool ShowOnlyConnectableDevices { get; set; }
    }




    public class FilterBuilder
    {
        private CosmedBluetoothLEAdvertisementFilter filter;

        private FilterBuilder()
        {
            filter = new CosmedBluetoothLEAdvertisementFilter();
        }

        private FilterBuilder(bool ShowOnlyConnectableDevices) : base()
        {
            filter.ShowOnlyConnectableDevices = ShowOnlyConnectableDevices;
        }

        public static FilterBuilder Init()
        {
            return new FilterBuilder();
        }

        public static FilterBuilder Init(bool ShowOnlyConnectableDevices)
        {
            return new FilterBuilder(ShowOnlyConnectableDevices);
        }


        //add advertisement service UUID element: 
        public FilterBuilder SetServiceUUID(Guid ServiceUUID)
        {
            //checkAdvertisementFilter();
            filter.AdvertisementFilter.Advertisement.ServiceUuids.Add(ServiceUUID);
            return this;
        }

        public FilterBuilder SetShowOnlyConnectableDevices(bool connectable)
        {
            filter.ShowOnlyConnectableDevices = connectable;
            return this;
        }

        public FilterBuilder SetFlags(BluetoothLEAdvertisementFlags flag)
        {
            filter.AdvertisementFilter.Advertisement.Flags = flag;
            return this;
        }


        public FilterBuilder SetLocalName(string LocalName)
        {
            if (LocalName == null)
            {
                throw new ArgumentNullException("argument was null");
            }
            filter.AdvertisementFilter.Advertisement.LocalName = LocalName;
            return this;
        }


        public FilterBuilder SetCompanyID(ushort CompanyId)
        {
            var manufacturerData = new BluetoothLEManufacturerData();

            // set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = CompanyId;
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        public FilterBuilder SetCompanyID(string CompanyIdHexString)
        {
            //checkAdvertisementFilter();
            ushort companyID = Convert.ToUInt16(CompanyIdHexString, 16);
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = companyID;
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        public FilterBuilder AddManufacturerData(ushort CompanyId, ushort ManufacturerData)
        {
            //create a manufacturer data section we wanted to match for
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. (For testing: 0xFFFE)
            manufacturerData.CompanyId = CompanyId;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            writer.WriteUInt16(ManufacturerData);

            // Make sure that the buffer length can fit within an advertisement payload (20 bytes). Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        //same as above
        public FilterBuilder SetDataBuffer(byte dataType, ushort data)
        {
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                dataSection.DataType = dataType;
                var writer = new DataWriter();
                writer.WriteUInt16(data);
                dataSection.Data = writer.DetachBuffer();
                filter.AdvertisementFilter.Advertisement.DataSections.Add(dataSection);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }
            return this;
        }

        public FilterBuilder SetDataBuffer(uint dataType, ushort data)
        {
            var dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                byte ADType = Convert.ToByte(dataType);
                dataSection.DataType = ADType;
                var writer = new DataWriter();
                writer.WriteUInt16(data);
                dataSection.Data = writer.DetachBuffer();
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }

            return this;
        }

        public FilterBuilder ClearAdvertisementFilter()
        {
            filter.AdvertisementFilter = new BluetoothLEAdvertisementFilter(); ;
            return this;
        }

        public FilterBuilder ClearSignalStrengthFilter()
        {
            filter.SignalStrengthFilter = new BluetoothSignalStrengthFilter();
            return this;
        }

        public FilterBuilder SetSignalStrengthFilter(short InRangeThresholdInDBm, short OutOfRangeThresholdInDBm, TimeSpan OutOfRangeTimeout)
        {
            {
                //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //will start to be considered "in-range"
                filter.SignalStrengthFilter.InRangeThresholdInDBm = InRangeThresholdInDBm;

                // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
                // to determine when an advertisement is no longer considered "in-range"
                filter.SignalStrengthFilter.OutOfRangeThresholdInDBm = OutOfRangeThresholdInDBm;

                // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
                // to determine when an advertisement is no longer considered "in-range"
                filter.SignalStrengthFilter.OutOfRangeTimeout = OutOfRangeTimeout;

                return this;
            }

        }

        public IFilter BuildFilter()
        {
            return filter;
        }
    }




    public sealed class CosmedBluetoothLEAdvertisementFilter : IFilter
    {
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; set; }
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; set; }

        public bool ShowOnlyConnectableDevices { get; set; }

        public CosmedBluetoothLEAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();
            SignalStrengthFilter = new BluetoothSignalStrengthFilter();
        }


        //set Signal Strength Filter
        public CosmedBluetoothLEAdvertisementFilter SetSignalStrengthFilter(short InRangeThresholdInDBm, short OutOfRangeThresholdInDBm, TimeSpan OutOfRangeTimeout)
        {
            //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
            //will start to be considered "in-range"
            SignalStrengthFilter.InRangeThresholdInDBm = InRangeThresholdInDBm;

            // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
            // to determine when an advertisement is no longer considered "in-range"
            SignalStrengthFilter.OutOfRangeThresholdInDBm = OutOfRangeThresholdInDBm;

            // Set the out-of-range timeout to be 2 seconds. Used in conjunction with OutOfRangeThresholdInDBm
            // to determine when an advertisement is no longer considered "in-range"
            SignalStrengthFilter.OutOfRangeTimeout = OutOfRangeTimeout;

            return this;
        }


        //add advertisement service UUID element: 
        public CosmedBluetoothLEAdvertisementFilter SetServiceUUID(Guid ServiceUUID)
        {
            //checkAdvertisementFilter();
            AdvertisementFilter.Advertisement.ServiceUuids.Add(ServiceUUID);
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter SetShowOnlyConnectableDevices(bool connectable)
        {
            ShowOnlyConnectableDevices = connectable;
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter SetFlags(BluetoothLEAdvertisementFlags flag)
        {
            AdvertisementFilter.Advertisement.Flags = flag;
            return this;
        }
        

        public CosmedBluetoothLEAdvertisementFilter SetLocalName(string LocalName)
        {
            if(LocalName == null)
            {
                throw new ArgumentNullException("argument was null");
            }
            AdvertisementFilter.Advertisement.LocalName = LocalName;
            return this;
        }


        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(ushort CompanyId)
        {
            var manufacturerData = new BluetoothLEManufacturerData();

            // set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = CompanyId;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(string CompanyIdHexString)
        {
            //checkAdvertisementFilter();
            ushort companyID = Convert.ToUInt16(CompanyIdHexString, 16);
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = companyID;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter AddManufacturerData(ushort CompanyId, ushort ManufacturerData)
        {
            //create a manufacturer data section we wanted to match for
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. (For testing: 0xFFFE)
            manufacturerData.CompanyId = CompanyId;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            writer.WriteUInt16(ManufacturerData);

            // Make sure that the buffer length can fit within an advertisement payload (20 bytes). Otherwise you will get an exception.
            manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }

        //same as above
        public CosmedBluetoothLEAdvertisementFilter SetDataBuffer(byte dataType, ushort data)
        {
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                dataSection.DataType = dataType;
                var writer = new DataWriter();
                writer.WriteUInt16(data);
                dataSection.Data = writer.DetachBuffer();
                AdvertisementFilter.Advertisement.DataSections.Add(dataSection);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }
            return this;
            }

        public CosmedBluetoothLEAdvertisementFilter SetDataBuffer(uint dataType, ushort data)
        {
            var dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                byte ADType = Convert.ToByte(dataType);
                dataSection.DataType = ADType;
                var writer = new DataWriter();
                writer.WriteUInt16(data);
                dataSection.Data = writer.DetachBuffer();
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }

            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter ClearAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter(); ;
            return this;
        }

        public CosmedBluetoothLEAdvertisementFilter ClearSignalStrengthFilter()
        {
            SignalStrengthFilter = new BluetoothSignalStrengthFilter();
            return this;
        }
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

     * use logical sum to request multiple flags: 
     * setFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported)
     * requested flag value = 6.
     * if only one flag is requested and the device advertises 2 flags, it will be excluded by the filter
  */

}


