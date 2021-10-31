using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    public class TestWatcher
    {
        public BluetoothLEAdvertisementWatcher watcher;
        public readonly object threadlock = new object();

        public TestWatcher()
        {
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += OnAdvertisement;
            watcher.Stopped += OnScanStopped;

        }
        public void StartScan()
        {
            lock (threadlock)
            {
                if (watcher == null)
                {
                    watcher = new BluetoothLEAdvertisementWatcher();
                    watcher.Received += OnAdvertisement;
                    watcher.Stopped += OnScanStopped;
                }
                    
                watcher.Start();
                while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Created)
                {
                    Console.WriteLine("created");   
                }

            }        
        }

        public void StopScan()
        {
            lock (threadlock)
            {
                watcher.Stop();
                while (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping)
                {
                    Console.WriteLine("stopping");
                }
                watcher = null;
            }

        

                
            
            
        }

        public void OnAdvertisement(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Console.WriteLine("adv received");
        }
        public void OnScanStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            if (watcher != null && watcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
            {
                Console.WriteLine("attenzione, stato aborted");
            }
            Console.WriteLine("scan stopping");
        }
    }
}
