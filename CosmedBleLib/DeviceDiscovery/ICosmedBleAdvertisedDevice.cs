using CosmedBleLib.Collections;
using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib.DeviceDiscovery
{
    public interface ICosmedBleAdvertisedDevice
    {
        BluetoothAddressType BluetoothAddressType { get; }
        DataSectionCollection DataSections { get; }
        DataSectionCollection DataSectionsFromScanResponse { get; }
        ulong DeviceAddress { get; set; }
        string DeviceName { get; }
        BluetoothLEAdvertisementFlags? Flags { get; }
        AdvertisementContent GetAdvertisementContent { get; }
        AdvertisementContent GetScanResponseAdvertisementContent { get; }
        bool HasScanResponse { get; }
        string HexDeviceAddress { get; }
        bool IsAnonymous { get; }
        bool IsConnectable { get; }
        bool IsDirected { get; }
        bool IsScannable { get; }
        ManufacturerDataCollection ManufacturerData { get; }
        ManufacturerDataCollection ManufacturerDataFromScanResponse { get; }
        short RawSignalStrengthInDBm { get; }
        IReadOnlyCollection<Guid> ServiceUuids { get; }
        IReadOnlyCollection<Guid> ServiceUuidsFromScanResponse { get; }
        DateTimeOffset Timestamp { get; set; }
        short? TransmitPowerLevelInDBm { get; }

        event Action<CosmedBleAdvertisedDevice> ScanResponseReceived;

        CosmedBleAdvertisedDevice SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args);
    }
}