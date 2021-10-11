using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CosmedBleLib;

namespace CosmedBleConsole
{


    /// <summary>
    /// Wraps and makes use of <see cref="BluetoothLEAdvertisementWatcher"/>
    /// </summary>
    public class ppppp
    {
        public static bool check = true;
        public static IReadOnlyCollection<CosmedBleDevice> dev;

        static void checkUpdate()
        {
            int count = 0;
            Thread.Sleep(500);
            while (check)
            {
                Thread.Sleep(5000);
                Console.WriteLine("lettura "+count);
                foreach(var device in dev)
                    Console.WriteLine("found: " + device.DeviceAddress);

                foreach (var device in CosmedBluetoothLEAdvertisementWatcher.allDiscoveredDevices2)
                    Console.WriteLine("found2: " + device.DeviceAddress);
                count += 1;
            }
        }
        static void Main(String[] args)
        {
            CosmedBluetoothLEAdvertisementFilter filter = new CosmedBluetoothLEAdvertisementFilter();
            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);

            CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher();

            scan.newDeviceDiscovered += (device) => { Console.WriteLine(device.ToString() + "++++++++++++++++++++++++++++++++"); };
            // scan.OnScanStopped += (sender, args) => { };

            dev = scan.getDiscoveredDevicesUpdated(5000);
            scan.devicesCollectionUpdated += (udatedDevices) => { dev = udatedDevices; };

                  scan.startPassiveScanning();
            Thread t = new Thread(checkUpdate);
            t.Start();
            Thread.Sleep(15000);
            scan.startPassiveScanning();
            Thread.Sleep(15000);
            scan.stopScanning();
            Thread.Sleep(10000);

            check = false;
            scan.stopScanning();

            var devices = scan.allDiscoveredDevices;
            foreach (var device in devices)
            {
                Console.WriteLine(device.Advertisement.LocalName);
                foreach(var l in device.Advertisement.ManufacturerData)
                    Console.WriteLine(l);
                Console.WriteLine(device.DeviceAddress);
                Console.WriteLine(device.Advertisement.ServiceUuids);
                Console.WriteLine(device.AdvertisementType.ToString());
            }
            scan.startActiveScanning();
            Thread.Sleep(3500);

            var devices2 = scan.allDiscoveredDevices;
            foreach(var device in devices2)
            {
                Console.WriteLine("localname: " + device.Advertisement.LocalName);
                Console.WriteLine("manufacturer: ");
                foreach (var man in device.Advertisement.ManufacturerData)
                    Console.WriteLine(man.CompanyId);
                Console.WriteLine("device address: " + device.DeviceAddress);
                Console.WriteLine("uuid: " + device.Advertisement.ServiceUuids);
                foreach (var man in device.Advertisement.ServiceUuids)
                    Console.WriteLine(man.ToString());
                Console.WriteLine("type: " + device.AdvertisementType.ToString());
            }
          // scan.stopScanning();

            while (true)
            {
                  Console.WriteLine("passive");
                scan.startPassiveScanning();
                Thread.Sleep(5000);
                   Console.WriteLine("active");
                scan.startActiveScanning();
                Thread.Sleep(5000);
                   Console.WriteLine("passive");             
                scan.startPassiveScanning();
                Thread.Sleep(5000);
                   Console.WriteLine("active");
                scan.startActiveScanning();
                Thread.Sleep(5000);
                   Console.WriteLine("stop");
                scan.stopScanning();
                object o = scan.allDiscoveredDevices;
            }
        }
    }
}

