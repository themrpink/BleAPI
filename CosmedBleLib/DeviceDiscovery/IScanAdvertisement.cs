using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace CosmedBleLib
{
    public interface IScanAdvertisement
    {
        IReadOnlyCollection<CosmedBleAdvertisedDevice> AllDiscoveredDevices { get; }
        IReadOnlyCollection<CosmedBleAdvertisedDevice> RecentlyDiscoveredDevices { get; }
        IReadOnlyCollection<CosmedBleAdvertisedDevice> LastDiscoveredDevices { get; }

     
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, CosmedBleAdvertisedDevice> NewDeviceDiscovered;
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;
        event Action<CosmedBluetoothLEAdvertisementWatcher, Exception> ScanInterrupted;
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
        event Action ScanModeChanged;

        Task StartPassiveScanning();
        Task StartActiveScanning();
        void StopScanning();
        void PauseScanning();
        void ResumeScanning();

    }

}
