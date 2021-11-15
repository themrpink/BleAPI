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
    public sealed class CosmedBluetoothLEAdvertisementWatcher : IScanAdvertisement
    {
        //private ObservableCollection<CosmedBleAdvertisedDevice> KnownDevices = new ObservableCollection<CosmedBleAdvertisedDevice>();
        //private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();


        #region Private fields

        private const int wait = 3000;
        private BluetoothLEAdvertisementWatcher watcher;

        private StateMachine Status;

        //the structure where the discovered devices are saved
        private Dictionary<ulong, CosmedBleAdvertisedDevice> discoveredDevices;

        //the structure where the last discovered devices are saved
        private Dictionary<ulong, CosmedBleAdvertisedDevice> lastDiscoveredDevices; 

        //filter to apply to the discovered devices
        private CosmedBluetoothLEAdvertisementFilter filter;

        //lock for safe access to shared resources:
        //dictionary
        private readonly object DevicesThreadLock = new object();

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

        #endregion


        #region Properties
        private int scanTimeout;
        public int ScanTimeout
        {
            get { return scanTimeout; }
            set
            {
                if (value > 0)
                {
                    scanTimeout = value;
                    HasScanTimeout = true;

                }
                else
                {
                    scanTimeout = 0;
                    HasScanTimeout = false;
                }
            }
        }

        public bool HasScanTimeout { get; private set; }
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

        //allows to set wether the watcher returns only Connectable devices
        public bool ShowOnlyConnectableDevices { get; set; } = true;

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
                    return discoveredDevices.Values.Where( d => check(d)).ToList().AsReadOnly();
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
                                return device.Timestamp >= diff && check(device);
                            }
                            return check(device);
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
                    IReadOnlyList<CosmedBleAdvertisedDevice> lastDiscoveredDevicesTemp = lastDiscoveredDevices.Values.Where(d => check(d)).ToList().AsReadOnly();
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
            discoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>();
            lastDiscoveredDevices = new Dictionary<ulong, CosmedBleAdvertisedDevice>();
            Status = StateMachine.Stopped;
            //questo va lasciato facoltativo
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
        public async Task StartPassiveScanning()
        {
            if(Status == StateMachine.Started && !IsScanningPassive)
            {
                watcher.ScanningMode = BluetoothLEScanningMode.Passive;
                ScanModeChanged?.Invoke();
            }
            else if(Status == StateMachine.Stopped || Status == StateMachine.Aborted)
            {
                Status = StateMachine.Starting;
                scanInit();

                //set the passive scan and start a new scanning thread 
                watcher.ScanningMode = BluetoothLEScanningMode.Passive;            
                await scan();
            }
        }


        /// <summary>
        /// Initialize and start Active scanning
        /// </summary>
        public async Task StartActiveScanning()
        {
            if (Status == StateMachine.Started && !IsScanningActive)
            {
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                ScanModeChanged?.Invoke();
            }
            else if (Status == StateMachine.Stopped || Status == StateMachine.Aborted)
            {
                Status = StateMachine.Starting;
                scanInit();

                //set the passive scan and start a new scanning thread 
                watcher.ScanningMode = BluetoothLEScanningMode.Active;
                await scan();
            }
        }
        

        private void scanInit()
        {
            clearDiscoveredDevices(discoveredDevices);
            clearDiscoveredDevices(lastDiscoveredDevices);

            if (watcher == null)
            {
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.Received += this.OnAdvertisementReceived;
                watcher.Stopped += this.OnScanStopped;
                checkFilterStatus();
            }

            //bool isExtendedAdvertisementSupported = await CosmedBluetoothLEAdapter.CreateAsync().IsExtendedAdvertisingSupported;
            //watcher.AllowExtendedAdvertisements = isExtendedAdvertisementSupported;          
        }


        private async Task scan()
        {    
            try
            {
                if (Status == StateMachine.Starting && await checkBLEAdapterError())
                {
                    if (watcher != null)
                    {
                        lastScanningMode = watcher.ScanningMode;
                        watcher.Start();
                        Status = StateMachine.Started;

                        int count = 0;
                        while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started && count < wait)
                        {
                            count++;
                        }
                    }
                    //this could be used to set a timed scan to avoid useless energy consumption
                    //if (HasScanTimeout)
                    //{
                    //    Task.Run(() => OnScanTimeout());
                    //}
                }

                StartedListening?.Invoke(this, watcher.ScanningMode);                 
            }
            catch (System.Exception e)
            {
                Status = StateMachine.Aborted;                
                await Task.Run( () => ScanInterrupted?.Invoke(this, new ScanAbortedException("scan aborted during initiation", e))).ConfigureAwait(false);
            }
        }


        public void StopScanning()
        {
            if(Status == StateMachine.Started || Status == StateMachine.Paused)
            {
                Status = StateMachine.Stopping;
                watcher.Received -= OnAdvertisementReceived;
                watcher.Stop();
                Status = StateMachine.Stopped;

                int count = 0;
                while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped && count < wait)
                {
                    count++;
                }

                //watcher.Stopped -= OnScanStopped;
                watcher = null;
            }
        }


        public void PauseScanning()
        {
            if (Status == StateMachine.Started)
            {
                if (watcher != null)
                {
                    watcher.Stop();
                    Status = StateMachine.Paused;

                    int count = 0;
                    while (watcher.Status != BluetoothLEAdvertisementWatcherStatus.Stopped && count < wait)
                    {
                        count++;
                    }

                    lastScanningMode = watcher.ScanningMode;
                }
            }
        }

        //allows to resume the scan without loosing the peviously collected devices
        // in case the method is called as first scanning call, the default scanning mode is Passive
        public async void ResumeScanning()
        {
            if (Status == StateMachine.Paused)
            {              
                if (watcher != null)
                {
                    Status = StateMachine.Starting;
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

        //add the filter to the watcher in case of active filtering so the filtering is not lost
        //when scan is stopped and restarted
        private void checkFilterStatus()
        {
            if(filter != null && IsFilteringActive)
            {
                if(watcher != null)
                {
                    watcher.AdvertisementFilter = filter.AdvertisementFilter;
                    watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
                }
            }
        }
        
        
        public void SetFilter(CosmedBluetoothLEAdvertisementFilter filter)
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


        //il filtro potrebbe essere già null, e quindi restituire null. Altrimenti dà la possibilità di modificare  il filtro rimosso
        public void RemoveFilter()
        {
            if(watcher != null)
            {
                watcher.AdvertisementFilter = new BluetoothLEAdvertisementFilter();
                watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter();
                CosmedBluetoothLEAdvertisementFilter filterTemp = filter;
                filter = null;              
            }
            IsFilteringActive = false;

        }
        #endregion


        #region Helper methods

        public BluetoothLEAdvertisementWatcherStatus GetWatcherStatus => watcher != null ? watcher.Status : BluetoothLEAdvertisementWatcherStatus.Stopped;
       
        public bool IsScanningStarted => Status == StateMachine.Started;
       
        public bool IsScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;
        
        public bool IsScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;



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

        
        private async Task<bool> checkBLEAdapterError()
        {
            try
            {
                CosmedBluetoothLEAdapter adapter = await CosmedBluetoothLEAdapter.CreateAsync();
                string str = adapter.HexAddress;
                Console.WriteLine(str);

                if (!adapter.IsLowEnergySupported)
                {
                    Status = StateMachine.Aborted;
                    var e = new BluetoothLeNotSupportedException("Your adapter " + str + " does not support Bluetooth Low Energy");
                    await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                    return false;
                }
                if (!adapter.IsCentralRoleSupported)
                {
                    Status = StateMachine.Aborted;
                    var e = new CentralRoleNotSupportedException("Your adapter does not support Central Role");
                    await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                    return false;
                }
            }
            catch(BluetoothAdapterCommunicationFailureException e)
            {
                Status = StateMachine.Aborted;
                await Task.Run(() => ScanInterrupted?.Invoke(this, e)).ConfigureAwait(false);
                return false;
            }

            return true;

        }


        private bool check(CosmedBleAdvertisedDevice dev)
        {
            if(ShowOnlyConnectableDevices)
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


        public BluetoothLEAdvertisementWatcher GetWatcher()
        {
            return watcher;
        }


        //provare a migliorarlo in modo che non sia bloccante (no while)
        private void CheckBleStatus()
        {
            //controlla se c´é un device BLE acceso, lasciare che lo gestisca l'utente

            bool bleIsOn = CosmedBluetoothLEAdapter.IsBluetoothLEOn();

            if (!bleIsOn)
            {
                Console.WriteLine("ble dovrebbe essere OFF");
            }
            else
            {
                Console.WriteLine("ble dovrebbe essere ON");
            }

            //se non c´è aspetta che ci sia per poi riprendere la scansione
            while (!CosmedBluetoothLEAdapter.IsBluetoothLEOn()) { Thread.Sleep(1000); }
            Console.WriteLine("bluetooth attivo, riprende lo scan");
            StartActiveScanning();
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
            Console.WriteLine("adv received +++++++++++++++");
            if (sender == watcher && args != null && Status == StateMachine.Started)
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
            if(sender.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {
                Status = StateMachine.Aborted;
                Task.Run( () => ScanInterrupted?.Invoke(this, new ScanAbortedException("scan aborted, please check your Bluetooth adapter"))).ConfigureAwait(false);              
            }
            else
            {
                Task.Run(() => ScanStopped?.Invoke(this, args)).ConfigureAwait(false);
                Console.WriteLine("Stopping scan");
                Console.WriteLine(args.Error.ToString());
            }

        }



        private void OnScanInterrupted(CosmedBluetoothLEAdvertisementWatcher sender, Exception arg)
        {
   
                Console.WriteLine("((((((((((((((" + arg.Message + ")))))))))))))))))))))))");

            //questo è solo per test, da lasciare implementare all`utente
            CheckBleStatus();  
        }


        private void OnScanTimeout()
        {
            Task.Delay(ScanTimeout * 1000);
            StopScanning();
        }
        #endregion

    }



    public enum StateMachine
    {
        Starting = 0,
        Started = 1,
        Aborted = 2,
        Stopping = 3,
        Stopped = 4,
        Paused = 5,
        Resuming = 6
    }


    //questo posso eliminarlo
    public class PeriodicTask
    {
        public static async Task Run(Action action, TimeSpan period, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(period, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                    action();
            }
        }

        public static Task Run(Action action, TimeSpan period)
        {
            return Run(action, period, CancellationToken.None);
        }
    }



}

