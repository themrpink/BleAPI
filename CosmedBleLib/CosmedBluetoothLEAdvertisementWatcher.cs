using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;


namespace CosmedBleLib
{
    /// <summary>
    /// wrapper class for the BleAdvertisementWatcher
    /// </summary>
    public class CosmedBluetoothLEAdvertisementWatcher
    {

        #region Private fields
        private readonly BluetoothLEAdvertisementWatcher watcher;

        //the structure where the discovered devices are saved
        private readonly Dictionary<ulong, CosmedBleDevice> DiscoveredDevices;

        //filter to apply to the discovered devices
        private CosmedBluetoothLEAdvertisementFilter filter;

        //lock for safe access to shared resources
        private readonly object ThreadLock = new object();

        //thread used for the scanning
        private Thread t;

        //
        private bool isCollectingDevices = false;

        //used for the discovered devices collection update frequency, in milliseconds
        private int UpdateTime;

        private AutoUpdateDiscoveredDevicesCollection UpdatedCollection;

        #endregion


        #region Delegates

        //public events
        public event Action startedListening;
        public event Action stoppedListening;
        public event Action scanModeChanged;

        /// <summary>
        ///Fired when a new device is discovered
        /// </summary>
        public event Action<CosmedBleDevice> newDeviceDiscovered;


        /// <summary>
        /// Subscribe to this event to get the discovered devices collection regulary updated
        /// </summary>
        public event Action<IReadOnlyCollection<CosmedBleDevice>> devicesCollectionUpdated;

        #endregion


        #region Properties

        //in milliseconds
        public double timeout { get; set; } = 0;

        /// <summary>
        /// this structure is created at every user request from the devices dictionary. The multithreaded access is
        /// protected by a lock
        /// </summary>
        public IReadOnlyCollection<CosmedBleDevice> allDiscoveredDevices
        {
            get
            {
                cleanTimeouts();
                lock (ThreadLock)
                {                  
                    return DiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        public IReadOnlyCollection<CosmedBleDevice> allDiscoveredDevicesUpdate;

        /// <summary>
        /// constructor: instatiate the watcher, the dictionary and add the events to the delegates of the watcher
        /// </summary>
        /// 


        #endregion


        #region constructors
        public CosmedBluetoothLEAdvertisementWatcher()
        {
            Thread.CurrentThread.Name = "main thread";
            watcher = new BluetoothLEAdvertisementWatcher();
            DiscoveredDevices = new Dictionary<ulong, CosmedBleDevice>();
            watcher.Received += this.OnAdvertisementReceived;
            watcher.Stopped += this.OnScanStopped;

            UpdatedCollection = new AutoUpdateDiscoveredDevicesCollection(this);
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


        #region Class methods
        /// <summary>
        /// Initialize and Start passive scanning. 
        /// </summary>
        public void startPassiveScanning()
        {
            //if a passive scan is already running, do nothing
            if (isScanningStarted && isScanningPassive)
                return;

            //if a not passive scanning is already active stop it
            else if (isScanningStarted && isScanningPassive)
            {               
                //watcher.Received -= this.OnAdvertisementReceived;
                clearDiscoveredDevices();
                watcher.Stop();
                if (t != null && t.IsAlive)
                    t.Join();
                scanModeChanged?.Invoke();
            }

            //set the passive scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;
            watcher.AllowExtendedAdvertisements = false;
            t = new Thread(watcher.Start)
            {
                Name = "passive scanning thread"
            };
            t.SetApartmentState(ApartmentState.STA);
            //watcher.Received += this.OnAdvertisementReceived;
            t.Start();
            startedListening?.Invoke();
        }


        /// <summary>
        /// Initialize and start Active scanning
        /// </summary>
        public void startActiveScanning()
        {
            //if an active scan is already running, do nothing
            if (isScanningStarted && isScanningActive)
                return;

            //if a not active scanning is already active stop it
            else if (isScanningStarted && isScanningActive)
            {
                clearDiscoveredDevices();
                watcher.Stop();
                //watcher.Received -= this.OnAdvertisementReceived;   
                if (t != null && t.IsAlive)
                    t.Join();
                scanModeChanged?.Invoke();
            }

            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.AllowExtendedAdvertisements = true;
            t = new Thread(watcher.Start)
            {
                Name = "active scanning thread"
            };
            t.SetApartmentState(ApartmentState.STA);
            //watcher.Received += this.OnAdvertisementReceived;
            t.Start();
            startedListening?.Invoke();
        }


        public void stopScanning()
        {
            //watcher.Received -= this.OnAdvertisementReceived;            
            watcher.Stop();
            clearDiscoveredDevices();
            isCollectingDevices = false;          
        }


        /// <summary>
        /// Restituisce una collezione di devices che con´registrando l´evento 
        /// scan.devicesCollectionUpdated += (updatedDevices) => { devices = updatedDevices; };
        /// si aggiorna ogni ms millisecondi
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public IReadOnlyCollection<CosmedBleDevice> getDiscoveredDevicesUpdated(int ms = 5000)
        {
            if (!isCollectingDevices)
                isCollectingDevices = true;
            else
                return allDiscoveredDevices;

            UpdateTime = ms;

            Thread update = new Thread(this.sendUpdatedDevicesService);
            update.Start();
            return allDiscoveredDevices;
        }

        #endregion


        #region Helper methods


        public BluetoothLEAdvertisementWatcherStatus getWatcherStatus => watcher.Status;
        public bool isScanningStarted => watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
        public bool isScanningPassive => watcher.ScanningMode != BluetoothLEScanningMode.Passive;
        public bool isScanningActive => watcher.ScanningMode != BluetoothLEScanningMode.Active;


        /// <summary>
        /// 
        /// svuota il dizionario dei devices
        /// </summary>
        private void clearDiscoveredDevices()
        {
            lock (ThreadLock)
            {
                DiscoveredDevices.Clear();
            }
        }

        private void cleanTimeouts()
        {
            if(timeout>0)
                lock(ThreadLock)
                {
                    var diff = DateTime.UtcNow - TimeSpan.FromSeconds(timeout);
                    DiscoveredDevices.Where(f => f.Value.timestamp < diff).ToList().ForEach(device =>
                    {
                        DiscoveredDevices.Remove(device.Key);
                    });
                }
        }

        //update the devices
        private void sendUpdatedDevicesService()
        {
            while (isCollectingDevices)
            {
                devicesCollectionUpdated?.Invoke(allDiscoveredDevices);
                Thread.Sleep(UpdateTime);             
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
            if (Thread.CurrentThread.Name==null)
                Thread.CurrentThread.Name = "OnAdvReceived " + sender.ScanningMode;

            Console.WriteLine("trovato device " + Thread.CurrentThread.Name + " " + sender.ScanningMode);
            Console.WriteLine(args.BluetoothAddress);
            cleanTimeouts();
            CosmedBleDevice device = new CosmedBleDevice(args.BluetoothAddress, args.Timestamp, args.IsConnectable, args.Advertisement, args.AdvertisementType);
            bool newDevice = !DiscoveredDevices.ContainsKey(args.BluetoothAddress);

            lock (ThreadLock)
            {
                DiscoveredDevices[args.BluetoothAddress] = device;
            }

            //qua fare un controllo: se per esempio il device é stato scoperto per la prima volta o dopo un certo lasso di tempo
            if(newDevice)
                newDeviceDiscovered?.Invoke(device);
        }

        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Console.WriteLine("Scanning stopped");
            stoppedListening?.Invoke();
        }


        //da implementare, forse
        private void OnScanModeChanged(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {

        }

        #endregion
    }
}