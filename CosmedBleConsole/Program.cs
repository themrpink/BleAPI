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
                     if (device.IsConnectable)
                     {
                        device.PrintAdvertisement();

                        var conn = await Connector.StartConnectionProcessAsync(scanner, device);

                        if (device.IsConnectable)  //&& device.DeviceName.Equals("myname")
                        {
                            scanner.StopScanning();
                            string s1 = BluetoothLEDevice.GetDeviceSelectorFromDeviceName("myname");

                            Console.WriteLine( s1);
                            c += 1;
                            //Paired bluetooth devices

                            CosmedBleConnectedDevice connection = await CosmedBleConnectedDevice.CreateAsync(device);
                            //var whatsthat = await DeviceInformation.FindAllAsync(s1).AsTask();
                            //int ount = whatsthat.Count;
                            //var dev = whatsthat[0];
                            //Console.WriteLine("nome: " + dev.Name + " is paired: " + dev.Pairing.IsPaired);
                            //Console.WriteLine("in connection with:" + device.DeviceAddress);
                            //Console.WriteLine("watcher status: " + scanner.GetWatcherStatus.ToString());
                            DeviceInformationCollection PairedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)).AsTask();
                            //Connected bluetooth devices
                            DeviceInformationCollection ConnectedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)).AsTask();
                            Thread.Sleep(3000);
                            //pairing
                            await connection.Pair(  DevicePairingKinds.None | 
                                                    DevicePairingKinds.ConfirmOnly |                                                                                       
                                                    DevicePairingKinds.ConfirmPinMatch |                                                                                       
                                                    DevicePairingKinds.DisplayPin |                                                                                       
                                                    DevicePairingKinds.ProvidePasswordCredential |                                                                                        
                                                    DevicePairingKinds.ProvidePin,
                                                    DevicePairingProtectionLevel.None |
                                                    DevicePairingProtectionLevel.Default |
                                                    DevicePairingProtectionLevel.Encryption |
                                                    DevicePairingProtectionLevel.EncryptionAndAuthentication);
                            Thread.Sleep(3000);
                            DeviceInformationCollection PairedBluetoothDevices2 = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)).AsTask();
                            //Connected bluetooth devices
                            DeviceInformationCollection ConnectedBluetoothDevices2 = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected)).AsTask();
                            Thread.Sleep(3000);
                            //connection.Dispose();

                            await connection.Unpair();
                            Thread.Sleep(3000);
                            
                            string s = BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(connection.BluetoothAddress);

                            connection.MaintainConnection();
                            Console.WriteLine("device selector: " + s);
                            await connection.Pair(DevicePairingKinds.None |
                        DevicePairingKinds.ConfirmOnly |
                        DevicePairingKinds.ConfirmPinMatch |
                        DevicePairingKinds.DisplayPin |
                        DevicePairingKinds.ProvidePasswordCredential |
                        DevicePairingKinds.ProvidePin,
                        DevicePairingProtectionLevel.None |
                        DevicePairingProtectionLevel.Default |
                        DevicePairingProtectionLevel.Encryption |
                        DevicePairingProtectionLevel.EncryptionAndAuthentication);
                            // connection.MaintainConnection();
                            var results2 = await connection.DiscoverAllCosmedGattServicesAndCharacteristics();

                            Console.WriteLine("primo turno, inserisco connection.CharacteristicErrorFound e vedo e ottengo l´errore");
                            //questo usa i CosmedGattCharacteristic
                            foreach (var service in results2.Keys)
                            {
                                Console.WriteLine("printing a service:");
                                Console.WriteLine("service handle: " + service.AttributeHandle.ToString("X2"));
                                Console.WriteLine("service uuid: " + service.Uuid.ToString());
                                Console.WriteLine("service device access information (current status): " + service.DeviceAccessInformation.CurrentStatus.ToString());
                                Console.WriteLine("service Gatt Session: " + service.Session);
                                foreach (var ch in await results2[service])
                                {
                                    ch.Print();
                                    Console.WriteLine("leggo chars 2, round: " + c);
                                    //await ch.Write(0x01, prova);
                                    var a = await ch.Read((sender, arg) => { Console.WriteLine("ok read"); });
                                    var b = await ch.SubscribeToIndication((sender, arg) => { Console.WriteLine("ok indicate"); });
                                    var d = await ch.SubscribeToNotification((sender, arg) => { Console.WriteLine("ok notify"); });
                                }
                            }

                            connection.Dispose();
                          //  ServicesWithCosmedCharacteristic(connection);

                          //  Thread.Sleep(10000);
                            string s2 = BluetoothLEDevice.GetDeviceSelectorFromDeviceName("myname");
                            Console.WriteLine(s1);

                            scanner.StartActiveScanning();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
            //scanner.StopScanning();
        }
        public static void prova (CosmedGattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        {
            Console.WriteLine("!!!!!!!!!!!!!Called from prova!!!!!!!!!!!!!!!!!!!!!!!");
        }
        public static void SetCallbacks(CosmedBluetoothLEAdvertisementWatcher watcher)
        {
            watcher.NewDeviceDiscovered += (watch, device) => { Console.WriteLine(device.ToString() + "+++++++++++++++new device+++++++++++++++"); };
            watcher.ScanStopped += (sender, args) => { };
        }

        public static async void ServicesWithExtensions(CosmedBleConnectedDevice connection)
        {
            //questo usa gli extension methods
            var results = await connection.DiscoverAllGattServicesAndCharacteristics();
            foreach (var service in results.Keys)
            {
                foreach (var ch in await results[service])
                {
                    Console.WriteLine("leggo chars 1, round: ");
                    await ch.Write(0x01);
                    var a = await ch.Read();
                   // await ch.SubscribeToIndications(connection.CharacteristicValueChanged);
                   // await ch.SubscribeToNotifications(connection.CharacteristicValueChanged);
                }
            }
        }


        public static async void ServicesWithCosmedCharacteristic(CosmedBleConnectedDevice connection)
        {
            var results2 = await connection.DiscoverAllCosmedGattServicesAndCharacteristics();
            foreach (var service in results2.Keys)
            {
                foreach (var ch in await results2[service])
                {
                    Console.WriteLine("============leggo chars 3, round: ================");
                    ch.Print();
                    var w = await ch.Write(0x01, prova);
                    var r = await ch.Read(prova);
                   // var i = await ch.SubscribeToIndications(connection.CharacteristicValueChanged, prova);
                   // var n = await ch.SubscribeToNotifications(connection.CharacteristicValueChanged);
                }
            }
        }

    }



}







