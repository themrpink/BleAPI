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

                count += 1;
            }
        }
        static void Main(String[] args)
        {

            //DeviceEnumeration();


            CosmedBluetoothLEAdvertisementFilter filter = new CosmedBluetoothLEAdvertisementFilter();
            //filter//.setFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported)
              //  .SetCompanyID("4D");
            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);

            CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher();
           //s scan.NewDeviceDiscovered += (device) => { Console.WriteLine("waiting"); Thread.Sleep(5000); };

            Console.WriteLine("_______________________scanning____________________");
          //  scan.NewDeviceDiscovered += (device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            // scan.OnScanStopped += (sender, args) => { };
            // var dev2 = scan.allDiscoveredDevicesUpdate;
            IAdvertisedDevicesCollection Devices = scan.getUpdatedDiscoveredDevices();
            scan.StartActiveScanning();
            while (true)
            {
                //scan.StartActiveScanning();
                foreach (var device in Devices.getLastDiscoveredDevices())
                {
                    Console.WriteLine("----------------------normal response-----------------");
               
                    //printAdvertisement(device);
                    device.PrintAdvertisement();
                    ReadDevice(device);
                    if (device.HasScanResponse)
                    {
                        Console.WriteLine("++++++++++++++++scan response+++++++++++++");
                        //printScanResponses(device);
                        device.PrintScanResponses();                       
                    }
                }
                Thread.Sleep(5000);
                // Devices = scan.GetUpdatedDiscoveredDevices();
                // scan.StopScanning();
                // Devices = scan.GetUpdatedDiscoveredDevices();
                // count++; ;
            }
            
            
            //Console.ReadLine();

            scan.StopScanning();
        }



        public static void ReadDevice(CosmedBleAdvertisedDevice device)
        {

            device.SetBleDevice();
            BluetoothLEDevice dev = device.device;
            if (dev != null)
            {
                Console.WriteLine("!!!!!! bledevice !!!!!!!!!!");
                Console.WriteLine("name " + dev.DeviceInformation.Name);
                Console.WriteLine("name " + dev.Name);
                Console.WriteLine("category: " + string.Format("X", dev.Appearance.Category));
                Console.WriteLine("!!!!!! fine bledevice !!!!!!!!!!");
            }
                //BluetoothLEDevice b = bledev.GetResults();

        }
        

        public static void DeviceEnumeration()
        {
            DeviceWatcher dw = DeviceInformation.CreateWatcher();
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };
            
            // BT_Code: Example showing paired and non-paired in a single query., questo é specifico per il BLE
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            dw =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);


            dw.Added += onDeviceDiscovered;
            dw.EnumerationCompleted += onEnumerationCompleted;
            dw.Start();

            Console.ReadLine();
            dw.Stop();
        }

        public static void onDeviceDiscovered(DeviceWatcher dw, DeviceInformation di)
        {
            Console.WriteLine("----");
            Console.WriteLine("name: " + di.Name.ToString());
            Console.WriteLine("kind: " + di.Kind.ToString());
            Console.WriteLine("id: " + di.Id);
            Console.WriteLine("bool: " + di.Pairing.CanPair);
            Console.WriteLine("protection: " + di.Pairing.ProtectionLevel.ToString());
            Console.WriteLine("is paired 1: " + di.Pairing.IsPaired);
            IAsyncOperation<DevicePairingResult> result = di.Pairing.PairAsync();
            Thread.Sleep(100);
            Console.WriteLine("status: " + result.GetResults().Status.ToString());
            Console.WriteLine(result.GetResults().ProtectionLevelUsed.ToString());
            foreach (var p in di.Properties.Keys)
            {
                Console.WriteLine("prop key: " + p);
                if (di.Properties[p] != null)
                    Console.WriteLine("prop value: " + di.Properties[p].ToString());
            }
        }

        public static void onEnumerationCompleted(DeviceWatcher dw, object di)
        {
            Console.WriteLine("----");
            Console.WriteLine("fine");
            Console.ReadLine();
        }
    }
}

