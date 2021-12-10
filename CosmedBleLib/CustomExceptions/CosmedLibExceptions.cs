using System;


namespace CosmedBleLib.CustomExceptions
{
    /// <summary>
    /// Thrown when an attempt of communication with the adapter fails
    /// </summary>
    [Serializable]
    public class BluetoothAdapterCommunicationFailureException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BluetoothAdapterCommunicationFailureException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public BluetoothAdapterCommunicationFailureException(string message) : base(message) { }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public BluetoothAdapterCommunicationFailureException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected BluetoothAdapterCommunicationFailureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// Thrown when an error during communication through the Gatt occurs
    /// </summary>
    [Serializable]
    public class GattCommunicationException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public GattCommunicationException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public GattCommunicationException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public GattCommunicationException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected GattCommunicationException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    
    /// <summary>
    /// Thrown when the connection with the device generate an error
    /// </summary>
    [Serializable]
    public class BleDeviceConnectionException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BleDeviceConnectionException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public BleDeviceConnectionException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public BleDeviceConnectionException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected BleDeviceConnectionException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// Thrown when the scan is aborted
    /// </summary>
    [Serializable]
    public class ScanAbortedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ScanAbortedException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public ScanAbortedException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public ScanAbortedException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected ScanAbortedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown when the Ble is not supported
    /// </summary>
    [Serializable]
    public class BluetoothLeNotSupportedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BluetoothLeNotSupportedException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public BluetoothLeNotSupportedException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public BluetoothLeNotSupportedException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected BluetoothLeNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown when central role is not supported
    /// </summary>
    [Serializable]
    public class CentralRoleNotSupportedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CentralRoleNotSupportedException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public CentralRoleNotSupportedException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public CentralRoleNotSupportedException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected CentralRoleNotSupportedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// Thrown when pairing causes an error.
    /// </summary>
    [Serializable]
    public class BlePairingException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public BlePairingException() : base() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public BlePairingException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Error messsage</param>
        /// <param name="inner">Wrapper exception</param>
        public BlePairingException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context></param>
        protected BlePairingException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
