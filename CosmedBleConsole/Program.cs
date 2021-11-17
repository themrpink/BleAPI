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

                        if (device.IsConnectable && device.DeviceName.Equals("myname"))
                        {

                            //var p = await Connector.StartConnectionProcessAsync(scanner, device);
                            scanner.StopScanning();
 
                            //get device. it´s possible to set some event handler
                            CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);
                            
                            //pairing. it´s possible to call an overload with custom event handler
                            PairingResult pairedDevice = await PairingService.GetPairedDevice(connectionDevice, ceremonySelection, minProtectionLevel);

                            //it´s possible to set some event handler
                            GattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(connectionDevice);


                            //request the gatt result (collection of services)
                            GattDeviceServicesResult gattResult = await discoveryService.GetGattServicesAsync();


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
                                    var read = await characteristic.Read();
                                    var write = await characteristic.Write(0x001);
                                    var notify = await characteristic.SubscribeToNotification(  
                                                                                                (s, a) => Console.WriteLine(a.Timestamp.ToString()), 
                                                                                                (s, e) => Console.WriteLine("error")
                                                                                              );
                                    var indicate = await characteristic.SubscribeToIndication();
                                    var unsub = await characteristic.UnSubscribe();
                                }
                            }
                        
                            discoveryService.ClearServices();
                            var r = await PairingService.Unpair(connectionDevice);
                            connectionDevice.Disconnect();                          
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

    //var guid = BluetoothUuidHelper.FromShortId(0x1800);
    //BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(pairedDevice.BluetoothDeviceId.Id).AsTask();
    //string selector = "(" + GattDeviceService.GetDeviceSelectorFromUuid(guid) + ")"
    //                    + " AND (System.DeviceInterface.Bluetooth.DeviceAddress:=\""
    //                    + bleDevice.BluetoothAddress.ToString("X") + "\")";
    //00001800-0000-1000-8000-00805f9b34fb

    //var gattServiceWatcher = DeviceInformation.CreateWatcher(selector);


    //gattServiceWatcher.Added += (s, a) => { Console.WriteLine("trovato il device " + a.Name); Console.WriteLine("pairing: " + a.Pairing.IsPaired); Console.WriteLine("press enter"); goon = true; Console.ReadLine(); };
    ////create a discovery service for a paired device
    //gattServiceWatcher.Start();



    //DeviceInformationCollection PairedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true)).AsTask();
    ////Connected bluetooth devices
    //DeviceInformationCollection ConnectedBluetoothDevices = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)).AsTask();
    //Thread.Sleep(3000);

}







