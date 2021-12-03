using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib.DeviceDiscovery
{

    /// <summary>
    /// Represents a discovered device with data obtained from it´s advertisement
    /// </summary>
    /// <typeparam name="T"></typeparam>
    interface IAdvertisedDevice<T>
    {

        /// <value>
        /// Gets and sets the device address value
        /// </value>
        ulong DeviceAddress { get; }

        /// <value>
        /// Gets the Signal Strength in dBm
        /// </value>
        short RawSignalStrengthInDBm { get; }

        /// <value>
        /// Gets and sets the Timestamp of the last received advertising
        /// </value>
        DateTimeOffset Timestamp { get; }


        /// <value>
        /// Gets the type of address (public - random)
        /// </value>
        BluetoothAddressType BluetoothAddressType { get; }

        /// <value>
        /// Gets the boolean indicating whether a Bluetooth Address was omitted from the received advertisement.
        /// </value>
        bool IsAnonymous { get; }


        /// <value>
        /// Gets the boolean indicating whether the Bluetooth LE device is currently advertising a connectable advertisement.
        /// </value>
        bool IsConnectable { get; }


        /// <value>
        /// Indicates whether the received advertisement is directed.
        /// </value>
        bool IsDirected { get; }

        /// <value>
        /// Indicates whether the received advertisement is scannable.
        /// </value>
        bool IsScannable { get; }

        /// <value>
        /// Represents the received transmit power of the advertisement.
        /// </value>
        short? TransmitPowerLevelInDBm { get; }

        /// <summary>
        /// Sets an advertisement received from the device
        /// </summary>
        /// <param name="args">The arguments containing all the data about a received advertisement</param>
        /// <returns>An instance of the class</returns>
        T SetAdvertisement(BluetoothLEAdvertisementReceivedEventArgs args);

    }
}
