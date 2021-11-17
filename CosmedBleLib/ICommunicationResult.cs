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
        GattCharacteristicProperties Property { get; }
        byte? ProtocolError { get; }
        CosmedGattCommunicationStatus Status { get; }  
    }
}
