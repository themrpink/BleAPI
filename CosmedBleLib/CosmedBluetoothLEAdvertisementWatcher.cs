using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CosmedBleLib
{

    /// <summary>
    /// wrapper class for the BleAdvertisementWatcher
    /// </summary>
    public class CosmedBluetoothLEAdvertisementWatcher
    {
        private ObservableCollection<CosmedBleAdvertisedDevice> KnownDevices = new ObservableCollection<CosmedBleAdvertisedDevice>();
        private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();


        #region Private fields
        private BluetoothLEAdvertisementWatcher watcher;
        private BluetoothLEAdvertisementWatcherStatus status;

        //the structure where the discovered devices are saved
        private Dictionary<ulong, CosmedBleAdvertisedDevice> discoveredDevices;

        //filter to apply to the discovered devices
        private CosmedBluetoothLEAdvertisementFilter filter;

        //auto updating collections
        private IAdvertisedDevicesCollection AutoUpdatedDevices;

        //lock for safe access to shared resources:
        //dictionary
        private readonly object ThreadLock = new object();
        //isCollectingDevices (protected because accessed by different threads. The solution developed prevents from undesidered behaviours, i.e. updating the collections after the scan has been stopped, or duplicate the updating thread, etc
        private readonly object LockUpdateDevices = new object();

        //used to send updated device collections
        private Thread updateThread;

        //tells if the devices is actually collecting updated device collections and sending them
        private bool isCollectingDevices = false;

        //max number of discoverable devices before auto-cleaning of the dictionary
        private int MaxScanResults = 128;




        //questi servono per i test sulle performance
        private Stopwatch stopwatchAdvertisement = new Stopwatch();
        private int countAdvertisement = 0;
        private List<long> timeElapsed = new List<long>();



        #endregion


        #region public Delegates

        //public events
        public event Action<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
        public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> StoppedListening;
        public event Action ScanModeChanged;

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event Action<CosmedBleAdvertisedDevice> NewDeviceDiscovered;

        /////questo potrebbe non servire
        /// <summary>
        /// Subscribe to this event to get the discovered devices collection regulary updated
        /// </summary>
        private event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> AllDevicesCollectionUpdated;
        private event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> RecentDevicesCollectionUpdated;
        #endregion


        #region Properties


        //in seconds
        public double timeout { get; set; } = 10;

        /// <summary>
        /// this structure is created at every user request from the devices dictionary. The multithreaded access is
        /// protected by a lock
        /// </summary>
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> allDiscoveredDevices
        {
            get
            {
                lock (ThreadLock)
                {
                    return discoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }


        /////questo potrebbe non servire
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> recentlyDiscoveredDevices
        {
            get
            {
                lock (ThreadLock)
                {
                    return discoveredDevices.Values.Where
                        ((device) =>
                        {
                            if (timeout > 0)
                            {
                                var diff = DateTime.UtcNow - TimeSpan.FromSeconds(timeout);
                                return device.Timestamp >= diff;
                            }
                            return true;
                        }
                        ).ToList().AsReadOnly();
                }
            }
        }


        #endregion


        #region constructors
        /// <summary>
        /// constructor: instatiate the watcher, the dictionary and add the events to the delegates of the watcher
        /// </summary>
        /// 
        public CosmedBluetoothLEAdvertisementWatcher()
        {
            watcher = new BluetoothLEAdvertisementWatcher();
            status = watcher.Status;
            discoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>();
            AutoUpdatedDevices = new AutoUpdateDiscoveredDevicesCollection();
            RecentDevicesCollectionUpdated += AutoUpdatedDevices.onRecentDevicesUpdated;
            AllDevicesCollectionUpdated += AutoUpdatedDevices.onAllDevicesUpdated;
            NewDeviceDiscovered += AutoUpdatedDevices.onNewDeviceDiscovered;
        }

        /// <summary>
        /// constructor setting the filter
        /// </summary>
        /// <param name="advertisementFilter"></param>
        public CosmedBluetoothLEAdvertisementWatcher(CosmedBluetoothLEAdvertisementFilter filter) : this()
        {
            
            SetFilter(filter ?? throw new ArgumentNullException(nameof(filter)));
        }

        #endregion


        #region Scanning methods


        /// <summary>
        /// Initialize and Start passive scanning. 
        /// </summary>
        public void StartPassiveScanning()
        {
            checkBLEAdapter();
            clearDiscoveredDevices();
            
            //if a passive scan is already running, do nothing
            if (IsScanningStarted && IsScanningPassive)
                return;

            //if a not passive scanning is already active stop it
            else if (IsScanningStarted && !IsScanningPassive)
            {
                StopScanning();
                ScanModeChanged?.Invoke();
            }
            scanInit();

            //set the passive scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;            
            scan();            
        }


        /// <summary>
        /// Initialize and start Active scanning
        /// </summary>
        public void StartActiveScanning()
        {
            checkBLEAdapter();
            clearDiscoveredDevices();
            //if an active scan is already running, do nothing
            if (IsScanningStarted && IsScanningActive)
                return;
            
            //if a not active scanning is already active stop it
            else if (IsScanningStarted && !IsScanningActive)
            {
                StopScanning();
                ScanModeChanged?.Invoke();
            }
            scanInit();
            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;

            scan();
        }


        private void scanInit()
        {           
            if (watcher == null)
            {
                watcher = new BluetoothLEAdvertisementWatcher();
            }
                
            bool isExtendedAdvertisementSupported = CosmedBluetoothLEAdapter.IsExtendedAdvertisingSupported;
            watcher.AllowExtendedAdvertisements = isExtendedAdvertisementSupported;
            watcher.Received += this.OnAdvertisementReceived;
            watcher.Stopped += this.OnScanStopped;            
        }


        private void scan()
        {
            try
            { 
                watcher.Start();
                status = BluetoothLEAdvertisementWatcherStatus.Started;
                StartedListening?.Invoke(this, watcher.ScanningMode);
            }
            catch (System.Exception e)
            {
                isCollectingDevices = false;
                watcher.Received -= this.OnAdvertisementReceived;
                throw e;
            }
        }


        public void StopScanning()
        {
            if(watcher != null && IsScanningStarted)
            {
                watcher.Received -= this.OnAdvertisementReceived;        
                watcher.Stop();  
                watcher.Stopped -= this.OnScanStopped;                           
                lock (LockUpdateDevices)
                {
                    isCollectingDevices = false;
                }
                watcher = null;
            }
            status = BluetoothLEAdvertisementWatcherStatus.Stopped;
            
            
        }


        #endregion


        #region Filter methods
        public void SetFilter(CosmedBluetoothLEAdvertisementFilter filter)
        {

            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));

            watcher.AdvertisementFilter = filter.AdvertisementFilter;
            watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
        }


        //attenzione che il filtro potrebbe essere già null, e quindi restituire null
        //gestire questa cosa
        public CosmedBluetoothLEAdvertisementFilter RemoveFilter()
        {
            watcher.AdvertisementFilter = null;
            watcher.SignalStrengthFilter = null;
            CosmedBluetoothLEAdvertisementFilter filterTemp = filter;
            filter = null;

            return filterTemp;
        }
        #endregion


        #region AutoUpadate methods


        /////questo potrebbe non servire
        /// <summary>
        /// start automatic update of devices collections. Reading an instance of the collections will result in a regularly self-updated content.
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public IAdvertisedDevicesCollection getUpdatedDiscoveredDevices(int ms = 5000)
        {
            lock (LockUpdateDevices)
            {
                if (!isCollectingDevices)
                {
                    isCollectingDevices = true;

                    if (updateThread == null)
                    {
                        updateThread = new Thread(() => sendUpdatedDevicesService(ms));
                        updateThread.Start();
                    }
                }
            }
            return AutoUpdatedDevices;
        }


        /////questo potrebbe non servire
        //update the devices
        private void sendUpdatedDevicesService(int ms)
        {
            while (true)
            {
                lock (LockUpdateDevices)
                {
                    if (!isCollectingDevices)
                    {
                        updateThread = null;
                        return;
                    }
                    RecentDevicesCollectionUpdated?.Invoke(recentlyDiscoveredDevices);
                    AllDevicesCollectionUpdated?.Invoke(allDiscoveredDevices);
                }
                Thread.Sleep(ms);
            }
        }


        //stop update
        public IAdvertisedDevicesCollection StopUpdateDevices()
        {
            lock (LockUpdateDevices)
            {
                isCollectingDevices = false;
            }
            return AutoUpdatedDevices;
        }

        #endregion


        #region Helper methods

        public BluetoothLEAdvertisementWatcherStatus getWatcherStatus => watcher!=null ? watcher.Status : status;
        public bool IsScanningStarted => watcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        public bool IsScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;
        public bool IsScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;
        public bool IsAutoUpdateActive => isCollectingDevices;
        public bool IsUpdatingThreadAlive => updateThread.IsAlive;
        public int GetUpdatingThreadID => updateThread.ManagedThreadId;
        public System.Threading.ThreadState GetUpdatingThreadState => updateThread.ThreadState;


        public IReadOnlyCollection<CosmedBleAdvertisedDevice> GetRecentlyAdvertisedDevices(int seconds = 30) 
        {                          
            lock (ThreadLock)
            {
                return discoveredDevices.Values.Where
                    ((device) =>
                    {
                         if (timeout > 0)
                         {
                            var diff = DateTime.UtcNow - TimeSpan.FromSeconds(seconds);
                            return device.Timestamp >= diff;
                         }
                         return true;
                    }
                    ).ToList().AsReadOnly();
            }
        }


 

        /// <summary>
        /// 
        /// svuota il dizionario dei devices
        /// </summary>
        private void clearDiscoveredDevices()
        {
            lock (ThreadLock)
            {
                discoveredDevices.Clear();
            }
        }


        private void CleanOlderDiscoveredDevices()
        {
            int numberOfLeftDevices = 10;
            if( discoveredDevices.Count > 10)
            {
                discoveredDevices = discoveredDevices.Values.OrderBy(d => d.Timestamp).Where(p => --numberOfLeftDevices >= 0).ToDictionary(d => d.DeviceAddress);
            }
        }


        private void checkBLEAdapter()
        {
            string str = CosmedBluetoothLEAdapter.HexAddress;
            Console.WriteLine(str);
            if (!CosmedBluetoothLEAdapter.IsLowEnergySupported)
                throw new InvalidOperationException("Your adapter " + str + " does not support Bluetooth Low Energy");

            if (!CosmedBluetoothLEAdapter.IsCentralRoleSupported)
                throw new InvalidOperationException("Your adapter does not support Central Role");
        }
        #endregion


        #region test helper

        public void addDiscoveredDevices(CosmedBleAdvertisedDevice device)
        {
            lock (ThreadLock)
            {
                discoveredDevices[device.DeviceAddress] = device;
                NewDeviceDiscovered?.Invoke(discoveredDevices[device.DeviceAddress]);      
            }
        }


        private void deleteOldAdvertisedDevice()
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            ulong oldKey = 0;
            foreach (var deviceKey in discoveredDevices.Keys)
            {
                DateTimeOffset deviceTimestamp = discoveredDevices[deviceKey].Timestamp;
                if (deviceTimestamp < timestamp)
                {
                    timestamp = deviceTimestamp;
                    oldKey = deviceKey;
                }
            }
            if (oldKey != 0)
                discoveredDevices.Remove(oldKey);
        }

        #endregion


        #region Callbacks


        /// <summary>
        /// Evento che salva gli advertisement nel dizionario come devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
           
            CosmedBleAdvertisedDevice device;
            lock (ThreadLock)
            {
                if (sender == watcher && args != null)
                {
                    bool newDevice = !discoveredDevices.ContainsKey(args.BluetoothAddress);
                    if (newDevice)
                    {
                        if (discoveredDevices.Count >= MaxScanResults)
                        {
                            CleanOlderDiscoveredDevices();
                        }
                        device = new CosmedBleAdvertisedDevice().SetAdvertisement(args);
                        discoveredDevices[args.BluetoothAddress] = device;
                    }
                    else
                    {
                        discoveredDevices[args.BluetoothAddress].SetAdvertisement(args);
                    }                  
                }
                device = discoveredDevices[args.BluetoothAddress];
            }
            await Task.Run(() => { NewDeviceDiscovered?.Invoke(device); });        
        }

        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Console.WriteLine("Stopping scan");
            Console.WriteLine(args.Error.ToString());
            StoppedListening?.Invoke(sender, args);
        }


        //da implementare, forse
        private void OnScanModeChanged(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {

        }

        #endregion
    
    
    }


    //#if DEBUG
    //#else
    //#endif

}
