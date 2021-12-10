using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace CosmedBleLib.ConnectionServices
{       


    public interface ICosmedBleDevice
    {
        /// <value>
        /// Ble remote device
        /// </value>
        BluetoothLEDevice BluetoothLeDevice { get; }


        /// <summary>
        /// Fired when the remote device access changes
        /// </summary>
        event TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> AccessChanged;

        /// <summary>
        /// Fired when the connection status of ble remote device changes
        /// </summary>
        event TypedEventHandler<CosmedBleDevice, object> ConnectionStatusChanged;

        /// <summary>
        /// Fired when the name of the ble remote device changes
        /// </summary>
        event TypedEventHandler<CosmedBleDevice, object> NameChanged;


        /// <summary>
        /// Update the device with updated status and data
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns>void Task</returns>
        Task UpdateDeviceAsync(string deviceId);


        /// <summary>
        /// Clear the device data and dispose the bluetooth le device object
        /// </summary>
        void ClearBluetoothLEDevice();
    }

}