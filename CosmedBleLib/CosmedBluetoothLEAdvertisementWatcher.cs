using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace CosmedBleLib
{
    /// <summary>
    /// wrapper class for the BleAdvertisementWatcher
    /// </summary>
    public class CosmedBluetoothLEAdvertisementWatcher
    {
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

        //public events
        public event Action startedListening = () => {};
        public event Action stoppedListening = () => {};
        public event Action scanModeChanged = () => {};
        public event Action<CosmedBleDevice> newDeviceDiscovered = (device) => { };


        /// <summary>
        /// Subscribe to this event to get the discovered devices collection regulary updated
        /// </summary>
        public event Action<IReadOnlyCollection<CosmedBleDevice>> devicesCollectionUpdated = (devices) => { IReadOnlyCollection<CosmedBleDevice> dev = devices; };


        /// <summary>
        /// this structure is create at every user request from the devices dictionary. The multithreaded access is
        /// protected by a lock
         /// </summary>
        public IReadOnlyCollection<CosmedBleDevice> allDiscoveredDevices
        {
            get
            {
                lock (ThreadLock)
                {
                    return DiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        public static IReadOnlyCollection<CosmedBleDevice> allDiscoveredDevices2;

        /// <summary>
        /// constructor: instatiate the watcher, the dictionary and add the events to the delegates of the watcher
        /// </summary>
        public CosmedBluetoothLEAdvertisementWatcher()
        {
            Thread.CurrentThread.Name = "main thread";
            watcher = new BluetoothLEAdvertisementWatcher();
            DiscoveredDevices = new Dictionary<ulong, CosmedBleDevice>();
            //watcher.Received += this.OnAdvertisementReceived;
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
           // watcher.AdvertisementFilter = filter.AdvertisementFilter;
            watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
        }

  
        /// <summary>
        /// Initialize and Start passive scanning. 
        /// </summary>
        public void startPassiveScanning()
        {
            //if a passive scan is already running, do nothing
            if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started && watcher.ScanningMode == BluetoothLEScanningMode.Passive)
                return;

            //if a not passive scanning is already active stop it
            else if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started && watcher.ScanningMode != BluetoothLEScanningMode.Passive)
            {               
                watcher.Received -= this.OnAdvertisementReceived;
                watcher.Stop();
                if (t != null && t.IsAlive)
                    t.Join();
                scanModeChanged();
            }

            //set the passive scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;
            watcher.AllowExtendedAdvertisements = false;
            t = new Thread(watcher.Start)
            {
                Name = "passive scanning thread"
            };
            t.SetApartmentState(ApartmentState.STA);
            watcher.Received += this.OnAdvertisementReceived;
            t.Start();
            startedListening();
        }


        /// <summary>
        /// Initialize and start Active scanning
        /// </summary>
        public void startActiveScanning()
        {
            //if an active scan is already running, do nothing
            if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started && watcher.ScanningMode == BluetoothLEScanningMode.Active)
                return;

            //if a not active scanning is already active stop it
            else if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started && watcher.ScanningMode != BluetoothLEScanningMode.Active)
            {
                watcher.Stop();
                watcher.Received -= this.OnAdvertisementReceived;   
                if (t != null && t.IsAlive)
                    t.Join();
                scanModeChanged();
            }

            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.AllowExtendedAdvertisements = true;
            t = new Thread(watcher.Start)
            {
                Name = "active scanning thread"
            };
            t.SetApartmentState(ApartmentState.STA);
            watcher.Received += this.OnAdvertisementReceived;
            t.Start();
            startedListening();
        }


        public void stopScanning()
        {
            watcher.Received -= this.OnAdvertisementReceived;
            watcher.Stop();
            isCollectingDevices = false;
        }


        public bool isScanning => watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
       
        public BluetoothLEAdvertisementWatcherStatus getWatcherStatus => watcher.Status;


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<CosmedBleDevice> getDiscoveredDevicesUpdated()
        {
            if (!isCollectingDevices)
                isCollectingDevices = true;
            else
                return allDiscoveredDevices;

            UpdateTime = 5000;

            Thread update = new Thread(this.sendUpdatedDevicesService);
            update.Start();
            return allDiscoveredDevices;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public IReadOnlyCollection<CosmedBleDevice> getDiscoveredDevicesUpdated(int ms)
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

        private void sendUpdatedDevicesService()
        {
            while (isCollectingDevices)
            {
                devicesCollectionUpdated(allDiscoveredDevices);
                allDiscoveredDevices2 = allDiscoveredDevices;
                Thread.Sleep(UpdateTime);             
            }
        }

        /// <summary>
        /// Evento che salva gli advertisement nel dizionario
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {


            if (Thread.CurrentThread.Name==null)
                Thread.CurrentThread.Name = "OnAdvReceived " + sender.ScanningMode;

            Console.WriteLine("trovato device " + Thread.CurrentThread.Name + " " + sender.ScanningMode);
            Console.WriteLine(args.BluetoothAddress);
           
            CosmedBleDevice device = new CosmedBleDevice(args.BluetoothAddress, args.Timestamp, args.IsConnectable, args.Advertisement, args.AdvertisementType);
            bool newDevice = !DiscoveredDevices.ContainsKey(args.BluetoothAddress);

            lock (ThreadLock)
            {
                DiscoveredDevices[args.BluetoothAddress] = device;

            }

            //qua fare un controllo: se per esempio il device é stato scoperto per la prima volta o dopo un certo lasso di tempo
            if(newDevice)
                newDeviceDiscovered(device);
        }

        //evento in caso di scansione interrotta
        private void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Console.WriteLine("Scanning stopped");
            stoppedListening();
        }


        //da implementare, forse
        private void OnScanModeChanged(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {

        }


    }
}