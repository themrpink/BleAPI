using System.Collections.Generic;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using CosmedBleLib.Extensions;

namespace CosmedBleLib.GattCommunication
{

    /// <summary>
    /// Represents a Gatt Communication Result
    /// </summary>
    public interface ICommunicationResult
    {
        /// <value>
        /// The protocol Error, in case an error occurred during communication
        /// </value>
        byte? ProtocolError { get; }

        /// <value>
        /// The communication status
        /// </value>
        CosmedGattCommunicationStatus Status { get; }
    }


    /// <summary>
    /// Represents a Characteristic Gatt Communication Result
    /// </summary>
    public interface ICharacteristicCommunicationResult : ICommunicationResult
    {
        /// <value>
        /// Gets the Characteristic property
        /// </value>
        GattCharacteristicProperties Property { get; }

        /// <value>
        /// Gets the Characteristics list
        /// </value>
        IReadOnlyList<GattCharacteristic> Characteristics { get; }
    }


    /// <summary>
    /// Represents a Service Gatt Communication Result
    /// </summary>
    public interface IServiceCommunicationResult : ICommunicationResult
    {
        /// <value>
        /// Gets the Services list
        /// </value>
        IReadOnlyList<GattDeviceService> Services { get; }
    }


}
