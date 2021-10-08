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

        //control to start or stop the scan
        private bool scanON = true;
        /// <summary>
        /// constructor: instatiate the watcher, the dictionary and add the events to the delegates of the watcher
        /// </summary>
        public CosmedBluetoothLEAdvertisementWatcher()
        {
            Thread.CurrentThread.Name = "main thread";
            watcher = new BluetoothLEAdvertisementWatcher();
            DiscoveredDevices = new Dictionary<ulong, CosmedBleDevice>();
            watcher.Received += this.OnAdvertisementReceived;
            watcher.Stopped += this.OnScanStopped;
        }

        /// <summary>
        /// constructor setting the filter
        /// </summary>
        /// <param name="advertisementFilter"></param>
        public CosmedBluetoothLEAdvertisementWatcher(CosmedBluetoothLEAdvertisementFilter filter) : this()
        {
            this.filter = filter;
            //watcher = new BluetoothLEAdvertisementWatcher(this.filter.AdvertisementFilter);
            watcher.AdvertisementFilter = filter.AdvertisementFilter;
            watcher.SignalStrengthFilter = filter.SignalStrengthFilter;
        }

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
                scanON = false;   
                if(t != null)
                    t.Join();               
            }

            //set the passive scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;
            watcher.AllowExtendedAdvertisements = false;
            scanON = true;
            t = new Thread(this.scan)
            {
                Name = "scanning thread"
            };
            t.Start();
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
                scanON = false;
                if (t != null)
                    t.Join();
            }

            //set the active scan and start a new scanning thread 
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.AllowExtendedAdvertisements = true;
            scanON = true;
            t = new Thread(this.scan)
            {
                Name = "scanning thread"
            };
            t.Start();
            if (t!=null&&(watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started))
            {
                scanON = false;
                t.Join();
            }
        }


        /// <summary>
        /// Run by the scanning thread, it scans repeatedly until the scanON is true, then it stops the scan.
        /// Allow the user to have a running scan
        /// </summary>
        private void scan()
        {
            watcher.Start();
            while (scanON)
            {
                Thread.Sleep(200);
            }
            watcher.Stop();
        }


        public void stopScanning()
        {
            scanON = false;
        }


        public bool isScanning => watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
       
        public BluetoothLEAdvertisementWatcherStatus getWatcherStatus => watcher.Status;
      

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
           /* if (scanON == false)
            {
                //Console.WriteLine("####################################scanON, elimina " + Thread.CurrentThread.Name);
                Thread.CurrentThread.Abort();
            }
              
            else if ( sender.ScanningMode != watcher.ScanningMode)
            {
                //Console.WriteLine("####################################ci sono due advertisers, elimina " + Thread.CurrentThread.Name);
                Thread.CurrentThread.Abort();
            }*/

            if (Thread.CurrentThread.Name==null)
                Thread.CurrentThread.Name = "OnAdvReceived "+ sender.ScanningMode.ToString();
            Console.WriteLine("trovato device " + Thread.CurrentThread.Name + " " + sender.ScanningMode + " " + watcher.ScanningMode);
            //Console.WriteLine(watcher.ScanningMode.ToString());
            Console.WriteLine(args.BluetoothAddress);
            lock (ThreadLock)
            {
                DiscoveredDevices[args.BluetoothAddress] = new CosmedBleDevice(args.BluetoothAddress, args.Timestamp, args.IsConnectable, args.Advertisement, args.AdvertisementType);
            }
           
            //Console.WriteLine("--------------");

            
        }

        public void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            Console.WriteLine("Scanning stopped");
        }


    }
}


/*
 * 
 * TODO:
 * dal video di angelsix vedere che struttura dati utilizza per mettere i dati sui device.
 * come crea l´oggetto device
 * Questa struttura verrà scritta dall´evento onAdvertisementReceived
 * in qualche modo si dovrà controllare se questo già esiste nella struttura , in tal caso sovrascriverlo
 * la struttura deve essere protetta da un mutex per scrittura produttore e lettura consumatore
 * devo anche capire come eliminare i device non piú validi
 * 
 * 
 * poi pensare a come usare il filtro:
 * 1) cosa filtrare:
 *      nome
 *      codice
 *      segnale
 *      white list
 * 2) come
 *      passare un filtro come parametro del costruttore, creare dei metodi per filtrare?
 *      creare (wrappare) oggetti nuovi o usare quelli preesistenti nella libreria?
 * 
 * 3)il watcher contiene la struttura dati? questa viene creata a ogni richiesta o sempre disponibile
 * con un metodo? la posso passare separata dall´oggetto (watcher) che l´ha creata? oppure é meglio tenerla attaccatta?
 * penso sia meglio tenerla attaccata. Ci vuole anche la possibilitá di verificare che lo stato del watcher sia attivo
 * cioè non stoppato, altrimenti la struttura dati deve essere invalidata.
 * */