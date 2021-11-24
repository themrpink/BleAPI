using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CosmedBleLib
{
    public interface ICommunicationResult
    {
        byte? ProtocolError { get; }
        CosmedGattCommunicationStatus Status { get; }
    }

    public interface ICharacteristicCommunicationResult : ICommunicationResult
    {
        GattCharacteristicProperties Property { get; }
        IReadOnlyList<GattCharacteristic> Characteristics { get; }
    }

    public interface IServiceCommunicationResult : ICommunicationResult
    {
        IReadOnlyList<GattDeviceService> Services { get; }
    }


}
