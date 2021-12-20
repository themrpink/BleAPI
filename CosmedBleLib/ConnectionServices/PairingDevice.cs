using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Credentials;
using CosmedBleLib.CustomExceptions;
using CosmedBleLib.DeviceDiscovery;
using CosmedBleLib.Collections;

namespace CosmedBleLib.ConnectionServices
{
    ///// <summary>
    ///// This interface represents a remote Ble device
    ///// </summary>
    //public interface ICosmedBleDevice
    //{

    //}


    /// <summary>
    /// Wraps a discoverable BluetoothLEDevice, showing tha data obtained from an unpaired connection
    /// </summary>
    public class CosmedBleDevice : ICosmedBleDevice
    {

        private BluetoothLEDevice bluetoothLeDevice;


        #region Public Properties
        /// <summary>
        /// Gets the instance of the device
        /// </summary>
        public BluetoothLEDevice BluetoothLeDevice { get { return bluetoothLeDevice; } }

        /// <summary>
        /// Gets the device address
        /// </summary>
        public ulong BluetoothAddress { get { return bluetoothLeDevice.BluetoothAddress; } }

        /// <summary>
        /// Gets the device name
        /// </summary>
        public string Name { get { return bluetoothLeDevice.Name; } }

        /// <summary>
        /// Gets the Bluetooth LE apperance value. 
        /// For category convertion <see cref="CosmedBleLib.Values.BluetoothAppearanceType"/>
        /// </summary>
        public BluetoothLEAppearance Appearance { get { return bluetoothLeDevice.Appearance; } }

        /// <summary>
        /// Gets the Bluetooth address type (public, random, unspecified).
        /// </summary>
        public BluetoothAddressType BluetoothAddressType { get { return bluetoothLeDevice.BluetoothAddressType; } }

        //inforations about device and pairing
        /// <summary>
        /// Gets the device information. From this object also pairing methods can be accessed.
        /// </summary>
        public DeviceInformation DeviceInformation { get { return bluetoothLeDevice.DeviceInformation; } }

        /// <summary>
        /// Gets the device access information
        /// </summary>
        public DeviceAccessInformation DeviceAccessInformation { get { return bluetoothLeDevice.DeviceAccessInformation; } }

        /// <summary>
        /// Gets a string indicating the device ID
        /// </summary>
        public string DeviceId { get { return bluetoothLeDevice.DeviceId; } }

        /// <summary>
        /// Gets a boolean indicating if the device is a Bluetooth Low Energy
        /// </summary>
        public bool IsLowEnergyDevice { get { return bluetoothLeDevice.BluetoothDeviceId.IsLowEnergyDevice; } }

        /// <summary>
        /// Gets a boolean indicating if the device is connected
        /// </summary>
        public bool IsConnected { get { return bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected; } }

        /// <summary>
        /// Gets a boolean indicating if the device can be paired
        /// </summary>
        public bool CanPair { get { return bluetoothLeDevice.DeviceInformation.Pairing.CanPair; } }

        /// <summary>
        /// Gets a boolean indicating if the device is paired
        /// </summary>
        public bool IsPaired { get { return bluetoothLeDevice.DeviceInformation.Pairing.IsPaired; } }

        #endregion


        #region Public events

        /// <summary>
        /// Fired when the remote device access changes
        /// </summary>
        public event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> AccessChanged;

        /// <summary>
        /// Fired when the connection status of ble remote device changes
        /// </summary>
        public event TypedEventHandler<CosmedBleDevice, object> ConnectionStatusChanged;

        /// <summary>
        /// Fired when the name of the ble remote device changes
        /// </summary>
        public event TypedEventHandler<CosmedBleDevice, object> NameChanged;
        #endregion


        #region Private Event handlers


        //this handler is subscribed by the class constructor to the underlying and not accessible NameChanged event.
        //When this is fired, its action is to fire the public NameChanged event accessible to the user, to which
        //the user can subscribe
        private void AccessChangedHanlder(DeviceAccessInformation accessInformation, DeviceAccessChangedEventArgs args)
        {
            AccessChanged?.Invoke(accessInformation, args);
        }

        //this handler is subscribed by the class constructor to the underlying and not accessible NameChanged event.
        //When this is fired, its action is to fire the public NameChanged event accessible to the user, to which
        //the user can subscribe
        private void ConnectionStatusChangedHandler(BluetoothLEDevice device, object args)
        {
            ConnectionStatusChanged?.Invoke(this, args);
        }


        //this handler is subscribed by the class constructor to the underlying and not accessible NameChanged event.
        //When this is fired, its action is to fire the public NameChanged event accessible to the user, to which
        //the user can subscribe
        private void NameChangedHandler(BluetoothLEDevice device, object args)
        {
            NameChanged?.Invoke(this, args);
        }

        //for test purposes
        private void setHandlers()
        {
            AccessChanged += (s, a) => Console.WriteLine("access changed: " + s.CurrentStatus);
            NameChanged += (s, a) => Console.WriteLine("-------------------------------name changed: " + s.Name);
            ConnectionStatusChanged += (s, a) =>
            {
                Console.WriteLine("------------------------");
                Console.WriteLine("new device (" + s.Name + ") connection status: " + bluetoothLeDevice?.ConnectionStatus.ToString());
                Console.WriteLine("------------------------");
            };

        }
        #endregion


        #region Constructors

        //constructor
        private CosmedBleDevice()
        {

        }


        //instantiate all the fields and properties of the class
        private async Task InitializeAsync(ulong deviceAddress)
        {
            try
            {
                // Verify: BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromBluetoothAddressAsync(deviceAddress);
                this.bluetoothLeDevice = await task.AsTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device", e);
            }


            if (bluetoothLeDevice == null)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device: device " + deviceAddress + " is null");
            }


            bluetoothLeDevice.DeviceAccessInformation.AccessChanged += AccessChangedHanlder;
            bluetoothLeDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged += NameChangedHandler;

            //this is for test purpose, the user can implement his own method
#if DEBUG
            setHandlers();
#endif


        }


        //instantiate all the fields and properties of the class
        private async Task InitializeAsync(string deviceId)
        {
            try
            {
                // Verificare: BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                IAsyncOperation<BluetoothLEDevice> task = BluetoothLEDevice.FromIdAsync(deviceId);
                this.bluetoothLeDevice = await task.AsTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device", e);
            }


            if (bluetoothLeDevice == null)
            {
                throw new BleDeviceConnectionException("Impossible to connect to device: device " + deviceId + " is null");
            }

            bluetoothLeDevice.DeviceAccessInformation.AccessChanged += AccessChangedHanlder;
            bluetoothLeDevice.ConnectionStatusChanged += ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged += NameChangedHandler;

            //this is for test purpose, the user can implement his own method
            setHandlers();

        }


        /// <summary>
        /// Creates and instance of the class from a device address.
        /// </summary>
        /// <param name="deviceAddress">The device address.</param>
        /// <returns>An instance of the class</returns>
        public static async Task<CosmedBleDevice> CreateAsync(ulong deviceAddress)
        {
            var device = new CosmedBleDevice();
            await device.InitializeAsync(deviceAddress);
            return device;
        }


        /// <summary>
        /// Creates and instance of the class from a device ID.
        /// </summary>
        /// <param name="deviceId">The device ID.</param>
        /// <returns>An instance of the class</returns>
        public static async Task<CosmedBleDevice> CreateAsync(string deviceId)
        {
            var device = new CosmedBleDevice();
            await device.InitializeAsync(deviceId);
            return device;
        }


        /// <summary>
        /// Creates and instance of the class from a advertising Device.
        /// </summary>
        /// <param name="advertisingDevice">The advertising Device.</param>
        /// <returns>An instance of the class</returns>
        public static async Task<CosmedBleDevice> CreateAsync(ICosmedBleAdvertisedDevice advertisingDevice)
        {
            if (advertisingDevice == null)
            {
                throw new ArgumentNullException("parameter cannot be null");
            }

            var device = new CosmedBleDevice();
            await device.InitializeAsync(advertisingDevice.DeviceAddress);
            return device;
        }


        /// <summary>
        /// Update the device with updated status and data
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns>void Task</returns>
        public async Task UpdateDeviceAsync(string deviceId)
        {
            bluetoothLeDevice.ConnectionStatusChanged -= ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged -= NameChangedHandler;
            bluetoothLeDevice.DeviceAccessInformation.AccessChanged -= AccessChangedHanlder;
            await this.InitializeAsync(deviceId);
        }

        #endregion


        #region Dispose device

        /// <summary>
        /// Diposes and clears all the elements, collections and events related to the device
        /// </summary>
        public void ClearBluetoothLEDevice()
        {
            // Need to clear the CCCD from the remote device to stop receiving notifications
            List<Task<GattCharacteristic>> removals = GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions.Where(c => c.Key.Service.Session.DeviceId.Equals(bluetoothLeDevice.DeviceId)).Select(async d =>
            {
                var result = await d.Key.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result == GattCommunicationStatus.Success)
                {
                    d.Key.ValueChanged -= d.Value;
                }
                return d.Key;

            }).ToList();

            TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> eventHandler;
            foreach (var r in removals)
            {
                var result = GattCharacteristicEventsCollector.CharacteristicsChangedSubscriptions.TryRemove(r.Result, out eventHandler);
            }

            bluetoothLeDevice.ConnectionStatusChanged -= ConnectionStatusChangedHandler;
            bluetoothLeDevice.NameChanged -= NameChangedHandler;
            bluetoothLeDevice.DeviceAccessInformation.AccessChanged -= AccessChangedHanlder;

            bluetoothLeDevice?.Dispose();
            bluetoothLeDevice = null;

            GC.Collect();
        }

        #endregion

    }


    /// <summary>
    /// This class contains the data resulting from a pairing attempt.
    /// </summary>
    public sealed class PairingResult
    {

        /// <summary>
        /// Gets a boolean indicating if a secure connection war used for pairing
        /// </summary>
        public bool WasSecureConnectionUsedForPairing { get; private set; }

        /// <summary>
        /// Gets the Protection level used for pairing
        /// </summary>
        public DevicePairingProtectionLevel ProtectionLevelUsed { get; private set; }

        /// <summary>
        /// Gets the pairing result status
        /// </summary>
        public DevicePairingResultStatus PairingResultStatus { get; private set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="protectionLevelUsed">Protection level</param>
        /// <param name="pairingResultStatus">Pairing result status</param>
        /// <param name="wasSecureConnectionUsedForPairing">Boolean for secure connection usage</param>
        public PairingResult(DevicePairingProtectionLevel protectionLevelUsed, DevicePairingResultStatus pairingResultStatus, bool wasSecureConnectionUsedForPairing)
        {
            ProtectionLevelUsed = protectionLevelUsed;
            PairingResultStatus = pairingResultStatus;
            WasSecureConnectionUsedForPairing = wasSecureConnectionUsedForPairing;
        }
    }



    /// <summary>
    /// The class offers the methods for pairing to a remote Ble device
    /// </summary>
    public static class PairingService
    {

        /// <summary>
        /// Gets the most generic DevicePairingKinds to be used as default in the pairing ´process. Windows 10 
        /// will apply the most secure pairing option available on both devices
        /// </summary>
        public static DevicePairingKinds CeremonySelection { get; } =   DevicePairingKinds.None |
                                                                        DevicePairingKinds.ConfirmOnly |
                                                                        DevicePairingKinds.ConfirmPinMatch |
                                                                        DevicePairingKinds.DisplayPin |
                                                                        DevicePairingKinds.ProvidePasswordCredential |
                                                                        DevicePairingKinds.ProvidePin;

        /// <summary>
        /// Gets the most generic DevicePairingProtectionMethod to be used as default in the pairing ´process. 
        /// Windows 10 will apply the most secure pairing option available on both devices
        /// </summary>
        public static DevicePairingProtectionLevel MinProtectionLevel { get; } = DevicePairingProtectionLevel.None |
                                                                                 DevicePairingProtectionLevel.Default |
                                                                                 DevicePairingProtectionLevel.Encryption |
                                                                                 DevicePairingProtectionLevel.EncryptionAndAuthentication;


        //this method, used as default handler to manage the pairing process should be implemented by the user and passed 
        //to the GetPairedDevice method that accept an event as argument
        private static void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    Console.WriteLine("ok presse enter to accept pairing");
                    Console.ReadLine();
                    args.Accept();
                    break;

                case DevicePairingKinds.DisplayPin:
                    // We just show the PIN on this side. The ceremony is actually completed when the user enters the PIN
                    // on the target device. We automatically accept here since we can't really "cancel" the operation
                    // from this side.
                    Console.WriteLine("ok presse enter to accept pin " + args.Pin);
                    Console.ReadLine();
                    args.Accept();

                    // No need for a deferral since we don't need any decision from the user
                    Console.WriteLine("Please enter this PIN on the device you are pairing with: " + args.Pin, args.PairingKind);
                    Console.ReadLine();
                    break;

                case DevicePairingKinds.ProvidePin:
                    // A PIN may be shown on the target device and the user needs to enter the matching PIN on
                    // this Windows device. Get a deferral so we can perform the async request to the user.
                    var collectPinDeferral = args.GetDeferral();

                    Console.WriteLine("Please enter the PIN shown on the device you're pairing with");
                    string pin = Console.ReadLine();
                    if (!string.IsNullOrEmpty(pin))
                    {
                        args.Accept(pin);
                    }

                    collectPinDeferral.Complete();

                    break;

                case DevicePairingKinds.ProvidePasswordCredential:

                    var collectCredentialDeferral = args.GetDeferral();
                    Console.WriteLine("insert username");
                    string username = Console.ReadLine();
                    Console.WriteLine("insert password");
                    string password = Console.ReadLine();
                    var credential = new PasswordCredential() { UserName = username, Password = password };
                    if (credential != null)
                    {
                        args.AcceptWithPasswordCredential(credential);
                    }
                    collectCredentialDeferral.Complete();

                    break;

                case DevicePairingKinds.ConfirmPinMatch:
                    // We show the PIN here and the user responds with whether the PIN matches what they see
                    // on the target device. 
                    var displayMessageDeferral = args.GetDeferral();
                    Console.WriteLine("pin: " + args.Pin);
                    Console.WriteLine("does the pin matches? Y/N");
                    string answer = Console.ReadLine();
                    while (!answer.ToLower().Equals("y") && !answer.ToLower().Equals("n"))
                    {
                        Console.WriteLine("please answer y or n");
                        answer = Console.ReadLine();
                    }
                    if (answer.ToLower().Equals("y"))
                    {
                        args.Accept();
                    }
  

                    displayMessageDeferral.Complete();
                    break;
            }
            
        }


        /// <summary>
        /// Attempts a pairing with the requested device, applying the most secure of the compatible requested protections.
        /// The pairing interaction is hanlded by the default method. A custom method should be used and passed to the overload
        /// version of this method.
        /// </summary>
        /// <param name="device">The device to pair with</param>
        /// <param name="ceremonySelection">The ceremony selection type</param>
        /// <param name="minProtectionLevel">The minimum protection level requested</param>
        /// <returns>The pairing result object</returns>
        public static async Task<PairingResult> PairDevice(ICosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel)
        {
            //saves the device information
            DeviceInformation deviceInformation = device.BluetoothLeDevice.DeviceInformation;
            try
            {
                //create a new device from deviceId (necessary to recognize previously paired devices)
                var bledevice = device.BluetoothLeDevice; // await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);

                //try the pairing process
                bledevice.DeviceInformation.Pairing.Custom.PairingRequested += PairingRequestedHandler;
                DevicePairingResult devicePairingResult = await bledevice.DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                bledevice.DeviceInformation.Pairing.Custom.PairingRequested -= PairingRequestedHandler;

                string deviceId = deviceInformation.Id;

                //update the device 
                await device.UpdateDeviceAsync(deviceId);

                //bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);
                ////update the device 
                //device.bluetoothLeDevice = bledevice;

                return new PairingResult(devicePairingResult.ProtectionLevelUsed, devicePairingResult.Status, bledevice.WasSecureConnectionUsedForPairing);
            }
            catch (Exception e)
            {
                //system exceptions
                throw;
            }
        }


        /// <summary>
        /// Attempts a pairing with the requested device, applying the most secure of the compatible requested protections.
        /// The pairing interaction is hanlded by the custom event handler passed by the user.
        /// </summary>
        /// <param name="device">The device to pair with</param>
        /// <param name="ceremonySelection">The ceremony selection type</param>
        /// <param name="minProtectionLevel">The minimum protection level requested</param>
        /// <param name="eventHandler">the event handler used to manage the pairing process</param>
        /// <returns>The pairing result object</returns>
        public static async Task<PairingResult> PairDevice(ICosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel, TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> eventHandler)
        {
            DeviceInformation deviceInformation = device.BluetoothLeDevice.DeviceInformation;
            try
            {
                //create a new device from deviceId (necessary to recognize previously paired devices)
                var bledevice = device.BluetoothLeDevice;

                //try the pairing process
                deviceInformation.Pairing.Custom.PairingRequested += eventHandler;
                DevicePairingResult devicePairingResult = await bledevice.DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                deviceInformation.Pairing.Custom.PairingRequested -= eventHandler;

                string deviceId = deviceInformation.Id; 
                
                //update the device 
                await device.UpdateDeviceAsync(deviceId);

                //bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);
                //device.bluetoothLeDevice = bledevice;

                return new PairingResult(devicePairingResult.ProtectionLevelUsed, devicePairingResult.Status, bledevice.WasSecureConnectionUsedForPairing);
            }
            catch (Exception e)
            {
                throw;
            }
        }


        /// <summary>
        /// Unpairs the devices
        /// </summary>
        /// <param name="device">Device to unpair</param>
        /// <returns>The unpair result</returns>
        public async static Task<DeviceUnpairingResult> Unpair(ICosmedBleDevice device)
        {
            try
            {
                //using (device.bluetoothLeDevice)
                //{
                    DeviceUnpairingResult unpairResult = await device.BluetoothLeDevice.DeviceInformation.Pairing.UnpairAsync();
                    return unpairResult;
                //}

            }
            catch (Exception e)
            {
                throw;
            }
        }
    }



    
}
