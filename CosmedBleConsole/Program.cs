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
using Windows.Devices.Radios;
using System.Management;

namespace CosmedBleConsole
{

    /// <summary>
    /// Wraps and makes use of <see cref="BluetoothLEAdvertisementWatcher"/>
    /// </summary>
    public class ppppp
    {
        public static bool check = true;
        public static IReadOnlyCollection<CosmedBleAdvertisedDevice> dev;

        public async static Task Main(String[] args)
        {
            //to use the other implementation option
            // DeviceEnumeration.StartDeviceEnumeration();

            //Console.ReadLine();

            var scanner = new CosmedBluetoothLEAdvertisementWatcher();

            //create a filter
            CosmedBluetoothLEAdvertisementFilter filter = new CosmedBluetoothLEAdvertisementFilter();

            //set the filter
            filter.SetFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported).SetCompanyID("4D");

            //scan with filter
            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);




            Console.WriteLine("_______________________scanning____________________");

            //start scanning
            scanner.StartActiveScanning();
            //scan.StartPassiveScanning();

            //scanner.StopScanning();
            //print the results and connect
            CosmedBleConnectedDevice connectionTemp;
            int c = 0;
            while (true)
            {
                scanner.StartActiveScanning();
                foreach (var device in scanner.AllDiscoveredDevices)
                {
                    // if (device.DeviceName.Equals("myname") && device.IsConnectable && device.HasScanResponse)
                    {
                        device.PrintAdvertisement();

                        var conn = await Connector.StartConnectionProcessAsync(scanner, device);

                        if (device.IsConnectable && device.DeviceName.Equals("myname"))
                        {
                            c += 1;
                            CosmedBleConnectedDevice connection = await CosmedBleConnectedDevice.CreateAsync(device);
                            if (c == 1)
                                connectionTemp = connection;
                            Console.WriteLine("in connection with:" + device.DeviceAddress);
                            Console.WriteLine("watcher status: " + scanner.GetWatcherStatus.ToString());


                            string s = BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(connection.BluetoothAddress);
                            Console.WriteLine("device selector: " + s);
                            Console.WriteLine("press enter");
                            // Console.ReadLine();

                            //connection.MaintainConnection();
                            //Thread.Sleep(5000);

                            //await connection.StartConnectionAsync();
                            //pairing
                            connection.Pair().Wait();

                            //var ser = await connection.get
                            var results = await connection.DiscoverAllGattServicesAndCharacteristics();
                            foreach (var service in results.Keys)
                            {
                                foreach (var ch in await results[service])
                                {
                                    Console.WriteLine("leggo chars, round: " + c);
                                    await ch.Write(0x01);
                                    var a = await ch.Read();
                                    await ch.SubscribeToIndications(connection.CharacteristicValueChanged);
                                    await ch.SubscribeToNotifications(connection.CharacteristicValueChanged);
                                }
                            }

                            var results2 = await connection.DiscoverAllCosmedGattServicesAndCharacteristics();
                            foreach (var service in results2.Keys)
                            {
                                foreach (var ch in await results2[service])
                                {
                                    Console.WriteLine("leggo chars, round: " + c);
                                    await ch.Write(0x01, prova);
                                    var a = await ch.Read(connection.CharacteristicErrorFound);
                                    await ch.SubscribeToIndications(connection.CharacteristicValueChanged);
                                    await ch.SubscribeToNotifications(connection.CharacteristicValueChanged);
                                }
                            }
                            foreach (var service in results2.Keys)
                            {
                                foreach (var ch in await results2[service])
                                {
                                    Console.WriteLine("leggo chars, round: " + c);
                                    await ch.Write(0x01, prova);
                                    var a = await ch.Read(connection.CharacteristicErrorFound);
                                    await ch.SubscribeToIndications(connection.CharacteristicValueChanged, prova);
                                    await ch.SubscribeToNotifications(connection.CharacteristicValueChanged);
                                }
                            }

                        }
                    }
                }
                Thread.Sleep(1000);
            }
            //scanner.StopScanning();
        }
        public static void prova (CosmedGattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        {
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }
        public static void SetCallbacks(CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            watcher.NewDeviceDiscovered += (watch, device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            watcher.ScanStopped += (sender, args) => { };
        }
    }


}




/*
 * TODO:
 * capire cosa è che fa connettere e come:
 * new BleDevice
 * GattService
 * GattResult
 * 
 * uno di questi o tutti e tre? se metto canMaintainConnection=false non si interrompe?
 * 
 * 
 * poi le cose che non è possibile fare magari cerco di farle con p/invoke
 * 
 * poi io devo ottenere: o la lista dei services, o un service o una caratteristica
 * con ognuno di questi devo poter comunicare. La meglio cosa sarebbe avere a disposizione dei metodi a seconda del tipo
 * se non può fare la read, allora la read non appare. Oppure questo è un overhead e lascio fare il controllo all´utente?
 *
 *
 
 se connection ha un metodo che restituisce gli uuid e glieli passo per interrogare un service, ha senso come design?
 
 poi, come usare le appearance? posso fare una ricerca più "approfondita"? magari ci sta, così ottengo più informazioni sui device
 
 
 */


