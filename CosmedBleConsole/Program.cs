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
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CosmedBleConsole
{

    /// <summary>
    /// Wraps and makes use of <see cref="BluetoothLEAdvertisementWatcher"/>
    /// </summary>
    public class ppppp
    {
        public static bool goon = false;
        public static bool check = true;
        public static IReadOnlyCollection<CosmedBleAdvertisedDevice> dev;
        public static DevicePairingKinds ceremonySelection =    DevicePairingKinds.None |
                                                                DevicePairingKinds.ConfirmOnly |
                                                                DevicePairingKinds.ConfirmPinMatch |
                                                                DevicePairingKinds.DisplayPin |
                                                                DevicePairingKinds.ProvidePasswordCredential |
                                                                DevicePairingKinds.ProvidePin;

        public static DevicePairingProtectionLevel minProtectionLevel =  DevicePairingProtectionLevel.None |
                                                                         DevicePairingProtectionLevel.Default |
                                                                         DevicePairingProtectionLevel.Encryption |
                                                                         DevicePairingProtectionLevel.EncryptionAndAuthentication;
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
            await scanner.StartActiveScanning();
            //scan.StartPassiveScanning();

            while (true)
            {
                await scanner.StartActiveScanning();
                foreach (var device in scanner.AllDiscoveredDevices)
                {
                     if (device.IsConnectable)
                     {
                        device.PrintAdvertisement();

                        var conn = await Connector.StartConnectionProcessAsync(scanner, device);

                        if (device.IsConnectable)  //&& device.DeviceName.Equals("myname")
                        {
                            scanner.StopScanning();
 
                            //get device
                            CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);
                            
                            //pairing
                            PairedDevice pairedDevice = await PairingService.GetPairedDevice(connectionDevice, ceremonySelection, minProtectionLevel);
                            
                            var guid = BluetoothUuidHelper.FromShortId(0x1800);
                            BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(pairedDevice.BluetoothDeviceId.Id).AsTask();
                            //string selector = "(" + GattDeviceService.GetDeviceSelectorFromUuid(guid) + ")"
                            //                    + " AND (System.DeviceInterface.Bluetooth.DeviceAddress:=\""
                            //                    + bleDevice.BluetoothAddress.ToString("X") + "\")";


                            //var gattServiceWatcher = DeviceInformation.CreateWatcher(selector);

                            
                            //gattServiceWatcher.Added += (s, a) => { Console.WriteLine("trovato il device " + a.Name); Console.WriteLine("pairing: " + a.Pairing.IsPaired); Console.WriteLine("press enter"); goon = true; Console.ReadLine(); };
                            ////create a discovery service for a paired device
                            //gattServiceWatcher.Start();
                      
                            GattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(pairedDevice);


                            //while (!goon) { }
                            //request the gatt result (collection of services)
                            GattDeviceServicesResult gattResult = await discoveryService.GetGattServicesAsync();

                            //request all services and relative characteristics (dictionary)
                            var allServicesAndCharacteristics = await discoveryService.DiscoverAllGattServicesAndCharacteristics();
                            
                            
                   
                            DeviceInformationCollection PairedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)).AsTask();
                            //Connected bluetooth devices
                            DeviceInformationCollection ConnectedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)).AsTask();
                            Thread.Sleep(3000);


                            Console.WriteLine("stampa dal gatt results");
                            foreach(var service in gattResult.Services)
                            {
                                Console.WriteLine("__service__");
                                service.Print();
                                var characteristics = await service.GetCharacteristicsAsync().AsTask();

                                foreach(var characteristic in characteristics.Characteristics)
                                {
                                    Console.WriteLine("__characteristic__");
                                    characteristic.Print();
                                }
                            }


                            Console.WriteLine("stampa dal dizionario");
                            foreach (var service in allServicesAndCharacteristics.Keys)
                            {
                                Console.WriteLine("__service__");
                                service.Print();
 
                                foreach (var characteristic in await allServicesAndCharacteristics[service])
                                {
                                    Console.WriteLine("__characteristic__");
                                    characteristic.Print();
                                }
                            }


                            discoveryService.ClearServices();
                            var r = await PairingService.Unpair(pairedDevice);
                            pairedDevice.Disconnect();
                            

                            scanner.StartActiveScanning();
                        }
                    }
                }
                Thread.Sleep(3000);
            }
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

    }



}







