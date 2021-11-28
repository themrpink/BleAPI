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

        protected BluetoothAdapterCommunicationFailureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }



    [Serializable]
    public class GattCommunicationException : Exception
    {
        public GattCommunicationException() : base() { }
        public GattCommunicationException(string message) : base(message) { }
        public GattCommunicationException(string message, Exception inner) : base(message, inner) { }
      
        protected GattCommunicationException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    

    [Serializable]
    public class BleDeviceConnectionException : Exception
    {
        public BleDeviceConnectionException() : base() { }
        public BleDeviceConnectionException(string message) : base(message) { }
        public BleDeviceConnectionException(string message, Exception inner) : base(message, inner) { }

        protected BleDeviceConnectionException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ScanAbortedException : Exception
    {
        public ScanAbortedException() : base() { }
        public ScanAbortedException(string message) : base(message) { }
        public ScanAbortedException(string message, Exception inner) : base(message, inner) { }

        protected ScanAbortedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class BluetoothLeNotSupportedException : Exception
    {
        public BluetoothLeNotSupportedException() : base() { }
        public BluetoothLeNotSupportedException(string message) : base(message) { }
        public BluetoothLeNotSupportedException(string message, Exception inner) : base(message, inner) { }

        protected BluetoothLeNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CentralRoleNotSupportedException : Exception
    {
        public CentralRoleNotSupportedException() : base() { }
        public CentralRoleNotSupportedException(string message) : base(message) { }
        public CentralRoleNotSupportedException(string message, Exception inner) : base(message, inner) { }

        protected CentralRoleNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class BlePairingException : Exception
    {
        public BlePairingException() : base() { }
        public BlePairingException(string message) : base(message) { }
        public BlePairingException(string message, Exception inner) : base(message, inner) { }

        protected BlePairingException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
