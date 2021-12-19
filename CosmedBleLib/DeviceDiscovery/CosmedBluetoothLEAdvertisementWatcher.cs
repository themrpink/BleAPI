using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using System.Threading.Tasks;
using CosmedBleLib.CustomExceptions;
using CosmedBleLib.Adapter;

namespace CosmedBleLib.DeviceDiscovery
{

    /// <summary>
    /// Wrapper class for the BleAdvertisementWatcher, allows passive or active scanning and filtering
    /// </summary>
    public sealed class CosmedBluetoothLEAdvertisementWatcher : IBleScanner
    {
        //private ObservableCollection<CosmedBleAdvertisedDevice> KnownDevices = new ObservableCollection<CosmedBleAdvertisedDevice>();
        //private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();


        #region Private fields

        // used as safety mechanism after starting and stopping scan control to avoid states overlap
        private const int wait = 3000;

        // wrapped watcher
        private BluetoothLEAdvertisementWatcher watcher;

        //the structure where all the discovered devices are saved. It gets updated at every advertisement received
        private Dictionary<ulong, ICosmedBleAdvertisedDevice> discoveredDevices;

        //the structure where the last discovered devices are saved
        private Dictionary<ulong, ICosmedBleAdvertisedDevice> lastDiscoveredDevices;

        //filter to apply to the discovered devices
        private IFilter filter;

        //lock for safe access to shared collections:
        private readonly object devicesThreadLock = new object();

        //max number of discoverable devices before auto-cleaning of the dictionary
        private int maxScanResults = 128;

        //the last requested scanning mode, used for resuming the scan
        private BluetoothLEScanningMode lastScanningMode;

        #endregion


        #region Public events

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, ICosmedBleAdvertisedDevice> NewDeviceDiscovered;

        /// <summary>
        /// Fired when the scan is stopped or aborted
        /// </summary>
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;

        /// <summary>
        /// Fired when the scan is interrupted
        /// </summary>
        public event Action<CosmedBluetoothLEAdvertisementWatcher, Exception> ScanInterrupted;

        /// <summary>
        /// Fired when the watcher starts listening
        /// </summary>
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;

        /// <summary>
        /// Fired when the scanning mode has been changed
        /// </summary>
        public event Action ScanModeChanged;

        #endregion


        #region Properties

        /// <summary>
        /// gets the actual state of the watcher
        /// </summary>
        public StateMachine status { get; private set; }

        /// <summary>
        /// then amount of time after which a device in RecentlyDiscoveredDevices can be updated.
        /// The default value is 10 seconds.
        /// <remarks>Time is expressed in seconds</remarks>
        /// </summary>
        public double TimeoutSeconds { get; set; } = 10;


        /// <summary>
        /// Check if the filter is active. Filter is active if is has been set to true.
        /// Default value is false.
        /// </summary>
        public bool IsFilteringActive { get; private set; } = false;


        /// <summary>
        /// The actual scanning mode. It can be active, passive or none.
        /// </summary>
        public BluetoothLEScanningMode ScanningMode
        {
            get
            {
                return watcher.ScanningMode;
            }
        }


        /// <summary>
        /// If set to true, the watcher select only Connectable devices 
        /// </summary>
        public bool ShowOnlyConnectableDevices { get; set; } = true;


        /// <summary>
        /// Contains all the discovered devices since the scan has been started.
        /// The collection is created at every user request from the devices dictionary. 
        /// Thread safe.
        /// </summary>
        public IReadOnlyCollection<ICosmedBleAdvertisedDevice> AllDiscoveredDevices
        {
            get
            {
                lock (devicesThreadLock)
                {
                    return discoveredDevices.Values.Where(d => check(d)).ToList().AsReadOnly();
                }
            }
        }


        /// <summary>
        /// Contains the recently discovered devices since the scan has been started.
        /// The user can set the time interval though the <see cref="TimeoutSeconds"/>
        /// The collection is created at every user request from the devices dictionary. 
        /// Thread safe.
        /// </summary>
        public IReadOnlyCollection<ICosmedBleAdvertisedDevice> RecentlyDiscoveredDevices
        {
            get
            {
                lock (devicesThreadLock)
                {
                    return discoveredDevices.Values.Where
                        ((device) =>
                        {
                            if (TimeoutSeconds > 0)
                            {
                                var diff = DateTime.UtcNow - TimeSpan.FromSeconds(TimeoutSeconds);
                                return device.Timestamp >= diff && check(device);
                            }
                            return check(device);
                        }
                        ).ToList().AsReadOnly();
                }
            }
        }


        /// <summary>
        /// Contains only devices discovered since the last access to this collection and since 
        /// the scan has started.
        /// The user can set the time interval though the <see cref="TimeoutSeconds"/>
        /// The collection is created at every user request from the devices dictionary. 
        /// Thread safe.
        /// </summary>
        public IReadOnlyCollection<ICosmedBleAdvertisedDevice> LastDiscoveredDevices
        {
            get
            {
                lock (devicesThreadLock)
                {
                    IReadOnlyList<ICosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.Values.Where(d => check(d)).ToList().AsReadOnly();
                    lastDiscoveredDevices.Clear();
                    return lastDiscoveredDevicesTemp;
                }
            }
        }

        #endregion


        #region constructors
        /// <summary>
        /// Constructor: instatiate the watcher, the dictionary and add the internal handlers to private events of the watcher
        /// </summary>
        /// 
        public CosmedBluetoothLEAdvertisementWatcher()
        {
            discoveredDevices = new Dictionary<ulong, ICosmedBleAdvertisedDevice>();
            lastDiscoveredDevices = new Dictionary<ulong, ICosmedBleAdvertisedDevice>();
            status = StateMachine.Stopped;
            //questo va lasciato facoltativo
            ScanInterrupted += OnScanInterrupted;
        }




        /// <summary>
        /// Constructor setting the filter
        /// </summary>
        /// <param name="filter">The filter object</param>
        public CosmedBluetoothLEAdvertisementWatcher(IFilter filter) : this()
        {
            SetFilter(filter ?? throw new ArgumentNullException(nameof(filter)));
            IsFilteringActive = true;
            this.filter = filter;
        }



        #endregion


        #region Scanning methods


        /// <summary>
        /// Initialize and start a passive scanning. If a passive scan is already running it just keep it going.
        /// If an active scanning is running, it switches to passive scanning and the ScanModeChanged event is raised.
        /// </summary>
        public async Task StartPassiveScanning()
        {
            if (status == StateMachine.Started && !IsScanningPassive)
            {
                watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                ScanModeChanged?.Invoke();
            }
            else if (status == StateMachine.Stopped || status == StateMachine.Aborted)
            {
                status = StateMachine.Starting;
                scanInit();

                //set the passive scan and start a new scanning thread 
                watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                await scan();
            }
        }


        /// <summary>
        /// Initialize and start an active scanning. If an active scan is already running it just keep it going.
        /// If a passive scanning is running, it switches to active scanning and the ScanModeChanged event is raised.
        /// </summary>
        public async Task StartActiveScanning()
        {
            if (status == StateMachine.Started && !IsScanningActive)
            {
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                ScanModeChanged?.Invoke();
            }
            else if (status == StateMachine.Stopped || status == StateMachine.Aborted)
            {
                status = StateMachine.Starting;
                scanInit();

                //set the passive scan and start a new scanning thread 
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                await scan();
            }
        }


        /// <summary>
        /// Scanner initialization. The previously discovered devices, if present, are deleted
        /// </summary>
        private async void scanInit()
        {
            clearDiscoveredDevices(discoveredDevices);
            clearDiscoveredDevices(lastDiscoveredDevices);

            if (watcher == null)
            {
                if (!IsFilteringActive)
                {
                    watcher = new BluetoothLEAdvertisementWatcher();
                }
                else
                {
                    if (filter.AdvertisementFilter != null)
                    {
                        watcher = new BluetoothLEAdvertisementWatcher(filter.AdvertisementFilter);
                    }
                    else
                    {
                        watcher = new BluetoothLEAdvertisementWatcher();
                    }
                    if (filter.SignalStrengthFilter != null)
                    {
                        watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
                    }

                }

                watcher.Received += this.OnAdvertisementReceived;
                watcher.Stopped += this.OnScanStopped;
                checkFilterStatus();
            }

            //Add the extended advertising, if supported
            var adapter = await CosmedBluetoothLEAdapter.CreateAsync();
            bool isExtendedAdvertisementSupported = adapter.IsExtendedAdvertisingSupported;
            watcher.AllowExtendedAdvertisements = isExtendedAdvertisementSupported;
        }


        /// <summary>
        /// It starts the scan
        /// </summary>
        /// <returns></returns>
        private async Task scan()
        {
            try
            {
                if (status == StateMachine.Starting && await checkBLEAdapterError())
                {
                    if (watcher != null)
                    {
                        lastScanningMode = watcher.ScanningMode;
                        watcher.Start();
                        status = StateMachine.Started;

                        int count = 0;
                        while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started && count < wait)
                        {
                            count++;
                        }
                    }
                }

                StartedListening?.Invoke(this, watcher.ScanningMode);
            }
            catch (System.Exception e)
            {
                status = StateMachine.Aborted;
                await Task.Run(() => ScanInterrupted?.Invoke(this, new ScanAbortedException("scan aborted during initiation", e))).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// It stops the scanner. 
        /// </summary>
        public void StopScanning()
        {
            if (status == StateMachine.Started || status == StateMachine.Paused)
            {
                status = StateMachine.Stopping;

                //the handler is removed to stop the send of pending discovered devices
                watcher.Received -= OnAdvertisementReceived;
                watcher.Stop();
                status = StateMachine.Stopped;

                int count = 0;

                //add a small delay to allow the background scan to effectively stop and avoid an anticipated
                //change of state
                while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped && count < wait)
                {
                    count++;
                }

                //watcher.Stopped -= OnScanStopped;
                watcher = null;
            }
        }


        /// <summary>
        /// The scan is paused, saving the scanning mode and the discovered devices
        /// </summary>
        public void PauseScanning()
        {
            if (status == StateMachine.Started)
            {
                if (watcher != null)
                {
                    watcher.Stop();
                    status = StateMachine.Paused;

                    int count = 0;
                    //add a small delay to allow the background scan to effectively pause and avoid an anticipated
                    //change of state
                    while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped && count < wait)
                    {
                        count++;
                    }

                    lastScanningMode = watcher.ScanningMode;
                }
            }
        }

        /// <summary>
        /// Allows to resume the scan without loosing the peviously collected devices.
        /// In case the method is called as first scanning call, the default scanning mode is Passive
        /// </summary>
        public async void ResumeScanning()
        {
            if (status == StateMachine.Paused)
            {
                if (watcher != null)
                {
                    status = StateMachine.Starting;
                    watcher.ScanningMode = lastScanningMode;
                    await scan();
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
        }



        #endregion


        #region Filter methods

        /// <summary>
        ///add the filter to the watcher in case of active filtering so the filtering is not lost
        ///when scan is stopped and restarte
        /// </summary>
        private void checkFilterStatus()
        {
            if (filter != null && IsFilteringActive)
            {
                if (watcher != null)
                {
                    watcher.AdvertisementFilter = filter.AdvertisementFilter;
                    watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
                }
            }
        }


        /// <summary>
        /// Add a filter to the watcher, starting a filtered scan
        /// </summary>
        /// <param name="filter">The filter object</param>
        public void SetFilter(IFilter filter)
        {
            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            if (watcher != null)
            {
                ShowOnlyConnectableDevices = filter.ShowOnlyConnectableDevices;
                watcher.AdvertisementFilter = filter.AdvertisementFilter;
                watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
                IsFilteringActive = true;
            }


        }


        /// <summary>
        /// Remove the filter from the watcher, starting an unfiltered scan
        /// </summary>
        public void RemoveFilter()
        {
            if (watcher != null)
            {
                watcher.AdvertisementFilter = new BluetoothLEAdvertisementFilter();
                watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter();
                filter = null;
            }
            IsFilteringActive = false;

        }
        #endregion


        #region Helper methods

        /// <summary>
        /// Get the watcher state
        /// </summary>
        public BluetoothLEAdvertisementWatcherStatus GetWatcherStatus => watcher != null ? watcher.Status : BluetoothLEAdvertisementWatcherStatus.Stopped;

        /// <summary>
        /// Check if scan is running
        /// </summary>
        public bool IsScanningStarted => status == StateMachine.Started;

        /// <summary>
        /// Check if scanning is in passive mode
        /// </summary>
        public bool IsScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;

        /// <summary>
        /// Check if scanning is in active mode
        /// </summary>
        public bool IsScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;



        /// <summary>
        /// Remove all the devices from the dictionary
        /// </summary>
        private void clearDiscoveredDevices(Dictionary<ulong, ICosmedBleAdvertisedDevice> dict)
        {
            lock (devicesThreadLock)
            {
                dict.Clear();
            }
        }

        /// <summary>
        /// Delete all the devices from the dictionary, except the 20 last discovered ones.
        /// </summary>
        /// <param name="dictionary"></param>
        private void CleanOlderDiscoveredDevices(Dictionary<ulong, ICosmedBleAdvertisedDevice> dictionary)
        {
            int numberOfLeftDevices = 20;
            if (dictionary.Count > 20)
            {
                dictionary = dictionary.Values.OrderByDescending(d =>
                    d.Timestamp).Where(p =>
                        --numberOfLeftDevices >= 0).ToDictionary(d =>
                            d.DeviceAddress);
            }
        }

        /// <summary>
        /// It checks the bluetooth adapter on the user machine, to see if Bluetooth Low Energy 
        /// and Central Role are supported.
        /// </summary>
        /// <returns>true if the checked options are supported, otherwire raise the event ScanInterrupted 
        /// and returns false and aborts the scan</returns>
        private async Task<bool> checkBLEAdapterError()
        {
            try
            {
                CosmedBluetoothLEAdapter adapter = await CosmedBluetoothLEAdapter.CreateAsync();
                string str = adapter.HexAddress;
                //Console.WriteLine(str);

                if (!adapter.IsLowEnergySupported)
                {
                    status = StateMachine.Aborted;
                    var e = new BluetoothLeNotSupportedException("Your adapter " + str + " does not support Bluetooth Low Energy");
                    await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                    return false;
                }
                if (!adapter.IsCentralRoleSupported)
                {
                    status = StateMachine.Aborted;
                    var e = new CentralRoleNotSupportedException("Your adapter does not support Central Role");
                    await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                    return false;
                }
            }
            catch (BluetoothAdapterCommunicationFailureException e)
            {
                status = StateMachine.Aborted;
                await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                return false;
            }

            return true;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dev">The filtered devices</param>
        /// <returns>True if the filtering option ShowOnlyConnectableDevices has been set to true.
        /// Otherwise returns the true if the device is connectable, false if not.</returns>
        private bool check(ICosmedBleAdvertisedDevice dev)
        {
            if (ShowOnlyConnectableDevices)
            {
                return dev.IsConnectable;
            }
            else
            {
                return true;
            }
        }

        #endregion


        #region test helper

        /*
         *these methods and fields have been used for testing, they should be removed 
         */



        public void addDiscoveredDevices(ICosmedBleAdvertisedDevice device)
        {
            lock (devicesThreadLock)
            {
                discoveredDevices[device.DeviceAddress] = device;
                lastDiscoveredDevices[device.DeviceAddress] = device;
                NewDeviceDiscovered?.Invoke(this, discoveredDevices[device.DeviceAddress]);
            }
        }

        public BluetoothLEAdvertisementWatcher GetWatcher()
        {
            return watcher;
        }

        private void CheckBleStatus()
        {
            //controlla se c´é un device BLE acceso, lasciare che lo gestisca l'utente

            bool bleIsOn = CosmedBluetoothLEAdapter.IsBluetoothLEOn;

            if (!bleIsOn)
            {
                Console.WriteLine("ble dovrebbe essere OFF");
            }
            else
            {
                Console.WriteLine("ble dovrebbe essere ON");
            }

            //se non c´è aspetta che ci sia per poi riprendere la scansione
            while (!CosmedBluetoothLEAdapter.IsBluetoothLEOn) { Thread.Sleep(1000); }
            Console.WriteLine("bluetooth attivo, riprende lo scan");
            StartActiveScanning();
        }

        #endregion


        #region Callbacks


        /// <summary>
        /// This event handler receive the advertisements from the scanning watcher. It saves them and raise an event
        /// when a new device has been discovered. When a number of devices >= maxScanResults (default 128) have been already
        /// saved, the devices collection gets cleaned, leaving only the last 20 discovered devices. If still in range the 
        /// </summary>
        /// <param name="sender">The scanning watcher</param>
        /// <param name="args">The advertisement data</param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            ICosmedBleAdvertisedDevice device;
            //Console.WriteLine("adv received +++++++++++++++");
            if (sender == watcher && args != null && status == StateMachine.Started)
            {
                lock (devicesThreadLock)
                {
                    bool newDevice = !discoveredDevices.ContainsKey(args.BluetoothAddress);
                    
                    if (newDevice)
                    {
                        if (discoveredDevices.Count >= maxScanResults)
                        {
                            CleanOlderDiscoveredDevices(discoveredDevices);
                        }
                        device = new CosmedBleAdvertisedDevice(args);

                        discoveredDevices[args.BluetoothAddress] = device;
                    }
                    else
                    {
                        discoveredDevices[args.BluetoothAddress].SetAdvertisement(args);
                    }

                    if (lastDiscoveredDevices.Count >= maxScanResults)
                    {
                        CleanOlderDiscoveredDevices(lastDiscoveredDevices);
                    }

                    lastDiscoveredDevices[args.BluetoothAddress] = discoveredDevices[args.BluetoothAddress];
                    device = discoveredDevices[args.BluetoothAddress];
                    //var d = (CosmedBleAdvertisedDevice)device;
                    //d.PrintAdvertisement();
                }
                Task.Run(() => { NewDeviceDiscovered?.Invoke(this, device); }).ConfigureAwait(false);
            }

        }


        /// <summary>
        /// Event handler that receive informations about the watcher and errors in case of scanning stopped or aborted.
        /// The scan can be aborted for example when the bluetooth signal is lost or the adapter turned off during a scan.
        /// </summary>
        /// <param name="sender">the stopped watcher</param>
        /// <param name="args">contains an error message if present</param>
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            if (sender.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {
                status = StateMachine.Aborted;
                Task.Run(() => ScanInterrupted?.Invoke(this, new ScanAbortedException("scan aborted, please check your Bluetooth adapter"))).ConfigureAwait(false);
            }
            else
            {
                Task.Run(() => ScanStopped?.Invoke(this, args)).ConfigureAwait(false);
                //Console.WriteLine("Stopping scan");
                //Console.WriteLine(args.Error.ToString());
            }

        }


        // only for test purposes, the user should implements his own event hanlder
        private void OnScanInterrupted(CosmedBluetoothLEAdvertisementWatcher sender, Exception arg)
        {
            Console.WriteLine("((((((((((((((" + arg.Message + ")))))))))))))))))))))))");

            // only for test purpose, the user should implements his own event hanlder
            CheckBleStatus();
        }

        #endregion

    }


    /// <summary>
    /// The possible states of the watcher
    /// </summary>
    public enum StateMachine
    {
        Starting = 0,
        Started = 1,
        Aborted = 2,
        Stopping = 3,
        Stopped = 4,
        Paused = 5,
    }


}