using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Credentials;

namespace CosmedBleLib
{

    //wrap a BluetoothLEDevice, which is obtained with a unpaired connection (therefore the device is reachable, otherwise the object is null)
    public class CosmedBleDevice
    {
       
        private BluetoothLEDevice bluetoothLeDevice;


        #region Public Properties

        public BluetoothLEDevice BluetoothLeDevice { get { return bluetoothLeDevice; } set { bluetoothLeDevice = value; } }

        public ulong BluetoothAddress { get { return bluetoothLeDevice.BluetoothAddress; } }

        public string Name { get { return bluetoothLeDevice.Name; } }

        public BluetoothLEAppearance Appearance { get { return bluetoothLeDevice.Appearance; } }

        public BluetoothAddressType BluetoothAddressType { get { return bluetoothLeDevice.BluetoothAddressType; } }

        //inforations about device and pairing
        public DeviceInformation DeviceInformation { get { return bluetoothLeDevice.DeviceInformation; } }

        public DeviceAccessInformation DeviceAccessInformation { get { return bluetoothLeDevice.DeviceAccessInformation; } }

        //device ID
        public string DeviceId { get { return bluetoothLeDevice.DeviceId; } }

        public bool IsLowEnergyDevice {  get { return bluetoothLeDevice.BluetoothDeviceId.IsLowEnergyDevice; } }
        public bool IsConnected { get { return bluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected; } }

        public bool CanPair { get { return bluetoothLeDevice.DeviceInformation.Pairing.CanPair; } }

        public bool IsPaired { get { return bluetoothLeDevice.DeviceInformation.Pairing.IsPaired; } }

        #endregion


        #region Public events
        public event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> AccessChanged;
        public event TypedEventHandler<CosmedBleDevice, object> ConnectionStatusChanged;
        public event TypedEventHandler<CosmedBleDevice, object> NameChanged;
        #endregion


        #region Private Event handlers


        //by disposal these 3 handler shall be unsubscribed
        private void AccessChangedHanlder(DeviceAccessInformation accessInformation, DeviceAccessChangedEventArgs args)
        {
            AccessChanged?.Invoke(accessInformation, args);
        }

        //these ones call the public event, to which the user can subscribe
        private void ConnectionStatusChangedHandler(BluetoothLEDevice device, object args)
        {
            ConnectionStatusChanged?.Invoke(this, args);
        }

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
                Console.WriteLine("new device (" + s.Name +") connection status: " + bluetoothLeDevice?.ConnectionStatus.ToString());
                Console.WriteLine("------------------------");
            };

        }
        #endregion


        #region Constructors

        private CosmedBleDevice()
        {

        }
                 
        private async Task InitializeAsync(ulong deviceAddress)
        {
            try
            {
                // Verificare: BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
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
            setHandlers();

        }

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

        public static async Task<CosmedBleDevice> CreateAsync(ulong deviceAddress)
        {
            var device = new CosmedBleDevice();
            await device.InitializeAsync(deviceAddress);
            return device;
        }

        public static async Task<CosmedBleDevice> CreateAsync(string deviceId)
        {
            var device = new CosmedBleDevice();
            await device.InitializeAsync(deviceId);
            return device;
        }

        public static async Task<CosmedBleDevice> CreateAsync(CosmedBleAdvertisedDevice advertisingDevice)
        {
            if (advertisingDevice == null)
            {
                throw new ArgumentNullException("parameter cannot be null");
            }

            var device = new CosmedBleDevice();
            await device.InitializeAsync(advertisingDevice.DeviceAddress);
            return device;
        }


        #endregion


        #region Dispose device

        public void Disconnect()
        {
            ClearBluetoothLEDevice();
        }

        private void ClearBluetoothLEDevice()
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


    public sealed class PairingResult
    {
        public bool WasSecureConnectionUsedForPairing { get; private set; }

        public DevicePairingProtectionLevel ProtectionLevelUsed { get; private set; }

        public DevicePairingResultStatus PairingResultStatus { get; private set; }


        public PairingResult(DevicePairingProtectionLevel protectionLevelUsed, DevicePairingResultStatus pairingResultStatus, bool wasSecureConnectionUsedForPairing)
        {
            ProtectionLevelUsed = protectionLevelUsed;
            PairingResultStatus = pairingResultStatus;
            WasSecureConnectionUsedForPairing = wasSecureConnectionUsedForPairing;
        }
    }


    //take a CosmedBleDevice and try to pair it. If pairing succeeds the return a PairedDevice
    public static class PairingService
    {
        private static void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' where there is no Windows Consent UX, you may want to provide your own confirmation.
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
                    // on the target device. Response comes back and we set it on the PinComparePairingRequestedData
                    // then complete the deferral.
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

        //uses the default pairing management handler
        public static async Task<PairingResult> GetPairedDevice(CosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel)
        {
            //saves the device information
            DeviceInformation deviceInformation = device.DeviceInformation;
            try
            {
                //create a new device from deviceId (necessary to recognize previously paired devices)
                var bledevice = device.BluetoothLeDevice;

                //try the pairing process
                bledevice.DeviceInformation.Pairing.Custom.PairingRequested += PairingRequestedHandler;
                DevicePairingResult devicePairingResult = await bledevice.DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                bledevice.DeviceInformation.Pairing.Custom.PairingRequested -= PairingRequestedHandler;

                string deviceId = deviceInformation.Id; //I reuse did to reload later.
                bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);

                //update the device 
                device.BluetoothLeDevice = bledevice;
                
                return new PairingResult(devicePairingResult.ProtectionLevelUsed, devicePairingResult.Status, bledevice.WasSecureConnectionUsedForPairing);
            }
            ////saves the device information
            //DeviceInformation deviceInformation = device.DeviceInformation;

            ////saves the device id
            //string deviceId = deviceInformation.Id; //I reuse did to reload later.

            ////dispose the device
            //device.Disconnect();
            //device = null;

            //try
            //{
            //    //create a new device from deviceId (necessary to recognize previously paired devices)
            //    var bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);

            //    //try the pairing process
            //    bledevice.DeviceInformation.Pairing.Custom.PairingRequested += PairingRequestedHandler;
            //    DevicePairingResult devicePairingResult = await bledevice.DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
            //    bledevice.DeviceInformation.Pairing.Custom.PairingRequested -= PairingRequestedHandler;
            //    //this is needed to have an updated state (i.e. isPaired = true) ?? verificare con test precisi
            //    bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);

            //    //create a paired device with the pairing results
            //    return PairedDevice.Create(bledevice, devicePairingResult);
            //}
            catch (Exception e)
            {
                throw;
            }
        }

        //allows to pass a handler to manage pairing
        public static async Task<PairingResult> GetPairedDevice(CosmedBleDevice device, DevicePairingKinds ceremonySelection, DevicePairingProtectionLevel minProtectionLevel, TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> eventHandler)
        {
            DeviceInformation deviceInformation = device.DeviceInformation;
            try
            {
                //create a new device from deviceId (necessary to recognize previously paired devices)
                var bledevice = device.BluetoothLeDevice;

                //try the pairing process
                deviceInformation.Pairing.Custom.PairingRequested += eventHandler;
                DevicePairingResult devicePairingResult = await bledevice.DeviceInformation.Pairing.Custom.PairAsync(ceremonySelection, minProtectionLevel);
                deviceInformation.Pairing.Custom.PairingRequested -= eventHandler;

                string deviceId = deviceInformation.Id; //I reuse did to reload later.
                bledevice = await BluetoothLEDevice.FromIdAsync(deviceId);

                //update the device 
                device.BluetoothLeDevice = bledevice;

                return new PairingResult(devicePairingResult.ProtectionLevelUsed, devicePairingResult.Status, bledevice.WasSecureConnectionUsedForPairing);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async static Task<DeviceUnpairingResult> Unpair(CosmedBleDevice device)
        {
            try
            {
                //using (device.bluetoothLeDevice)
                //{
                    DeviceUnpairingResult unpairResult = await device.DeviceInformation.Pairing.UnpairAsync();
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
