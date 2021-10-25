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
            filter//.setFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported)
                .SetCompanyID("4D");
            CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);

            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher();

        

            Console.WriteLine("_______________________scanning____________________");
          //  scan.NewDeviceDiscovered += (device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            // scan.OnScanStopped += (sender, args) => { };
            // var dev2 = scan.allDiscoveredDevicesUpdate;
            IAdvertisedDevicesCollection Devices = scan.getUpdatedDiscoveredDevices();
            scan.startActiveScanning();
            int count = 0;
            while (true)
            {
                scan.startActiveScanning();
                Thread.Sleep(5000);
                foreach (var device in Devices.getLastDiscoveredDevices())
                {
                    Console.WriteLine("----------------------normal response-----------------");
               
                    //printAdvertisement(device);
                    device.printAdvertisement();
                    ReadDevice(device);
                    if (device.hasAScanResponse)
                    {
                        Console.WriteLine("++++++++++++++++scan response+++++++++++++");
                        //printScanResponses(device);
                        device.printScanResponses();                       
                    }
                }
               // Devices = scan.getUpdatedDiscoveredDevices();
               // scan.stopScanning();
               // Devices = scan.getUpdatedDiscoveredDevices();
                count++; ;
            }
            
            
            //Console.ReadLine();

            scan.stopScanning();
        }




        public static void printAdvertisement(CosmedBleAdvertisedDevice device)
        {
            Console.WriteLine("found: " + device.DeviceAddress);
            Console.WriteLine("mac: " + device.getDeviceAddress());
            Console.WriteLine("scan type: " + device.advertisementContent.AdvertisementType.ToString());
            Console.WriteLine("local name: " + device.advertisementContent.Advertisement.LocalName);
            Console.WriteLine("company numb: " + device.advertisementContent.Advertisement.ManufacturerData.Count);

            printNameFlagsGuid(device.scanResponseAdvertisementContent);
            printManufacturerData(device.scanResponseAdvertisementContent.Advertisement.ManufacturerData);
            printDataSections(device.scanResponseAdvertisementContent.Advertisement.DataSections);
        }


        public static void printScanResponses(CosmedBleAdvertisedDevice device)
        {
            if (device.scanResponseAdvertisementContent != null
                && device.scanResponseAdvertisementContent.Advertisement != null
                && device.scanResponseAdvertisementContent.Advertisement.DataSections != null)
            {
                printNameFlagsGuid(device.scanResponseAdvertisementContent, "sr");
                printManufacturerData(device.scanResponseAdvertisementContent.Advertisement.ManufacturerData, "sr");
                printDataSections(device.scanResponseAdvertisementContent.Advertisement.DataSections, "sr");

            }
        }

        public static void printNameFlagsGuid(AdvertisementContent devAdv, string advType="")
        {
            Console.WriteLine(advType + " localname: " + devAdv.Advertisement.LocalName);
            Console.WriteLine(advType + " flags: " + devAdv.Advertisement.Flags.ToString());
            Console.WriteLine(advType + " guid numb: " + devAdv.Advertisement.ServiceUuids.Count);
            foreach (Guid g in devAdv.Advertisement.ServiceUuids)
                Console.WriteLine(advType + " guid: " + g.ToString());
        }

        public static void printManufacturerData(IList<BluetoothLEManufacturerData> manData, string advType = "")
        {
            Console.WriteLine(advType + " Manufacturer data count: " + manData.Count);

            foreach (BluetoothLEManufacturerData m in manData)
            {
                Console.WriteLine(advType + " company id: " + m.CompanyId);
                Console.WriteLine(advType + " company id HEX: " + m.CompanyId.ToString("X"));
                Console.WriteLine(advType + " manufacturer data capacity: " + m.Data.Capacity);
                Console.WriteLine(advType + " manufacturer data length: " + m.Data.Length);

                var data = new byte[m.Data.Length];
                using (var reader = DataReader.FromBuffer(m.Data))
                {
                    reader.ReadBytes(data);
                }
                string dataContent = BitConverter.ToString(data); ;
                Console.WriteLine(advType + " manufacturer buffer: " + dataContent);
     
            }
        }
        public static void printDataSections(IList<BluetoothLEAdvertisementDataSection> dataSections, string advType = "")
        {
            Console.WriteLine(advType + " data numb: " + dataSections.Count);
            foreach (BluetoothLEAdvertisementDataSection ds in dataSections)
            {

                Console.WriteLine(advType + "data type (data section): " + ds.DataType);
                Console.WriteLine(advType + "data type in hex (data section): " + ds.DataType.ToString("X"));
                Console.WriteLine(advType + "data length: " + ds.Data.Length);
                Console.WriteLine(advType + "data capacity: " + ds.Data.Capacity);

                var data = new byte[ds.Data.Length];
                using (var reader = DataReader.FromBuffer(ds.Data))
                {
                    reader.ReadBytes(data);
                }
                string dataContent = BitConverter.ToString(data);
                Console.WriteLine(advType + "data buffer with bit converter: " + string.Format("0x: {0}", dataContent));

            }
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

