using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmedBleLib
{
    public interface ICommunicationResult
    {
        byte? ProtocolError { get; }

        CosmedGattCommunicationStatus Status { get; }  
    }
}
