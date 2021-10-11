using System;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    public class CosmedBleDevice
    {
        public ulong DeviceAddress { get; }
        public DateTimeOffset timestamp { get; }
        public bool isConnectable { get; }
        public BluetoothLEAdvertisement Advertisement { get; }
        public BluetoothLEAdvertisementType AdvertisementType { get; }

        public CosmedBleDevice(ulong address, DateTimeOffset timestamp, bool isConnectable, BluetoothLEAdvertisement adv,  BluetoothLEAdvertisementType advType)
        {
            DeviceAddress = address;
            this.timestamp = timestamp;
            this.isConnectable = isConnectable;
            Advertisement = adv;
            AdvertisementType = advType;
        }

  
        public override string ToString()
        {
            return DeviceAddress.ToString();
        }
    }
}

