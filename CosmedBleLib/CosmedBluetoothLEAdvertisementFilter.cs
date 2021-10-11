using System;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    public class CosmedBluetoothLEAdvertisementFilter
    {
        public BluetoothLEAdvertisementFilter AdvertisementFilter { get; }
        public BluetoothSignalStrengthFilter SignalStrengthFilter { get; }

        public CosmedBluetoothLEAdvertisementFilter()
        {
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();
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
            AdvertisementFilter = new BluetoothLEAdvertisementFilter();
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


