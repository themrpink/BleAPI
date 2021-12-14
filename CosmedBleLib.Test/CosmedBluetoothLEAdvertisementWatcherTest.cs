using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using CosmedBleLib.DeviceDiscovery;

namespace CosmedBleLib.MSTest.UnitTest
{


    [TestClass]
    public class CosmedBluetoothLEAdvertisementWatcherTest
    {
        private ICosmedBleAdvertisedDevice device;
        public ICosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher watcher;




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

        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_startScan_ScanIsActive()
        {
            await watcher.StartActiveScanning();
            Assert.IsTrue(watcher.IsScanningActive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_startScan_ScanIsNotPassive()
        {
            await watcher.StartActiveScanning();
            Assert.IsFalse(watcher.IsScanningPassive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_startScan_ScanIsStarting()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_stopAndStartScan_ScanIsStarted()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(100);
            watcher.StopScanning();
            Thread.Sleep(100);
            await watcher.StartActiveScanning();
            Thread.Sleep(100);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_stopScan_ScanIsStopped()
        {
            await watcher.StartActiveScanning();

            watcher.StopScanning();

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Stopped);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartActiveScanning_changeScanMode_ScanIsPassive()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(100);
            await watcher.StartPassiveScanning();
            //delay must be added because every mode change has implicit scan stop in between, which is not instantaneous
            Thread.Sleep(100);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
            Assert.IsTrue(watcher.IsScanningPassive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_changeScanMode_ScanIsActive()
        {
            await watcher.StartPassiveScanning();
            Thread.Sleep(100);
            await watcher.StartActiveScanning();
            //delay must be added because every mode change has implicit scan stop in between, which is not instantaneous
            Thread.Sleep(30);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
            Assert.IsTrue(watcher.IsScanningActive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_startScan_ScanIsPassive()
        {
            await watcher.StartPassiveScanning();
            Assert.IsTrue(watcher.IsScanningPassive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_startScan_ScanIsNotActive()
        {
            await watcher.StartPassiveScanning();
            Assert.IsFalse(watcher.IsScanningActive);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_startScan_ScanIsStarted()
        {
            await watcher.StartPassiveScanning();
            Thread.Sleep(30);
            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_stopAndStartScan_ScanIsStarted()
        {
            await watcher.StartPassiveScanning();
            Thread.Sleep(100);
            watcher.StopScanning();
            Thread.Sleep(100);
            await watcher.StartPassiveScanning();
            Thread.Sleep(100);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }



        [TestMethod]
        [TestCategory("scanning.mode")]
        public async Task StartPassiveScanning_stopScan_ScanIsStopped()
        {
            await watcher.StartPassiveScanning();

            watcher.StopScanning();

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Stopped);
        }



        //    [TestMethod]
        //    [TestCategory("scanning.mode")]
        //    public void StartScanning_startBothModes_eventsAreNotEmpty()
        //    {
        //        bool check;
        //        BluetoothLEAdvertisementWatcher BleWatcher = watcher.getBleWatcher();
        //        watcher.StartActiveScanning();

        //        check = VerifyDelegateAttachedTo(watcher, nameof(watcher.AllDevicesCollectionUpdated));
        //        Assert.IsTrue(check);

        //        check = VerifyDelegateAttachedTo(watcher, nameof(watcher.NewDeviceDiscovered));
        //        Assert.IsTrue(check);

        //        check = VerifyDelegateAttachedTo(watcher, nameof(watcher.RecentDevicesCollectionUpdated));
        //        Assert.IsTrue(check);

        //*   questi non funzionano, restiuiscono null al fieldInfo
        //        check = VerifyDelegateAttachedTo(BleWatcher, nameof(BleWatcher.Received));
        //        Assert.IsTrue(check);

        //        check = VerifyDelegateAttachedTo(BleWatcher, nameof(BleWatcher.Stopped));
        //        Assert.IsTrue(check);

        //    }

        private bool VerifyDelegateAttachedTo(object objectWithEvent, string eventName)
        {
            var allBindings = BindingFlags.IgnoreCase | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreReturn |
            BindingFlags.Default | BindingFlags.CreateInstance | BindingFlags.InvokeMethod;

            var type = objectWithEvent.GetType();
            var fieldInfo = type.GetField(eventName, allBindings);

            var value = fieldInfo.GetValue(objectWithEvent);

            var handler = value as Delegate;
            return handler != null;
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void allDeviceDiscovered_NewDevice_DeviceAdded()
        {
            watcher.addDiscoveredDevices(device);

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public async Task StartScanning_newScanStartedinBothModes_DictionaryIsEmpty()
        {
            await watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            await watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public async Task StartScanning_scanStoppedAndStartedInAllCombinations_DictionaryIsEmpty()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);
            Thread.Sleep(50);
            watcher.StopScanning();
            Thread.Sleep(50);
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList());

            await watcher.StartPassiveScanning();
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            Thread.Sleep(50);
            await watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            Thread.Sleep(50);
            await watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            await watcher.StartPassiveScanning();
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            Thread.Sleep(50);
            await watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void recentlyDiscoveredDevices_AddDeviceAndWaitTimeout_DeviceRemoved()
        {
            watcher.TimeoutSeconds = 1;
            double timeout = watcher.TimeoutSeconds;
            //IAdvertisedDevicesCollection collections = watcher.GetUpdatedDiscoveredDevices();

            watcher.addDiscoveredDevices(device);
            Thread.Sleep((int)timeout * 1000);
            //we need extra sleep because the update loop makes 2x5 seconds cycles, which end shortly after the timeout
            // Thread.Sleep(1000);

            CollectionAssert.DoesNotContain(watcher.RecentlyDiscoveredDevices.ToList(), device);
            //CollectionAssert.DoesNotContain(collections.recentDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void lastDiscoveredDevices_AddDeviceAndRead_DeviceRemoved()
        {
            double timeout = watcher.TimeoutSeconds;
            //IAdvertisedDevicesCollection collections = watcher.GetUpdatedDiscoveredDevices();

            watcher.addDiscoveredDevices(device);
            var lastDevices = watcher.LastDiscoveredDevices;

            CollectionAssert.DoesNotContain(watcher.LastDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void EachDeviceCollection_NewDeviceAdded_DeviceExists()
        {
            watcher.addDiscoveredDevices(device);

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(), device);
            CollectionAssert.Contains(watcher.RecentlyDiscoveredDevices.ToList(), device);
            CollectionAssert.Contains(watcher.LastDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public async Task RecentlyDevicesCollection_UpdateTimeElapsed_CollectionChanged()
        {
            IReadOnlyCollection<ICosmedBleAdvertisedDevice> recent;
            watcher.TimeoutSeconds = 1;
            double timeout = watcher.TimeoutSeconds;
            await watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            recent = watcher.RecentlyDiscoveredDevices;
            Thread.Sleep((int)timeout * 1000);

            CollectionAssert.AreNotEquivalent(watcher.RecentlyDiscoveredDevices.ToList(), recent.ToList());
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void RecentlyDiscoveredDevicesCollection_CollectionsUpdateTimeNotElapsedYet_CollectionNotChanged()
        {
            IReadOnlyCollection<ICosmedBleAdvertisedDevice> recent;
            watcher.TimeoutSeconds = 1;
            watcher.addDiscoveredDevices(device);
            recent = watcher.RecentlyDiscoveredDevices;
            Thread.Sleep((int)watcher.TimeoutSeconds * 1000 / 2);
            CollectionAssert.AreEquivalent(watcher.RecentlyDiscoveredDevices.ToList(), recent.ToList());
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public async Task ResumeScanning_scanPausedAndResumedAndDeviceAdded_DeviceIsNotLost()
        {
            await watcher.StartActiveScanning();
            //device added after start, otherwise would be deleted by the initialization
            watcher.addDiscoveredDevices(device);
            watcher.PauseScanning();
            watcher.ResumeScanning();

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public async Task ResumeScanning_scanResumedAndStopped_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            await watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.ResumeScanning();
            watcher.StopScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public async Task PauseScanning_scanPausedAndStopped_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            await watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.StopScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public void StopScanning_deviceAdded_ListIsNotEmpty()
        {
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(), device);
            CollectionAssert.AreNotEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public async Task StartScanning_afterPause_DeviceDeleted()
        {
            watcher.addDiscoveredDevices(device);
            await watcher.StartActiveScanning();
            watcher.PauseScanning();
            await watcher.StartActiveScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public async Task StartScanning_afterResume_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            await watcher.StartActiveScanning();
            watcher.PauseScanning();
            await watcher.StartActiveScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
            //CollectionAssert.AreNotEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }



        [TestMethod]
        [TestCategory("scanning.status")]
        public async Task Resume_afterStop_StatusIsStopped()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.StopScanning();
            watcher.ResumeScanning();
            Thread.Sleep(50);
            Assert.IsFalse(watcher.IsScanningStarted);
        }



        [TestMethod]
        [TestCategory("scanning.status")]
        public async Task Resume_afterPause_StatusIsStarted()
        {
            await watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.ResumeScanning();
            Thread.Sleep(50);
            Assert.IsTrue(watcher.IsScanningStarted);
        }



        [TestMethod]
        [TestCategory("scanning.status")]
        public async Task Pause_afterPause_StatusIsStarted()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(100);
            watcher.PauseScanning();
            watcher.PauseScanning();
            Thread.Sleep(100);
            Assert.IsFalse(watcher.IsScanningStarted);
        }



        [TestMethod]
        [TestCategory("scanning.status")]
        public async Task StopScan_afterPause_WatcherIsNull()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.PauseScanning();
            Thread.Sleep(50);
            watcher.StopScanning();

            Assert.IsNull(watcher.GetWatcher());
        }



        [TestMethod]
        [TestCategory("scanning.status")]
        public async Task StopScan_afterStart_WatcherIsNull()
        {
            await watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.StopScanning();

            Assert.IsNull(watcher.GetWatcher());
        }



        [TestCleanup]
        public void Cleanup()
        {
            watcher.StopScanning();
            //Thread.Sleep(100);
            watcher = null;
        }


    }
}