using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using CosmedBleLib.Values;

/// <summary>
/// Filter 
/// </summary>
namespace CosmedBleLib.DeviceDiscovery
{

    /// <summary>
    /// A generic filter that can be used by the watcher
    /// </summary>
    public interface IFilter
    {
        /// <value>
        /// The filter based on advertised data
        /// </value>
        BluetoothLEAdvertisementFilter AdvertisementFilter { get; set; }


        /// <value>
        /// Filter based on advertising device signal
        /// </value>
        BluetoothSignalStrengthFilter SignalStrengthFilter { get; set; }


        /// <value>
        /// Option to filter by connectable devices
        /// </value>
        bool ShowOnlyConnectableDevices { get; set; }
    }



    /// <summary>
    /// Builder of the Filter
    /// </summary>
    public class FilterBuilder
    {
        /// <summary>
        /// The filter instantiated by the builder
        /// </summary>
        private CosmedBluetoothLEAdvertisementFilter filter;


        /// <summary>
        /// Constructor
        /// </summary>
        private FilterBuilder()
        {
            filter = new CosmedBluetoothLEAdvertisementFilter();
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="showOnlyConnectableDevices">Set the filtering by connectable devices</param>
        private FilterBuilder(bool showOnlyConnectableDevices) : this()
        {
            filter.ShowOnlyConnectableDevices = showOnlyConnectableDevices;
        }


        /// <summary>
        /// Inits the builder to start building the filter object
        /// </summary>
        /// <returns>an empty FilterBuilder</returns>
        public static FilterBuilder Init()
        {
            
            return new FilterBuilder();
        }


        /// <summary>
        /// Inits the builder to start building the filter object
        /// </summary>
        /// <param name="showOnlyConnectableDevices">True to filter by connectable devices</param>
        /// <returns>an empty FilterBuilder</returns>
        public static FilterBuilder Init(bool showOnlyConnectableDevices)
        {
            return new FilterBuilder(showOnlyConnectableDevices);
        }


        /// <summary>
        /// Adds filtering by service UUID
        /// </summary>
        /// <param name="serviceUUID">UUID to be filtered</param>
        /// <returns>An instance of the builder in the updated state</returns>
        public FilterBuilder SetServiceUUID(Guid serviceUUID)
        {
            //checkAdvertisementFilter();
            filter.AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceUUID);
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by connectable devices
        /// </summary>
        /// <param name="connectable">True to activate this filter option, false to deactivate</param>
        /// <returns>An instance of the builder in its udapted state</returns>
        public FilterBuilder SetShowOnlyConnectableDevices(bool connectable)
        {
            filter.ShowOnlyConnectableDevices = connectable;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by flags. Flags indicate the kind of advertising device 
        /// related to its connectivity properties
        /// </summary>
        /// <param name="flag">The kind of device</param>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetFlags(BluetoothLEAdvertisementFlags flag)
        {
            filter.AdvertisementFilter.Advertisement.Flags = flag;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by the local name of the device
        /// </summary>
        /// <param name="localName">The requested local name </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if argument is null
        /// </exception>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetLocalName(string localName)
        {
            if (localName == null)
            {
                throw new ArgumentNullException("null argument");
            }
            filter.AdvertisementFilter.Advertisement.LocalName = localName;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID
        /// </summary>
        /// <param name="companyId">The company id. </param>
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetCompanyID(ushort companyId)
        {
            var manufacturerData = new BluetoothLEManufacturerData();

            // set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = companyId;
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID.
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <param name="companyIdHexString">The company id, expressed as string representation of the hexadecimal value. </param>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetCompanyID(string companyIdHexString)
        {
            //checkAdvertisementFilter();
            ushort companyID = Convert.ToUInt16(companyIdHexString, 16);
            var manufacturerData = new BluetoothLEManufacturerData
            {
                CompanyId = companyID
            };
            
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID and Manufacturer Data.
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the buffer length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="manufacturerData"></param>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder AddManufacturerData(BluetoothLEManufacturerData manufacturerData)
        {
            // Add the manufacturer data to the advertisement filter on the watcher:
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID and Manufacturer Data.
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the buffer length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="companyId">The company id</param>
        /// <param name="manufacturerData"></param>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder AddManufacturerData(ushort companyId, byte[] manufacturerData)
        {
            //create a manufacturer data section we wanted to match for
            var _manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. (For testing: 0xFFFE)
            _manufacturerData.CompanyId = companyId;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            writer.WriteBytes(manufacturerData);

            // Make sure that the buffer length can fit within an advertisement payload (20 bytes). Otherwise you will get an exception.
            _manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            filter.AdvertisementFilter.Advertisement.ManufacturerData.Add(_manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by data and its type,
        /// as defined by the Bluetooth Special Interest Group (SIG).
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the data length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="dataType">The data type</param>
        /// <param name="data">The data value</param>
        /// <exception cref="System.ArgumentException">Thrown in case of data overlflow</exception>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetDataBuffer(AdvertisementSectionType dataType, byte[] data)
        {
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                dataSection.DataType = ((byte)dataType);
                var writer = new DataWriter();
                writer.WriteBytes(data);
                dataSection.Data = writer.DetachBuffer();
                filter.AdvertisementFilter.Advertisement.DataSections.Add(dataSection);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }
            return this;
        }


        /// <summary>
        /// Clears and resets the AdvertisementFilter
        /// </summary>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder ClearAdvertisementFilter()
        {
            filter.AdvertisementFilter = new BluetoothLEAdvertisementFilter(); ;
            return this;
        }


        /// <summary>
        /// Clears and resets the SignalStrengthFilter
        /// </summary>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder ClearSignalStrengthFilter()
        {
            filter.SignalStrengthFilter = new BluetoothSignalStrengthFilter();
            return this;
        }


        /// <summary>
        /// Sets the signal strength filter
        /// </summary>
        /// <param name="InRangeThresholdInDBm">the in-range threshold in dBm(RSSI)</param>
        /// <param name="OutOfRangeThresholdInDBm">the out-of-range threshold in dBm. Used in conjunction with OutOfRangeTimeout
        ///to determine when an advertisement is no longer considered "in-range</param>
        /// <param name="OutOfRangeTimeout">Timeout. Used in conjunction with OutOfRangeThresholdInDBm to determine when an advertisement is no longer considered "in-range</param>
        /// <returns>An instance of the builder in its updated state</returns>
        public FilterBuilder SetSignalStrengthFilter(short InRangeThresholdInDBm, short OutOfRangeThresholdInDBm, TimeSpan OutOfRangeTimeout)
        {
            {
                //Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //will start to be considered "in-range"
                filter.SignalStrengthFilter.InRangeThresholdInDBm = InRangeThresholdInDBm;

                //Used in conjunction with OutOfRangeTimeout
                // to determine when an advertisement is no longer considered "in-range"
                filter.SignalStrengthFilter.OutOfRangeThresholdInDBm = OutOfRangeThresholdInDBm;

                //Used in conjunction with OutOfRangeThresholdInDBm to determine when an advertisement is no longer considered "in-range"
                filter.SignalStrengthFilter.OutOfRangeTimeout = OutOfRangeTimeout;

                return this;
            }

        }


        /// <summary>
        /// Finalizes the filter construction
        /// </summary>
        /// <returns>The built filter instance</returns>
        public IFilter BuildFilter()
        {
            return filter;
        }
    }




    /// <summary>
    /// Represents a Ble Advertisement Filter
    /// </summary>
    public sealed class CosmedBluetoothLEAdvertisementFilter : IFilter
    {
        /// <value>
        /// Gets and sets an AdvertisementFilter
        /// </value>
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; set; }


        /// <value>
        /// Gets and sets a SignalStrengthFilter
        /// </value>
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; set; }


        /// <value>
        /// Gets and sets the boolean indicating that only connectable devices should be showed
        /// </value>
        public bool ShowOnlyConnectableDevices { get; set; }


        /// <summary>
        /// Constructor of the class
        /// </summary>
        public CosmedBluetoothLEAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();
            SignalStrengthFilter = new BluetoothSignalStrengthFilter();
        }


        /// <summary>
        /// Sets the signal strength filter
        /// </summary>
        /// <param name="InRangeThresholdInDBm">the in-range threshold in dBm(RSSI)</param>
        /// <param name="OutOfRangeThresholdInDBm">the out-of-range threshold in dBm. Used in conjunction with OutOfRangeTimeout
        ///to determine when an advertisement is no longer considered "in-range</param>
        /// <param name="OutOfRangeTimeout">Timeout. Used in conjunction with OutOfRangeThresholdInDBm to determine when an advertisement is no longer considered "in-range</param>
        /// <returns>An instance of the filter in its updated state</returns>
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


        /// <summary>
        /// Adds filtering by service UUID
        /// </summary>
        /// <param name="serviceUUID">UUID to be filtered</param>
        /// <returns>An instance of the Filter in the updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetServiceUUID(Guid serviceUUID)
        {
            //checkAdvertisementFilter();
            AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceUUID);
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by connectable devices
        /// </summary>
        /// <param name="connectable">True to activate this filter option, false to deactivate</param>
        /// <returns>An instance of the Filter in its udapted state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetShowOnlyConnectableDevices(bool connectable)
        {
            ShowOnlyConnectableDevices = connectable;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by flags. Flags indicate the kind of advertising device 
        /// related to its connectivity properties
        /// </summary>
        /// <param name="flag">The kind of device</param>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetFlags(BluetoothLEAdvertisementFlags flag)
        {
            AdvertisementFilter.Advertisement.Flags = flag;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by the local name of the device
        /// </summary>
        /// <param name="localName">The requested local name </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if argument is null
        /// </exception>
        /// <returns>An instance of the filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetLocalName(string localName)
        {
            if(localName == null)
            {
                throw new ArgumentNullException("argument was null");
            }
            AdvertisementFilter.Advertisement.LocalName = localName;
            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID
        /// </summary>
        /// <param name="companyId">The company id expressed as string representation of the hexadecimal value.</param>
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(ushort companyId)
        {
            var manufacturerData = new BluetoothLEManufacturerData();
            // set the company ID for the manufacturer data
            manufacturerData.CompanyId = companyId;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID
        /// </summary>
        /// <param name="companyIdHexString">The company id expressed as string representation of the hexadecimal value</param>
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetCompanyID(string companyIdHexString)
        {
            //checkAdvertisementFilter();
            ushort companyID = Convert.ToUInt16(companyIdHexString, 16);
            var manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. Here we picked an unused value: 0xFFFE
            manufacturerData.CompanyId = companyID;
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID and Manufacturer Data.
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the buffer length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="manufacturerData">Instance of the BluetoothLEManufacturerData class</param>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter AddManufacturerData(BluetoothLEManufacturerData manufacturerData)
        {
            // Add the manufacturer data to the advertisement filter on the watcher:
            AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by company ID and Manufacturer Data.
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the buffer length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="companyId">The company id</param>
        /// <param name="manufacturerData">Byte array containing the manufacturer data</param>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter AddManufacturerData(ushort companyId, byte[] manufacturerData)
        {
            //create a manufacturer data section we wanted to match for
            var _manufacturerData = new BluetoothLEManufacturerData();

            // Then, set the company ID for the manufacturer data. (For testing: 0xFFFE)
            _manufacturerData.CompanyId = companyId;

            // Finally set the data payload within the manufacturer-specific section
            // Here, use a 16-bit UUID: 0x1234 -> {0x34, 0x12} (little-endian)
            var writer = new DataWriter();
            writer.WriteBytes(manufacturerData);

            // Make sure that the buffer length can fit within an advertisement payload (20 bytes). Otherwise you will get an exception.
            _manufacturerData.Data = writer.DetachBuffer();

            // Add the manufacturer data to the advertisement filter on the watcher:
            AdvertisementFilter.Advertisement.ManufacturerData.Add(_manufacturerData);

            return this;
        }


        /// <summary>
        /// Adds to the filter the property of filtering by data and its type,
        /// as defined by the Bluetooth Special Interest Group (SIG).
        /// <see href="https://www.bluetooth.com/specifications/assigned-numbers">See Bluetooth Assigned Numbers</see>
        /// </summary>
        /// <remarks>Make sure that the data length can fit within an advertisement payload (20 bytes)</remarks>
        /// <param name="dataType">The data type</param>
        /// <param name="data">The data value</param>
        /// <exception cref="System.ArgumentException">Thrown in case of data overlflow</exception>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter SetDataBuffer(AdvertisementSectionType dataType, byte[] data)
        {
            BluetoothLEAdvertisementDataSection dataSection = new BluetoothLEAdvertisementDataSection();
            try
            {
                dataSection.DataType = ((byte)dataType);
                var writer = new DataWriter();
                writer.WriteBytes(data);
                dataSection.Data = writer.DetachBuffer();
                AdvertisementFilter.Advertisement.DataSections.Add(dataSection);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("data size too big", e);
            }
            return this;
        }


        /// <summary>
        /// Clears and resets the AdvertisementFilter
        /// </summary>
        /// <returns>An instance of the Filter in its updated state</returns>
        public CosmedBluetoothLEAdvertisementFilter ClearAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter(); ;
            return this;
        }


        /// <summary>
        /// Clears and resets the SignalStrengthFilter
        /// </summary>
        /// <returns>An instance of the Filter in its updated state</returns>
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


