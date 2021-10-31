using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CosmedBleLib;
using System.Text.RegularExpressions;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;

namespace CosmedBleConsole
{

    /// <summary>
    /// Wraps and makes use of <see cref="BluetoothLEAdvertisementWatcher"/>
    /// </summary>
    public class ppppp
    {
        public static bool check = true;
        public static IReadOnlyCollection<CosmedBleAdvertisedDevice> dev;

        public static Task Main(String[] args)
        {
            //to use the other implementation option
            //DeviceEnumeration();

            var scanner = new CosmedBluetoothLEAdvertisementWatcher();

            //create a filter
            CosmedBluetoothLEAdvertisementFilter filter = new CosmedBluetoothLEAdvertisementFilter();
            
            //set the filter
            filter.setFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported).SetCompanyID("4D");
           
            //scan with filter
            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);

            

            Console.WriteLine("_______________________scanning____________________");

            //start scanning
            scanner.StartActiveScanning();
            //scan.StartPassiveScanning();


            //print the results and connect
            while (true)
            {
                foreach (var device in scanner.RecentlyDiscoveredDevices)
                {
                    // if (device.DeviceName.Equals("myname") && device.IsConnectable && device.HasScanResponse)
                    {

                        device.PrintAdvertisement();

                        if (device.IsConnectable && device.DeviceName.Equals("myname"))
                        {
                            CosmedBleConnection connection = new CosmedBleConnection(device, scanner);
                            Console.WriteLine("in connection with:" + device.DeviceAddress);
                            Console.WriteLine("watcher status: " + scanner.GetWatcherStatus.ToString());
                            connection.StartConnectionAsync();
                            Task.WaitAll();
                            connection.Pair().Wait();
                        }

                    }

                }
                Thread.Sleep(5000);
            }

            //scanner.StopScanning();
        }


        public static void SetCallbacks(CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            //  scan.NewDeviceDiscovered += (device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            //scan.OnScanStopped += (sender, args) => { };
        }
    }
}

