using CosmedBleLib.Adapter;
using CosmedBleLib.DeviceDiscovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib
{
    static class StaticManualIntegrationTests
    {

        public static void TestStartStop()
        {
            TestWatcher test = new TestWatcher();
            test.StartScan();
            test.StopScan();
            Console.WriteLine("press enter");
            Console.ReadLine();
        }

        public static void TestStartStopStart()
        {
            TestWatcher test = new TestWatcher();
            test.StartScan();
            test.StopScan();
            test.StartScan();
            Console.WriteLine("press enter");
            Console.ReadLine();
        }


        public async static void CheckBleAdapter()
        {
            Console.WriteLine("going to check, press enter");
            Console.ReadLine();
            var adapter = await CosmedBluetoothLEAdapter.CreateAsync();
            Thread.Sleep(2000);
            Console.WriteLine("checked, press enter");
            Console.ReadLine();
        }



        public static async void TestBleOff()
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
