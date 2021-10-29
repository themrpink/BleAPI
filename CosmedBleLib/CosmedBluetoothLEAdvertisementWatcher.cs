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
        public string numb { get; set; } = "0";

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
        //private readonly object LockUpdateDevices = new object();

        //used to send updated device collections
        //private Thread updatingThread;

        //tells if the devices is actually collecting updated device collections and sending them
       //private bool isCollectingDevices = false;

        //max number of discoverable devices before auto-cleaning of the dictionary
        private int MaxScanResults = 128;

        //the last requested scanning mode, used for resuming the scan
        private BluetoothLEScanningMode lastScanningMode;


        #endregion


        #region Delegates

        //public events
        public event Action<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
        public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> StoppedListening;
        public event Action ScanModeChanged;

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event Action<CosmedBleAdvertisedDevice> NewDeviceDiscovered;
       // public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> NewDeviceDiscovered3;
        public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> DeviceNameChanged;
        /////questo potrebbe non servire

        //private event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> AllDevicesCollectionUpdated;
        //private event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> RecentDevicesCollectionUpdated;
        #endregion


        #region Properties


        //in seconds
        public double timeout { get; set; } = 10;

        public bool IsFilteringActive { get; private set; } = false;

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

        public BluetoothLEScanningMode ScanningMode
        {
            get
            {
                return watcher.ScanningMode; 
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
            discoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>();
            AutoUpdatedDevices = new AutoUpdateDiscoveredDevicesCollection();
            //AutoUpdatedDevices2 = new AutoUpdateDiscoveredDevicesCollection2();
            //RecentDevicesCollectionUpdated += AutoUpdatedDevices.RecentlyUpdatedDevicesHandler;
            //AllDevicesCollectionUpdated += AutoUpdatedDevices.AllDevicesUpdatedHandler;
            NewDeviceDiscovered += AutoUpdatedDevices.NewDiscoveredDeviceHandler;
            DeviceNameChanged += OnDeviceNameChanged;
        }

        /// <summary>
        /// constructor setting the filter
        /// </summary>
        /// <param name="advertisementFilter"></param>
        public CosmedBluetoothLEAdvertisementWatcher(CosmedBluetoothLEAdvertisementFilter filter) : this()
        {            
            SetFilter(filter ?? throw new ArgumentNullException(nameof(filter)));
            IsFilteringActive = true;
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
            watcher.Received += this.OnAdvertisementReceivedAsync;
            watcher.Stopped += this.OnScanStopped;            
        }


        private void scan()
        {
            try
            { 
                watcher.Start();
                StartedListening?.Invoke(this, watcher.ScanningMode);                    
            }
            catch (System.Exception e)
            {
                watcher.Received -= this.OnAdvertisementReceivedAsync;
                throw e;
            }
        }

        public static void StopScan()
        {
            //watcher.Stop();
        }

        public void StopScanning()
        {
            if(watcher != null && IsScanningStarted)
            {
                watcher.Received -= this.OnAdvertisementReceivedAsync;        
                watcher.Stop();  
                watcher.Stopped -= this.OnScanStopped;                           
                watcher = null;
            }
           // status = BluetoothLEAdvertisementWatcherStatus.Stopped;       
        }

        public void PauseScanning()
        {
            if (watcher != null && IsScanningStarted)
            {
                watcher.Received -= this.OnAdvertisementReceivedAsync;
                watcher.Stop();
                watcher.Stopped -= this.OnScanStopped;              
            }
        }

        //allows to resume the scan without loosing the peviously collected devices
        // in case the method is called as first scanning call, the default scanning mode is Passive
        public void ResumeScanning()
        {
            checkBLEAdapter();

            if (IsScanningStarted)
            {
                return;
            }

            scanInit();

            watcher.ScanningMode = lastScanningMode;
            scan();
        }

        #endregion


        #region Filter methods
        public void SetFilter(CosmedBluetoothLEAdvertisementFilter filter)
        {

            filter = filter ?? throw new ArgumentNullException(nameof(filter));

            watcher.AdvertisementFilter = filter.AdvertisementFilter;
            watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
            IsFilteringActive = true;
        }


        //attenzione che il filtro potrebbe essere già null, e quindi restituire null
        //gestire questa cosa
        public CosmedBluetoothLEAdvertisementFilter RemoveFilter()
        {
            watcher.AdvertisementFilter = null;
            watcher.SignalStrengthFilter = null;
            CosmedBluetoothLEAdvertisementFilter filterTemp = filter;
            filter = null;
            IsFilteringActive = false;

            return filterTemp;
        }
        #endregion



        #region Helper methods

        public BluetoothLEAdvertisementWatcherStatus GetWatcherStatus =>  watcher!=null ? watcher.Status : BluetoothLEAdvertisementWatcherStatus.Stopped; 
        public bool IsScanningStarted => watcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        public bool IsScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;
        public bool IsScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;
        //public bool IsAutoUpdateActive => isCollectingDevices;
        //public bool IsUpdatingThreadAlive => updatingThread.IsAlive;
        //public System.Threading.ThreadState GetUpdatingThreadState => updatingThread.ThreadState;


        public IReadOnlyCollection<CosmedBleAdvertisedDevice> GetRecentlyAdvertisedDevices(int seconds = 10) 
        {                          
            lock (ThreadLock)
            {
                return discoveredDevices.Values.Where((device) =>
                {
                     if (seconds > 0)
                     {
                         var diff = DateTime.UtcNow - TimeSpan.FromSeconds(seconds);
                         return device.Timestamp >= diff;
                     }
                     return true;

                }).ToList().AsReadOnly();
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
                discoveredDevices = discoveredDevices.Values.OrderByDescending(d => 
                    d.Timestamp).Where(p => 
                        --numberOfLeftDevices >= 0).ToDictionary(d => 
                            d.DeviceAddress);
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


        //questi servono per i test sulle performance
        private Stopwatch stopwatchAdvertisement = new Stopwatch();
        private int countAdvertisement = 0;
        private List<long> timeElapsed = new List<long>();
        private List<long> timeElapsedWithNewDeviceDiscovered = new List<long>();
        private List<int> threadIds = new List<int>();

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
        private async void OnAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
           // countAdvertisement += 1;
           // threadIds.Add(Thread.CurrentThread.ManagedThreadId);
           // stopwatchAdvertisement.Start();
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
                        device = DeviceFactory.CreateAdvertisedDevice(args);
                        discoveredDevices[args.BluetoothAddress] = device;
                    }
                    else
                    {
                        string name = discoveredDevices[args.BluetoothAddress].DeviceName;
                        if(!name.Equals(args.Advertisement.LocalName))
                        {
                            DeviceNameChanged?.Invoke(sender, args);
                        }
                        
                        discoveredDevices[args.BluetoothAddress].SetAdvertisement(args);
                    }                  
                }
                device = discoveredDevices[args.BluetoothAddress];
            }
            //stopwatchAdvertisement.Stop();
            //timeElapsed.Add(stopwatchAdvertisement.ElapsedMilliseconds);
            await Task.Run(() => { NewDeviceDiscovered?.Invoke(device); });
        }

        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Console.WriteLine("Stopping scan");
            Console.WriteLine(args.Error.ToString());
            StoppedListening?.Invoke(sender, args);
        }


        private void OnDeviceNameChanged(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Console.WriteLine("___________________device name changed: " + args.Advertisement.LocalName + " isConnectable: " + args.IsConnectable + " address: " + args.BluetoothAddress + " isscanresponse " + args.IsScanResponse);           
        }
    }

    #endregion





    //#if DEBUG
    //#else
    //#endif

}
