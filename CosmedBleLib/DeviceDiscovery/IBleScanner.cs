using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace CosmedBleLib.DeviceDiscovery
{

    /// <summary>
    /// Reprensents and advertisements scanner
    /// </summary>
    public interface IBleScanner
    {
        /// <summary>
        /// Contains all the discovered devices since the scan has been started.
        /// </summary>
        IReadOnlyCollection<ICosmedBleAdvertisedDevice> AllDiscoveredDevices { get; }

        /// <summary>
        /// Contains the recently discovered devices since the scan has been started.
        /// </summary>
        IReadOnlyCollection<ICosmedBleAdvertisedDevice> RecentlyDiscoveredDevices { get; }

        /// <summary>
        /// Contains the last discovered devices since the scan has been started.
        /// </summary>
        IReadOnlyCollection<ICosmedBleAdvertisedDevice> LastDiscoveredDevices { get; }

        /// <summary>
        /// then amount of time after which a device in RecentlyDiscoveredDevices can be updated.
        /// The default value is 10 seconds.
        /// <remarks>Time is expressed in seconds</remarks>
        /// </summary>
        double TimeoutSeconds { get; set; }

        /// <summary>
        /// Check if the filter is active. Filter is active if is has been set to true.
        /// Default value is false.
        /// </summary>
        bool IsFilteringActive { get; }


        /// <summary>
        /// The actual scanning mode. It can be active, passive or none.
        /// </summary>
        BluetoothLEScanningMode ScanningMode { get; } 


        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, ICosmedBleAdvertisedDevice> NewDeviceDiscovered;

        /// <summary>
        /// Fired when the scan is stopped or aborted
        /// </summary>
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;

        /// <summary>
        /// Fired when the scan is interrupted
        /// </summary>
        event Action<CosmedBluetoothLEAdvertisementWatcher, Exception> ScanInterrupted;

        /// <summary>
        /// Fired when the watcher starts listening
        /// </summary>
        event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;

        /// <summary>
        /// Fired when the scanning mode has been changed
        /// </summary>
        event Action ScanModeChanged;


        /// <summary>
        /// Starts a passive scanning.
        /// </summary>
        /// <returns>Task</returns>
        Task StartPassiveScanning();

        /// <summary>
        /// Starts an active scan.
        /// </summary>
        /// <returns>Task</returns>
        Task StartActiveScanning();

        /// <summary>
        /// Stops a started scan.
        /// </summary>
        void StopScanning();

        /// <summary>
        /// Pauses a started scan.
        /// </summary>
        void PauseScanning();

        /// <summary>
        /// Resume a paused scan.
        /// </summary>
        void ResumeScanning();


        /// <summary>
        /// Add a filter to the watcher, starting a filtered scan
        /// </summary>
        /// <param name="filter">The filter object</param>
        void SetFilter(IFilter filter);

        /// <summary>
        /// Remove the filter from the watcher, starting an unfiltered scan
        /// </summary>
        void RemoveFilter();

        /// <summary>
        /// gets the actual state of the watcher
        /// </summary>
        StateMachine status { get; }

       
    }


}
