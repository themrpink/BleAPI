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

        //the structure where the last discovered devices are saved
        private Dictionary<ulong, CosmedBleAdvertisedDevice> lastDiscoveredDevices; 

        //auto updating collections
        private IAdvertisedDevicesCollection AutoUpdatedDevices;

        //filter to apply to the discovered devices
        private CosmedBluetoothLEAdvertisementFilter filter;

        //lock for safe access to shared resources:
        //dictionary
        private readonly object DevicesThreadLock = new object();
        private readonly object WatcherThreadLock = new object();
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
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;
        public event Action ScanModeChanged;
        public event Action<Exception> ScanInterrupted;

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, CosmedBleAdvertisedDevice> NewDeviceDiscovered;
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> DeviceNameChanged;
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
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> AllDiscoveredDevices
        {
            get
            {
                lock (DevicesThreadLock)
                {
                    return discoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        /////questo potrebbe non servire
        public IReadOnlyCollection<CosmedBleAdvertisedDevice> RecentlyDiscoveredDevices
        {
            get
            {
                lock (DevicesThreadLock)
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

        public IReadOnlyCollection<CosmedBleAdvertisedDevice> LastDiscoveredDevices
        {
            get
            {
                lock (DevicesThreadLock)
                {
                    IReadOnlyList<CosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.Values.ToList().AsReadOnly();
                    lastDiscoveredDevices.Clear();
                    return lastDiscoveredDevicesTemp;
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
            //watcher = new BluetoothLEAdvertisementWatcher();
            discoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>();
            AutoUpdatedDevices = new AutoUpdateDiscoveredDevicesCollection();
            lastDiscoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>(); 
            DeviceNameChanged += OnDeviceNameChanged;
            ScanInterrupted += OnBluetoothConnectionInterrupted;
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
            //if a passive scan is already running, do nothing
            if (IsScanningStarted && IsScanningPassive)
                return;

            //if a not passive scanning is already active stop it
            else if (IsScanningStarted && !IsScanningPassive)
            {
                resetScanningState();
                //questo meglio eliminarlo, perché potrei mandare un messaggio prima dell´éffettiva r
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
            //if an active scan is already running, do nothing
            if (IsScanningStarted && IsScanningActive)
                return;
            
            //if a not active scanning is already active stop it
            else if (IsScanningStarted && !IsScanningActive)
            {
                resetScanningState();
                ScanModeChanged?.Invoke();
            }
            scanInit();
            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;

            scan();
        }


        private void scanInit()
        {
            clearDiscoveredDevices(discoveredDevices);
            clearDiscoveredDevices(lastDiscoveredDevices);
            lock (WatcherThreadLock)
            {
                if (GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Aborted)
                {
                    resetScanningState();
                }
                if (watcher == null)
                {
                    watcher = new BluetoothLEAdvertisementWatcher();
                    watcher.Received += this.OnAdvertisementReceived;
                    watcher.Stopped += this.OnScanStopped;
                }
            }               
            bool isExtendedAdvertisementSupported = CosmedBluetoothLEAdapter.IsExtendedAdvertisingSupported;
            watcher.AllowExtendedAdvertisements = isExtendedAdvertisementSupported;          
        }


        private void scan()
        {    
            try
            {
                checkBLEAdapterError();
                lock (WatcherThreadLock)
                {
                    watcher.Start();

                    //this avoid starting a new command before the Start has completed its background initialization
                    while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Created) { }

                    //if the scan is aborted, there are two possible cases:
                    //1) problem is present (ie bth OFF) before Start call: that makes start scan fail. Start throws then an exception, catched below
                    //2) problem arise during the scan. The scan is stopped, the event Stopped invoked and the Status set to "Aborted". This can catched only by the event call, checking the watcher status
                }
                
                StartedListening?.Invoke(this, watcher.ScanningMode);                 
            }
            catch (System.Exception e)
            {
                resetScanningState();
                Task.Run(()=>ScanInterrupted?.Invoke(e));

                //lanciare l´eccezione o proseguire?
                //throw e;
            }

        }


        public void StopScanning()
        {
            if (IsScanningStarted || GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {
                resetScanningState();
            }
        }


        public void PauseScanning()
        { 
            lock (WatcherThreadLock)
            {
                if (watcher != null && IsScanningStarted)
                {
                    watcher.Stop();
                    while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping) { }
                    lastScanningMode = watcher.ScanningMode;
                }
            }
        }

        //allows to resume the scan without loosing the peviously collected devices
        // in case the method is called as first scanning call, the default scanning mode is Passive
        public void ResumeScanning()
        {
            lock (WatcherThreadLock)
            {
                if (IsScanningStarted)
                {
                    return;
                }
                if (watcher != null)
                {
                    watcher.ScanningMode = lastScanningMode;
                    scan();
                }
            }
        }


        private void resetScanningState()
        {            
            lock (WatcherThreadLock)    
            {                  
                if (watcher != null)
                {
                    watcher.Stop();
                    //while stopping the Received event could still be invoked
                    watcher.Received -= OnAdvertisementReceived;
                    watcher.Stopped -= OnScanStopped;
                    while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping) { }
                    watcher = null;
                } 
            }
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


        public BluetoothLEScanningMode ScanningMode
        {
            get
            {
                return watcher.ScanningMode;
            }
        }


        /// <summary>
        /// 
        /// svuota il dizionario dei devices
        /// </summary>
        private void clearDiscoveredDevices(Dictionary<ulong, CosmedBleAdvertisedDevice> dict)
        {
            lock (DevicesThreadLock)
            {
                dict.Clear();
            }
        }


        private void CleanOlderDiscoveredDevices(Dictionary<ulong, CosmedBleAdvertisedDevice> dictionary)
        {
            int numberOfLeftDevices = 10;
            if( dictionary.Count > 10)
            {
                dictionary = dictionary.Values.OrderByDescending(d => 
                    d.Timestamp).Where(p => 
                        --numberOfLeftDevices >= 0).ToDictionary(d => 
                            d.DeviceAddress);
            }
        }

        
        private void checkBLEAdapterError()
        {
            //CosmedBluetoothLEAdapter
            string str = CosmedBluetoothLEAdapter.HexAddress;
            Console.WriteLine(str);        

            if (! CosmedBluetoothLEAdapter.IsLowEnergySupported)
            {
                throw new BluetoothLeNotSupportedException("Your adapter " + str + " does not support Bluetooth Low Energy");
            }
            if (! CosmedBluetoothLEAdapter.IsCentralRoleSupported)
            {
                throw new CentralRoleNotSupportedException("Your adapter does not support Central Role");
            }
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
            lock (DevicesThreadLock)
            {
                discoveredDevices[device.DeviceAddress] = device;
                lastDiscoveredDevices[device.DeviceAddress] = device;
                NewDeviceDiscovered?.Invoke(this, discoveredDevices[device.DeviceAddress]);      
            }
        }



        #endregion


        #region Callbacks


        /// <summary>
        /// Evento che salva gli advertisement nel dizionario come devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            CosmedBleAdvertisedDevice device;

            lock (DevicesThreadLock)
            {             
                if (sender == watcher && args != null)
                {
                    bool newDevice = !discoveredDevices.ContainsKey(args.BluetoothAddress);
                    if (newDevice)
                    {
                        if (discoveredDevices.Count >= MaxScanResults)
                        {
                            CleanOlderDiscoveredDevices(discoveredDevices);
                        }
                        device = DeviceFactory.CreateAdvertisedDevice(args);
                        discoveredDevices[args.BluetoothAddress] = device;
                    }
                    else
                    {
                        string name = discoveredDevices[args.BluetoothAddress].DeviceName;
                        if(!name.Equals(args.Advertisement.LocalName))
                        {
                            DeviceNameChanged?.Invoke(this, args);
                        }
                        
                        discoveredDevices[args.BluetoothAddress].SetAdvertisement(args);
                    }
                }
                if (lastDiscoveredDevices.Count >= MaxScanResults)
                {
                    CleanOlderDiscoveredDevices(lastDiscoveredDevices);
                }
                lastDiscoveredDevices[args.BluetoothAddress] = discoveredDevices[args.BluetoothAddress];
                device = discoveredDevices[args.BluetoothAddress];
            }
            Task.Run(() => { NewDeviceDiscovered?.Invoke(this, device); });
        }


        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            //qua non credo serva il lock
            if(watcher!=null && watcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {   
                Task.Run(()=>ScanInterrupted?.Invoke(new ScanAbortedException("scan aborted, please check your Bluetooth adapter")));
            }
            Task.Run(() => ScanStopped?.Invoke(this, args));
            Console.WriteLine("Stopping scan");
            Console.WriteLine(args.Error.ToString());
            Console.WriteLine("press enter");
            Console.ReadLine();         
        }


        private void OnDeviceNameChanged(CosmedBluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            //Console.WriteLine("___________________device name changed: " + args.Advertisement.LocalName + " isConnectable: " + args.IsConnectable + " address: " + args.BluetoothAddress + " isscanresponse " + args.IsScanResponse);           
        }

        private void OnConnectionEstablished()
        {
            StopScanning();
        }

        private void OnBluetoothConnectionInterrupted(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("press enter");
            Console.ReadLine();
        }
    }

    #endregion


    //#if DEBUG
    //#else
    //#endif



}
