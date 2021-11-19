using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using CosmedBleLib;
namespace CosmedBleConsole
{
    class DeviceEnumeration
    {
        public static void StartDeviceEnumeration()
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

        public static async void DeviceDiscoveredHandler(DeviceWatcher dw, DeviceInformation di)
        {
            Console.WriteLine("----");
            Console.WriteLine("name: " + di.Name.ToString());
            Console.WriteLine("kind: " + di.Kind.ToString());
            Console.WriteLine("id: " + di.Id);
            Console.WriteLine("bool: " + di.Pairing.CanPair);

            foreach (var p in di.Properties.Keys)
            {
                Console.WriteLine("prop key: " + p);
                if (di.Properties[p] != null)
                    Console.WriteLine("prop value: " + di.Properties[p].ToString());
            }

            if (di.Pairing.CanPair)
            {
                Console.WriteLine("trying to pair...");
                Console.WriteLine("protection: " + di.Pairing.ProtectionLevel.ToString());
                Console.WriteLine("is paired: " + di.Pairing.IsPaired);
                var result = await di.Pairing.PairAsync().AsTask();
                ///Thread.Sleep(100);
                Console.WriteLine("status: " + result.Status.ToString());
                Console.WriteLine(result.ProtectionLevelUsed.ToString());
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
