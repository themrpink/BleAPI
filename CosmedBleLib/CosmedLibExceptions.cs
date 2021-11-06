using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace CosmedBleLib
{

    [Serializable]
    public class BluetoothAdapterCommunicationFailureException : Exception
    {
        public BluetoothAdapterCommunicationFailureException() : base() { }
        public BluetoothAdapterCommunicationFailureException(string message) : base(message) { }
        public BluetoothAdapterCommunicationFailureException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected BluetoothAdapterCommunicationFailureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }



    [Serializable]
    public class GattCommunicationFailureException : Exception
    {
        public GattCommunicationFailureException() : base() { }
        public GattCommunicationFailureException(string message) : base(message) { }
        public GattCommunicationFailureException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected GattCommunicationFailureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    

    [Serializable]
    public class BleDeviceNotFoundException : Exception
    {
        public BleDeviceNotFoundException() : base() { }
        public BleDeviceNotFoundException(string message) : base(message) { }
        public BleDeviceNotFoundException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected BleDeviceNotFoundException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ScanAbortedException : Exception
    {
        public ScanAbortedException() : base() { }
        public ScanAbortedException(string message) : base(message) { }
        public ScanAbortedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected ScanAbortedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class BluetoothLeNotSupportedException : Exception
    {
        public BluetoothLeNotSupportedException() : base() { }
        public BluetoothLeNotSupportedException(string message) : base(message) { }
        public BluetoothLeNotSupportedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected BluetoothLeNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CentralRoleNotSupportedException : Exception
    {
        public CentralRoleNotSupportedException() : base() { }
        public CentralRoleNotSupportedException(string message) : base(message) { }
        public CentralRoleNotSupportedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected CentralRoleNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }



}
