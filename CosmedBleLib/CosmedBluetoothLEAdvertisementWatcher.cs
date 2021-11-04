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
using System.Management;

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


        #region Public events

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, CosmedBleAdvertisedDevice> NewDeviceDiscovered;
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;       
        public event Action<CosmedBluetoothLEAdvertisementWatcher, Exception> ScanInterrupted;

        
        //questi potrebbero non servire
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
        public event Action ScanModeChanged;
        public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> DeviceNameChanged;

        #endregion


        #region Properties


        //seconds, used for the update of the RecentlyDiscoveredDevices
        public double timeoutSeconds { get; set; } = 10;
        
        //filter is active if is has been set to true
        public bool IsFilteringActive { get; private set; } = false;
        
        public BluetoothLEScanningMode ScanningMode
        {
            get
            {
                return watcher.ScanningMode;
            }
        }


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
                            if (timeoutSeconds > 0)
                            {
                                var diff = DateTime.UtcNow - TimeSpan.FromSeconds(timeoutSeconds);
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
            ScanInterrupted += OnScanInterrupted;
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

            if (GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {
                resetScanningState();
            }
            lock (WatcherThreadLock)
            {
                if (watcher == null)
                {
                    watcher = new BluetoothLEAdvertisementWatcher();
                    watcher.Received += this.OnAdvertisementReceived;
                    watcher.Stopped += this.OnScanStopped;
                }
            }               
            bool isExtendedAdvertisementSupported = CosmedBluetoothLEAdapter.IsExtendedAdvertisingSupported;
            //watcher.AllowExtendedAdvertisements = isExtendedAdvertisementSupported;          
        }


        private void scan()
        {    
            try
            {
                checkBLEAdapterError();
                lock (WatcherThreadLock)
                {    
                    if(watcher != null)
                    {
                        lastScanningMode = watcher.ScanningMode;
                        watcher.Start();
                        
                        //this avoid starting a new command before the Start has completed its background initialization
                        while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Created) { }
                        //oppure:
                        if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Created) Thread.Sleep(50);
                    }
                    //if the scan is aborted, there are two possible cases:
                    //1) problem is present (ie bth OFF) before Start call: that makes start scan fail. Start throws then an exception, catched below
                    //2) problem arise during the scan. The scan is stopped, the event Stopped invoked and the Status set to "Aborted". This can catched only by the event call, checking the watcher status
                }
                
                StartedListening?.Invoke(this, watcher.ScanningMode);                 
            }
            catch (System.Exception e)
            {
                //resetta lo stato iniziale del watcher
                resetScanningState();

                Task.Run(()=>ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);

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
                else
                {                   
                    StartActiveScanning();
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
                    //while stopping the Received event could still be , therefore the watcher = null
                    watcher.Received -= OnAdvertisementReceived;
                    watcher.Stopped -= OnScanStopped;
                    while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping) { }                    
                    watcher = null;
                }
                else
                {
                    if(lastScanningMode == BluetoothLEScanningMode.Active)
                    {
                        StartActiveScanning();
                    }
                    else
                    {
                        StartPassiveScanning();
                    }
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


        //il filtro potrebbe essere già null, e quindi restituire null. Altrimenti dà la possibilità di modificare  il filtro rimosso
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

        public BluetoothLEAdvertisementWatcherStatus GetWatcherStatus => watcher != null ? watcher.Status : BluetoothLEAdvertisementWatcherStatus.Stopped;
        public bool IsScanningStarted => watcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        public bool IsScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;
        public bool IsScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;
        //public bool IsAutoUpdateActive => isCollectingDevices;
        //public bool IsUpdatingThreadAlive => updatingThread.IsAlive;
        //public System.Threading.ThreadState GetUpdatingThreadState => updatingThread.ThreadState;


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

        
        private async void checkBLEAdapterError()
        {
            CosmedBluetoothLEAdapter adapter = await CosmedBluetoothLEAdapter.GetAdapterAsync();
 
            string str = adapter.HexAddress;
            Console.WriteLine(str);        

            if (! adapter.IsLowEnergySupported)
            {
                throw new BluetoothLeNotSupportedException("Your adapter " + str + " does not support Bluetooth Low Energy");
            }
            if (! adapter.IsCentralRoleSupported)
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

            if (sender == watcher && args != null)
            {
                lock (DevicesThreadLock) 
                {
                    bool newDevice = !discoveredDevices.ContainsKey(args.BluetoothAddress);
                    if (newDevice)
                    {
                        if (discoveredDevices.Count >= MaxScanResults)
                        {
                            CleanOlderDiscoveredDevices(discoveredDevices);
                        }
                        device = DeviceBuilder.CreateAdvertisedDevice(args);
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

                    if (lastDiscoveredDevices.Count >= MaxScanResults)
                    {
                        CleanOlderDiscoveredDevices(lastDiscoveredDevices);
                    }

                    lastDiscoveredDevices[args.BluetoothAddress] = discoveredDevices[args.BluetoothAddress];
                    device = discoveredDevices[args.BluetoothAddress];
                } 
                Task.Run(() => { NewDeviceDiscovered?.Invoke(this, device); }).ConfigureAwait(false);
            }
            
        }


        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            //qua non credo serva il lock
            if(sender.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {   
                Task.Run(()=>ScanInterrupted?.Invoke(this, new ScanAbortedException("scan aborted, please check your Bluetooth adapter"))).ConfigureAwait(false);              
            }
            Task.Run(() => ScanStopped?.Invoke(this, args)).ConfigureAwait(false);
            Console.WriteLine("Stopping scan");
            Console.WriteLine(args.Error.ToString());           
        }


        private void OnScanInterrupted(CosmedBluetoothLEAdvertisementWatcher sender, Exception arg)
        {
   
                Console.WriteLine("((((((((((((((" + arg.Message + ")))))))))))))))))))))))");

            //controlla se c´é un device BLE acceso
                SelectQuery sq = new SelectQuery("SELECT DeviceId FROM Win32_PnPEntity WHERE service='BthLEEnum'");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(sq);

                if (searcher.Get().Count == 0)
                {
                    Console.WriteLine("ble dovrebbe essere OFF");
                }
                else
                {
                    Console.WriteLine("ble dovrebbe essere ON");
                }

                //se non c´è aspetta che ci sia per poi riprendere la scansione
                while (searcher.Get().Count == 0) { Thread.Sleep(1000); }
                Console.WriteLine("bluetooth attivo, riprende lo scan");
                sender.ResumeScanning();
   
        }



    }

    #endregion


    //#if DEBUG
    //#else
    //#endif



}

