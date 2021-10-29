using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Windows.Devices.Bluetooth.Advertisement;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;

namespace CosmedBleLib.MSTest.UnitTest
{
    [TestClass]
    public class CosmedBluetoothLEAdvertisementWatcherTest
    {
        /*
        private CosmedBleAdvertisedDevice device;
        public CosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionAll;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionRecent;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionLast;
        private IAdvertisedDevicesCollection autoUpdatedCollections;
        private IAdvertisedDevicesCollection2 autoUpdatedCollections2;
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
            autoUpdatedCollections = watcher.getUpdatedDiscoveredDevices();
            collectionAll = autoUpdatedCollections.allDiscoveredDevices;
            collectionRecent = autoUpdatedCollections.recentDiscoveredDevices;
            collectionLast = autoUpdatedCollections.getLastDiscoveredDevices();
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_startScan_ScanIsActive()
        {
            watcher.StartActiveScanning();
            Assert.IsTrue(watcher.IsScanningActive);
        }


        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_startScan_ScanIsNotPassive()
        {
            watcher.StartActiveScanning();
            Assert.IsFalse(watcher.IsScanningPassive);
        }


        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_startScan_ScanIsStarted()
        {
            watcher.StartActiveScanning();
            Thread.Sleep(10);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_stopAndStartScan_ScanIsStarted()
        {
            watcher.StartActiveScanning();
            watcher.StopScanning();
            watcher.StartActiveScanning();
            Thread.Sleep(10);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_stopScan_ScanIsStopped()
        {
            watcher.StartActiveScanning();
            Thread.Sleep(50);
            watcher.StopScanning();

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Stopped);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartActiveScanning_changeScanMode_ScanIsPassive()
        {
            watcher.StartActiveScanning();
            watcher.StartPassiveScanning();

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
            Assert.IsTrue(watcher.IsScanningPassive);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_changeScanMode_ScanIsActive()
        {
            watcher.StartPassiveScanning();
            watcher.StartActiveScanning();

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
            Assert.IsTrue(watcher.IsScanningActive);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_startScan_ScanIsPassive()
        {
            watcher.StartPassiveScanning();
            Assert.IsTrue(watcher.IsScanningPassive);
        }


        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_startScan_ScanIsNotActive()
        {
            watcher.StartPassiveScanning();
            Assert.IsFalse(watcher.IsScanningActive);
        }


        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_startScan_ScanIsStarted()
        {
            watcher.StartPassiveScanning();
            Thread.Sleep(10);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }


        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_stopAndStartScan_ScanIsStarted()
        {
            watcher.StartPassiveScanning();
            watcher.StopScanning();
            watcher.StartPassiveScanning();
            Thread.Sleep(10);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_stopScan_ScanIsStopped()
        {
            watcher.StartPassiveScanning();
            Thread.Sleep(50);
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

            CollectionAssert.Contains(watcher.allDiscoveredDevices.ToList(),  device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void StartScanning_newScanStartedinBothModes_DictionaryIsEmpty()
        {
            watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void StartScanning_scanStoppedAndStartedInAllCombinations_DictionaryIsEmpty()
        {
            watcher.StartActiveScanning();
            Thread.Sleep(1000);
            watcher.StopScanning();
            watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            
            watcher.StartPassiveScanning();
            Thread.Sleep(1000);
            watcher.StopScanning();
            watcher.StartPassiveScanning();
            
            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            
            watcher.StartActiveScanning();
            Thread.Sleep(1000);
            watcher.StopScanning();
            watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            watcher.StartPassiveScanning();
            Thread.Sleep(1000);
            watcher.StopScanning();
            watcher.StartActiveScanning();
            
            CollectionAssert.AreEquivalent(watcher.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void recentlyDiscoveredDevices_AddDeviceAndWaitTimeout_DeviceRemoved()
        {         
            double timeout = watcher.timeout;
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices();

            watcher.addDiscoveredDevices(device);
            Thread.Sleep((int)timeout * 1000);
            //we need extra sleep because the update loop makes 2x5 seconds cycles, which end shortly after the timeout
            Thread.Sleep(1000);

            CollectionAssert.DoesNotContain(watcher.GetRecentlyAdvertisedDevices(1).ToList(), device);
            CollectionAssert.DoesNotContain(collections.recentDiscoveredDevices.ToList(), device);
        }




        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoAupdateAllDeviceDiscovered_NewDevice_DeviceAdded()
        {
            watcher.addDiscoveredDevices(device);
            Thread.Sleep(1000);
            Assert.IsTrue(autoUpdatedCollections.allDiscoveredDevices.Count == 1);
            //CollectionAssert.Contains(collectionRecent.ToList(), device);
            //CollectionAssert.Contains(collectionLast.ToList(), device);
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void DiscoveredDeviceCollections_DeviceNotAdded_ListIsEmpty()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            CollectionAssert.AreEquivalent(collections.recentDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            CollectionAssert.AreEquivalent(collections.allDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            CollectionAssert.AreEquivalent(collections.getLastDiscoveredDevices().ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_UpdateTimeElapsed_CollectionChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices();

            watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.getUpdatedDiscoveredDevices(1000);
            Thread.Sleep(1000);

            Assert.AreNotSame(collections.allDiscoveredDevices, collectionAll);
            Assert.IsTrue(collections.allDiscoveredDevices != collectionAll);
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_CollectionsUpdateTimeNotElapsedYet_CollectionNotChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);
            
            Thread.Sleep(50);
            watcher.addDiscoveredDevices(device);

            //with sleep it fails
            //

            CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.allDiscoveredDevices.ToList());
            CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.recentDiscoveredDevices.ToList());
            //this one gets immediately updated:
            CollectionAssert.AreNotEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.getLastDiscoveredDevices().ToList());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_DelayAddingElementToAllAndRecentCollections_CollectionsNotChangedYet()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            watcher.addDiscoveredDevices(device);

            CollectionAssert.AreEquivalent(collectionAll.ToList(), collections.allDiscoveredDevices.ToList());
            CollectionAssert.AreEquivalent(collectionRecent.ToList(), collections.recentDiscoveredDevices.ToList());
            //this one gets immediately updated:
           // CollectionAssert.AreEquivalent(Array.Empty<CosmedBleAdvertisedDevice>().ToList(), collections.getLastDiscoveredDevices().ToList());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void AutoUpdateDiscoveredDevicesCollection_NoDelayInAddingElementToLastCollection_CollectionChanged()
        {
            IAdvertisedDevicesCollection collections = watcher.getUpdatedDiscoveredDevices(1000);

            watcher.addDiscoveredDevices(device);

            CollectionAssert.AreNotEquivalent(collectionLast.ToList(), collections.getLastDiscoveredDevices().ToList());
        }


        [TestMethod]
        [TestCategory("autoUpdateCollections")]
        public void getUpdatedDiscoveredDevices_methodCalled_AutoUpdateNotActive()
        {
            watcher.StopUpdateDevices();
   
            Assert.IsFalse(watcher.IsAutoUpdateActive);

        }


        [TestMethod]
        [TestCategory("autoUpdateCollections")]
        public void getUpdatedDiscoveredDevices_methodCalled_ThreadIsStartedAndRunning()
        {
            watcher.StopUpdateDevices();

            var autoUpdatedCollections = watcher.getUpdatedDiscoveredDevices();

            Assert.IsTrue(watcher.IsAutoUpdateActive);
            Assert.AreEqual(watcher.GetUpdatingThreadState, ThreadState.Running|ThreadState.WaitSleepJoin);
        }



        [TestCleanup]
        public void Cleanup()
        {
            watcher.StopScanning();
            watcher.StopUpdateDevices();
            watcher = null;
        }

        */
    }
}
