using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Enumeration;
using System.Runtime;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using CosmedBleLib.DeviceDiscovery;
using CosmedBleLib.ConnectionServices;
using CosmedBleLib.GattCommunication;
using CosmedBleLib.Extensions;
using CosmedBleLib.Helpers;


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
            //await UseCaseTests.GattDiscovery();
            //await UseCaseTests.QuickStart();
            await GeneralTest();
        }


        public static async Task GeneralTest()
        {
            FilterBuilder cfb = FilterBuilder.Init(true);
            IFilter f = cfb.ClearAdvertisementFilter().BuildFilter();

            //creates a filter for devices called "BLE Peripheral"
            IFilter filter = FilterBuilder.Init().SetLocalName("BLE Peripheral").BuildFilter();//.SetFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode |   BluetoothLEAdvertisementFlags.LimitedDiscoverableMode |BluetoothLEAdvertisementFlags.ClassicNotSupported).BuildFilter();

            //create a scanner with the given filter
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();

            //starts an active scan
            await scanner.StartActiveScanning();

            //to use the other implementation option
            // DeviceEnumeration.StartDeviceEnumeration();

            //Console.ReadLine();

            //var scanner = new CosmedBluetoothLEAdvertisementWatcher();

            //create a filter
            //CosmedBluetoothLEAdvertisementFilter filter = new CosmedBluetoothLEAdvertisementFilter();

            //set the filter
            //filter.SetFlags(BluetoothLEAdvertisementFlags.GeneralDiscoverableMode | BluetoothLEAdvertisementFlags.ClassicNotSupported).SetCompanyID("4D");

            //scan with filter
            //CosmedBluetoothLEAdvertisementWatcher scan = new CosmedBluetoothLEAdvertisementWatcher(filter);

            Console.WriteLine("_______________________scanning____________________");

            //start scanning
            await scanner.StartActiveScanning();
            //scan.StartPassiveScanning();
            ErrorFoundClass.ErrorFound += (a, s) => Console.WriteLine("?????????????????error ???????????????");
            
            while (scanner.status != StateMachine.Stopped)
            {

                foreach (var device in scanner.AllDiscoveredDevices)
                {
                    if (device.IsConnectable)
                    {
                        ((CosmedBleAdvertisedDevice)device).PrintAdvertisement();

                        if (device.IsConnectable)// && device.DeviceName.Equals("myname"))
                        {
                            scanner.PauseScanning();
                            try
                            {
                                //get device. it´s possible to set some event handler
                                CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);

                                //pairing. it´s possible to call an overload with custom event handler
                                //PairingResult pairedDevice = await PairingService.PairDevice(connectionDevice, ceremonySelection, minProtectionLevel);

                                //it´s possible to set some event handler
                                IGattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(connectionDevice);
                                var rw = discoveryService.StartReliableWriteTransaction();
                                //request the gatt result (collection of services)
                                GattDeviceServicesResult gattResult = await discoveryService.GetAllGattServicesAsync();
                                
                                Console.WriteLine("stampa dal gatt results");
                                foreach (var service in gattResult.Services)
                                {
                                    Console.WriteLine("__service__");
                                    service.Print();

                                    GattCharacteristicsResult characteristics;
                                    
                                    characteristics = await service.GetCharacteristicsAsync().ToTask();
                                    ErrorFoundClass.ErrorFound += (a, s) => Console.WriteLine("error");
                                    foreach (var characteristic in characteristics.Characteristics)
                                    {
                                        
                                        Console.WriteLine("__characteristic__");
                                        characteristic.Print();
                                        var read = await characteristic.Read();
                                        Console.WriteLine("read result hex: " + read.HexValue);
                                        Console.WriteLine("read result utf8: " + read.UTF8Value);

                                        byte[] value = { 0x001 };
                                        var buff = BufferWriter.ToIBuffer(value);
                                        characteristic.AddCharacteristicToReliableWrite(rw, buff);

                                        var write = await characteristic.WriteWithResult(value, GattWriteOption.WriteWithResponse);
                                        var write2 = await characteristic.WriteWithResult(value, GattWriteOption.WriteWithoutResponse);

                                        var notify = await characteristic.SubscribeToNotification(
                                                                                                    (s, a) =>

                                                                                                    {
                                                                                                        Console.WriteLine("notification:");
                                                                                                        Console.WriteLine(a.Timestamp.ToString());
                                                                                                        IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                                                        string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                                                        Console.WriteLine("buffer content: " + val);

                                                                                                    }
                                                                                                   // (s, e) => Console.WriteLine("error")
                                                                                                  );
                                        if (notify == CosmedGattCommunicationStatus.Success)
                                            Thread.Sleep(1000);
                                        var unsub = await characteristic.UnSubscribe();
                                        var indicate = await characteristic.SubscribeToIndication(
                                                                                                    (s, a) =>

                                                                                                    {
                                                                                                        Console.WriteLine("indication:");
                                                                                                        Console.WriteLine(a.Timestamp.ToString());
                                                                                                        IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                                                        string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                                                        Console.WriteLine("buffer content: " + val);

                                                                                                    }
                                                                                                    //(s, e) => Console.WriteLine("error")
                                                                                                  );
                                        if (indicate == CosmedGattCommunicationStatus.Success)
                                            Thread.Sleep(1000);
                                        var unsub2 = await characteristic.UnSubscribe();
                                    }
                                }
                               // var rwr = await rw.CommitWithResultAsync().ToTask();
                                discoveryService.ClearServices();
                                var r = await PairingService.Unpair(connectionDevice);
                                connectionDevice.ClearBluetoothLEDevice();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                //throw new GattCommunicationException("impossible to access the Service " + service.Uuid.ToString(), e);
                            }

                        }
                    }
                }

                scanner.ResumeScanning();
                Thread.Sleep(3000);
            }
        }
        
   



        //public static void prova (CosmedGattCharacteristic sender, CosmedGattErrorFoundEventArgs args)
        //{
        //    Console.WriteLine("!!!!!!!!!!!!!Called from prova!!!!!!!!!!!!!!!!!!!!!!!");
        //}

        public static void UseInterfaces() 
        {
            IBleScanner watcher = new CosmedBluetoothLEAdvertisementWatcher();

            IFilter filter = FilterBuilder.Init().BuildFilter();

            watcher.SetFilter(filter);

            watcher.StartActiveScanning();

            var devices = watcher.AllDiscoveredDevices;


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

