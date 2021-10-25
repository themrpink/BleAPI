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

        //the structure where the discovered devices are saved
        private readonly Dictionary<ulong, CosmedBleAdvertisedDevice> discoveredDevices;

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

        //questo dovrebbe servire a limitare il numero di devices scoperti in caso di scan molto lunghi
        //potrebbe essere usato per svuotare il dizionario ogni volta questo valore raggiunto
        //oppure per sostituire i nuovi trovati (se non presenti quindi nel dizionario) con quello trovato meno recentemente (confrontare i timestamps)
        //quindi al momento del controllo quando viene ricevuto un advertisement, se il numero MAX di elementi nel dizionario
        //è stato raggiunto, applicare le operazioni appena definite
        private readonly int defualtMaxScanResults = 256;

        //questi servono per i test sulle performance
        private Stopwatch stopwatchAdvertisement = new Stopwatch();
        private int countAdvertisement = 0;
        private List<long> timeElapsed = new List<long>();

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


        /// <summary>
        /// Subscribe to this event to get the discovered devices collection regulary updated
        /// </summary>
        public event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> AllDevicesCollectionUpdated;
        public event Action<IReadOnlyCollection<CosmedBleAdvertisedDevice>> RecentDevicesCollectionUpdated;
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

        public IReadOnlyCollection<CosmedBleAdvertisedDevice> recentlyDiscoveredDevices
        {
            get
            {
                lock (ThreadLock)
                {
                    return discoveredDevices.Values.Where
                        ( (device) =>
                            {
                                if (timeout > 0)
                                {
                                    var diff = DateTime.UtcNow - TimeSpan.FromSeconds(timeout);
                                    return device.timestamp >= diff;
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
            this.filter = filter;
            //watcher = new BluetoothLEAdvertisementWatcher(this.filter.AdvertisementFilter);
            //watcher.AdvertisementFilter = filter.AdvertisementFilter;
            watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
        }

        #endregion

        //#if DEBUG
        //#else
        //#endif

        #region Class methods
        /// <summary>
        /// Initialize and Start passive scanning. 
        /// </summary>
        public void startPassiveScanning()
        {
            CheckBLEAdapter();
            clearDiscoveredDevices();
            
            //if a passive scan is already running, do nothing
            if (isScanningStarted && isScanningPassive)
                return;

            //if a not passive scanning is already active stop it
            else if (isScanningStarted && !isScanningPassive)
            {
                stopScanning();
                ScanModeChanged?.Invoke();
            }
            scanInit();

            //set the passive scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;            
            Scan();            
        }


        /// <summary>
        /// Initialize and start Active scanning
        /// </summary>
        public void startActiveScanning()
        {
            CheckBLEAdapter();
            clearDiscoveredDevices();
            //if an active scan is already running, do nothing
            if (isScanningStarted && isScanningActive)
                return;
            
            //if a not active scanning is already active stop it
            else if (isScanningStarted && !isScanningActive)
            {
                stopScanning();
                ScanModeChanged?.Invoke();
            }
            scanInit();
            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;

            Scan();
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


        private void Scan()
        {
            try
            { 
                watcher.Start();
                StartedListening?.Invoke(this, watcher.ScanningMode);
            }
            catch (System.Exception e)
            {
                isCollectingDevices = false;
                watcher.Received -= this.OnAdvertisementReceived;
                throw e;
            }
        }


        public void stopScanning()
        {
            if(watcher != null)
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

        }


        /// <summary>
        /// avvia l´aggiornamento automatico della lista di devices
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

                    if (updateThread is null)
                    {
                        updateThread = new Thread(() => sendUpdatedDevicesService(ms));
                        updateThread.Start();
                    }
                }          
            }
            return AutoUpdatedDevices;
        }


        public IAdvertisedDevicesCollection StopUpdateDevices()
        {
            lock (LockUpdateDevices)
            {
                isCollectingDevices = false;
            }
            return AutoUpdatedDevices;
        }

        //update the devices
        private void sendUpdatedDevicesService(int ms)
        {
            while (true)
            {
                lock (LockUpdateDevices)
                {
                    if(!isCollectingDevices)
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

        #endregion


        #region Helper methods

        public BluetoothLEAdvertisementWatcherStatus getWatcherStatus => watcher!=null ? watcher.Status : BluetoothLEAdvertisementWatcherStatus.Stopped;
        public bool isScanningStarted => watcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        public bool isScanningPassive => watcher?.ScanningMode == BluetoothLEScanningMode.Passive;
        public bool isScanningActive => watcher?.ScanningMode == BluetoothLEScanningMode.Active;


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



        public void addDiscoveredDevices(CosmedBleAdvertisedDevice device)
        {            
            lock (ThreadLock)
            {
                bool oldDevice = discoveredDevices.ContainsKey(device.DeviceAddress);
                if (oldDevice)
                {
                    discoveredDevices[device.DeviceAddress].setAdvertisement    (
                                                                                device.advertisementContent.Advertisement, 
                                                                                device.advertisementContent.AdvertisementType, 
                                                                                device.timestamp
                                                                                );
                }
                else
                {
                    discoveredDevices[device.DeviceAddress] = device;
                    NewDeviceDiscovered?.Invoke(device);
                }
            }
        }

        private void CheckBLEAdapter()
        {
            string str = CosmedBluetoothLEAdapter.HexAddress;
            Console.WriteLine(str);
            if (!CosmedBluetoothLEAdapter.IsLowEnergySupported)
                throw new InvalidOperationException("Your adapter " + str + " does not support Bluetooth Low Energy");

            if (!CosmedBluetoothLEAdapter.IsCentralRoleSupported)
                throw new InvalidOperationException("Your adapter does not support Central Role");
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
            /*
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
            if(device != null)
                Console.WriteLine($"BLEWATCHER Found: {device.Name}" + "@@@@@@@@@@@@@@@@@@@@@@");*/
            countAdvertisement += 1;
            stopwatchAdvertisement.Start();

            lock (ThreadLock)
            {
                if (sender == watcher) 
                {
                    bool newDevice = !discoveredDevices.ContainsKey(args.BluetoothAddress);
                    if (newDevice)
                    {
                        discoveredDevices[args.BluetoothAddress] = new CosmedBleAdvertisedDevice(args.BluetoothAddress, args.Timestamp, args.IsConnectable, args.Advertisement, args.AdvertisementType);
                        KnownDevices.Add(discoveredDevices[args.BluetoothAddress]);
                    }
                    else
                    {
                        try
                        {
                            discoveredDevices[args.BluetoothAddress].setAdvertisement(args.Advertisement, args.AdvertisementType, args.Timestamp);
                        }
                        catch (KeyNotFoundException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    NewDeviceDiscovered?.Invoke(discoveredDevices[args.BluetoothAddress]);
                }                                                           
            }   
            
            stopwatchAdvertisement.Stop();
            timeElapsed.Add(stopwatchAdvertisement.ElapsedMilliseconds);
            
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



}



/*
 * TODO:
 * aggiungere gli eventi al watcher e all updateCollection, in modo da avere:
 * tutti i device
 * quelli recenti
 * solo quelli aggiunti (i new discovered vanno in una lista che svuoto ogni volta che viene richiesta)
 * 
 * 
 * come fare con lo active scan? devo creare per i device una struttura in modo da avere i dati aggiornati
 * certo che se lo sovrascrivo poi i dati non sono piú ordinati
 * 
 * ecco come:
 * ogni device ha i suoi dati, devo metterli sotto scanResponse e advertisment
 * per fare questo wrappo gli BleAdvertisement (ci metto anche BleAdvertisementType) in ScanResponse e Advertisemet (forse basta che li estendano?
 * tipo con ScanResponseAdvertisement e ScanResponseAdvertisementType)
 * 
 */