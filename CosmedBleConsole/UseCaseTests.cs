using CosmedBleLib.ConnectionServices;
using CosmedBleLib.DeviceDiscovery;
using CosmedBleLib.Extensions;
using CosmedBleLib.GattCommunication;
using CosmedBleLib.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace CosmedBleConsole
{
    public static class UseCaseTests

    {

        public static DevicePairingKinds ceremonySelection = DevicePairingKinds.None |
                                                        DevicePairingKinds.ConfirmOnly |
                                                        DevicePairingKinds.ConfirmPinMatch |
                                                        DevicePairingKinds.DisplayPin |
                                                        DevicePairingKinds.ProvidePasswordCredential |
                                                        DevicePairingKinds.ProvidePin;

        public static DevicePairingProtectionLevel minProtectionLevel = DevicePairingProtectionLevel.None |
                                                                         DevicePairingProtectionLevel.Default |
                                                                         DevicePairingProtectionLevel.Encryption |
                                                                         DevicePairingProtectionLevel.EncryptionAndAuthentication;

        public static string path = "C:\\Users\\themr\\Desktop\\tirocinio\\test\\";

        public static void InitTest(string filename)
        {
            //return System.IO.File.CreateText(filename);
            TextWriterTraceListener[] listeners = new TextWriterTraceListener[] { new TextWriterTraceListener(path + filename) };
            Debug.Listeners.AddRange(listeners);
            Debug.AutoFlush = true;

        }


        public static void UC1_DiscoverDevices_1_active()
        {
            InitTest("UC1_DiscoverDevices");
            Debug.WriteLine("Some Value", "Some Category");
            Debug.WriteLine("Some Other Value");
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();
            scanner.StartActiveScanning();
            while (true) {; }
            Debug.Flush();
        }

        public static void UC1_DiscoverDevices_1_passive()
        {
            InitTest("UC1_DiscoverDevices");
            Debug.WriteLine("Some Value", "Some Category");
            Debug.WriteLine("Some Other Value");
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();
            scanner.StartPassiveScanning();
            while (true) {; }
            Debug.Flush();
        }

        public static void UC1_DiscoverDevices_2_active()
        {
            InitTest("UC1_DiscoverDevices");
            Debug.WriteLine("Some Value", "Some Category");
            Debug.WriteLine("Some Other Value");
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();
            scanner.StartActiveScanning();
            while (true) {; }
            Debug.Flush();
        }

        public static async Task UC2_a_1_ConnectToDevice()
        {
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();
           
            //starts an active scan
            await scanner.StartActiveScanning();


            while (scanner.status != StateMachine.Stopped)
            {

                foreach (var device in scanner.AllDiscoveredDevices)
                {
                  
                    if (device.IsConnectable)
                    {
                        device.ScanResponseReceived += (s,a) => { Console.WriteLine("!!!!!!sca response received!!!!!!!!!!!"); };
                        Thread.Sleep(5000);
                        ((CosmedBleAdvertisedDevice)device).PrintAdvertisement();

                        if (device.IsConnectable)// && device.DeviceName.Equals("myname"))

                        {
                            scanner.PauseScanning();
                            try
                            {
                                //get device. it´s possible to set some event handler
                                CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);

                                //pairing. it´s possible to call an overload with custom event handler
                                PairingResult pairedDevice = await PairingService.PairDevice(connectionDevice, ceremonySelection, minProtectionLevel);

                                //it´s possible to set some event handler
                                IGattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(connectionDevice);

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

                                        var write = await characteristic.WriteWithResult(value, GattWriteOption.WriteWithResponse);

                                        var notify = await characteristic.SubscribeToNotification(
                                                                                                    (s, a) =>

                                                                                                    {
                                                                                                        Console.WriteLine("notification:");
                                                                                                        Console.WriteLine(a.Timestamp.ToString());
                                                                                                        IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                                                        string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                                                        Console.WriteLine("buffer content: " + val);

                                                                                                    },
                                                                                                    (s, e) => Console.WriteLine("error")
                                                                                                  );

                                        var indicate = await characteristic.SubscribeToIndication(
                                                                                                    (s, a) =>

                                                                                                    {
                                                                                                        Console.WriteLine("indication:");
                                                                                                        Console.WriteLine(a.Timestamp.ToString());
                                                                                                        IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                                                        string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                                                        Console.WriteLine("buffer content: " + val);

                                                                                                    },
                                                                                                    (s, e) => Console.WriteLine("error")
                                                                                                  );
                                        var unsub = await characteristic.UnSubscribe();
                                    }
                                }

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
            }
        }


        public static async Task QuickStart()
        {
            //Step 1: creates a filter for devices called "HeartRateDevice"
            Guid uuid = BluetoothUuidHelper.FromShortId(0x180d);
            IFilter filter = FilterBuilder.Init().SetServiceUUID(uuid).BuildFilter();

            //Step 2: creates a scanner with the given filter
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher(filter);

            //Step 3: starts an active scan
            await scanner.StartActiveScanning();

            //Step 4: checking the results until the scanner is stopped
            while (scanner.status != StateMachine.Stopped)
            {
                //this collection will be updated at every cycle of the while loop
                var discoveredDevices = scanner.AllDiscoveredDevices;

                //iteration over the discovered devices
                foreach (var device in discoveredDevices)
                {
                    //we are only interested in connectable devices for further Gatt communication
                    if (device.IsConnectable)
                    {
                        //scannig can be stopped here
                        scanner.PauseScanning();

                        try
                        {
                            //creates an instance of the remote device
                            CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);

                            //sets all the possible ceremony options. Windows will choose the most secure option 
                            //compatible with both devices
                            DevicePairingKinds ceremonySelection =  DevicePairingKinds.None |
                                                                    DevicePairingKinds.ConfirmOnly |
                                                                    DevicePairingKinds.ConfirmPinMatch |
                                                                    DevicePairingKinds.DisplayPin |
                                                                    DevicePairingKinds.ProvidePasswordCredential |
                                                                    DevicePairingKinds.ProvidePin;

                            //sets all the possible protecton levels. Windows will choose the most secure option 
                            //compatible with both devices
                            DevicePairingProtectionLevel minProtectionLevel = DevicePairingProtectionLevel.None |
                                                                              DevicePairingProtectionLevel.Default |
                                                                              DevicePairingProtectionLevel.Encryption |
                                                                              DevicePairingProtectionLevel.EncryptionAndAuthentication;

                            //starts the pairing process. It´s possible to call an overload with custom event handler, but
                            //since no pairing handle has been passed, the default one will be used
                            PairingResult pairedDevice = await PairingService.PairDevice(connectionDevice, ceremonySelection, minProtectionLevel);

                            //creates the discovery service
                            IGattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(connectionDevice);

                            //creates Guid object from Heart Rate Service`s 16 bit uuid
                            //Guid uuid = BluetoothUuidHelper.FromShortId(0x180D);


                            //requests the gatt result (collection of services) for the desired uuid
                            GattDeviceServicesResult gattResult = await discoveryService.FindGattServicesByUuidAsync(uuid);

                            //iterates over the resulted services
                            foreach (var service in gattResult.Services)
                            {
                                //obtains the characteristics of the service
                                GattCharacteristicsResult characteristics;
                                characteristics = await service.GetCharacteristicsAsync().ToTask();

                                //iterates over the resulted characteristics
                                foreach (var characteristic in characteristics.Characteristics)
                                {
                                    //subscribes to all notifing characteristics
                                    var notify = await characteristic.SubscribeToNotification
                                                        (
                                                            // passes an action to handle the notification events
                                                            (s, a) =>
                                                            {
                                                                Console.WriteLine("notification:");
                                                                Console.WriteLine(a.Timestamp.ToString());
                                                                IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                Console.WriteLine("buffer content: " + val);
                                                            },
                                                            //passes an action to handle exceptions raised during the subscription 
                                                            (s, e) => Console.WriteLine("error")
                                                        );
                                }
                            }

                            //when finished: clear, unpair and disconnect
                            discoveryService.ClearServices();
                            var result = await PairingService.Unpair(connectionDevice);
                            connectionDevice.ClearBluetoothLEDevice();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }

                //scan can be restarted, or stopped to end the while loop
                scanner.ResumeScanning();

                //leaves some time to discover new devices
                Thread.Sleep(3000);
            }
        }

        public static async Task GattDiscovery()
        {
            //Step 1: creates a filter for devices called "HeartRateDevice"
            Guid uuid = BluetoothUuidHelper.FromShortId(0x180d);
            IFilter filter = FilterBuilder.Init().SetServiceUUID(uuid).BuildFilter();

            //Step 2: creates a scanner with the given filter
            IBleScanner scanner = new CosmedBluetoothLEAdvertisementWatcher();

            //Step 3: starts an active scan
            await scanner.StartPassiveScanning();

            //Step 4: checking the results until the scanner is stopped
            while (scanner.status != StateMachine.Stopped)
            {
                //leaves some time to discover new devices
                Thread.Sleep(3000);

                //this collection will be updated at every cycle of the while loop
                var discoveredDevices = scanner.AllDiscoveredDevices;

                //scannig can be stopped here
                scanner.PauseScanning();
                //iteration over the discovered devices
                foreach (var device in discoveredDevices)
                {
                    //we are only interested in connectable devices for further Gatt communication
                    if (device.IsConnectable)
                    {
                        try
                        {
                            //creates an instance of the remote device
                            CosmedBleDevice connectionDevice = await CosmedBleDevice.CreateAsync(device);
                            PairingResult pairedDevice = await PairingService.PairDevice(connectionDevice, ceremonySelection, minProtectionLevel);

                            //connectionDevice is an instance of CosmebBleDevice, which implements the ICosmedBleDevice interface
                            IGattDiscoveryService discoveryService = await GattDiscoveryService.CreateAsync(connectionDevice);
                            //gets all the services of the remote device, possibly from the cache
                            GattDeviceServicesResult result = await discoveryService.GetAllGattServicesAsync();

                            //creates Guid object from DeviceName Characteristic 16 bit uuid
                            Guid uuid2 = BluetoothUuidHelper.FromShortId(0x2a37);

                            //the caracteristic 
                            foreach (var service in result.Services)
                            {
                                //tries to get the requested, uncached, characteristics
                                GattCharacteristicsResult characteristics;
                                characteristics = await discoveryService.FindGattCharacteristicsByUuidAsync(service, uuid2);

                                //gets if available the requested caracteristic
                                GattCharacteristic characteristic;
                                if (characteristics.Status == GattCommunicationStatus.Success)
                                {
                                    characteristic = characteristics.Characteristics[0];
                                }
                                var r = await service.GetIncludedServicesForUuidAsync(uuid, BluetoothCacheMode.Uncached).ToTask();
                            }
                            //creates Guid object from Heart Rate Service`s 16 bit uuid
                            //Guid uuid = BluetoothUuidHelper.FromShortId(0x180D);


                            //requests the gatt result (collection of services) for the desired uuid
                            GattDeviceServicesResult gattResult = await discoveryService.FindGattServicesByUuidAsync(uuid);

                            if(gattResult.Services != null)
                            {
                                //iterates over the resulted services
                                foreach (var service in gattResult.Services)
                                {
                                    //obtains the characteristics of the service
                                    GattCharacteristicsResult characteristics;
                                    characteristics = await service.GetCharacteristicsAsync().ToTask();

                                    //iterates over the resulted characteristics
                                    foreach (var characteristic in characteristics.Characteristics)
                                    {
                                        //subscribes to all notifing characteristics
                                        var notify = await characteristic.SubscribeToNotification
                                                            (
                                                                // passes an action to handle the notification events
                                                                (s, a) =>
                                                                {
                                                                    Console.WriteLine("notification:");
                                                                    Console.WriteLine(a.Timestamp.ToString());
                                                                    IBuffer CharacteristicValue = a.CharacteristicValue;
                                                                    string val = ClientBufferReader.ToUTF8String(CharacteristicValue);
                                                                    Console.WriteLine("buffer content: " + val);
                                                                },
                                                                //passes an action to handle exceptions raised during the subscription 
                                                                (s, e) => Console.WriteLine("error")
                                                            );
                                    }
                                }
                            }


                            //when finished: clear, unpair and disconnect
                            discoveryService.ClearServices();
                            var unpairResult = await PairingService.Unpair(connectionDevice);
                            connectionDevice.ClearBluetoothLEDevice();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }

                //scan can be restarted, or stopped to end the while loop
                scanner.ResumeScanning();


            }
        }


        public static async Task GattCommunication()
        {

        }
    }
    
}
