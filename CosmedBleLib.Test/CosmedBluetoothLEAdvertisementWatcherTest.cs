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
        
        private CosmedBleAdvertisedDevice device;
        public CosmedBleAdvertisedDevice device2;
        private CosmedBluetoothLEAdvertisementWatcher watcher;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionAll;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionRecent;
        private IReadOnlyCollection<CosmedBleAdvertisedDevice> collectionLast;

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
            //autoUpdatedCollections = watcher.getUpdatedDiscoveredDevices();
            collectionAll = watcher.AllDiscoveredDevices;
            collectionRecent = watcher.RecentlyDiscoveredDevices;
            collectionLast = watcher.LastDiscoveredDevices;
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
            //delay must be added because every mode change has implicit scan stop in between, which is not instantaneous
            Thread.Sleep(100);

            Assert.IsTrue(watcher.GetWatcherStatus == BluetoothLEAdvertisementWatcherStatus.Started);
            Assert.IsTrue(watcher.IsScanningPassive);
        }

        [TestMethod]
        [TestCategory("scanning.mode")]
        public void StartPassiveScanning_changeScanMode_ScanIsActive()
        {
            watcher.StartPassiveScanning();
            watcher.StartActiveScanning();
            //delay must be added because every mode change has implicit scan stop in between, which is not instantaneous
            Thread.Sleep(100);

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

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(),  device);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void StartScanning_newScanStartedinBothModes_DictionaryIsEmpty()
        {
            watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }


        [TestMethod]
        [TestCategory("scanning.collections")]
        public void StartScanning_scanStoppedAndStartedInAllCombinations_DictionaryIsEmpty()
        {
            watcher.StartActiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            watcher.StartActiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            
            watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            watcher.StartPassiveScanning();
            
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
            
            watcher.StartActiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            watcher.StartPassiveScanning();

            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());

            watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            watcher.StopScanning();
            watcher.StartActiveScanning();
            
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), Array.Empty<CosmedBleAdvertisedDevice>().ToList().AsReadOnly());
        }

        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public void ResumeScanning_scanPausedAndResumedAndDeviceAdded_DeviceIsNotLost()
        {           
            watcher.StartActiveScanning();
            //device added after start, otherwise would be deleted by the initialization
            watcher.addDiscoveredDevices(device);
            watcher.PauseScanning();
            watcher.ResumeScanning();

            CollectionAssert.Contains(watcher.AllDiscoveredDevices.ToList(), device);
        }


        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public void ResumeScanning_scanResumedAndStopped_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.ResumeScanning();
            watcher.StopScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
            CollectionAssert.AreEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }



        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public void PauseScanning_scanPausedAndStopped_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            watcher.StartActiveScanning();
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
        public void StartScanning_afterPause_DeviceDeleted()
        {
            watcher.addDiscoveredDevices(device);
            watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.StartActiveScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
        }

        [TestMethod]
        [TestCategory("scanning.collections.status")]
        public void StartScanning_afterResume_ListIsEmpty()
        {
            watcher.addDiscoveredDevices(device);
            watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.StartActiveScanning();

            CollectionAssert.DoesNotContain(watcher.AllDiscoveredDevices.ToList(), device);
            //CollectionAssert.AreNotEquivalent(watcher.AllDiscoveredDevices.ToList(), new List<CosmedBleAdvertisedDevice>());
        }

        [TestMethod]
        [TestCategory("scanning.status")]
        public void Resume_afterStop_StatusIsStopped()
        {
            watcher.StartActiveScanning();
            //it takes a little while to start the scan and update status
            Thread.Sleep(500);
            watcher.StopScanning();
            watcher.ResumeScanning();

            Assert.IsFalse(watcher.IsScanningStarted);
        }

        [TestMethod]
        [TestCategory("scanning.status")]
        public void Resume_afterPause_StatusIsstarted()
        {
            watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.ResumeScanning();

            Assert.IsTrue(watcher.IsScanningStarted);
        }

        [TestMethod]
        [TestCategory("scanning.status")]
        public void Pause_afterPause_StatusIsstarted()
        {
            watcher.StartActiveScanning();
            watcher.PauseScanning();
            watcher.ResumeScanning();

            Assert.IsTrue(watcher.IsScanningStarted);
        }



        [TestMethod]
        [TestCategory("scanning.collections")]
        public void recentlyDiscoveredDevices_AddDeviceAndWaitTimeout_DeviceRemoved()
        {         
            double timeout = watcher.timeout;
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
            double timeout = watcher.timeout;
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
        public void RecentlyDevicesCollection_UpdateTimeElapsed_CollectionChanged()
        {
            IReadOnlyCollection<CosmedBleAdvertisedDevice> recent;

            double timeout = watcher.timeout;
            watcher.StartPassiveScanning();
            watcher.addDiscoveredDevices(device);
            recent = watcher.RecentlyDiscoveredDevices;
            Thread.Sleep((int)timeout * 1000);
          
            CollectionAssert.AreNotEquivalent(watcher.RecentlyDiscoveredDevices.ToList(), recent.ToList());
        }
      

        [TestMethod]
        [TestCategory("scanning.collections")]
        public void RecentlyDiscoveredDevicesCollection_CollectionsUpdateTimeNotElapsedYet_CollectionNotChanged()
        {
            IReadOnlyCollection<CosmedBleAdvertisedDevice> recent;

            watcher.addDiscoveredDevices(device);
            recent = watcher.RecentlyDiscoveredDevices;
            Thread.Sleep((int)watcher.timeout * 1000 / 2);
            CollectionAssert.AreEquivalent(watcher.RecentlyDiscoveredDevices.ToList(), recent.ToList());
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
