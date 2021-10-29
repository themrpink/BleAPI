using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    interface IAdvertisedDevice<T>
    {
        ulong DeviceAddress { get; }
        short RawSignalStrengthInDBm { get; }
        DateTimeOffset Timestamp { get; }
        BluetoothAddressType BluetoothAddressType { get; }
        bool IsAnonymous { get; }
        bool IsConnectable { get; }
        bool IsDirected { get; }
        bool IsScannable { get; }
        short? TransmitPowerLevelInDBm { get; }


        T SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args);

    }
}
