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
        /// <summary>
        /// The protocol Error, in case an error occurred during communication
        /// </summary>
        byte? ProtocolError { get; }

        /// <summary>
        /// The communication status
        /// </summary>
        CosmedGattCommunicationStatus Status { get; }
    }


    /// <summary>
    /// Represents a Characteristic Gatt Communication Result
    /// </summary>
    public interface ICharacteristicCommunicationResult : ICommunicationResult
    {
        /// <summary>
        /// Gets the Characteristic property
        /// </summary>
        GattCharacteristicProperties Property { get; }

        /// <summary>
        /// Gets the Characteristics list
        /// </summary>
        IReadOnlyList<GattCharacteristic> Characteristics { get; }
    }


    /// <summary>
    /// Represents a Service Gatt Communication Result
    /// </summary>
    public interface IServiceCommunicationResult : ICommunicationResult
    {
        /// <summary>
        /// Gets the Services list
        /// </summary>
        IReadOnlyList<GattDeviceService> Services { get; }
    }


}
