using CosmedBleLib.DeviceDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace CosmedBleLib.Test
{
    [TestClass]
    public class EventsTest
    {

        private CosmedBleAdvertisedDevice device;
        public CosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        public CosmedBluetoothLEAdvertisementFilter Filter;
        public static int count = 0;


        [TestInitialize]
        public void Setup()
        {
            device = new CosmedBleAdvertisedDevice(0,
                                    DateTimeOffset.UtcNow,
                                    true,
                                    new BluetoothLEAdvertisement(),
                                    new BluetoothLEAdvertisementType()
                                );

            device2 = new CosmedBleAdvertisedDevice(2,
                        DateTimeOffset.UtcNow,
                        true,
                        new BluetoothLEAdvertisement(),
                        new BluetoothLEAdvertisementType()
                    );

            watcher = new CosmedBluetoothLEAdvertisementWatcher();

            Filter = new CosmedBluetoothLEAdvertisementFilter();

        }
       
        
        
        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherNewDeviceDiscoveredEvent_newDeviceDiscovered_EventCalled()           
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.NewDeviceDiscovered += (s, a) => count = 1;

            await watcher.StartActiveScanning();
            watcher.addDiscoveredDevices(device);

            Assert.IsTrue(count == 1);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherStartedListeningEvent_scanPassiveStarted_eventCalled()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.StartedListening += (s, a) => count += 1;

            await watcher.StartPassiveScanning();

            Assert.IsTrue(count == 1);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherStartedListeningEvent_scanActiveStarted_eventCalled()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();

            watcher.StartedListening += (s, a) => count = 1;
            await watcher.StartActiveScanning();

            Assert.IsTrue(count == 1);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherStopScanningEvent_PassiveScanStopped_EventRaised()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.ScanStopped += (s, a) => count+=1;

            await watcher.StartPassiveScanning();
            Thread.Sleep(100);
            watcher.StopScanning();
            Thread.Sleep(100);

            Assert.IsTrue(count == 1);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherStopScanningEvent_ActiveScanStoppedAndStartedAndStopped_EventRaisedTwice()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.ScanStopped += (s, a) => { count += 1; };
            
            await watcher.StartActiveScanning();
            Thread.Sleep(100);
            watcher.StopScanning();
            Thread.Sleep(100);
            await watcher.StartActiveScanning();
            watcher.StopScanning();
            Thread.Sleep(100);

            Assert.IsTrue(count == 2);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherScanModeChangedEvent_ChangeScanModeMultipleTimes_EventRaised()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.ScanModeChanged += () => count += 1;

            await watcher.StartPassiveScanning();
            await watcher.StartActiveScanning();
            await watcher.StartPassiveScanning();
      
            Assert.IsTrue(count == 2);
        }



        [TestMethod]
        [TestCategory("events.watcher")]
        public async Task WatcherScanInterruptedEvent_ChangeScanModeMultipleTimes_EventRaised()
        {
            count = 0;
            watcher = new CosmedBluetoothLEAdvertisementWatcher();
            watcher.ScanInterrupted += (s,a) => count += 1;

            await watcher.StartPassiveScanning();
            watcher.StopScanning();


            Assert.IsTrue(count == 0);
        }


        [TestMethod]
        [TestCategory("events.advertisingDevice")]
        public void AdvDeviceEScanResponsevent_ScanResponseReceived_EventRaised()
        {
            //TODO with integration test


            Assert.IsTrue(true);
        }


        /**
         * the other events shall be tests with integration tests
         */

        [TestCleanup]
        public void Cleanup()
        {
            watcher.StopScanning();
            watcher = null;
            Filter = null;
            count = 0;
        }

    }
}


/*
watcher:
		//offerti da uwp
        public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> Received;
public event TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> Stopped;

//vengono gestiti nell´init (e poi rimossi a ogni stop)
watcher.Received += this.OnAdvertisementReceived;
watcher.Stopped += this.OnScanStopped;

//aggiunti io
public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, CosmedBleAdvertisedDevice> NewDeviceDiscovered;
public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs> ScanStopped;
public event Action<CosmedBluetoothLEAdvertisementWatcher, Exception> ScanInterrupted;
public event TypedEventHandler<CosmedBluetoothLEAdvertisementWatcher, BluetoothLEScanningMode> StartedListening;
public event Action ScanModeChanged;

*/