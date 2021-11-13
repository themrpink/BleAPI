using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmedBleLib
{
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


    public class ConnectionProcess
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




    /*
     * potrei avere : o due oggetti diversi che implementano un interfaccia comune, sia per l´oggetto
     * pieno che per quello vuoto
     * se ci sono eccezioni ? 
     * allora se raccolgo l´oggetto con una eccezione potrebbe non essere male.
     * 
     * 
     * no interfaccia no.
     * 
     * oggetto + exception
     * potrei fare una carrellata di exceptions e poi restituirle? no non ha senso, ne prendo solo una.
     * quindi
     * connection proccess ha una connection, il device e le exceptions. può sempre far comodo.
     * forse anche il watcher, così da poter gestire le chiamate se serve.
     * quindi
    
     * 
     * 
     * 
     */
}

