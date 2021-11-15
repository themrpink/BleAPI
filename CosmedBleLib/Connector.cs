using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace CosmedBleLib
{

    /*
     * per permettere di disconnettere, questo deve avere sia il gattResult, che la gattSession, che l´adapter, che il bleDevice
     * e poi alla fine, con la disconnessione, ripulire tutto. Forse il dizionario degli eventi potrebbe tenerlo lui e gestirselo in qualche modo
     * 
     */
    public static class Connector
    {

        private static CosmedBleConnectedDevice connectedDevice;

        public static async Task<ConnectionProcess> StartConnectionProcessAsync(CosmedBluetoothLEAdvertisementWatcher watcher, CosmedBleAdvertisedDevice advDevice)
        {
            CosmedBleConnectedDevice connectedDeviceTemp = null;
            ConnectionProcess connectionProcess;
            try
            { 
                watcher.StopScanning();

                ulong deviceAddress = advDevice.DeviceAddress;

                connectedDeviceTemp = await CosmedBleConnectedDevice.CreateAsync(deviceAddress);

                connectionProcess = new ConnectionProcess(connectedDeviceTemp, watcher, advDevice);

                //if (connectedDevice != null)
                //{
                //    connectedDevice.BluetoothLeDevice?.Dispose();
                //    connectedDevice.GattSession?.Dispose();
                //}

                connectedDevice = connectedDeviceTemp;   
            }
            catch (Exception e)
            {
                connectionProcess = new ConnectionProcess(connectedDeviceTemp, watcher, advDevice, e);
            }
            return connectionProcess ;
        }
    }



    public sealed class ConnectionProcess
    {
        public CosmedBleConnectedDevice device { get; private set; }
        public Exception exception { get; private set; }
        public CosmedBluetoothLEAdvertisementWatcher watcher { get; private set; }
        public CosmedBleAdvertisedDevice advDevice { get; private set; }

        public ConnectionProcess(CosmedBleConnectedDevice device, CosmedBluetoothLEAdvertisementWatcher watcher, CosmedBleAdvertisedDevice advDevice)
        {
            this.device = device;
            this.watcher = watcher;
            this.advDevice = advDevice;
        }

        public ConnectionProcess(CosmedBleConnectedDevice device, CosmedBluetoothLEAdvertisementWatcher watcher, CosmedBleAdvertisedDevice advDevice, Exception exception) : this(device, watcher, advDevice)
        {
            this.exception = exception;
        }
    }



    public class ConnectionBuilder
    {
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private CosmedBleAdvertisedDevice advDevice;
        private CosmedBleDevice bleDevice;

        private DevicePairingKinds ceremonySelection = DevicePairingKinds.None |
                                                        DevicePairingKinds.ConfirmOnly |
                                                        DevicePairingKinds.ConfirmPinMatch |
                                                        DevicePairingKinds.DisplayPin |
                                                        DevicePairingKinds.ProvidePasswordCredential |
                                                        DevicePairingKinds.ProvidePin;

        private DevicePairingProtectionLevel minProtectionLevel = DevicePairingProtectionLevel.None |
                                                                         DevicePairingProtectionLevel.Default |
                                                                         DevicePairingProtectionLevel.Encryption |
                                                                         DevicePairingProtectionLevel.EncryptionAndAuthentication;
      
        public ConnectionBuilder(CosmedBluetoothLEAdvertisementWatcher watcher, CosmedBleAdvertisedDevice advDevice)
        {
            this.watcher = watcher;
            this.advDevice = advDevice;
        }


        public async Task<CosmedBleDevice> GetConnectableDevice()
        {
            watcher?.StopScanning();
            bleDevice = await CosmedBleDevice.CreateAsync(advDevice);
            return bleDevice;
        }


        public async Task<PairedDevice> TryPairDevice()
        {
            GattLocalCharacteristic a;
            watcher?.StopScanning();
            return await PairingService.GetPairedDevice(bleDevice, ceremonySelection, minProtectionLevel);
        }


    }



}

