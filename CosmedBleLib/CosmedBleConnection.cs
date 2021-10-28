using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace CosmedBleLib
{
    public class CosmedBleConnection
    {

        #region Private fields

        public BluetoothLEDevice bluetoothLeDevice = null;
        private GattCharacteristic selectedCharacteristic;
        // Only one registered characteristic at a time.
        private GattCharacteristic registeredCharacteristic;
        private GattPresentationFormat presentationFormat;

        #endregion


        #region Properties

        public ulong BluetoothAddress { get; private set; }
        public BluetoothConnectionStatus ConnectionStatus { get; private set; }

        //removed because duplicate of BluetoothDeviceId.DeviceId
       // public string DeviceId { get; private set; }
        public string Name { get; private set; }
        public BluetoothLEAppearance Appearance { get; private set; }
        public BluetoothAddressType BluetoothAddressType { get; private set; }

        //inforations about device and pairing
        public DeviceInformation DeviceInformation { get; private set; }

        public DeviceAccessInformation DeviceAccessInformation { get; private set; }

        //device ID
        public BluetoothDeviceId BluetoothDeviceId { get; private set; }

    
        public bool WasSecureConnectionUsedForPairing { get; private set; }

        #endregion


        #region Constructors
        public CosmedBleConnection(BluetoothLEDevice bluetoothLeDevice)
        {
            this.bluetoothLeDevice = bluetoothLeDevice ?? throw new ArgumentNullException(nameof(bluetoothLeDevice));
        }

        public CosmedBleConnection(CosmedBleAdvertisedDevice advertisingDevice)
        {
            try
            {
                _ = SetBluetoothLEDeviceAsync(advertisingDevice.DeviceAddress);
            }
            catch(ArgumentNullException e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        public CosmedBleConnection(ulong deviceAddress)
        {
           _ = SetBluetoothLEDeviceAsync(deviceAddress);
        }

        private async Task SetBluetoothLEDeviceAsync(ulong deviceAddress)
        {
            IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
            this.bluetoothLeDevice = await task.AsTask();

            if (bluetoothLeDevice == null)
            {
                Console.WriteLine("no device found");
                return;
            }
                

            BluetoothAddress = bluetoothLeDevice.BluetoothAddress;
            ConnectionStatus = bluetoothLeDevice.ConnectionStatus;
            //DeviceId = bluetoothLeDevice.DeviceId;
            Name = bluetoothLeDevice.Name;
            Appearance = bluetoothLeDevice.Appearance;
            BluetoothAddressType = bluetoothLeDevice.BluetoothAddressType;
            DeviceInformation = bluetoothLeDevice.DeviceInformation;
            DeviceAccessInformation = bluetoothLeDevice.DeviceAccessInformation;
            BluetoothDeviceId = bluetoothLeDevice.BluetoothDeviceId;
            WasSecureConnectionUsedForPairing = bluetoothLeDevice.WasSecureConnectionUsedForPairing;
        }

        #endregion


        #region Pairing

        public async Task Pair()
        {
            if(DeviceInformation != null)
            {
                DevicePairingResult dpr = await DeviceInformation.Pairing.PairAsync().AsTask();
                Console.WriteLine("paring status: " + dpr.Status.ToString());
                Console.WriteLine("pairing protection level: " + dpr.ProtectionLevelUsed.ToString());
               
            }
        }

        #endregion


        #region Connection

        public async Task startConnectionAsync()
        {
            GattDeviceServicesResult result;
            try
            {
                result = await bluetoothLeDevice.GetGattServicesAsync().AsTask();
            }
            catch(Exception e)
            {
                Console.WriteLine("exception: " + e.Message);           
                return;
            }

            if(result.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> resultServices = result.Services;
                Console.WriteLine("iterating the services");
                foreach( var service in resultServices)
                {
                    Console.WriteLine("printing a service:");
                    GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync().AsTask();
                    Console.WriteLine("service handle: " + service.AttributeHandle);
                    Console.WriteLine("service uuid: " + service.Uuid.ToString());
                    Console.WriteLine("service device access information (da spacchettare): " + service.DeviceAccessInformation);
                    /*
                        dalla GattSession posso ottenere dati importanti come MaintainConnection, etc etc
                        public sealed class GattSession : IGattSession, IDisposable
                        {
                            public void Dispose();
                            [RemoteAsync]
                            public static IAsyncOperation<GattSession> FromDeviceIdAsync(BluetoothDeviceId deviceId);

                            public bool MaintainConnection { get; set; }
                            public bool CanMaintainConnection { get; }
                            public BluetoothDeviceId DeviceId { get; }
                            public ushort MaxPduSize { get; }
                            public GattSessionStatus SessionStatus { get; }

                            public event TypedEventHandler<GattSession, object> MaxPduSizeChanged;
                            public event TypedEventHandler<GattSession, GattSessionStatusChangedEventArgs> SessionStatusChanged;
                        }
                     * */
                    Console.WriteLine("service Gatt Session: " + service.Session);


                    if (resultCharacteristics.Status == GattCommunicationStatus.Success)
                    {
                        Console.WriteLine("iterating the characteristics:");
                        IReadOnlyList<GattCharacteristic> characteristics = resultCharacteristics.Characteristics;
                        int i = characteristics.Count;
                        
                        foreach(GattCharacteristic characteristic in characteristics)
                        {
                            Console.WriteLine(characteristic.UserDescription);
                            Console.WriteLine(characteristic.Uuid);
                            Console.WriteLine(characteristic.ToString());

                            CharacteristicCommunication(characteristic, result);
                        }
                    }
                    else if(resultCharacteristics.Status == GattCommunicationStatus.ProtocolError)
                    {
                        Console.WriteLine("protocol error");
                    }
                    else
                    {
                        Console.WriteLine("protocol status: " + GattCommunicationStatus.AccessDenied.ToString() + " or " + GattCommunicationStatus.Unreachable.ToString());
                    }
                }
            }
            else
            {
                var error = result.ProtocolError;
            }

 
        }

        private async Task CharacteristicCommunication(GattCharacteristic characteristic, GattDeviceServicesResult result)
        {
            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
            string advType = characteristic.Uuid.ToString();

            if (properties.HasFlag(GattCharacteristicProperties.Read))
            {
                // This characteristic supports reading from it.
                GattReadResult value = await characteristic.ReadValueAsync().AsTask();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    var reader = DataReader.FromBuffer(value.Value);
                    byte[] data = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(data);
                    // Utilize the data as needed

                    string dataContent = BitConverter.ToString(data); ;
                    Console.WriteLine(advType + " characteristic buffer: " + dataContent);

                    string result1 = System.Text.Encoding.UTF8.GetString(data);
                    Console.WriteLine(advType + " characteristic buffer UTF8: " + result1);

                    string result2 = System.Text.Encoding.ASCII.GetString(data);
                    Console.WriteLine(advType + " characteristic buffer ASCII: " + result2);

                    System.Text.UnicodeEncoding unicode = new System.Text.UnicodeEncoding();
                    String decodedString = unicode.GetString(data);
                    Console.WriteLine(advType + " characteristic buffer UTF16: " + decodedString);

                }
            }
            if (properties.HasFlag(GattCharacteristicProperties.Write))
            {
                // This characteristic supports writing to it.
                var writer = new DataWriter();
                // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                writer.WriteByte(0x01);

                GattCommunicationStatus value = await characteristic.WriteValueAsync(writer.DetachBuffer()).AsTask();
                if (value == GattCommunicationStatus.Success)
                {
                    // Successfully wrote to device

                }


            }
            if (properties.HasFlag(GattCharacteristicProperties.Notify))
            {
                // This characteristic supports subscribing to notifications.
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask();
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                }

                characteristic.ValueChanged += Characteristic_ValueChanged;

                void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
                {
                    // An Indicate or Notify reported that the value has changed.
                    var reader = DataReader.FromBuffer(args.CharacteristicValue);
                    // Parse the data however required.
                    byte[] data = new byte[reader.UnconsumedBufferLength];
                    reader.ReadBytes(data);
                    // Utilize the data as needed

                    string dataContent = BitConverter.ToString(data); ;
                    Console.WriteLine(advType + " characteristic buffer: " + dataContent);
                }
            }

            if (properties.HasFlag(GattCharacteristicProperties.Indicate))
            {
                // This characteristic supports subscribing to notifications.
                GattCommunicationStatus status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Indicate).AsTask();
                if (status == GattCommunicationStatus.Success)
                {
                    // Server has been informed of clients interest.
                }

                characteristic.ValueChanged += Characteristic_ValueChanged;

                void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
                {
                    // An Indicate or Notify reported that the value has changed.
                    var reader = DataReader.FromBuffer(args.CharacteristicValue);
                    // Parse the data however required.
                }
            }

            /*
                    public enum GattCharacteristicProperties : uint
                    {
                        None = 0,
                        Broadcast = 1,
                        Read = 2,
                        WriteWithoutResponse = 4,
                        Write = 8,
                        Notify = 16,
                        Indicate = 32,
                        AuthenticatedSignedWrites = 64,
                        ExtendedProperties = 128,
                        ReliableWrites = 256,
                        WritableAuxiliaries = 512
                    }
             */
        }

        #endregion


        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion
    }
}
