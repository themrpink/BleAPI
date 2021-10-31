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
            /*      CosmedBluetoothLEAdvertisementWatcher scanner = new CosmedBluetoothLEAdvertisementWatcher();
                  TestWatcher test = new TestWatcher();
                  test.StartScan();
                  test.StopScan();
                  test.StartScan();
                  Console.WriteLine("stacco bth");
                  Thread.Sleep(5000);
                  Console.WriteLine(  "vediamo se esce dal lock, primi enter");
                  CheckStatusAfterAbortAndNewStart();
                  CheckBthConnectionIsOffDuringScan();
                  CheckStartBthConnectionIsOn();
                  CheckBleAdapter();

                  Console.WriteLine("press enter for next test");
                  Console.ReadLine();
                  TestBleOff();

                  General general = new General();

                  general.AddOperation(new BleReadOperation());
                  //general.AddOperation(new BleWriteOperation());

                  foreach (var l in general.GetList())
                  {
                      //l.ExcecuteOperation();
                  }

                  Console.ReadLine();
      */
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


            //  scan.NewDeviceDiscovered += (device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            // scan.OnScanStopped += (sender, args) => { };

            //start the auto update collection
            //IAdvertisedDevicesCollection Devices = scan.GetUpdatedDiscoveredDevices();

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

                // Devices = scan.GetUpdatedDiscoveredDevices();
                // scan.StopScanning();
                // Devices = scan.GetUpdatedDiscoveredDevices();
                // count++; ;
            }


            //Console.ReadLine();

            scanner.StopScanning();
        }



        public async static void ReadDevice(CosmedBleAdvertisedDevice device)
        {

            //device.SetBleDevice();
            //CosmedBleConnection connection = new CosmedBleConnection(device);
            /*BluetoothLEDevice dev = await device.GetBleDevice();
            if (dev != null)
            {
                Console.WriteLine("!!!!!! bledevice !!!!!!!!!!");
                Console.WriteLine("name " + dev.DeviceInformation.Name);
                Console.WriteLine("name " + dev.Name);
                Console.WriteLine("category: " + dev.Appearance.Category.ToString("X"));
                Console.WriteLine("!!!!!! fine bledevice !!!!!!!!!!");
            }
                //BluetoothLEDevice b = bledev.GetResults();
            */
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


            dw.Added += DeviceDiscoveredHandler;
            dw.EnumerationCompleted += onEnumerationCompleted;
            dw.Start();

            Console.ReadLine();
            dw.Stop();
        }

        public static void DeviceDiscoveredHandler(DeviceWatcher dw, DeviceInformation di)
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


        public static void CheckBleAdapter()
        {
            Console.WriteLine("going to check, press enter");
            Console.ReadLine();
            bool b = CosmedBluetoothLEAdapter.IsLowEnergySupported;
            Thread.Sleep(2000);
            Console.WriteLine("checked, press enter");
            Console.ReadLine();

        }



        public static void TestBleOff()
        {
            CosmedBluetoothLEAdvertisementWatcher watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.StartPassiveScanning();
            Thread.Sleep(1000);
            Console.WriteLine("now turn off ble and press a key");
            Console.ReadLine();
            Console.WriteLine("now waíting 1 second");
            Thread.Sleep(1000);
            //here I should turn ble off on the machine and see if an exception is thrown
            Console.WriteLine("now pausing scan");
            watcher.PauseScanning();
            Thread.Sleep(1000);
            Console.WriteLine("scan paused");
            Console.WriteLine("press a key");
            Console.WriteLine("now resuming scan");
            watcher.ResumeScanning();
            Thread.Sleep(1000);
            Console.WriteLine("è successo qualcosa? press enter");
            Console.ReadLine();
        }

        public static void CheckStartBthConnectionIsOn()
        {
            Console.WriteLine("turn off ble and press a key");
            Console.ReadLine();
            CosmedBluetoothLEAdvertisementWatcher watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.StartPassiveScanning();
            watcher = null;
        }

        public static void CheckStatusAfterAbortAndNewStart()
        {
            Console.WriteLine("turn on ble and press a key");
            Console.ReadLine();
            CosmedBluetoothLEAdvertisementWatcher watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.StartPassiveScanning();
            Console.WriteLine("turn bth off and wait, then press enter");
            Thread.Sleep(10000);
            watcher.StartPassiveScanning();
            Console.ReadLine();
            watcher.StopScanning();
            watcher = null;
        }
        public static void CheckBthConnectionIsOffDuringScan()
        {
            
            Console.WriteLine("turn on ble and press a key");
            Console.ReadLine();
            CosmedBluetoothLEAdvertisementWatcher watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.StartPassiveScanning();
            Console.WriteLine("turn bth off and wait, then press enter");
            Thread.Sleep(10000);
            Console.ReadLine();
            watcher = null;

        }

        public static void MeasureElapsingCounts()
        {
            List<BluetoothLEAdvertisementWatcherStatus> stati = new List<BluetoothLEAdvertisementWatcherStatus>();
            int i = 0;
            int j = 0;
            int k = 0;
            var testw = new TestWatcher();

            testw.StartScan();
            while (testw.watcher.Status != BluetoothLEAdvertisementWatcherStatus.Started)
            {
                i++;
            }

            testw.StopScan();
            while (testw.watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                k++;
            }
            //testw.StartScan();
            while (testw.watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping)
            {
                j++;
            }
            while (i < 15000000)
            {
                stati.Add(testw.watcher.Status);
                //if(testw.watcher.Status == BluetoothLEAdvertisementWatcherStatus.Stopping)
                //{
                //    Console.WriteLine("stato stopping");
                //}
                i++;
            }

            for (i = 0; i < stati.Count; i++)
            {
                if (stati[i] == BluetoothLEAdvertisementWatcherStatus.Stopped)
                {
                    Console.WriteLine("stato stopping");
                }
                //Console.WriteLine(stati[i].ToString());
            }

            Console.WriteLine("finito, premi enter");
            Console.ReadLine();
        }
    }
}

